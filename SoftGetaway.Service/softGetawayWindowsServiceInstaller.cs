using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;
using System.Diagnostics;

namespace softGetawayService {
    [RunInstaller(true)]
    public class softGetawayWindowsServiceInstaller : Installer {
        string strServiceName = "Software Getaway";

   /*     private EventLogInstaller FindInstaller(InstallerCollection installers) {
            foreach (Installer installer in installers) {
                if (installer is EventLogInstaller) {
                    return (EventLogInstaller)installer;
                }

                EventLogInstaller eventLogInstaller = FindInstaller(installer.Installers);
                if (eventLogInstaller != null) {
                    return eventLogInstaller;
                }
            }
            return null;
        }                     */

        public softGetawayWindowsServiceInstaller() {
            var processInstaller = new ServiceProcessInstaller();
            var serviceInstaller = new ServiceInstaller();

            processInstaller.Account = ServiceAccount.LocalSystem;
            processInstaller.Username = null;
            processInstaller.Password = null;

            serviceInstaller.DisplayName = AssemblyAttributes.AssemblyTitle;
            serviceInstaller.Description = AssemblyAttributes.AssemblyDescription;
            serviceInstaller.StartType = ServiceStartMode.Automatic;
            serviceInstaller.DelayedAutoStart = true;

            //            serviceInstaller.ServicesDependedOn = new[] { "SharedAccess", "winmgmt" };
            serviceInstaller.ServicesDependedOn = new[] { "winmgmt" };
            serviceInstaller.ServiceName = strServiceName;
            // Remove Event Source if already there
        /*    if (EventLog.SourceExists(strServiceName))
                EventLog.DeleteEventSource(strServiceName);     */

            this.Installers.Add(processInstaller);
            this.Installers.Add(serviceInstaller);

  /*          EventLogInstaller installer = FindInstaller(this.Installers);
            if (installer != null) {
                installer.Log = strServiceName; // enter your event log name here
            }   */

            this.Committed += new InstallEventHandler(softGetawayWindowsServiceInstaller_Committed);
        }

        void softGetawayWindowsServiceInstaller_Committed(object sender, InstallEventArgs e) {
            // Auto Start the Service Once Installation is Finished.
            var controller = new ServiceController(strServiceName);
            controller.Start();
        }
    }
}
