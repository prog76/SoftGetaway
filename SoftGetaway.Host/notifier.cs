using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Security.Principal;
using softGetawayHost;
using System.Threading;

namespace softGetaway {
    class Notifier: IDisposable {
        string machineId;
        string fSoftId;
        bool disposed;
        softEventQueue<string> events;

        public void notify(string action){
            events.put(action, 0);   
        }
        public Notifier(string softId) {
            machineId=new HardwareHelper().GetHardwareID();
            fSoftId = softId;
            disposed = false;
            events = new softEventQueue<string>();
            events.processor += new EventHandler<softEventQueue<string>.EventType>(events_processor);
            events.start();
        }

        void events_processor(object sender, softEventQueue<string>.EventType e) {
            while (!disposed) {
                try {
                    string url = "http://softgetaway.netai.net/logaction.php?uid=" + machineId + "&gid=" + fSoftId + "&action=" + e.ev;
                    var request = (HttpWebRequest)HttpWebRequest.Create(url);
                    request.Method = "GET";
                    var response = (HttpWebResponse)request.GetResponse();
                    break;
                } catch {
                    Thread.Sleep(100000);
                }
            }
        }

        public void Dispose() {
            disposed = true;
            events.Dispose();
        }
    }
}
