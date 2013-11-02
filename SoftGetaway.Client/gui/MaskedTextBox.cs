using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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

namespace softGetawayClient
{
    /// <summary>
    /// Interaction logic for NumericUpDownTextBox.xaml
    /// </summary>    
    public partial class MaskedTextBox : TextBox
    {
        #region Private Members

        /// <summary>
        /// Set to indicate if the Buttons are displayed.
        /// The buttons are not displayed if the width to height 
        /// ratio of the control is less than a certain value
        /// </summary>
        #endregion

        #region Constructor

        /// <summary>
        /// Constructor: initializes the TextBox, creates the buttons,
        /// and attaches event handlers for the buttons and TextBox
        /// </summary>
        public MaskedTextBox()
        {  
            //Hook up text event handlers
            PreviewTextInput += control_PreviewTextInput;
            PreviewKeyDown += control_PreviewKeyDown;
            LostFocus += control_LostFocus;
        }
        #endregion

        #region Event Handlers

        /// <summary>
        /// Ensures that keypress is a valid character (numeric or negative sign)
        /// - negative sign ('-') only allowed if Minimum value less than 0, the entry 
        ///     is at the beginning of the text and there is not already a negative sign
        /// - number only allowed if not beginning of text and there is a negative sign
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e">Contains arguments associated with changes to
        ///                          a System.Windows.Input.TextComposition</param>
        private void control_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
         /*   // Catch any non-numeric keys
            if ("0123456789".IndexOf(e.Text) < 0)
            {
           
                e.Handled = true;
            }
            else // A digit has been pressed
            {
                // We now know that have good value: check for attempting to put number before '-'
                if (Text.Length > 0 && CaretIndex == 0 &&
                    Text[0] == '-' && SelectionLength == 0)
                {
                    // Can't put number before '-'
                    e.Handled = true;
                }
                else
                {
                    // check for what new value will be:
                    StringBuilder sb = new StringBuilder(Text);
                    sb.Remove(CaretIndex, SelectionLength);
                    sb.Insert(CaretIndex, e.Text);
                    int newValue = int.Parse(sb.ToString());
                    // check if beyond allowed values
                }
            }   */
        }

        /// <summary>
        /// Checks if the keypress is the up or down key, and then handles keyboard input
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void control_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
                e.Handled = true;
            else
                e.Handled = false;
        }

        private void control_LostFocus(object sender, RoutedEventArgs e)
        {
            FixValue();
        }

        #endregion

        #region Private methods
        /// <summary>
        /// Only does something if the Textbox contains an out of range number
        /// </summary>
        private void FixValue()
        {
            int value;
            if (int.TryParse(Text, out value))
            {
               // FixValue(value);
            }
            //not a number--what to do
        }

        private void UpdateText(int value)
        {
            Text = value.ToString();
            CaretIndex = Text.Length;
        }
        #endregion

        #region Dependency Properties

		  public bool? IsValid
		  {
			  get;
			  set;
		  }

        public string Mask
        {
			get { return (string)GetValue(MaskProperty); }
            set { SetValue(MaskProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MaxValue.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MaskProperty =
            DependencyProperty.Register("Mask", typeof(string), typeof(MaskedTextBox), new UIPropertyMetadata(""));

        private void MaskChangedCallback(string oldValue, string newValue) {
            FixValue();
        }

        public string ErrorMessage
        {
            get { return (string)GetValue(ErrorMessageProperty); }
            set { SetValue(ErrorMessageProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MinValue.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ErrorMessageProperty =
            DependencyProperty.Register("ErrorMessage", typeof(string), typeof(MaskedTextBox), new UIPropertyMetadata(""));
       
        private void ErrorMessagePropertyCallback(int oldValue, int newValue)
        {
            FixValue();
        }
        /// <summary>
        /// TextBox Text value converted to an integer
        /// </summary>
        public bool IsEditable
        {
            get { return (bool)GetValue(IsEditableProperty); }
            set { SetValue(IsEditableProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsEditableProperty =
            DependencyProperty.Register("IsEditable", typeof(bool), typeof(MaskedTextBox),
            new UIPropertyMetadata(true, new PropertyChangedCallback(IsEditableChangedCallback)));

        private static void IsEditableChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as MaskedTextBox;
            control.IsEditableChangedCallback((bool)e.OldValue, (bool)e.NewValue);
        }

        private void IsEditableChangedCallback(bool oldValue, bool newValue)
        {
            IsReadOnly = newValue;
            if (newValue) {
                PropertyDescriptor prop = TypeDescriptor.GetProperties(this)["Background"];
                if (prop.CanResetValue(this)) {
                    prop.ResetValue(this);
                }
            }else
                Background = SystemColors.ControlBrush;
            
            Text = newValue.ToString();
        }

        #endregion

    }
}