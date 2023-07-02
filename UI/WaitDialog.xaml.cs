using GifTest;
using System;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using UI.UITools;

namespace UI
{
    /// <summary>
    /// Dialog.xaml 的交互逻辑
    /// </summary>
    public partial class WaitDialog : UserControl
    {
        private AnimateImage image = null;
        public Brush ButtonColor = Brushes.SkyBlue;
        public string ButtonText = "取消";
        public bool HaveButton = false;
        public string Title = "";
        public string content = "";
        public delegate void ButtonClickDele(object sender);
        public event ButtonClickDele ButtonClick = null;
        public WaitDialog()
        {
            Loaded += Dialog_Loaded;
            InitializeComponent();
        }
        private void Dialog_Loaded(object sender, RoutedEventArgs e)
        {
            MemoryStream stream = new MemoryStream();
            Properties.Resources.Waiting.Save(stream, ImageFormat.Gif);
            image = new AnimateImage(System.Drawing.Image.FromStream(stream));
            image.OnFrameChanged += Image_OnFrameChanged;
            image.Play();
            if (HaveButton) Button.Visibility = Visibility.Visible;
            else Button.Visibility = Visibility.Collapsed;
            Button.Text = ButtonText;
            Button.Color = ButtonColor;
            Button.Thickness = new Thickness(2);
            title.Text = Title;
            con.Text = content;
            Button.InitLoaded();
        }
        private void Image_OnFrameChanged(object sender, EventArgs e)
        {
            if (image.CurrentFrame == image.FrameCount) image.Reset();
            Dispatcher.Invoke(new Action(delegate
            {
                GifShower.Source = ImageTools.BitmapToBitmapImage((System.Drawing.Bitmap)sender);
            }));
        }
        public void InitLoaded()
        {
            if (HaveButton) Button.Visibility = Visibility.Visible;
            else Button.Visibility = Visibility.Collapsed;
            Button.Text = ButtonText;
            Button.Color = ButtonColor;
            Button.Thickness = new Thickness(2);
            title.Text = Title;
            con.Text = content;
            Button.InitLoaded();
        }
        public void ResetEvent()
        {
            ButtonClick = null;
            ButtonText = "取消";
        }
        private void Button_OnClick(object sender)
        {
            Visibility = Visibility.Collapsed;
            try
            {
                ButtonClick(sender);
            }
            catch { }
        }
    }
}
