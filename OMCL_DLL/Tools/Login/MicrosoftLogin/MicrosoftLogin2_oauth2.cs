﻿using Newtonsoft.Json.Linq;
using OMCL_DLL.Tools.Login.Result;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Packaging;
using System.Net;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Windows;
using OMCL_DLL.Tools.LocalException;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using TextCopy;

namespace OMCL_DLL.Tools.Login.MicrosoftLogin
{
    public class MicrosoftLogin2_oauth2
    {
        public static bool IsC = false;
        public delegate void GetCodeDelegate(string code, string verification_uri, string message);
        public static event GetCodeDelegate OnGetCode;
        [STAThread]
        public static async Task<MicrosoftLoginResult> Login()
        {
            IsC = false;
            JObject o = JObject.Parse(await Web.Web.Post("https://login.microsoftonline.com/consumers/oauth2/v2.0/devicecode", "client_id=dc3ede96-48e5-400f-971f-4e1672d8ae7f&scope=offline_access%20XboxLive.signin", "application/x-www-form-urlencoded"));
            string verification_uri = o.SelectToken("verification_uri").ToString();
            string user_code = (string)o.SelectToken("user_code");
            string message = o.SelectToken("message").ToString();
            await new Clipboard().SetTextAsync(user_code);
            /*
            Thread thread = new(() =>
            {
                try
                {
                    Clipboard.SetText(user_code);
                }
                catch (Exception e)
                {
                    OMCLLog.WriteLog("Error on setting text into the chipboard:" + e, OMCLExceptionClass.DLL, OMCLExceptionType.Warning);
                }
            })
            {
                IsBackground = true,
            };
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            */
            await Task.Run(() =>
            {
                try
                {
                    OnGetCode(user_code, verification_uri, message);
                }
                catch { }
            });
            /*
            UI.MicrosoftLogin.url = "https://www.microsoft.com/link";
            Thread thd = new Thread(new ThreadStart(MicrosoftLogin.Start));
            thd.SetApartmentState(ApartmentState.STA);
            thd.IsBackground = true;
            thd.Start();
            */
            Web.Web.OpenUrl(verification_uri/* + (verification_uri == "https://www.microsoft.com/link" ? "?otc=" + user_code : "")*/);
            string dc = (string)o.SelectToken("device_code");
            while (true)
            {
                if (IsC) break;
                Thread.Sleep(5000);
                /*
                string result = "";
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create("https://login.microsoftonline.com/consumers/oauth2/v2.0/token");
                req.Method = "POST";
                req.ContentType = "application/x-www-form-urlencoded";
                byte[] data = Encoding.UTF8.GetBytes("grant_type=urn:ietf:params:oauth:grant-type:device_code&device_code=" + dc + "&client_id=dc3ede96-48e5-400f-971f-4e1672d8ae7f");
                req.ContentLength = data.Length;
                using (Stream reqStream = req.GetRequestStream())
                {
                    reqStream.Write(data, 0, data.Length);
                    reqStream.Close();
                }
                HttpWebResponse resp;
                try
                {
                    resp = (HttpWebResponse)req.GetResponse();
                }
                catch (WebException e)
                {
                    resp = (HttpWebResponse)e.Response;
                }
                catch (Exception e)
                {
                    OMCLLog.WriteLog("[Login_Web_POST]出现错误：" + e, OMCLExceptionClass.DLL, OMCLExceptionType.Error);
                    throw new OMCLException("错误：登录时出现错误，POST时出现错误！", e);
                }
                Stream stream = resp.GetResponseStream();
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                {
                    result = reader.ReadToEnd();
                }
                */
                string result = await Web.Web.Post("https://login.microsoftonline.com/consumers/oauth2/v2.0/token", "grant_type=urn:ietf:params:oauth:grant-type:device_code&device_code=" + dc + "&client_id=dc3ede96-48e5-400f-971f-4e1672d8ae7f", "application/x-www-form-urlencoded");
                o = JObject.Parse(result);
                string error = (string)o.SelectToken("error");
                if (string.IsNullOrEmpty(error))
                {
                    try
                    {
                        string access_token = (string)o.SelectToken("access_token");
                        string refresh_token = (string)o.SelectToken("refresh_token");
                        MicrosoftLoginResult lresult = await MicrosoftLogin.GetLoginMessage(access_token, true);
                        lresult.refresh_token = refresh_token;
                        return lresult;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        throw new OMCLException("错误：登录时出现错误，获取登录信息时出现错误！", e);
                    }
                }
                else
                {
                    switch (error)
                    {
                        case "authorization_pending":
                            continue;
                        case "authorization_declined":
                            throw new OMCLException("用户取消登录！");
                        case "bad_verification_code":
                            throw new OMCLException("验证失败，请重新尝试登录！");
                        case "expired_token":
                            throw new OMCLException("登录超时，请尝试重新登录！");
                        case "invalid_request":
                            throw new OMCLException("登录请求错误，请尝试重新登录！");
                        default:
                            throw new OMCLException("错误：登录时出现未知错误，请检查并尝试重新登录！");
                    }
                }
            }
            return null;
        }
    }
}