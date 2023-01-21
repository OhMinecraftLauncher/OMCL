using System;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace OMCL_DLL.Tools.Login.MicrosoftLogin.UI
{
    public partial class MicrosoftLogin : Form
    {
        public static string Code;
        public static string url;
        public static bool IsLogin = false;
        public static bool IsClose = false;
        public MicrosoftLogin()
        {
            FormClosing += MicrosoftLogin_FormClosing;
            InitializeComponent();
        }
        private void MicrosoftLogin_FormClosing(object sender, FormClosingEventArgs e)
        {
            IsClose = true;
        }
        private void MicrosoftLogin_Load(object sender, EventArgs e)
        {
            ServicePointManager.DefaultConnectionLimit = 2048;
            webBrowser1.ScriptErrorsSuppressed = false;
            webBrowser1.Navigated += WebBrowser1_Navigated;
            webBrowser1.Disposed += WebBrowser1_Disposed;
            webBrowser1.Navigate(url);
        }
        private void WebBrowser1_Disposed(object sender, EventArgs e)
        {
            IsLogin = true;
            Close();
        }
        private void WebBrowser1_Navigated(object sender, WebBrowserNavigatedEventArgs e)
        {
            Code = Regex.Split(webBrowser1.Url.ToString(), "code=")[1].Split('&')[0];
            webBrowser1.Dispose();
            Close();
        }
    }
}
