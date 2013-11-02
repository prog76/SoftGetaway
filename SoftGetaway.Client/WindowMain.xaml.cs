using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using softGetawayClient.softGetawayService;
using System.Diagnostics;
using System.Collections.Generic;
using softGetawayClient.Properties;
using softGetawayClient.AeroGlass;
using System.Net;
using System.Windows.Controls;
using System.Drawing;

namespace softGetawayClient {
    /// <summary>
    /// Interaction logic for WindowMain.xaml
    /// </summary>
    public partial class WindowMain : Window {
        private App myApp = (App)App.Current;
        private Thread threadUpdateUI;
        AutoResetEvent uiUpdateEvent;
        bool processValidate;

        private WpfNotifyIcon trayIcon;

        public WindowMain() {
            uiUpdateEvent = new AutoResetEvent(false);
            InitializeComponent();

            this.Loaded += new RoutedEventHandler(Window1_Loaded);
            this.Closing += new System.ComponentModel.CancelEventHandler(WindowMain_Closing);

            myApp.VirtualRouterServiceConnected += new EventHandler(myApp_VirtualRouterServiceConnected);
            myApp.VirtualRouterServiceDisconnected += new EventHandler(myApp_VirtualRouterServiceDisconnected);
        }

        private void WindowMain_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            Settings.Default.SSID = txtSSID.Text;
            Settings.Default.Password = txtPassword.Text;
            Settings.Default.IP = txtIP.Text;
            Settings.Default.Save();
        }

        private void Window1_Loaded(object sender, RoutedEventArgs e) {
            //       AeroGlassHelper.ExtendGlass(this, (int)windowContent.Margin.Left, (int)windowContent.Margin.Right, (int)windowContent.Margin.Top, (int)windowContent.Margin.Bottom);

            txtSSID.Text = Settings.Default.SSID;
            txtPassword.Text = Settings.Default.Password;
            txtIP.Text = Settings.Default.IP;

            // This line is for testing purposes
            //panelConnections.Children.Add(new PeerDevice(new ConnectedPeer() { MacAddress = "AA-22-33-EE-EE-FF" }));

            var args = System.Environment.GetCommandLineArgs();
            var minarg = (from a in args
                          where a.ToLowerInvariant().Contains("/min")
                          select a).FirstOrDefault();
            if (!string.IsNullOrEmpty(minarg)) {
                this.WindowState = WindowState.Minimized;
                this.ShowInTaskbar = false;
            }

            this.AddSystemMenuItems();

            this.threadUpdateUI = new Thread(new ThreadStart(this.UpdateUIThread));
            this.threadUpdateUI.Start();

            this.Closed += new EventHandler(Window1_Closed);

            this.trayIcon = new WpfNotifyIcon();
            this.trayIcon.Show();
            this.trayIcon.DoubleClick += new EventHandler(trayIcon_DoubleClick);

            var trayMenu = new System.Windows.Forms.ContextMenuStrip();
            trayMenu.Items.Add("&Manage Software Getaway", null, new EventHandler(this.TrayIcon_Menu_Manage));
            trayMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
            trayMenu.Items.Add("Check for &Updates", null, new EventHandler(this.TrayIcon_Menu_Update));
            trayMenu.Items.Add("&About", null, new EventHandler(this.TrayIcon_Menu_About));
            this.trayIcon.ContextMenuStrip = trayMenu;

            this.StateChanged += new EventHandler(WindowMain_StateChanged);

            varContainer.serviceStopped += new EventHandler(varContainer_serviceStopped);
            varContainer.serviceStarted += new EventHandler(varContainer_serviceStarted);
            varContainer.getawayStarted += new EventHandler(varContainer_getawayStarted);
            varContainer.getawayStopped += new EventHandler(varContainer_getawayStopped);
            varContainer.clientNotify += new EventHandler(varContainer_clientNotify);
            if (DateTime.Now > DateTime.ParseExact("01.06.2033", "dd.MM.yyyy", null)) {
                MessageBoxResult result = MessageBox.Show("Your version of Soft Getaway is outdated. Do you want to check for updates?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes) {
                    Process.Start("http://softgetaway.netai.net");
                }
            }
            UpdateDisplay();
        }

        void TrayIcon_Menu_Update(object sender, EventArgs e) {
            CheckUpdates();
        }

        public static void CheckUpdates() {
            Process.Start("http://softgetaway.netai.net");
        }

        void TrayIcon_Menu_About(object sender, EventArgs e) {
            ShowAboutBox();
        }

        void TrayIcon_Menu_Manage(object sender, EventArgs e) {
            this.WindowState = WindowState.Normal;
        }

