using Microsoft.VisualStudio.Shell;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SolutionIconSwitcher
{
    internal sealed class MessageWindow : NativeWindow, IDisposable
    {
        private const int ACTIVATEAPP = 0x001C;

        private const string WindowActivated = nameof(WindowActivated);
        private const string TaskbarCreated = nameof(TaskbarCreated);

        private readonly TimeSpan _delay = TimeSpan.FromSeconds(10);
        private readonly SwitcherPackage _package;
        private readonly uint _taskbarCreatedMsg;

        private bool _isTryRefresh;

        public MessageWindow(SwitcherPackage package)
        {
            _package = package;
            _taskbarCreatedMsg = RegisterWindowMessage(TaskbarCreated);
            CreateHandle(new CreateParams());
            Logger.LogDebug("Окно сообщений инициализировано");
        }

        public void Dispose()
        {
            if (Handle == IntPtr.Zero)
            {
                return;
            }

            Logger.LogDebug("Уничтожение окна сообщений");
            DestroyHandle();
        }

        protected override void WndProc(ref Message message)
        {
            try
            {
                Logger.LogDebug($"Сообщение: 0x{message.Msg:X4} WParam: 0x{message.WParam.ToInt64():X8} LParam: 0x{message.LParam.ToInt64():X8}");

                switch (message.Msg)
                {
                    case int _ when message.Msg == (int)_taskbarCreatedMsg:
                        HandleSystemEvent(TaskbarCreated);
                        break;

                    case ACTIVATEAPP:
                        HandleSystemEvent(WindowActivated);
                        break;
                }
            }
            catch (Exception exception)
            {
                Logger.LogError($"Ошибка обработки сообщения: {exception.Dump()}");
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
            Logger.LogDebug($"Обработка события: {eventName}");

            if (_isTryRefresh && eventName != TaskbarCreated)
            {
                Logger.LogDebug($"Событие {eventName} пропущено - обновление уже выполнялось в последние {_delay.TotalSeconds} секунд");
                return;
            }

            _isTryRefresh = true;

            Logger.LogDebug($"HandleOpenSolution вызвано событием: {eventName}");

            _package.JoinableTaskFactory.RunAsync(async () =>
                {
                    try
                    {
                        await _package.JoinableTaskFactory.SwitchToMainThreadAsync();
                        _package.HandleOpenSolution();
                        await Task.Delay(_delay);
                    }
                    finally
                    {
                        _isTryRefresh = false;
                    }
                })
                .FileAndForget("HandleSystemEvent");
        }
    }
}
