using System;
using System.Threading;
using System.Windows.Controls;
using softGetawayClient.softGetawayService;
using System.Windows.Media;
using System.Windows.Data;
using System.Globalization;

namespace softGetawayClient {
    /// <summary>
    /// Interaction logic for PeerDevice.xaml
    /// </summary>
    public partial class PeerDevice : UserControl {
        private static App myApp = (App)App.Current;

        void init(NetworkPeerService peer) {
            InitializeComponent();
            this.ContextMenu = new ContextMenu();
            var propertiesMenuItem = new MenuItem();
            propertiesMenuItem.Header = "_Properties...";
            propertiesMenuItem.Click += new System.Windows.RoutedEventHandler(PropertiesMenuItem_Click);
            this.ContextMenu.Items.Add(propertiesMenuItem);
            this.Peer = peer;
        }

        public PeerDevice(NetworkPeerService peer) {
            init(peer);
        }

        private NetworkPeerService _Peer;
        public NetworkPeerService Peer {
            get {
                return this._Peer;
            }
            set {
                this._Peer = value;
                UpdateDeviceIcon();
            }
        }

        public void UpdateDeviceIcon() {
            var icon = DeviceIconManager.LoadIcon(this._Peer.storage.MACAddress);
            var resourceName = icon.Icon.ToResourceName();
//            imgDeviceIcon.Source = (ImageSource)FindResource(resourceName);
            this.lblMACAddress.Content = Peer.storage.MACAddress;
            this.lblIPAddress.Content = Peer.storage.IPAddress;
            this.cbIsStatick.IsChecked = Peer.isSetIP;
            this.imgDeviceIcon.IsEnabled = Peer.isActive;
            lblDisplayName.Text = Peer.storage.HostName;
        }

        private void UserControl_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e) {
            ShowPropertiesDialog();
        }

        private void PropertiesMenuItem_Click(object sender, System.Windows.RoutedEventArgs e) {
            ShowPropertiesDialog();
        }

        public void ShowPropertiesDialog() {
            var window = new PeerDeviceProperties(this);
            window.Owner = App.Current.MainWindow;
            window.ShowDialog();
        }

        private void UserControl_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e) {
            ShowPropertiesDialog();
        }
    }

    public class MathConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            return (double)value + double.Parse(parameter.ToString());
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            return null;
        }
    }

	 public class ContentSelector : IValueConverter
	 {
		 public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		 {
			 string parameterString = parameter as string;
			 if (!string.IsNullOrEmpty(parameterString))
			 {
				 string[] parameters = parameterString.Split(new char[] { '|' });
				 // Now do something with the parameters
				 bool? valueBool = value as bool?;
				 if (valueBool != null)
				 {
					 switch (valueBool){
						 case true:
							 return parameters[0];
						 case false:
							 return parameters[1];
						 case null:
							 return parameters[2];
					 }
				 }
			 }
			 return null;
		 }

		 public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		 {
			 return null;
		 }
	 }

}
