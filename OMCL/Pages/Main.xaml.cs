using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
using System.Windows.Threading;
using UI;
using static UI.Dialog;

namespace OMCL.Pages
{
    /// <summary>
    /// Main.xaml 的交互逻辑
    /// </summary>
    public partial class Main : Page
    {
        public DispatcherTimer timer = new DispatcherTimer();
        public Main()
        {
            Loaded += Main_Loaded;
            InitializeComponent();
        }
        private void Main_Loaded(object sender, RoutedEventArgs e)
        {
            dia.Visibility = Visibility.Collapsed;
            dia.IsVisibleChanged += Dia_IsVisibleChanged;
            Button.Color = Brushes.Black;
        }
        private void Dia_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (dia.Visibility == Visibility.Visible) OpenMask();
            else CloseMask();
        }
        private void Pop_up_dialog(string Title, string Content, ButtonClickDele Button1Click, ButtonClickDele Button2Click, ButtonClickDele Button3Click, string Button1Text, string Button2Text, string Button3Text, Brush Color = null)
        {
            dia.ResetEvent();
            dia.Title = Title;
            dia.content = Content;
            dia.Button2Click += Button2Click;
            dia.Button1Click += Button1Click;
            dia.Button3Click += Button3Click;
            dia.Button1Text = Button1Text;
            dia.Button2Text = Button2Text;
            dia.Button3Text = Button3Text;
            dia.HaveButton3 = true;
            dia.HaveButton2 = true;
            if (Color != null) dia.ButtonColor = Color; else dia.ButtonColor = Brushes.SkyBlue;
            dia.InitLoaded();
            dia.Visibility = Visibility.Visible;
        }
        private void Pop_up_dialog(string Title, string Content, ButtonClickDele Button1Click, ButtonClickDele Button2Click, string Button1Text, string Button2Text, Brush Color = null)
        {
            dia.ResetEvent();
            dia.HaveButton2 = true;
            dia.Title = Title;
            dia.content = Content;
            dia.Button2Click += Button2Click;
            dia.Button1Click += Button1Click;
            dia.Button1Text = Button1Text;
            dia.Button2Text = Button2Text;
            if (Color != null) dia.ButtonColor = Color; else dia.ButtonColor = Brushes.SkyBlue;
            dia.InitLoaded();
            dia.Visibility = Visibility.Visible;
        }
        private void Pop_up_dialog(string Title, string Content, ButtonClickDele Button1Click, ButtonClickDele Button2Click, Brush Color = null)
        {
            dia.ResetEvent();
            dia.HaveButton2 = true;
            dia.Title = Title;
            dia.content = Content;
            dia.Button2Click += Button2Click;
            dia.Button1Click += Button1Click;
            if (Color != null) dia.ButtonColor = Color; else dia.ButtonColor = Brushes.SkyBlue;
            dia.InitLoaded();
            dia.Visibility = Visibility.Visible;
        }
        private void Pop_up_dialog(string Title, string Content, ButtonClickDele Button1Click, string Button1Text, Brush Color = null)
        {
            dia.ResetEvent();
            dia.Button1Click += Button1Click;
            dia.HaveButton2 = false;
            dia.Title = Title;
            dia.content = Content;
            if (Color != null) dia.ButtonColor = Color; else dia.ButtonColor = Brushes.SkyBlue;
            dia.InitLoaded();
            dia.Visibility = Visibility.Visible;
        }
        private void Pop_up_dialog(string Title, string Content, ButtonClickDele Button1Click, Brush Color = null)
        {
            dia.ResetEvent();
            dia.Button1Click += Button1Click;
            dia.HaveButton2 = false;
            dia.Title = Title;
            dia.content = Content;
            if (Color != null) dia.ButtonColor = Color; else dia.ButtonColor = Brushes.SkyBlue;
            dia.InitLoaded();
            dia.Visibility = Visibility.Visible;
        }
        private void Pop_up_dialog(string Title, string Content, Brush Color = null)
        {
            dia.ResetEvent();
            dia.HaveButton2 = false;
            dia.Title = Title;
            dia.content = Content;
            if (Color != null) dia.ButtonColor = Color; else dia.ButtonColor = Brushes.SkyBlue;
            dia.InitLoaded();
            dia.Visibility = Visibility.Visible;
        }
        private void OpenMask()
        {
            timer.Stop();
            timer = new DispatcherTimer();
            Mask.Opacity = 0;
            Mask.Visibility = Visibility.Visible;
            timer.Interval = TimeSpan.FromMilliseconds(10);
            timer.Tick += Pop_up_dialog_Mask_Timer_Tick;
            timer.Start();
        }
        private void CloseMask()
        {
            timer.Stop();
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(10);
            timer.Tick += Exit_dialog_Mask_Timer_Tick;
            timer.Start();
        }
        private void Exit_dialog_Mask_Timer_Tick(object sender, EventArgs e)
        {
            Mask.Opacity -= 0.01;
            if (Mask.Opacity <= 0)
            {
                ((DispatcherTimer)sender).Stop();
                Mask.Visibility = Visibility.Collapsed;
            }
        }
        private void Pop_up_dialog_Mask_Timer_Tick(object sender, EventArgs e)
        {
            Mask.Opacity += 0.01;
            if (Mask.Opacity >= 0.2) ((DispatcherTimer)sender).Stop();
        }
    }
}
