using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.WindowsAPICodePack.Taskbar;
using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Task = System.Threading.Tasks.Task;

namespace SolutionIconSwitcher
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(PackageGuidString)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionOpening_string, PackageAutoLoadFlags.BackgroundLoad)]
    public sealed class SwitcherPackage : AsyncPackage
    {
        public const string PackageGuidString = "4417bdde-9c84-4b53-bf7b-a3ce30921b55";
        private string _iconPath;
        private MessageWindow _messageWindow;

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            Logger.LogInfo("Package initialization started");

            try
            {
                await base.InitializeAsync(cancellationToken, progress);
                Logger.LogDebug("Base initialization completed");

                var isSolutionLoaded = await IsSolutionLoadedAsync();
                Logger.LogInfo($"Solution loaded state: {isSolutionLoaded}");

                if (isSolutionLoaded)
                {
                    HandleOpenSolution();
                }

                await JoinableTaskFactory.SwitchToMainThreadAsync();
                _messageWindow = new MessageWindow(this);
                Logger.LogInfo("Message window created");
            }
            catch (Exception exception)
            {
                Logger.LogError($"Initialization failed: {exception.Dump()}");
                throw;
            }
        }

        protected override void Dispose(bool disposing)
        {
            Logger.LogInfo("Package disposal started");

            try
            {
                _messageWindow?.Dispose();
                base.Dispose(disposing);
                Logger.LogInfo("Package disposed");
            }
            catch (Exception exception)
            {
                Logger.LogError($"Dispose error: {exception.Dump()}");
                throw;
            }
        }

        private void HandleOpenSolution()
        {
            Logger.LogInfo($"Starting icon refresh [Thread: {Thread.CurrentThread.ManagedThreadId}]");

            try
            {
                if (TaskbarManager.IsPlatformSupported ==false)
                {
                    Logger.LogWarning("Taskbar API unavailable");
                    return;
                }

                RefreshTaskbarIcon();
            }
            catch (Exception exception)
            {
                Logger.LogError($"Refresh failed: {exception.Dump()}");
            }
        }

        private async Task<bool> IsSolutionLoadedAsync()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();
            var solService = await GetServiceAsync(typeof(SVsSolution)) as IVsSolution;

            ErrorHandler.ThrowOnFailure(solService.GetProperty((int)__VSPROPID.VSPROPID_SolutionFileName, out var solutionPathObj));

            var solutionPath = solutionPathObj.ToString();
            _iconPath = $"{solutionPath}.solutioniconswitcher.user";

            ErrorHandler.ThrowOnFailure(solService.GetProperty((int)__VSPROPID.VSPROPID_IsSolutionOpen, out var isOpenObj));

            return isOpenObj is bool isOpen && isOpen;
        }

        private void RefreshTaskbarIcon()
        {
            try
            {
                Logger.LogInfo($"Checking icon: {_iconPath}");

                if (File.Exists(_iconPath) == false)
                {
                    Logger.LogWarning("Icon file missing");
                    TaskbarManager.Instance.SetOverlayIcon(null, "");
                    return;
                }

                using (var icon = Icon.ExtractAssociatedIcon(_iconPath))
                {
                    if (icon == null)
                    {
                        Logger.LogWarning("Icon cannot be loaded");
                    }
                    else
                    {
                        Logger.LogInfo($"Icon loaded: {icon.Size.Width}x{icon.Size.Height}");
                        TaskbarManager.Instance.SetOverlayIcon(icon, "");
                        Logger.LogSuccess("Icon updated successfully");
                    }
                }
            }
            catch (Exception exception)
            {
                Logger.LogError($"Icon error: {exception.Dump()}");
                TaskbarManager.Instance.SetOverlayIcon(null, "");
            }
        }

        private sealed class MessageWindow : NativeWindow, IDisposable
        {
            private const int ACTIVATEAPP = 0x001C;
            private readonly SwitcherPackage _package;
            private readonly uint _taskbarCreatedMsg;

            public MessageWindow(SwitcherPackage package)
            {
                _package = package;
                _taskbarCreatedMsg = RegisterWindowMessage("TaskbarCreated");
                CreateHandle(new CreateParams());
                Logger.LogInfo("Message window initialized");
            }

            public void Dispose()
            {
                if (Handle == IntPtr.Zero)
                {
                    return;
                }

                Logger.LogDebug("Destroying message window");
                DestroyHandle();
            }

            protected override void WndProc(ref Message message)
            {
                try
                {
                    Logger.LogDebug($"Message: 0x{message.Msg:X4} " + $"WParam: 0x{message.WParam.ToInt64():X8} " + $"LParam: 0x{message.LParam.ToInt64():X8}");

                    switch (message.Msg)
                    {
                        case int _ when message.Msg == (int)_taskbarCreatedMsg:
                            HandleSystemEvent("TaskbarCreated");
                            break;

                        case ACTIVATEAPP:
                            HandleSystemEvent("SystemSettingsChanged");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Message error: {ex.Dump()}");
                }
                finally
                {
                    base.WndProc(ref message);
                }
            }

            [DllImport("user32.dll", CharSet = CharSet.Auto)]
            private static extern uint RegisterWindowMessage(string lpString);

            private void HandleSystemEvent(string eventName)
            {
                Logger.LogInfo($"Handling event: {eventName}");

                Task.Run(async () =>
                {
                    await Task.Delay(500);

                    await _package.JoinableTaskFactory.RunAsync(async () =>
                    {
                        await _package.JoinableTaskFactory.SwitchToMainThreadAsync();
                        _package.HandleOpenSolution();
                    });
                });
            }
        }
    }

    internal static class Logger
    {
        private const string LogFile = "SolutionIconSwitcher.log";
        private static readonly string LogPath = Path.Combine(Path.GetTempPath(), "SolutionIconSwitcher", LogFile);
        private static readonly object Lock = new object();

        static Logger()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LogPath) ?? string.Empty);
            LogInfo($"=== Session started v{Assembly.GetExecutingAssembly().GetName().Version} ===");
        }

        public static void LogInfo(string message, [CallerMemberName] string caller = "", [CallerLineNumber] int line = 0)
        {
            WriteEntry("INFO", message, caller, line);
        }

        public static void LogWarning(string message, [CallerMemberName] string caller = "", [CallerLineNumber] int line = 0)
        {
            WriteEntry("WARN", message, caller, line);
        }

        public static void LogError(string message, [CallerMemberName] string caller = "", [CallerLineNumber] int line = 0)
        {
            WriteEntry("ERROR", message, caller, line);
        }

        public static void LogDebug(string message, [CallerMemberName] string caller = "", [CallerLineNumber] int line = 0)
        {
#if DEBUG
            WriteEntry("DEBUG", message, caller, line);
#endif
        }

        public static void LogSuccess(string message, [CallerMemberName] string caller = "", [CallerLineNumber] int line = 0)
        {
            WriteEntry("SUCCESS", message, caller, line);
        }

        private static void WriteEntry(string level, string message, string caller, int line)
        {
            lock (Lock)
            {
                File.AppendAllText(LogPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level,-7}] {caller}:{line} - {message}\n");
            }
        }
    }

    internal static class ExceptionExtensions
    {
        public static string Dump(this Exception exception)
        {
            return $"{exception.GetType().Name}: {exception.Message}\n{exception.StackTrace}";
        }
    }
}
