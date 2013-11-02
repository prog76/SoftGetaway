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
using System.Text.RegularExpressions;

namespace softGetawayClient.gui {
    /// <summary>
    /// Interaction logic for MyTextBox.xaml
    /// </summary>
    public partial class MyTextBox : UserControl, INotifyPropertyChanged {
        Regex rex;
        bool fIsValid;

        public MyTextBox() {
            InitializeComponent();
            rex = new Regex(".*");
            maskedBox.TextChanged += new TextChangedEventHandler(maskedBox_TextChanged);
            DependencyPropertyDescriptor dpd = DependencyPropertyDescriptor.FromProperty(TextBox.IsEnabledProperty, typeof(TextBox));
            if (dpd != null)
                dpd.AddValueChanged(maskedBox, delegate {
                    setBackground();
                    OnPropertyChanged(new PropertyChangedEventArgs("IsValidOrDisabled"));
                });
        }

        void maskedBox_TextChanged(object sender, TextChangedEventArgs e) {
            bool _IsValid = rex.IsMatch(maskedBox.Text);
            if (_IsValid != fIsValid) {
                fIsValid = _IsValid;
                labl.FontSize = maskedBox.FontSize + (fIsValid ? 5 : 2);
                labl.Measure(new Size(Double.MaxValue, Double.MaxValue));
                Size lablSize = labl.DesiredSize;
                Rect rect = new Rect(0, (lablSize.Height - maskedBox.ActualHeight) / 2, lablSize.Width, maskedBox.ActualHeight);
                labl.Clip = new RectangleGeometry(rect);
                OnPropertyChanged(new PropertyChangedEventArgs("IsValid"));
                OnPropertyChanged(new PropertyChangedEventArgs("IsValidOrDisabled"));
                if (!fIsValid)
                    toolTip.Content = ErrorMessage;
                toolTip.IsOpen = !fIsValid;
            }
        }

        public event TextChangedEventHandler TextChanged {
            add {
                maskedBox.TextChanged += value;
            }
            remove {
                maskedBox.TextChanged -= value;
            }
        }

        public bool IsEditable {
            get { return (bool)GetValue(IsEditableProperty); }
            set { SetValue(IsEditableProperty, value); }
        }
        public String Mask {
            get { return rex.ToString(); }
            set { rex = new Regex(value); }
        }
        public String ErrorMessage {
            get { return (string)GetValue(ErrorMessageProperty); }
            set { SetValue(ErrorMessageProperty, value); }
        }
        public String Text {
            get { return maskedBox.Text; }
            set { maskedBox.Text = value; }
        }

		  public String ValidTextOrNull
		  {
			  get { return IsValid ? Text : null; }
		  }

        public bool IsValid {
            get { return fIsValid; }
        }

        public bool IsValidOrDisabled {
            get { return !IsEditable || !IsEnabled || fIsValid; }
        }

        void setBackground() {
            if (IsEditable && maskedBox.IsEnabled)
                Background = SystemColors.ControlLightLightBrush;
            else
                if (IsEnabled)
                    Background = SystemColors.ControlLightBrush;
                else
                    Background = SystemColors.ControlBrush;
        }

        // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsEditableProperty =
             DependencyProperty.Register("IsEditable", typeof(bool), typeof(MyTextBox),
             new UIPropertyMetadata(true, new PropertyChangedCallback(IsEditableChangedCallback)));

        private static void IsEditableChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var control = d as MyTextBox;
            control.IsEditableChangedCallback((bool)e.OldValue, (bool)e.NewValue);
        }

        private void IsEditableChangedCallback(bool oldValue, bool newValue) {
            maskedBox.IsReadOnly = !newValue;
            setBackground();
        }

        public static readonly DependencyProperty ErrorMessageProperty =
             DependencyProperty.Register("ErrorMessage", typeof(string), typeof(MyTextBox),
             new UIPropertyMetadata("", null));

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(PropertyChangedEventArgs e) {
            if (PropertyChanged != null) {
                PropertyChanged(this, e);
            }
        }

       
    }
}