        void WindowMain_StateChanged(object sender, EventArgs e) {
            if (this.WindowState == WindowState.Minimized) {
                this.ShowInTaskbar = false;
            } else {
                this.ShowInTaskbar = true;
            }
        }

        void trayIcon_DoubleClick(object sender, EventArgs e) {
            this.WindowState = WindowState.Normal;
        }

        private void Window1_Closed(object sender, EventArgs e) {
            this.threadUpdateUI.Abort();
            this.trayIcon.Hide();
            this.trayIcon.Dispose();
        }

        void UpdateUIThread() {
            while (true) {
                this.Dispatcher.Invoke(new Action(this.UpdateDisplay));
                uiUpdateEvent.WaitOne(5000);
            }
        }

        void myApp_VirtualRouterServiceDisconnected(object sender, EventArgs e) {
            lblStatus.Content = "Soft Getaway Service is not running.";
            UpdateDisplay();
        }

        void myApp_VirtualRouterServiceConnected(object sender, EventArgs e) {
            processValidate = false;
            try {
                var settings = myApp.softGetaway.GetPrivateConnectionSettings();
                txtSSID.Text = settings.SSID;
                txtPassword.Text = settings.Password;
            } catch {
            }
            txtIP.Text = myApp.softGetaway.GetIP();
            cbIsDHCPEnabled.IsChecked = (txtIP.Text.Length > 6);
            UpdateDisplay();
            processValidate = true;
        }

        private void UpdateDisplay() {
            try {
                if (myApp.IsVirtualRouterServiceConnected) {
                    UpdateUIDisplay();
                    var lines = myApp.softGetaway.GetTraceLines();
                    string lastLine = null;
                    foreach (var line in lines) {
                        logList.Items.Add(line);
                        lastLine = line;
                    }
                    if (lastLine != null) {
                        logList.Items.MoveCurrentTo(lastLine);
                        logList.ScrollIntoView(lastLine);
                    }
                    if (varContainer.IsGetawayStartedTrue) {
                        var peers = myApp.softGetaway.GetPeers();
                        varContainer.IsClientConnected = peers.Count() > 0;
                        groupBoxPeersConnected.Header = "Peers Connections/reservations (" + peers.Count().ToString() + "):";
                        foreach (var p in peers) {
                            PeerDevice element = this.isPeerAlreadyConnected(p);
                            if (element == null) {
                                element = new PeerDevice(p);
                                panelConnections.Children.Add(element);
                            } else {
                                element.Peer = p;
                            }
                        }
                        this.removeDisconnectedPeers(peers);
                    } else {
                        groupBoxPeersConnected.Header = "Peers Connected (0):";
                        if (varContainer.IsServiceInstalledFalse)
                            lblStatus.Content = "Service not Installed";
                        else
                            if (varContainer.IsServiceStartedFalse)
                                lblStatus.Content = "Service not Started";
                    }
                }
            } catch { }
        }

        private PeerDevice isPeerAlreadyConnected(NetworkPeerService peer) {
            foreach (var element in panelConnections.Children) {
                var elem = element as PeerDevice;
                if (elem != null) {
                    if (elem.Peer.storage.MACAddress.ToLowerInvariant() == peer.storage.MACAddress.ToLowerInvariant()) {
                        return elem;
                    }
                }
            }
            return null;
        }
        private void removeDisconnectedPeers(NetworkPeerService[] peers) {
            List<PeerDevice> peersToRemove = new List<PeerDevice>();

            foreach (var element in panelConnections.Children) {
                var elem = element as PeerDevice;
                if (elem != null) {
                    var exists = false;
                    foreach (var p in peers) {
                        if (p.storage.MACAddress.ToLowerInvariant() == elem.Peer.storage.MACAddress.ToLowerInvariant()) {
                            exists = true;
                            break;
                        }
                    }
                    if (!exists) {
                        peersToRemove.Add(elem);
                    }
                }
            }
            foreach (var elem in peersToRemove) {
                panelConnections.Children.Remove(elem);
            }
        }

        #region "System Menu Stuff"

        #region Win32 API Stuff

        // Define the Win32 API methods we are going to use
        [DllImport("user32.dll")]
        private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

        [DllImport("user32.dll")]
        private static extern bool InsertMenu(IntPtr hMenu, Int32 wPosition, Int32 wFlags, Int32 wIDNewItem, string lpNewItem);

        /// Define our Constants we will use
        public const Int32 WM_SYSCOMMAND = 0x112;
        public const Int32 MF_SEPARATOR = 0x800;
        public const Int32 MF_BYPOSITION = 0x400;
        public const Int32 MF_STRING = 0x0;

        #endregion

