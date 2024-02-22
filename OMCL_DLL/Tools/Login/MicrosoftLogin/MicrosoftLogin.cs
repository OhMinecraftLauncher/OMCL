using Newtonsoft.Json.Linq;
using OMCL_DLL.Tools.Login.Result;
using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using OMCL_DLL.Tools.LocalException;
using System.Collections.Generic;

namespace OMCL_DLL.Tools.Login.MicrosoftLogin
{
    internal class MicrosoftLogin
    {
        /*
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
        */
        public static void OpenLoginUrl(bool IsAuto)
        {
            string url;
            if (IsAuto) url = "https://login.live.com/oauth20_authorize.srf?client_id=00000000402b5328&response_type=code&scope=service%3A%3Auser.auth.xboxlive.com%3A%3AMBI_SSL&redirect_uri=https%3A%2F%2Flogin.live.com%2Foauth20_desktop.srf";
            else url = "https://login.live.com/oauth20_authorize.srf?client_id=00000000402b5328&scope=service%3a%3auser.auth.xboxlive.com%3a%3aMBI_SSL&redirect_uri=https%3a%2f%2flogin.live.com%2foauth20_desktop.srf&response_type=code&prompt=login&uaid=057b3be0fc6a4324adfa39149843f54e&msproxy=1&issuer=mso&tenant=consumers&ui_locales=zh-CN#";
            OMCLLog.WriteLog("[MicrosoftLogin_Login]登录开始，自动登录：" + IsAuto.ToString() + "，将请求Url：" + url + "。", OMCLExceptionClass.DLL, OMCLExceptionType.Message);
            Web.Web.OpenUrl(url);
            /*
            Thread thd = new Thread(new ThreadStart(Start));
            thd.SetApartmentState(ApartmentState.STA);
            thd.IsBackground = true;
            thd.Start();
            await tcs.Task;
            if (UI.MicrosoftLogin.IsLogin)
            {
                try
                {
                    OMCLLog.WriteLog("[MicrosoftLogin_Login]后续微软登录操作由POST和GET进行！", OMCLExceptionClass.DLL, OMCLExceptionType.Message);
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
                    OMCLLog.WriteLog("[Login_MicrosoftLogin]错误：登录时出现错误，请确认你的网络正常且你已经拥有Minecraft！", OMCLExceptionClass.DLL, OMCLExceptionType.Error);
                    throw new OMCLException("登录时出现错误！", e);
                }
            }
            else if (UI.MicrosoftLogin.IsOnWebSite)
            {
                throw new NotAnException("用户正在使用网页登录！");
            }
            else
            {
                OMCLLog.WriteLog("[Login_MicrosoftLogin]错误：用户取消登录！", OMCLExceptionClass.DLL, OMCLExceptionType.Error);
                return null;
            }
            */
        }
        public static async Task<MicrosoftLoginResult> Login(string url)
        {
            try
            {
                OMCLLog.WriteLog("[MicrosoftLogin_Login]后续微软登录操作由POST和GET进行！", OMCLExceptionClass.DLL, OMCLExceptionType.Message);
                JObject o;
                string code = Regex.Split(url, "code=")[1].Split('&')[0];
                if (code == "") return null;
                o = JObject.Parse(await Web.Web.Post("https://login.live.com/oauth20_token.srf", "client_id=00000000402b5328&code= " + code + " &grant_type=authorization_code&redirect_uri=https%3A%2F%2Flogin.live.com%2Foauth20_desktop.srf", "application/x-www-form-urlencoded"));
                string refresh_token = o.SelectToken("refresh_token").ToString();
                MicrosoftLoginResult result = await GetLoginMessage(o.SelectToken("access_token").ToString(), false);
                MicrosoftLoginResult loginResult = new()
                {
                    refresh_token = refresh_token,
                    access_token = result.access_token,
                    uuid = result.uuid,
                    name = result.name,
                    IsNew = false,
                };
                return loginResult;
            }
            catch (Exception e)
            {
                OMCLLog.WriteLog("[Login_MicrosoftLogin]错误：登录时出现错误，请确认你的网络正常且你已经拥有Minecraft，或请确认你输入的网址正确！", OMCLExceptionClass.DLL, OMCLExceptionType.Error);
                throw new OMCLException("错误：登录时出现错误！请确认你的网络正常且你已经拥有Minecraft，或请确认你输入的网址正确！", e);
            }
        }
        public static async Task<MicrosoftLoginResult> RefreshLogin(MicrosoftLoginResult login)
        {
            JObject o = JObject.Parse(await Web.Web.Post("https://login.live.com/oauth20_token.srf", "client_id=" + (login.IsNew ? "dc3ede96-48e5-400f-971f-4e1672d8ae7f" : "00000000402b5328") + "&refresh_token=" + login.refresh_token + "&grant_type=refresh_token" + (login.IsNew ? "" :"&redirect_uri=https://login.live.com/oauth20_desktop.srf&scope=service::user.auth.xboxlive.com::MBI_SSL"), "application/x-www-form-urlencoded"));
            string new_refresh_token = o.SelectToken("refresh_token").ToString();
            MicrosoftLoginResult result = await GetLoginMessage(o.SelectToken("access_token").ToString(), login.IsNew);
            return new MicrosoftLoginResult
            {
                refresh_token = new_refresh_token,
                access_token = result.access_token,
                uuid = result.uuid,
                name = result.name,
                IsNew = result.IsNew,
            };
        }
        public static async Task<MicrosoftLoginResult> GetLoginMessage(string token, bool Isnew)
        {
            JObject o = JObject.Parse(await Web.Web.Post("https://user.auth.xboxlive.com/user/authenticate", "{ \"Properties\": { \"AuthMethod\": \"RPS\", \"SiteName\": \"user.auth.xboxlive.com\", \"RpsTicket\": \"" + (Isnew ? "d=" : "") + token + "\" }, \"RelyingParty\": \"http://auth.xboxlive.com\", \"TokenType\": \"JWT\" }", "application/json"));
            o = JObject.Parse(await Web.Web.Post("https://xsts.auth.xboxlive.com/xsts/authorize", "{\"Properties\": {\"SandboxId\": \"RETAIL\",\"UserTokens\": [\"" + o.SelectToken("Token").ToString() + "\"]},\"RelyingParty\": \"rp://api.minecraftservices.com/\",\"TokenType\": \"JWT\"}", "application/json"));
            o = JObject.Parse(await Web.Web.Post("https://api.minecraftservices.com/authentication/login_with_xbox", "{\"identityToken\": \"XBL3.0 x=" + o.SelectToken("DisplayClaims.xui[0].uhs").ToString() + ";" + o.SelectToken("Token").ToString() + "\"}", "application/json"));
            string access_token = o.SelectToken("access_token").ToString();
            //o = JObject.Parse(await Web.Web.Get("https://api.minecraftservices.com/minecraft/profile", "Authorization: Bearer " + access_token));
            o = JObject.Parse(await Web.Web.Get("https://api.minecraftservices.com/minecraft/profile",
            new()
            {
                new()
                {
                    name = "Authorization",
                    value = "Bearer " + access_token,
                },
            }));
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