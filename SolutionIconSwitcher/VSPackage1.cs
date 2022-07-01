using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Taskbar;
using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace VSIXProject1
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(PackageGuidString)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionOpening_string, PackageAutoLoadFlags.BackgroundLoad)]
    public sealed class VSPackage1 : AsyncPackage
    {
        /// <summary>
        /// VSPackage1 GUID string.
        /// </summary>
        public const string PackageGuidString = "4417bdde-9c84-4b53-bf7b-a3ce30921b55";
        private string _iconPath;

        /// <summary>
        /// Initializes a new instance of the <see cref="VSPackage1"/> class.
        /// </summary>
        public VSPackage1()
        {
        }

        #region Package Members

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            bool isSolutionLoaded = await IsSolutionLoadedAsync();
            if (isSolutionLoaded)
            {
                HandleOpenSolution();
            }

            //// Listen for subsequent solution events
            //SolutionEvents.OnAfterBackgroundSolutionLoadComplete += HandleOpenSolution;
        }

        private async Task<bool> IsSolutionLoadedAsync()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();
            var solService = await GetServiceAsync(typeof(SVsSolution)) as IVsSolution;
            ErrorHandler.ThrowOnFailure(solService.GetProperty((int)__VSPROPID.VSPROPID_SolutionFileName, out object value1));
            var slnPath = value1.ToString();
            _iconPath = slnPath + ".solutioniconswitcher.user";
            ErrorHandler.ThrowOnFailure(solService.GetProperty((int)__VSPROPID.VSPROPID_IsSolutionOpen, out object value));
            return value is bool isSolOpen && isSolOpen;
        }

        private void HandleOpenSolution(object sender = null, EventArgs e = null)
        {
            if (!TaskbarManager.IsPlatformSupported)
            {
                Debug(() => { return "taskbar not supported"; });
                return;
            }

            if (File.Exists(_iconPath))
            {
                Debug(() => { return "try get " + _iconPath; });
                var programicon = Icon.ExtractAssociatedIcon(_iconPath);
                Debug(() => { return "size " + _iconPath + " " + programicon.Size.Width.ToString(); });
                TaskbarManager.Instance.SetOverlayIcon(programicon, "");
                Debug(() => { return "success " + _iconPath; });
            }
            else
            {
                Debug(() => { return "not found " + _iconPath; });
            }
        }

        private void Debug(Func<string> txt)
        {
            if(1 == 0)
            {
                File.AppendAllText("E:\\test.txt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")+ txt());
            }
        }
        #endregion
    }
}
