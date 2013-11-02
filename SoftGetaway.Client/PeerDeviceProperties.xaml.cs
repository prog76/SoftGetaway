using System.Windows;
using softGetawayClient.AeroGlass;
using System;

namespace softGetawayClient {
    /// <summary>
    /// Interaction logic for PeerDeviceProperties.xaml
    /// </summary>
    public partial class PeerDeviceProperties : Window {
        bool processValidate;
        public PeerDeviceProperties(PeerDevice peerDevice) {
            processValidate = false;
            this.PeerDevice = peerDevice;
            InitializeComponent();
            this.UpdateDisplay();
            processValidate = true;
        }

        public PeerDevice PeerDevice { get; private set; }

        private void UpdateDisplay() {
            if (this.PeerDevice != null) {
                //               this.Icon = this.imgDeviceIcon.Source = this.PeerDevice.imgDeviceIcon.Source;
                this.lblDisplayName.Content = this.lblDisplayName.ToolTip = this.Title = this.PeerDevice.lblDisplayName.Text.ToString();
                this.txtMACAddress.Text = this.PeerDevice.Peer.storage.MACAddress;
                this.txtIPAddress.Text = this.PeerDevice.Peer.storage.IPAddress;
                this.txtHostName.Text = this.PeerDevice.Peer.storage.HostName;
                this.cbIsStatic.IsChecked = this.PeerDevice.Peer.isSetIP;
                this.cbChangeName.IsChecked = this.PeerDevice.Peer.isSetHostName;
            }
        }

        private void btnChangeIcon_Click(object sender, RoutedEventArgs e) {
            var dialog = new DeviceIconPicker(lblDisplayName.Content.ToString());
            dialog.SelectedIcon = DeviceIconManager.LoadIcon(this.PeerDevice.Peer.storage.MACAddress).Icon;
            dialog.Owner = this;
            if (dialog.ShowDialog() == true) {
                DeviceIconManager.SaveIcon(this.PeerDevice.Peer.storage.MACAddress, dialog.SelectedIcon);
                this.PeerDevice.UpdateDeviceIcon();
                UpdateDisplay();
            }
        }

        private void btnOk_Click(object sender, RoutedEventArgs e) {
            this.PeerDevice.Peer.isSetIP = this.cbIsStatic.IsChecked == true;
            this.PeerDevice.Peer.storage.IPAddress = this.txtIPAddress.ValidTextOrNull;
            this.PeerDevice.Peer.storage.HostName = this.txtHostName.ValidTextOrNull;
            this.PeerDevice.Peer.isSetHostName = this.cbChangeName.IsChecked == true;
            ((App)App.Current).softGetaway.SetPeer(this.PeerDevice.Peer);
            this.PeerDevice.UpdateDeviceIcon();
            varContainer.IsConfigChanged = true;
            Close();
        }

        private void validate() {
            if (!processValidate) return;
            btnOk.IsEnabled = txtIPAddress.IsValidOrDisabled && txtHostName.IsValidOrDisabled && (txtIPAddress.IsEditable || txtHostName.IsEditable);
        }

        private void cbValidate(object sender, RoutedEventArgs e) {
            validate();
        }

        private void txtValidate(object sender, System.Windows.Controls.TextChangedEventArgs e) {
            validate();
        }
    }
}
