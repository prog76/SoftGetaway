using Microsoft.VisualBasic.ApplicationServices;
using System;
 
namespace softGetawayClient
{
    public class SingleInstanceManager : WindowsFormsApplicationBase
    {
        App app;

        public SingleInstanceManager()
        {
            this.IsSingleInstance = true;
        }

        protected override bool OnStartup(Microsoft.VisualBasic.ApplicationServices.StartupEventArgs e)
        {
            // First time app is launched
            app = new App();
            app.InitializeComponent();
				try
				{
					app.Run();
				}
				catch (Exception ex)
				{
				}
            return false;
        }

        protected override void OnStartupNextInstance(StartupNextInstanceEventArgs eventArgs)
        {
            // Subsequent launches
            base.OnStartupNextInstance(eventArgs);
            app.Activate();
        }
    }
}
