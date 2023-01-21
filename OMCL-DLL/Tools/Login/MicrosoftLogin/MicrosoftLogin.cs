using Newtonsoft.Json.Linq;
using OMCL_DLL.Tools.Login.Result;
using System;
using System.Threading;
using System.Windows.Forms;

namespace OMCL_DLL.Tools.Login.MicrosoftLogin
{
    public class MicrosoftLogin
    {
        [STAThread]
        public static void Start()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new UI.MicrosoftLogin());
        }
        [STAThread]
        public static LoginResult Login(bool IsAuto)
        {
            if (IsAuto) UI.MicrosoftLogin.url = "https://login.live.com/oauth20_authorize.srf?client_id=00000000402b5328&response_type=code&scope=service%3A%3Auser.auth.xboxlive.com%3A%3AMBI_SSL&redirect_uri=https%3A%2F%2Flogin.live.com%2Foauth20_desktop.srf";
            else UI.MicrosoftLogin.url = "https://login.live.com/oauth20_authorize.srf?client_id=00000000402b5328&scope=service%3a%3auser.auth.xboxlive.com%3a%3aMBI_SSL&redirect_uri=https%3a%2f%2flogin.live.com%2foauth20_desktop.srf&response_type=code&prompt=login&uaid=057b3be0fc6a4324adfa39149843f54e&msproxy=1&issuer=mso&tenant=consumers&ui_locales=zh-CN#";
            Thread thd = new Thread(new ThreadStart(Start));
            thd.SetApartmentState(ApartmentState.STA);
            thd.IsBackground = true;
            thd.Start();
            while (true)
            {
                if (UI.MicrosoftLogin.IsClose)
                {
                    break;
                }
            }
            if (UI.MicrosoftLogin.IsLogin)
            {
                try
                {
                    JObject o;
                    o = JObject.Parse(Web.Web.Post("https://login.live.com/oauth20_token.srf", "client_id=00000000402b5328&code= " + UI.MicrosoftLogin.Code + " &grant_type=authorization_code&redirect_uri=https%3A%2F%2Flogin.live.com%2Foauth20_desktop.srf", "application/x-www-form-urlencoded"));
                    string refresh_token = o.SelectToken("refresh_token").ToString();
                    LoginResult result = GetLoginMessage(o.SelectToken("access_token").ToString());
                    LoginResult loginResult = new LoginResult
                    {
                        refresh_token = refresh_token,
                        access_token = result.access_token,
                        uuid = result.uuid,
                        name = result.name,
                    };
                    return loginResult;
                }
                catch
                {
                    new OMCLLog("[Login_MicrosoftLogin]错误：登录时出现错误，请确认你的网络正常且你已经拥有Minecraft！", OMCLExceptionClass.DLL, OMCLExceptionType.Error);
                    throw;
                }
            }
            else
            {
                new OMCLLog("[Login_MicrosoftLogin]错误：用户取消登录！", OMCLExceptionClass.DLL, OMCLExceptionType.Error);
                return null;
            }
        }
        public static LoginResult RefreshLogin(string refresh_token)
        {
            JObject o = JObject.Parse(Web.Web.Post("https://login.live.com/oauth20_token.srf", "client_id=00000000402b5328&refresh_token=" + refresh_token + "&grant_type=refresh_token&redirect_uri=https://login.live.com/oauth20_desktop.srf&scope=service::user.auth.xboxlive.com::MBI_SSL", "application/x-www-form-urlencoded"));
            string new_refresh_token = o.SelectToken("refresh_token").ToString();
            LoginResult result = GetLoginMessage(o.SelectToken("access_token").ToString());
            return new LoginResult
            {
                refresh_token = new_refresh_token,
                access_token = result.access_token,
                uuid = result.uuid,
                name = result.name,
            };
        }
        private static LoginResult GetLoginMessage(string token)
        {
            JObject o = JObject.Parse(Web.Web.Post("https://user.auth.xboxlive.com/user/authenticate", "{ \"Properties\": { \"AuthMethod\": \"RPS\", \"SiteName\": \"user.auth.xboxlive.com\", \"RpsTicket\": \"" + token + "\" }, \"RelyingParty\": \"http://auth.xboxlive.com\", \"TokenType\": \"JWT\" }", "application/json"));
            o = JObject.Parse(Web.Web.Post("https://xsts.auth.xboxlive.com/xsts/authorize", "{\"Properties\": {\"SandboxId\": \"RETAIL\",\"UserTokens\": [\"" + o.SelectToken("Token").ToString() + "\"]},\"RelyingParty\": \"rp://api.minecraftservices.com/\",\"TokenType\": \"JWT\"}", "application/json"));
            o = JObject.Parse(Web.Web.Post("https://api.minecraftservices.com/authentication/login_with_xbox", "{\"identityToken\": \"XBL3.0 x=" + o.SelectToken("DisplayClaims.xui[0].uhs").ToString() + ";" + o.SelectToken("Token").ToString() + "\"}", "application/json"));
            string access_token = o.SelectToken("access_token").ToString();
            o = JObject.Parse(Web.Web.Get("https://api.minecraftservices.com/minecraft/profile", access_token));
            string uuid = o.SelectToken("id").ToString();
            string name = o.SelectToken("name").ToString();
            return new LoginResult
            {
                access_token = access_token,
                uuid = uuid,
                name = name,
            };
        }
    }
}