using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace UI
{
    /// <summary>
    /// CurcleButton.xaml 的交互逻辑
    /// </summary>
    public partial class CurcleButton : UserControl
    {
        public delegate void OnClickDele(object sender);
        public event OnClickDele OnClick;
        public Brush Color = Brushes.Transparent;
        public CurcleButton()
        {
            Loaded += CurcleButton_Loaded;
            InitializeComponent();
        }
        private void CurcleButton_Loaded(object sender, RoutedEventArgs e)
        {
            ((Border)Button.Template.FindName("Border",Button)).Background = Color;
        }
        public void InitLoaded()
        {
            ((Border)Button.Template.FindName("Border", Button)).Background = Color;
        }
        private void Border_MouseEnter(object sender, MouseEventArgs e)
        {
            ((Border)sender).Opacity = 0.6;
        }
        private void Border_MouseLeave(object sender, MouseEventArgs e)
        {
            ((Border)sender).Opacity = 1;
        }
        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                try
                {
                    OnClick(sender);
                }
                catch { }
            }
        }
    }
}
