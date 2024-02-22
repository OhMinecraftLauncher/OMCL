using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using OMCL_DLL.Tools.LocalException;

namespace OMCL_DLL.Tools.Login.Web
{
    public static class Web
    {
        /*
        internal static string Post(string url, string content, string MIMEType)
        {
            try
            {
                OMCLLog.WriteLog("[Web_POST]:Post：Url：" + url + "，MIME类型：" + MIMEType + "，信息隐藏。", OMCLExceptionClass.DLL, OMCLExceptionType.Message);
                string result = "";
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
                req.Method = "POST";
                req.ContentType = MIMEType;
                byte[] data = Encoding.UTF8.GetBytes(content);
                req.ContentLength = data.Length;
                using (Stream reqStream = req.GetRequestStream())
                {
                    reqStream.Write(data, 0, data.Length);
                    reqStream.Close();
                }
                HttpWebResponse resp = (HttpWebResponse)req.GetResponse();
                Stream stream = resp.GetResponseStream();
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                {
                    result = reader.ReadToEnd();
                }
                OMCLLog.WriteLog("[Web_POST]:Post：成功，返回结果隐藏！", OMCLExceptionClass.DLL, OMCLExceptionType.Message);
                return result;
            }
            catch (WebException e)
            {
                WebResponse response = e.Response;
                Stream stream = response.GetResponseStream();
                StreamReader reader = new StreamReader(stream);
                Console.WriteLine(reader.ReadToEnd());
                reader.Close();
                reader.Dispose();
                throw e;
            }
            catch (Exception e)
            {
                OMCLLog.WriteLog("[Login_Web_POST]出现错误：" + e, OMCLExceptionClass.DLL, OMCLExceptionType.Error);
                throw new OMCLException("Post时出现错误！", e);
            }
        }
        internal static string Get(string url, string Header = null)
        {
            try
            {
                OMCLLog.WriteLog("[Web_GET]:Get：Url：" + url + "，Header隐藏。", OMCLExceptionClass.DLL, OMCLExceptionType.Message);
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
                req.Method = "GET";
                if (Header != null)
                {
                    req.Headers.Add(Header);
                }
                HttpWebResponse resp = (HttpWebResponse)req.GetResponse();
                Stream stream = resp.GetResponseStream();
                string result = "";
                using (StreamReader reader = new StreamReader(stream))
                {
                    result = reader.ReadToEnd();
                }
                OMCLLog.WriteLog("[Web_GET]:Get：成功，返回结果隐藏！", OMCLExceptionClass.DLL, OMCLExceptionType.Message);
                return result;
            }
            catch (Exception e)
            {
                OMCLLog.WriteLog("[Login_Web_GET]出现错误：" + e, OMCLExceptionClass.DLL, OMCLExceptionType.Error);
                throw new OMCLException("Get时出现错误！", e);
            }
        }
        */
        public static TimeSpan Timeout = new(0, 0, 15);
        public static bool CheckStatusCode = false;
        public struct Header
        {
            public string name;
            public string value;
        }
        public static async Task<string> Post(string url, string content, string MIMEType)
        {
            HttpClient client = new()
            {
                BaseAddress = new Uri(url),
                Timeout = Timeout,
            };
            HttpResponseMessage response = await client.PostAsync("", new StringContent(content, Encoding.UTF8, MIMEType));
            if (CheckStatusCode)
            {
                return await response.EnsureSuccessStatusCode().Content.ReadAsStringAsync();
            }
            else
            {
                return await response.Content.ReadAsStringAsync();
            }
        }
        public static async Task<string> Get(string url, List<Header> headers = null)
        {
            HttpClient http = new()
            {
                BaseAddress = new Uri(url),
                Timeout = Timeout,
            };
            if (headers != null)
            {
                foreach(Header header in headers)
                {
                    http.DefaultRequestHeaders.Add(header.name, header.value);
                }
            }
            HttpResponseMessage response = await http.GetAsync("");
            if (CheckStatusCode)
            {
                return await response.EnsureSuccessStatusCode().Content.ReadAsStringAsync();
            }
            else
            {
                return await response.Content.ReadAsStringAsync();
            }
        }
        public static void OpenUrl(string url)
        {
            Task.Run(() =>
            {
                new Process()
                {
                    StartInfo = new()
                    {
                        FileName = url,
                        UseShellExecute = true,
                    },
                }.Start();
            });
        }
    }
}