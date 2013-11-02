using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace softGetawayClient
{
    public partial class WpfNotifyIcon : Component
    {
        public WpfNotifyIcon()
        {
            InitializeComponent();
        }

        public WpfNotifyIcon(IContainer container)
        {
            container.Add(this);

            InitializeComponent();
        }

        public EventHandler DoubleClick;

        private void notifyIcon1_MouseDoubleClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (this.DoubleClick != null)
            {
                this.DoubleClick(sender, e);
            }
        }

        public void Show()
        {
            this.notifyIcon1.Visible = true;
        }

        public void Hide()
        {
            this.notifyIcon1.Visible = false;
        }

        public ContextMenuStrip ContextMenuStrip
        {
            get
            {
                return this.notifyIcon1.ContextMenuStrip;
            }
            set
            {
                this.notifyIcon1.ContextMenuStrip = value;
            }
        }

        public System.Drawing.Icon Icon
        {
            get
            {
                return this.notifyIcon1.Icon;
            }
            set
            {
                this.notifyIcon1.Icon = value;
            }
        }

        public string Text
        {
            get
            {
                return this.notifyIcon1.Text;
            }
            set
            {
                this.notifyIcon1.Text = value;
            }
        }
    }
}
