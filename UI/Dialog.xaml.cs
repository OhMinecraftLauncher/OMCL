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

namespace UI
{
    /// <summary>
    /// Dialog.xaml 的交互逻辑
    /// </summary>
    public partial class Dialog : UserControl
    {
        public Brush ButtonColor = Brushes.SkyBlue;
        public string Button1Text = "确定";
        public string Button2Text = "取消";
        public string Button3Text = "";
        public bool HaveButton3 = false;
        public bool HaveButton2 = true;
        public string Title = "";
        public string content = "";
        public delegate void ButtonClickDele(object sender);
        public event ButtonClickDele Button1Click = null;
        public event ButtonClickDele Button2Click = null;
        public event ButtonClickDele Button3Click = null;
        public Dialog()
        {
            Loaded += Dialog_Loaded;
            InitializeComponent();
        }
        private void Dialog_Loaded(object sender, RoutedEventArgs e)
        {
            if (HaveButton3) Button3.Visibility = Visibility.Visible;
            else Button3.Visibility = Visibility.Collapsed;
            if (HaveButton2) Button2.Visibility = Visibility.Visible;
            else Button2.Visibility = Visibility.Collapsed;
            Button1.Text = Button1Text;
            Button2.Text = Button2Text;
            Button3.Text = Button3Text;
            Button1.Color = ButtonColor;
            Button2.Color = ButtonColor;
            Button3.Color = ButtonColor;
            Button1.Thickness = new Thickness(2);
            Button2.Thickness = new Thickness(2);
            Button3.Thickness = new Thickness(2);
            title.Text = Title;
            con.Text = content;
            Button1.InitLoaded();
            Button2.InitLoaded();
            Button3.InitLoaded();
        }
        public void InitLoaded()
        {
            if (HaveButton3) Button3.Visibility = Visibility.Visible;
            else Button3.Visibility = Visibility.Collapsed;
            if (HaveButton2) Button2.Visibility = Visibility.Visible;
            else Button2.Visibility = Visibility.Collapsed;
            Button1.Text = Button1Text;
            Button2.Text = Button2Text;
            Button3.Text = Button3Text;
            Button1.Color = ButtonColor;
            Button2.Color = ButtonColor;
            Button3.Color = ButtonColor;
            Button1.Thickness = new Thickness(2);
            Button2.Thickness = new Thickness(2);
            Button3.Thickness = new Thickness(2);
            title.Text = Title;
            con.Text = content;
            Button1.InitLoaded();
            Button2.InitLoaded();
            Button3.InitLoaded();
        }
        public void ResetEvent()
        {
            Button1Click = null;
            Button2Click = null;
            Button3Click = null;
            Button1Text = "确定";
            Button2Text = "取消";
            Button3Text = "";
        }
        private void Button1_OnClick(object sender)
        {
            Visibility = Visibility.Collapsed;
            try
            {
                if (Button1Click != null) Button1Click(sender);
            }
            catch { }
        }
        private void Button2_OnClick(object sender)
        {
            Visibility = Visibility.Collapsed;
            try
            {
                if (Button1Click != null) Button2Click(sender);
            }
            catch { }
        }
        private void Button3_OnClick(object sender)
        {
            Visibility = Visibility.Collapsed;
            try
            {
                if (Button1Click != null) Button3Click(sender);
            }
            catch { }
        }
    }
}
