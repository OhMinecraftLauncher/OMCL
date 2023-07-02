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
    public partial class NiceButton : UserControl
    {
        public delegate void OnClickDele(object sender);
        public event OnClickDele OnClick;
        public Brush Color = Brushes.Transparent;
        public string Text = "";
        public int Size_Font = 12;
        public bool IsBold = true;
        public Thickness Thickness = new Thickness(0.5);
        public NiceButton()
        {
            Loaded += NiceButton_Loaded;
            InitializeComponent();
        }
        private void NiceButton_Loaded(object sender, RoutedEventArgs e)
        {
            ((Border)Button.Template.FindName("Border",Button)).BorderBrush = Color;
            ((Border)Button.Template.FindName("Border", Button)).BorderThickness = Thickness;
            if (IsBold) TextBlock.FontWeight = FontWeights.Bold;
            else TextBlock.FontWeight = FontWeights.Normal;
            TextBlock.FontSize = Size_Font;
            TextBlock.Text = Text;
        }
        public void InitLoaded()
        {
            ((Border)Button.Template.FindName("Border", Button)).BorderBrush = Color;
            ((Border)Button.Template.FindName("Border", Button)).BorderThickness = Thickness;
            if (IsBold) TextBlock.FontWeight = FontWeights.Bold;
            else TextBlock.FontWeight = FontWeights.Normal;
            TextBlock.FontSize = Size_Font;
            TextBlock.Text = Text;
        }
        private void Border_MouseEnter(object sender, MouseEventArgs e)
        {
            Opacity = 0.6;
        }
        private void Border_MouseLeave(object sender, MouseEventArgs e)
        {
            Opacity = 1;
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
