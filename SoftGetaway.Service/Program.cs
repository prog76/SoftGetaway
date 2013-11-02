using System.ServiceProcess;
using System;
using System.Configuration.Install;
using System.Collections;

namespace softGetawayService
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static int Main(string[] args)
        {
            bool install = false, uninstall = false, console = false, rethrow = false;

            try
            {
                foreach (string arg in args)
                {
                    switch (arg)
                    {
                        case "-i":
                        case "-install":
                            install = true; break;
                        case "-u":
                        case "-uninstall":
                            uninstall = true; break;
                        case "-c":
                        case "-console":
                            console = true; break;
                        default:
                            Console.Error.WriteLine("Argument not expected: " + arg);
                            break;
                    }
                }
                if (uninstall)
                {
                    Install(true, args);
                }
                if (install)
                {
                    Install(false, args);
                }
                if (console)
                {
                    Console.WriteLine("Starting...");
                    softGetawayWindowsService router = new softGetawayWindowsService();
                    router.startUp();
                    Console.WriteLine("System running; press any key to stop");
                    Console.ReadKey(true);
                    router.Stop();
                    Console.WriteLine("System stopped");
                }
                else if (!(install || uninstall))
                {
                    rethrow = true; // so that windows sees error...
                    ServiceBase[] ServicesToRun;
                    ServicesToRun = new ServiceBase[] 
            { 
                new softGetawayWindowsService() 
            };
                    ServiceBase.Run(ServicesToRun);

                    rethrow = false;
                }
                return 0;
            }
            catch (Exception ex)
            {
                if (rethrow) throw;
                Console.Error.WriteLine(ex.Message);
                return -1;
            }
        }
        static void Install(bool undo, string[] args)
        {
            try
            {
                Console.WriteLine(undo ? "uninstalling" : "installing");
                using (AssemblyInstaller inst = new AssemblyInstaller(typeof(Program).Assembly, args))
                {
                    IDictionary state = new Hashtable();
                    inst.UseNewContext = true;
                    try
                    {
                        if (undo)
                        {
                            inst.Uninstall(state);
                        }
                        else
                        {
                            inst.Install(state);
                            inst.Commit(state);
                        }
                    }
                    catch
                    {
                        try
                        {
                            inst.Rollback(state);
                        }
                        catch { }
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                throw;
            }
        }
    }

}
