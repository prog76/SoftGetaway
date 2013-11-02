using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceProcess;
using softGetawayHost;
using System.Collections.Generic;

namespace softGetawayService
{
    public partial class softGetawayWindowsService : ServiceBase
    {
        private ServiceHost serviceHost = null;
        private softGetawayHost.softGetawayHost softGetawayHost = null;

        public softGetawayWindowsService()
        {
            InitializeComponent();

            this.AutoLog = true;
            this.CanShutdown = true;
            this.CanPauseAndContinue = true;
        }

        internal void startUp()
        {
            if (this.serviceHost != null)
            {
                this.serviceHost.Close();
            }
            this.softGetawayHost = new softGetawayHost.softGetawayHost();
            this.serviceHost = new ServiceHost(this.softGetawayHost);

            if (this.serviceHost.State != CommunicationState.Opened)
            {
                this.serviceHost.Open();
            }
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                startUp();  
            }
            catch (Exception ex)
            {
                WriteLog("Starting service failed\n" + ex.ToString());
            }
        }

        protected override void OnStop()
        {
            if (softGetawayHost != null)
            {
                this.softGetawayHost.Stop();
					 softGetawayHost.Dispose();
            }

            if (this.serviceHost != null)
            {
                this.serviceHost.Close();
                this.serviceHost = null;
            }
        }

        protected override void OnShutdown()
        {
            this.OnStop();
        }

        protected override void OnPause()
        {
            this.OnShutdown();
        }

        protected override void OnContinue()
        {
            this.OnStart(new string[0]);
        }

        private void WriteLog(string message)
        {
            EventLog.WriteEntry("Getaway Service", message, EventLogEntryType.Information);
        }
    }
}
