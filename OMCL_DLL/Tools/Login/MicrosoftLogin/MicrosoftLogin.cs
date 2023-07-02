using Newtonsoft.Json.Linq;
using OMCL_DLL.Tools.Login.MicrosoftLogin.UI;
using OMCL_DLL.Tools.Login.Result;
using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Forms;

namespace OMCL_DLL.Tools.Login.MicrosoftLogin
{
    internal class MicrosoftLogin
    {
        private static TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
        [STAThread]
        public static void Start()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new UI.MicrosoftLogin());
            tcs.SetResult(true);
        }
        [STAThread]
        public static async Task<MicrosoftLoginResult> LoginAsync(bool IsAuto)
        {
            if (IsAuto) UI.MicrosoftLogin.url = "https://login.live.com/oauth20_authorize.srf?client_id=00000000402b5328&response_type=code&scope=service%3A%3Auser.auth.xboxlive.com%3A%3AMBI_SSL&redirect_uri=https%3A%2F%2Flogin.live.com%2Foauth20_desktop.srf";
            else UI.MicrosoftLogin.url = "https://login.live.com/oauth20_authorize.srf?client_id=00000000402b5328&scope=service%3a%3auser.auth.xboxlive.com%3a%3aMBI_SSL&redirect_uri=https%3a%2f%2flogin.live.com%2foauth20_desktop.srf&response_type=code&prompt=login&uaid=057b3be0fc6a4324adfa39149843f54e&msproxy=1&issuer=mso&tenant=consumers&ui_locales=zh-CN#";
            new OMCLLog("[MicrosoftLogin_Login]登录开始，自动登录：" + IsAuto.ToString() + "，将请求Url：" + UI.MicrosoftLogin.url + "。", OMCLExceptionClass.DLL, OMCLExceptionType.Message);
            Thread thd = new Thread(new ThreadStart(Start));
            thd.SetApartmentState(ApartmentState.STA);
            thd.IsBackground = true;
            thd.Start();
            await tcs.Task;
            if (UI.MicrosoftLogin.IsLogin)
            {
                try
                {
                    new OMCLLog("[MicrosoftLogin_Login]后续微软登录操作由POST和GET进行！", OMCLExceptionClass.DLL, OMCLExceptionType.Message);
                    JObject o;
                    string code = UI.MicrosoftLogin.Code;
                    if (code == "") return null;
                    o = JObject.Parse(Web.Web.Post("https://login.live.com/oauth20_token.srf", "client_id=00000000402b5328&code= " + code + " &grant_type=authorization_code&redirect_uri=https%3A%2F%2Flogin.live.com%2Foauth20_desktop.srf", "application/x-www-form-urlencoded"));
                    string refresh_token = o.SelectToken("refresh_token").ToString();
                    MicrosoftLoginResult result = GetLoginMessage(o.SelectToken("access_token").ToString(), false);
                    MicrosoftLoginResult loginResult = new MicrosoftLoginResult
                    {
                        refresh_token = refresh_token,
                        access_token = result.access_token,
                        uuid = result.uuid,
                        name = result.name,
                        IsNew = true,
                    };
                    return loginResult;
                }
                catch
                {
                    new OMCLLog("[Login_MicrosoftLogin]错误：登录时出现错误，请确认你的网络正常且你已经拥有Minecraft！", OMCLExceptionClass.DLL, OMCLExceptionType.Error);
                    throw new Exception("登录时出现错误！");
                }
            }
            else if (UI.MicrosoftLogin.IsOnWebSite)
            {
                throw new NotAnException("用户正在使用网页登录！");
            }
            else
            {
                new OMCLLog("[Login_MicrosoftLogin]错误：用户取消登录！", OMCLExceptionClass.DLL, OMCLExceptionType.Error);
                return null;
            }
        }
        public static MicrosoftLoginResult LoginByWebSite(string url)
        {
            try
            {
                new OMCLLog("[MicrosoftLogin_Login]后续微软登录操作由POST和GET进行！", OMCLExceptionClass.DLL, OMCLExceptionType.Message);
                JObject o;
                string code = Regex.Split(url, "code=")[1].Split('&')[0];
                if (code == "") return null;
                o = JObject.Parse(Web.Web.Post("https://login.live.com/oauth20_token.srf", "client_id=00000000402b5328&code= " + code + " &grant_type=authorization_code&redirect_uri=https%3A%2F%2Flogin.live.com%2Foauth20_desktop.srf", "application/x-www-form-urlencoded"));
                string refresh_token = o.SelectToken("refresh_token").ToString();
                MicrosoftLoginResult result = GetLoginMessage(o.SelectToken("access_token").ToString(), false);
                MicrosoftLoginResult loginResult = new MicrosoftLoginResult
                {
                    refresh_token = refresh_token,
                    access_token = result.access_token,
                    uuid = result.uuid,
                    name = result.name,
                    IsNew = true,
                };
                return loginResult;
            }
            catch
            {
                new OMCLLog("[Login_MicrosoftLogin]错误：登录时出现错误，请确认你的网络正常且你已经拥有Minecraft，或请确认你输入的网址正确！", OMCLExceptionClass.DLL, OMCLExceptionType.Error);
                throw new Exception("错误：登录时出现错误！请确认你的网络正常且你已经拥有Minecraft，或请确认你输入的网址正确！");
            }
        }
        public static MicrosoftLoginResult RefreshLogin(MicrosoftLoginResult login)
        {
            JObject o = JObject.Parse(Web.Web.Post("https://login.live.com/oauth20_token.srf", "client_id=00000000402b5328&refresh_token=" + login.refresh_token + "&grant_type=refresh_token&redirect_uri=https://login.live.com/oauth20_desktop.srf&scope=service::user.auth.xboxlive.com::MBI_SSL", "application/x-www-form-urlencoded"));
            string new_refresh_token = o.SelectToken("refresh_token").ToString();
            MicrosoftLoginResult result = GetLoginMessage(o.SelectToken("access_token").ToString(), login.IsNew);
            return new MicrosoftLoginResult
            {
                refresh_token = new_refresh_token,
                access_token = result.access_token,
                uuid = result.uuid,
                name = result.name,
                IsNew = result.IsNew,
            };
        }
        public static MicrosoftLoginResult GetLoginMessage(string token, bool Isnew)
        {
            JObject o = JObject.Parse(Web.Web.Post("https://user.auth.xboxlive.com/user/authenticate", "{ \"Properties\": { \"AuthMethod\": \"RPS\", \"SiteName\": \"user.auth.xboxlive.com\", \"RpsTicket\": \"" + (Isnew ? "d=" : "") + token + "\" }, \"RelyingParty\": \"http://auth.xboxlive.com\", \"TokenType\": \"JWT\" }", "application/json"));
            o = JObject.Parse(Web.Web.Post("https://xsts.auth.xboxlive.com/xsts/authorize", "{\"Properties\": {\"SandboxId\": \"RETAIL\",\"UserTokens\": [\"" + o.SelectToken("Token").ToString() + "\"]},\"RelyingParty\": \"rp://api.minecraftservices.com/\",\"TokenType\": \"JWT\"}", "application/json"));
            o = JObject.Parse(Web.Web.Post("https://api.minecraftservices.com/authentication/login_with_xbox", "{\"identityToken\": \"XBL3.0 x=" + o.SelectToken("DisplayClaims.xui[0].uhs").ToString() + ";" + o.SelectToken("Token").ToString() + "\"}", "application/json"));
            string access_token = o.SelectToken("access_token").ToString();
            o = JObject.Parse(Web.Web.Get("https://api.minecraftservices.com/minecraft/profile", "Authorization: Bearer " + access_token));
            string uuid = o.SelectToken("id").ToString();
            string name = o.SelectToken("name").ToString();
            return new MicrosoftLoginResult
            {
                access_token = access_token,
                uuid = uuid,
                name = name,
                IsNew = Isnew,
            };
        }
    }
}