using OMCL_DLL.Tools.Login.Result;
using System;
using System.Diagnostics;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace OMCL_DLL.Tools.Login.MicrosoftLogin.UI
{
    public partial class MicrosoftLogin : Form
    {
        public static string Code = "";
        public static string url;
        public static bool IsLogin = false;
        public static bool IsClose = false;
        public static bool IsOnWebSite = false;
        public MicrosoftLogin()
        {
            FormClosing += MicrosoftLogin_FormClosing;
            InitializeComponent();
        }
        private void MicrosoftLogin_FormClosing(object sender, FormClosingEventArgs e)
        {
            IsClose = true;
            webBrowser1.Dispose();
        }
        private void MicrosoftLogin_Load(object sender, EventArgs e)
        {
            IsLogin = false;
            IsClose = false;
            IsOnWebSite = false;
            ServicePointManager.DefaultConnectionLimit = 2048;
            webBrowser1.ScriptErrorsSuppressed = false;
            webBrowser1.Navigated += WebBrowser1_Navigated;
            webBrowser1.Navigate(url);
            OMCLLog.WriteLog("[MicrosoftLogin_Login]创建窗口： OMCL - 登录Microsoft 成功！", OMCLExceptionClass.DLL, OMCLExceptionType.Message);
        }
        private void WebBrowser1_Navigated(object sender, WebBrowserNavigatedEventArgs e)
        {
            if (webBrowser1.Url.ToString().Contains("https://login.live.com/oauth20_remoteconnect.srf") && webBrowser1.Url.ToString().Contains("res=success")) Close();
            Code = Regex.Split(webBrowser1.Url.ToString(), "code=")[1].Split('&')[0];
            OMCLLog.WriteLog("[MicrosoftLogin_Login]已跳转，获取Code成功，Code隐藏。", OMCLExceptionClass.DLL, OMCLExceptionType.Message);
            IsLogin = true;
            Close();
        }
        private void button1_Click(object sender, EventArgs e)
        {
            IsOnWebSite = true;
            try
            {
                OMCLLog.WriteLog("[MicrosoftLogin_Login]正在打开网页[" + url + "]，请稍后.....", OMCLExceptionClass.DLL, OMCLExceptionType.Message);
                Process.Start(url);
                Close();
            }
            catch
            {
                OMCLLog.WriteLog("[MicrosoftLogin_Login]错误：打开网页时出现错误！该电脑可能没有浏览器！", OMCLExceptionClass.DLL, OMCLExceptionType.Error);
                throw new Exception("错误：打开网页时出现错误！登录失败！");
            }
        }
    }
}
