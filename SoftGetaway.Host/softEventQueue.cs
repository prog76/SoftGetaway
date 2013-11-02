using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace softGetawayHost {

    class softEventQueue<T> : IDisposable {
        public class EventType : EventArgs {
            public T ev;
            public EventType(T _ev) {
                ev = _ev;
            }
        }
        LinkedList<T> list;
        AutoResetEvent waitEvent;
        EventHandler<EventType> fProcessor;
        bool disposed;


        public event EventHandler<EventType> processor {
            add {
                fProcessor += value;
            }
            remove {
                fProcessor -= value;
            }
        }

        private void eventProcessor() {
            T ev;
            while (!disposed) {
                ev = get();
                if (!disposed)
                    fProcessor.Invoke(this, new EventType(ev));
            }
        }

        public softEventQueue() {
            list = new LinkedList<T>();
            waitEvent = new AutoResetEvent(false);
            disposed = false;
        }

        internal void put(T softEvent, int priority) {
            lock (this) {
                while (list.Remove(softEvent)) ;
                list.AddLast(softEvent);
                waitEvent.Set();
            }
        }

        internal T get() {
            T softEvent;
            bool wait = false;
            do {
                if (wait) waitEvent.WaitOne();
                lock (this) {
                    wait = (list.Count == 0);
                    if (!wait) {
                        softEvent = list.First.Value;
                        list.RemoveFirst();
                        break;
                    }
                }
            } while (true);
            return softEvent;
        }

        internal void start() {
            new Thread(eventProcessor).Start();
        }

        public void Dispose() {
            disposed = true;
            put(default(T), 0);
        }
    }
}
