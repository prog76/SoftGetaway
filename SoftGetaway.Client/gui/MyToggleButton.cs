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
using System.Windows.Controls.Primitives;
using System.Reflection;

namespace softGetawayClient
{
	public partial class MyToggleButton : ToggleButton
	{
		#region Private Members

		/// <summary>
		/// Set to indicate if the Buttons are displayed.
		/// The buttons are not displayed if the width to height 
		/// ratio of the control is less than a certain value
		/// </summary>
		#endregion

		RoutedEventHandler clickHandler;

		public event RoutedEventHandler MyClick
		{
			add
			{
				clickHandler += value;
			}
			remove
			{
				clickHandler -= value;
			}
		}
		
		protected override void OnClick()
		{
			if (clickHandler!=null) clickHandler(this, null);			
		}

		public MyToggleButton()
		{
			DependencyPropertyDescriptor dpd;     
			dpd = DependencyPropertyDescriptor.FromProperty(ToggleButton.IsCheckedProperty, typeof(ToggleButton));
			if (dpd != null)
			{
				dpd.AddValueChanged(this, delegate
				{
					updateContent();
				});
			}
		}

		void updateContent()
		{
			if (IsChecked == null)
			{
                if(ContentMiddle!=null)
				    base.Content = ContentMiddle;
				base.IsEnabled = false;
				return;
			}
			base.IsEnabled = true;
			if (IsChecked.Equals(true))
				base.Content = ContentDown;
			else
				base.Content = ContentUp;
		}

		#region Dependency Properties

		public string ContentDown
		{
			get { return (string)GetValue(ContentDownProperty); }
			set { SetValue(ContentDownProperty, value); }
		}

		// Using a DependencyProperty as the backing store for MaxValue.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty ContentDownProperty =
			 DependencyProperty.Register("ContentDown", typeof(string), typeof(MyToggleButton), new UIPropertyMetadata("", ContenChangedCallback));

		public string ContentUp
		{
			get { return (string)GetValue(ContentUpProperty); }
			set { SetValue(ContentUpProperty, value); }
		}

		// Using a DependencyProperty as the backing store for MaxValue.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty ContentUpProperty =
			 DependencyProperty.Register("ContentUp", typeof(string), typeof(MyToggleButton), new UIPropertyMetadata("", ContenChangedCallback));

		public string ContentMiddle
		{
			get { return (string)GetValue(ContentMiddleProperty); }
			set { SetValue(ContentMiddleProperty, value); }
		}

		// Using a DependencyProperty as the backing store for MaxValue.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty ContentMiddleProperty =
			 DependencyProperty.Register("ContentMiddle", typeof(string), typeof(MyToggleButton), new UIPropertyMetadata(null, ContenChangedCallback));

		private static void ContenChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var control = d as MyToggleButton;
			control.updateContent();
		}
		#endregion
	}
}