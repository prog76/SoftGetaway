using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel;
using System.Threading;

namespace softGetawayClient {
    /// <summary>
    /// Interaction logic for MyVarContainer.xaml
    /// </summary>
    public partial class MyVarContainer : UserControl, INotifyPropertyChanged {
        static bool serviceOverride = false, fIsConfigChanged = false, fIsConfigApplied = true;
        static bool? fIsWiFiMode = null, fIsServiceStarted = null, fIsServiceInstalled = null, fIsGetawayStarted = null, fIsDHCPEnabled = null, fIsClientConnected = null;
        static List<MyVarContainer> instancesList = new List<MyVarContainer>();
        EventHandler _serviceStarted, _serviceStopped, _getawayStarted, _getawayStopped, _clientNotify;

        public event EventHandler clientNotify {
            add {
                _clientNotify += value;
                fireHandler(fIsClientConnected != null, _clientNotify);
            }
            remove {
                _clientNotify -= value;
            }
        }

        public event EventHandler serviceStarted {
            add {
                _serviceStarted += value;
                fireHandler(IsServiceStarted.Equals(true), _serviceStarted);
            }
            remove {
                _serviceStarted -= value;
            }
        }

        public event EventHandler serviceStopped {
            add {
                _serviceStopped += value;
                fireHandler(!IsServiceStarted.Equals(true), _serviceStopped);
            }
            remove {
                _serviceStopped -= value;
            }
        }

        public event EventHandler getawayStarted {
            add {
                _getawayStarted += value;
                fireHandler(IsGetawayStarted.Equals(true), _getawayStarted);
            }
            remove {
                _getawayStarted -= value;
            }
        }

        public event EventHandler getawayStopped {
            add {
                _getawayStopped += value;
                fireHandler(IsGetawayStartedFalse, _getawayStopped);
            }
            remove {
                _getawayStopped -= value;
            }
        }

        public MyVarContainer() {
            lock (instancesList) {
                instancesList.Add(this);
            }
        }

        public bool? IsServiceInstalled {
            get { return serviceOverride ? true : fIsServiceInstalled; }
            set {
                if (fIsServiceInstalled != value) {
                    fIsServiceInstalled = value;
                    updateProperties("IsServiceInstalled");
                    updateProperties("IsServiceInstalledTrue");
                    updateProperties("IsServiceInstalledFalse");
                    if (!fIsServiceInstalled.Equals(true))
                        IsServiceStarted = null;
                }
            }
        }

        public bool IsServiceInstalledTrue {
            get { return IsServiceInstalled.Equals(true); }
        }

        public bool IsServiceInstalledFalse {
            get { return IsServiceInstalled.Equals(false); }
        }

        public bool? IsServiceStarted {
            get { return dependantProperty(serviceOverride ? true : fIsServiceStarted, IsServiceInstalled); }
            set {
                if (serviceOverride) value = serviceOverride;
                if (fIsServiceStarted != value) {
                    fIsServiceStarted = value;
                    updateProperties("IsServiceStarted");
                    updateProperties("IsServiceStartedTrue");
                    updateProperties("IsServiceStartedFalse");
                    BackgroundWorker bw = new BackgroundWorker();
                    // what to do in the background thread
                    bw.DoWork += new DoWorkEventHandler(
                    delegate(object o, DoWorkEventArgs args) {
                        lock (instancesList) {
                            foreach (var instance in instancesList) {
                                instance.fireHandler(value.Equals(true), instance._serviceStarted);
                                instance.fireHandler(value.Equals(false), instance._serviceStopped);
                            }
                        }
                    });
                    bw.RunWorkerAsync();
                    if (!fIsServiceStarted.Equals(true))
                        IsGetawayStarted = null;
                }
            }
        }


        public bool IsServiceStartedTrue {
            get { return IsServiceStarted.Equals(true); }
        }

        public bool IsServiceStartedFalse {
            get { return IsServiceStarted.Equals(false); }
        }

        public bool? IsGetawayStarted {
            get {
                return dependantProperty(fIsGetawayStarted, IsServiceStarted);
            }
            set {
                if (fIsGetawayStarted != value) {
                    fIsGetawayStarted = value;
                    updateProperties("IsGetawayStarted");
                    updateProperties("IsGetawayStartedTrue");
                    updateProperties("IsGetawayStartedFalse");
                    updateProperties("IsCouldSaveConfig");
                    foreach (var instance in instancesList) {
                        instance.fireHandler(value.Equals(true), instance._getawayStarted);
                        instance.fireHandler(value.Equals(false), instance._getawayStopped);
                    }
                    if (!fIsGetawayStarted.Equals(true))
                        IsClientConnected = null;
                }
            }
        }

