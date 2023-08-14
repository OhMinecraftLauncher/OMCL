using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace UI
{
    /// <summary>
    /// BoolButton.xaml 的交互逻辑
    /// </summary>
    public partial class BoolButton : UserControl
    {
        public delegate void IsCheckedChangedDele(object sender, bool e);
        public event IsCheckedChangedDele IsCheckedChanged;
        public bool IsChecked = false;
        public BoolButton()
        {
            Loaded += BoolButton_Loaded;
            InitializeComponent();
        }
        private void BoolButton_Loaded(object sender, RoutedEventArgs e)
        {
            if (IsChecked)
            {
                False.Opacity = 0;
                True.Opacity = 1;
            }
            else
            {
                False.Opacity = 1;
                True.Opacity = 0;
            }
        }
        public void InitLoaded()
        {
            if (IsChecked)
            {
                False.Opacity = 0;
                True.Opacity = 1;
            }
            else
            {
                False.Opacity = 1;
                True.Opacity = 0;
            }
        }
        private void False_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (IsChecked)
                {
                    IsChecked = false;
                }
                else
                {
                    IsChecked = true;
                }
                try
                {
                    IsCheckedChanged(this, IsChecked);
                }
                catch { }
                InitLoaded();
            }
        }
        private void True_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (IsChecked)
                {
                    IsChecked = false;
                }
                else
                {
                    IsChecked = true;
                }
                try
                {
                    IsCheckedChanged(this, IsChecked);
                }
                catch { }
                InitLoaded();
            }
        }
    }
}
