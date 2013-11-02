using System.ComponentModel;
using System.Configuration.Install;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace softGetawayClient
{
    [RunInstaller(true)]
    public class softGetawayClientInstaller : Installer
    {
        public softGetawayClientInstaller()
        {
            //this.Committed += new InstallEventHandler(softGetawayClientInstallerr_Committed);
            this.AfterInstall += new InstallEventHandler(softGetawayClientInstaller_AfterInstall);
        }

        void softGetawayClientInstaller_AfterInstall(object sender, InstallEventArgs e)
        {
            try
            {
                Directory.SetCurrentDirectory(Path.GetDirectoryName
                (Assembly.GetExecutingAssembly().Location));
                Process.Start(Path.GetDirectoryName(
                  Assembly.GetExecutingAssembly().Location) + "\\softGetawayClient.exe");
            }
            catch
            {
                // Do nothing... 
            }
        }

        //void softGetawayClientInstaller_Committed(object sender, InstallEventArgs e)
        //{
        //}

        public override void Install(System.Collections.IDictionary stateSaver)
        {
            base.Install(stateSaver);
        }

        public override void Commit(System.Collections.IDictionary savedState)
        {
            base.Commit(savedState);
        }

        public override void Rollback(System.Collections.IDictionary savedState)
        {
            base.Rollback(savedState);
        }
    }
}