        private const int _AboutSysMenuID = 1001;
        private const int _UpdateSysMenuID = 1002;

        private void AddSystemMenuItems() {
            IntPtr windowHandle = new WindowInteropHelper(this).Handle;
            IntPtr systemMenu = GetSystemMenu(windowHandle, false);

            InsertMenu(systemMenu, 5, MF_BYPOSITION | MF_SEPARATOR, 0, string.Empty);
            InsertMenu(systemMenu, 6, MF_BYPOSITION, _UpdateSysMenuID, "Check for Updates...");
            InsertMenu(systemMenu, 7, MF_BYPOSITION, _AboutSysMenuID, "About...");

            HwndSource source = HwndSource.FromHwnd(windowHandle);
            source.AddHook(new HwndSourceHook(WndProc));
        }

        private static IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) {
            // Check if a System Command has been executed
            if (msg == WM_SYSCOMMAND) {
                // Execute the appropriate code for the System Menu item that was clicked
                switch (wParam.ToInt32()) {
                    case _AboutSysMenuID:
                        ShowAboutBox();
                        handled = true;
                        break;
                    case _UpdateSysMenuID:
                        CheckUpdates();
                        handled = true;
                        break;
                }
            }

            return IntPtr.Zero;
        }

        #endregion

        static void ShowAboutBox() {
            System.Windows.MessageBox.Show(
                                                 AssemblyAttributes.AssemblyProduct + " " + AssemblyAttributes.AssemblyVersion + Environment.NewLine
                                                 + Environment.NewLine + AssemblyAttributes.AssemblyDescription + Environment.NewLine
                                                 + Environment.NewLine + AssemblyAttributes.AssemblyCopyright + Environment.NewLine
                                                 + Environment.NewLine + "http://softgetaway.netai.net"

                                                 , "About " + AssemblyAttributes.AssemblyProduct + "...");
        }

        private void UpdateUIDisplay() {
            switch (myApp.softGetaway.GetState()) {
                case getawayState.Idle:
                    this.trayIcon.Text = "Soft Getaway Idle";
                    varContainer.IsGetawayStarted = false;
                    break;
                case getawayState.Initialization:
                    this.trayIcon.Text = "Soft Getaway Initialization";
                    break;
                case getawayState.StartingIP:
                    this.trayIcon.Text = "Soft Getaway Setting up IP Address";
                    break;
                case getawayState.Starting:
                    this.trayIcon.Text = "Soft Getaway Starting";
                    break;
                case getawayState.Started:
                    this.trayIcon.Text = "Soft Getaway Started";
                    varContainer.IsGetawayStarted = true;
                    break;
                case getawayState.StartFailed:
                    varContainer.IsGetawayStarted = false;
                    this.trayIcon.Text = "Soft Getaway Start failed";
                    break;
                case getawayState.Stopping:
                    this.trayIcon.Text = "Soft Getaway Stopping";
                    break;
                case getawayState.Stopped:
                    varContainer.IsGetawayStarted = false;
                    this.trayIcon.Text = "Soft Getaway Stopped";
                    break;
                case getawayState.StopFailed:
                    this.trayIcon.Text = "Soft Getaway Stoped with errors";
                    varContainer.IsGetawayStarted = true;
                    break;
            }
            lblStatus.Content = this.trayIcon.Text;
            if (cbSharedConnection.SelectedItem == null)
                RefreshSharableConnectionsDisplay(null);
        }

        private void RefreshSharableConnectionsDisplay(string guid) {
            cbSharedConnection.DisplayMemberPath = "Name";
            var connections = myApp.softGetaway.GetSharableConnections();

            string selectedId = guid;
            if (selectedId == null) {
                if (myApp.softGetaway.IsShouldStart()) {
                    selectedId = myApp.softGetaway.GetSharedConnection();
                } else {
                    var previousItem = cbSharedConnection.SelectedItem as SharableConnection;
                    if (previousItem != null) {
                        selectedId = previousItem.Guid;
                    }
                }
            }

            bool oldProcessValidate = processValidate;
            processValidate = false;
            cbSharedConnection.Items.Clear();
            foreach (var c in connections) {
                cbSharedConnection.Items.Add(c);
                if (c.Guid == selectedId) {
                    cbSharedConnection.SelectedItem = c;
                }
            }
            processValidate = oldProcessValidate;
        }

        private void loadConfig_Click(object sender, RoutedEventArgs e) {
            myApp.softGetaway.LoadConfig();
            myApp_VirtualRouterServiceConnected(null, null);
            varContainer.IsConfigChanged = false;
        }

        private void saveConfig_Click(object sender, RoutedEventArgs e) {
            myApp.softGetaway.SaveConfig();
            varContainer.IsConfigChanged = false;
        }

        private void cbSharedConnection_DropDownOpened(object sender, EventArgs e) {
            if (cbSharedConnection.SelectedItem != null)
                RefreshSharableConnectionsDisplay(((SharableConnection)cbSharedConnection.SelectedItem).Guid);
            else
                RefreshSharableConnectionsDisplay(null);
        }

        bool processToggleButton(object sender, string app, string format, string arg1, string arg2) {
            MyToggleButton tb = (MyToggleButton)sender;
            return myApp.RunElevated(app, String.Format(format, (tb.IsChecked.Equals(true) ? arg1 : arg2)));
        }

        private void clkServiceInstall(object sender, RoutedEventArgs e) {
            if (processToggleButton(sender, System.AppDomain.CurrentDomain.BaseDirectory + "\\softGetawayService.exe",
                    "{0}", "-u", "-i")) {
            }
            varContainer.IsServiceInstalled = null;
        }

        private void clkServiceStart(object sender, RoutedEventArgs e) {
            if (processToggleButton(sender, Environment.GetFolderPath(Environment.SpecialFolder.System) + "\\cmd.exe",
                    "/c net {0} \"Software Getaway\"", "stop", "start")) {
            }
            varContainer.IsServiceStarted = null;
        }

        private void clkGetawayStart(object sender, RoutedEventArgs e) {
            if (varContainer.IsGetawayStartedTrue)
                myApp.softGetaway.Stop();
            else {
                myApp.softGetaway.SetPrivateConnectionSettings(new ConnectionSettings() {
                    SSID = txtSSID.Text,
                    MaxPeerCount = 100,
                    Password = txtPassword.Text
                });
                myApp.softGetaway.SetIP(cbIsDHCPEnabled.IsChecked.Equals(true) ? txtIP.Text : null);
                myApp.softGetaway.Start(((SharableConnection)cbSharedConnection.SelectedItem).Guid);
            }
            varContainer.IsGetawayStarted = null;
            uiUpdateEvent.Set();
        }

        void updateIcons(string uri) {
            Bitmap bmp = new Bitmap(Application.GetResourceStream(new Uri(uri, UriKind.Relative)).Stream);
            this.trayIcon.Icon = System.Drawing.Icon.FromHandle(bmp.GetHicon());
            this.imgIcon.Source = Imaging.CreateBitmapSourceFromHBitmap(bmp.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            this.Icon = BitmapFrame.Create(Application.GetResourceStream(new Uri(uri, UriKind.Relative)).Stream);
        }

        private void varContainer_getawayStarted(object sender, EventArgs e) {
            varContainer.IsConfigApplied = true;
            updateIcons("icons/rEnabled.png");
            uiUpdateEvent.Set();
        }

        void varContainer_getawayStopped(object sender, EventArgs e) {
            updateIcons("icons/rDisabled.png");
            uiUpdateEvent.Set();
        }

        void varContainer_clientNotify(object sender, EventArgs e) {
            if (varContainer.IsClientConnected.Equals(true))
                updateIcons("icons/rConnected.png");
            else if (varContainer.IsClientConnected.Equals(false))
                updateIcons("icons/rEnabled.png");
        }

        private void varContainer_serviceStarted(object sender, EventArgs e) {
            tabControl.SelectedItem = tabGetawayControl;
            uiUpdateEvent.Set();
        }

        private void varContainer_serviceStopped(object sender, EventArgs e) {
            tabControl.SelectedItem = tabServiceControl;
            updateIcons("icons/rDisabled.png");
            uiUpdateEvent.Set();
        }

        private void cbPrivateConnection_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            bool wifi = false;
            foreach (var item in e.AddedItems) {
                if (item.ToString().Contains("WiFi")) {
                    wifi = true;
                    break;
                }
            }
            varContainer.IsWiFiMode = wifi;
        }

        private void cbSharedConnection_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            ValidateText(null, null);
        }

        private void ValidateText(object sender, TextChangedEventArgs e) {
            if (!processValidate) return;
            btStartStop.IsEnabled = varContainer.IsGetawayStartedTrue || (
                  (cbSharedConnection.SelectedItem != null) && (cbPrivateConnection.SelectedItem != null) &&
            (((!varContainer.IsWiFiMode) || (varContainer.IsWiFiMode && txtSSID.IsValid && txtPassword.IsValid)) &&
                  txtIP.IsValidOrDisabled));
            if (btStartStop.IsEnabled) {
                varContainer.IsConfigChanged = true;
                varContainer.IsConfigApplied = false;
            }
        }
    }
}
