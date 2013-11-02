using System;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Diagnostics;
using System.Collections.Generic;
using System.ComponentModel;
using System.ServiceProcess;
using System.Security.Principal;

namespace softGetawayClient {
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application {
        MyVarContainer varContainer;
        private Thread threadServiceChecker;
        AutoResetEvent testService;
        bool hasAdministrativeRight, testServiceInstalled;

        public void Activate() {
            this.MainWindow.WindowState = WindowState.Normal;
            this.MainWindow.Activate();
        }

        protected override void OnStartup(StartupEventArgs e) {
            varContainer = new MyVarContainer();
            testService = new AutoResetEvent(false);
            testServiceInstalled = !e.Args.Contains("-no_service_check");
            WindowsPrincipal pricipal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
            hasAdministrativeRight = pricipal.IsInRole(WindowsBuiltInRole.Administrator);

            base.OnStartup(e);
            this.threadServiceChecker = new Thread(new ThreadStart(this.ServiceChecker));
            this.threadServiceChecker.Start();
        }

        protected override void OnExit(ExitEventArgs e) {
            this.threadServiceChecker.Abort();
            base.OnExit(e);
        }

        private void ServiceChecker() {
            while (true) {
                if ((this._softGetaway != null) && (this._softGetaway.State == System.ServiceModel.CommunicationState.Faulted))
                    this._softGetaway = null;
                if (this._softGetaway == null) {
                    if (testServiceInstalled) {
                        var ctl = (from s in ServiceController.GetServices()
                                   where (s.ServiceName.Equals("Software Getaway"))
                                   select s).FirstOrDefault();
                        varContainer.IsServiceInstalled = (ctl != null);
                        if (varContainer.IsServiceInstalledTrue) {
                            if (ctl != null) {
                                if (ctl.Status.Equals(ServiceControllerStatus.Running))
                                    varContainer.IsServiceStarted = true;
                                else if (ctl.Status.Equals(ServiceControllerStatus.Stopped))
                                    varContainer.IsServiceStarted = false;
                                else varContainer.IsServiceStarted = null;
                            }
                        }
                    }
                        if ((!testServiceInstalled) || (varContainer.IsServiceStartedTrue))
                            this.ConnectService();
                    

                }
                testService.WaitOne(5000);
            }
        }

        private void ConnectService() {
            if (this._softGetaway == null) {
                this._softGetaway = new softGetawayClient.softGetawayService.SoftGetawayHostClient();
                this._softGetaway.InnerChannel.Faulted += new EventHandler(InnerChannel_Faulted);
                this._softGetaway.InnerChannel.Closed += new EventHandler(InnerChannel_Closed);
                this._softGetaway.InnerChannel.Opened += new EventHandler(InnerChannel_Opened);
                try {
                    this._softGetaway.Open();
                } catch (Exception ex) {
                }
            }
        }

        private softGetawayService.SoftGetawayHostClient _softGetaway;
        public softGetawayService.SoftGetawayHostClient softGetaway {
            get {
                return this._softGetaway;
            }
        }

        public event EventHandler VirtualRouterServiceConnected;
        public event EventHandler VirtualRouterServiceDisconnected;

        private void InvokeVirtualRouterServiceDisconnected() {
            varContainer.IsServiceStarted = false;
            if (this.VirtualRouterServiceDisconnected != null) {
                this.Dispatcher.Invoke(this.VirtualRouterServiceDisconnected, this, EventArgs.Empty);
            }
        }

        private void InnerChannel_Opened(object sender, EventArgs e) {
            this._IsVirtualRouterServiceConnected = true;
            varContainer.IsServiceStarted = true;
            if (this.VirtualRouterServiceConnected != null) {
                this.Dispatcher.Invoke(this.VirtualRouterServiceConnected, this, EventArgs.Empty);
            }
        }

        private void InnerChannel_Closed(object sender, EventArgs e) {
            this._IsVirtualRouterServiceConnected = false;
            InvokeVirtualRouterServiceDisconnected();
        }

        private void InnerChannel_Faulted(object sender, EventArgs e) {
            testService.Set();
            this._IsVirtualRouterServiceConnected = false;
            InvokeVirtualRouterServiceDisconnected();
        }

        private bool _IsVirtualRouterServiceConnected = false;

        public bool IsVirtualRouterServiceConnected {
            get {
                return this._IsVirtualRouterServiceConnected;
            }
        }

        public bool RunElevated(string fileName, string args) {
            ProcessStartInfo processInfo = new ProcessStartInfo();
            if (!hasAdministrativeRight) {
                processInfo.Verb = "runas";
            }
            processInfo.FileName = fileName;
            processInfo.Arguments = args;
            processInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            try {
                Process proc = new Process();
                proc.Exited += new EventHandler(proc_Exited);
                proc.StartInfo = processInfo;
                proc.Start();
                return true;
            } catch (Exception ex) {
                Trace.TraceInformation("Client:Failed to start {0} {1} {2}", fileName, args, ex.Message);
            }
            testService.Set();
            return false;
        }

        void proc_Exited(object sender, EventArgs e) {
            testService.Set();
        }
    }
}
