using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace softGetawayHost {
    public class GetawayTraceListener : TraceListener {
        static List<string> lines = new List<string>();
        public override void TraceData(TraceEventCache eventCache, string source, TraceEventType eventType, int id, object data) {
            this.WriteLine(data.ToString());
        }

        public override void Write(string message) {
        /*    lock (lines) {
                lines.Add(message);
            }           */
        }

        public override void WriteLine(string message) {
            lock (lines) {
                lines.Add(DateTime.Now.ToShortTimeString()+"\t"+message);
            }
        }

        public static IEnumerable<String> getLines() {
            lock (lines) {
                foreach (var line in lines)
                    yield return line;
                lines.Clear();
            }
        }
    }
}

