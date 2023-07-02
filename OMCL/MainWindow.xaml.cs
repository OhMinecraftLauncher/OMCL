using OMCL.Pages;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace OMCL
{
    /// <summary>
    /// MyWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            Loaded += MainWindow_Loaded;
            InitializeComponent();
        }
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Opacity = 0;
            Exit.Color = Brushes.Red;
            Small.Color = Brushes.Blue;
            Main main = new Main();
            Con.Content = new Frame
            {
                Content = main,
            };
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(30);
            timer.Tick += Loaded_Timer_Tick;
            timer.Start();
        }
        private void Loaded_Timer_Tick(object sender, EventArgs e)
        {
            Opacity += 0.05;
            if (Opacity >= 1) ((DispatcherTimer)sender).Stop();
        }
        private void Title_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }
        private void Exit_Timer_Tick(object sender, EventArgs e)
        {
            Opacity -= 0.05;
            if (Opacity <= 0.3)
            {
                ((DispatcherTimer)sender).Stop();
                Close();
            }
        }
        private void Exit_OnClick(object sender)
        {
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(30);
            timer.Tick += Exit_Timer_Tick;
            timer.Start();
        }
        private void Small_OnClick(object sender)
        {
            WindowState = WindowState.Minimized;
        }
    }
}
