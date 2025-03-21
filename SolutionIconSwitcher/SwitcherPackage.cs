using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.WindowsAPICodePack.Taskbar;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace SolutionIconSwitcher
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(PackageGuidString)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionOpening_string, PackageAutoLoadFlags.BackgroundLoad)]
    public sealed class SwitcherPackage : AsyncPackage
    {
        public const string PackageGuidString = "4417bdde-9c84-4b53-bf7b-a3ce30921b55";
        private readonly string[] _iconPostfixes = { ".solutioniconswitcher.user", ".sis.user" };
        private string _solutionPath;
        private MessageWindow _messageWindow;

        public void HandleOpenSolution()
        {
            Logger.LogDebug($"Начало обновления иконки [Поток: {Thread.CurrentThread.ManagedThreadId}]");

            if (TaskbarManager.IsPlatformSupported == false)
            {
                Logger.LogWarning("API панели задач недоступно");
                return;
            }

            try
            {
                RefreshTaskbarIcon();
            }
            catch (Exception exception)
            {
                Logger.LogError($"Ошибка обновления: {exception.Dump()}");
            }
        }

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            Logger.LogInfo("Запущена инициализация пакета");

            try
            {
                await base.InitializeAsync(cancellationToken, progress);
                Logger.LogDebug("Базовая инициализация завершена");

                var isSolutionLoaded = await IsSolutionLoadedAsync();
                Logger.LogDebug($"Состояние загрузки решения: {isSolutionLoaded}");

                if (isSolutionLoaded)
                {
                    HandleOpenSolution();
                }

                await JoinableTaskFactory.SwitchToMainThreadAsync();
                _messageWindow = new MessageWindow(this);
                Logger.LogDebug("Окно сообщений создано");
            }
            catch (Exception exception)
            {
                Logger.LogError($"Ошибка инициализации: {exception.Dump()}");
                throw;
            }
        }

        protected override void Dispose(bool disposing)
        {
            Logger.LogDebug("Начало освобождения ресурсов пакета");

            try
            {
                _messageWindow?.Dispose();
                base.Dispose(disposing);
                Logger.LogDebug("Ресурсы пакета освобождены");
            }
            catch (Exception exception)
            {
                Logger.LogError($"Ошибка освобождения: {exception.Dump()}");
                throw;
            }
        }

        private async Task<bool> IsSolutionLoadedAsync()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();
            var solService = await GetServiceAsync(typeof(SVsSolution)) as IVsSolution;

            ErrorHandler.ThrowOnFailure(solService.GetProperty((int)__VSPROPID.VSPROPID_SolutionFileName, out var solutionPathObj));

            _solutionPath = solutionPathObj.ToString();

            ErrorHandler.ThrowOnFailure(solService.GetProperty((int)__VSPROPID.VSPROPID_IsSolutionOpen, out var isOpenObj));

            return isOpenObj is bool isOpen && isOpen;
        }

        private void RefreshTaskbarIcon()
        {
            try
            {
                foreach (var iconPath in _iconPostfixes.Select(x => _solutionPath + x))
                {
                    Logger.LogDebug($"Проверка иконки: {iconPath}");

                    if (File.Exists(iconPath) == false)
                    {
                        Logger.LogWarning($"Файл иконки отсутствует: {iconPath}");
                        TaskbarManager.Instance.SetOverlayIcon(null, "");
                        continue;
                    }

                    using (var icon = Icon.ExtractAssociatedIcon(iconPath))
                    {
                        if (icon == null)
                        {
                            Logger.LogWarning($"Невозможно загрузить иконку: {iconPath}");
                        }
                        else
                        {
                            Logger.LogDebug($"Иконка загружена: {icon.Size.Width}x{icon.Size.Height}");
                            TaskbarManager.Instance.SetOverlayIcon(icon, "");
                            Logger.LogDebug("Иконка успешно обновлена");
                            break;
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                Logger.LogError($"Ошибка иконки: {exception.Dump()}");
                TaskbarManager.Instance.SetOverlayIcon(null, "");
            }
        }
    }
}