        public bool IsGetawayStartedTrue {
            get { return IsGetawayStarted.Equals(true); }
        }

        public bool IsGetawayStartedFalse {
            get { return IsGetawayStarted.Equals(false); }
        }

        public bool IsWiFiMode {
            get { return fIsWiFiMode.Equals(true); }
            set {
                if (fIsWiFiMode != value) {
                    fIsWiFiMode = value;
                    updateProperties("IsWiFiMode");
                    updateProperties("IsNotWiFiMode");
                }
            }
        }

        public bool IsNotWiFiMode {
            get { return !IsWiFiMode; }
        }

        public bool IsDHCPEnabled {
            get { return fIsDHCPEnabled.Equals(true); }
            set {
                if (fIsDHCPEnabled != value) {
                    fIsDHCPEnabled = value;
                    updateProperties("IsDHCPEnabled");
                    updateProperties("IsDHCPEnabledFalse");
                }
            }
        }
        public bool IsDHCPEnabledFalse {
            get { return !IsDHCPEnabled; }
        }

        public bool? IsClientConnected {
            get { return fIsClientConnected; }
            set {
                if (fIsClientConnected != value) {
                    updateProperties("IsClientConnected");
                }
            }
        }

        public bool IsConfigChanged {
            get { return fIsConfigChanged; }
            set {
                if (value != fIsConfigChanged) {
                    fIsConfigChanged = value;
                    updateProperties("IsConfigChanged");
                    updateProperties("IsCouldSaveConfig");
                }
            }
        }

        public bool IsConfigApplied {
            get { return fIsConfigApplied; }
            set {
                if (value != fIsConfigApplied) {
                    fIsConfigApplied = value;
                    updateProperties("IsConfigApplied");
                    updateProperties("IsCouldSaveConfig");
                }
            }
        }

        public bool IsCouldSaveConfig {
            get { return IsConfigChanged && IsConfigApplied; }
        }

        void updateProperties(string propertyName) {
            BackgroundWorker bw = new BackgroundWorker();
            // what to do in the background thread
            bw.DoWork += new DoWorkEventHandler(
            delegate(object o, DoWorkEventArgs args) {
                lock (instancesList) {
                    foreach (var instance in instancesList)
                        instance.OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
                }
            });
            bw.RunWorkerAsync();
        }

        delegate void fireHandlerDelegate(EventHandler item);

        bool? dependantProperty(bool? prop, bool? dependant) {
            if (dependant == null) return prop;
            if (prop == null) return prop;
            return dependant.Equals(true) && prop.Equals(true);
        }

        void realHandler(EventHandler handler) {
            if (Parent.Dispatcher.CheckAccess())
                handler(this, null);
            else
                Parent.Dispatcher.Invoke(new fireHandlerDelegate(realHandler), new object[] { handler });
        }

        void fireHandler(bool fire, EventHandler handler) {
            if ((fire) && (handler != null)) realHandler(handler);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(PropertyChangedEventArgs e) {
            if (PropertyChanged != null) {
                PropertyChanged(this, e);
            }
        }

        #region Dependency Properties

        public bool setServiceInstalled { get; set; }
        public bool setServiceStarted { get; set; }
        public bool setGetawayStarted { get; set; }

        public static readonly DependencyProperty ServiceInstalledProperty =
              DependencyProperty.Register("setServiceInstalled", typeof(bool), typeof(MyVarContainer), new UIPropertyMetadata(false, propChanged));

        public static readonly DependencyProperty ServiceStartedProperty =
              DependencyProperty.Register("setServiceStarted", typeof(bool), typeof(MyVarContainer), new UIPropertyMetadata(false, propChanged));

        public static readonly DependencyProperty GetawayStartedProperty =
              DependencyProperty.Register("setGetawayStarted", typeof(bool), typeof(MyVarContainer), new UIPropertyMetadata(false, propChanged));

        public static readonly DependencyProperty GetawayDHCPEnabledProperty =
              DependencyProperty.Register("setDHCPEnabled", typeof(bool), typeof(MyVarContainer), new UIPropertyMetadata(false, propChanged));

        private static void propChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
                d.GetType().GetProperty(e.Property.Name.Replace("set", "Is")).SetValue(d, e.NewValue, null);
        }

        #endregion Dependency Properties




    }
}
