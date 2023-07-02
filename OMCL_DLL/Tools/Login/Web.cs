using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Text;

namespace OMCL_DLL.Tools.Login.Web
{
    public static class Web
    {
        internal static string Post(string url, string content, string MIMEType)
        {
            try
            {
                new OMCLLog("[Web_POST]:Post：Url：" + url + "，MIME类型：" + MIMEType + "，信息隐藏。", OMCLExceptionClass.DLL, OMCLExceptionType.Message);
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
                new OMCLLog("[Web_POST]:Post：成功，返回结果隐藏！", OMCLExceptionClass.DLL, OMCLExceptionType.Message);
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
                new OMCLLog("[Login_Web_POST]出现错误：" + e, OMCLExceptionClass.DLL, OMCLExceptionType.Error);
                throw e;
            }
        }
        internal static string Get(string url, string Header = null)
        {
            try
            {
                new OMCLLog("[Web_GET]:Get：Url：" + url + "，Header隐藏。", OMCLExceptionClass.DLL, OMCLExceptionType.Message);
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
                new OMCLLog("[Web_GET]:Get：成功，返回结果隐藏！", OMCLExceptionClass.DLL, OMCLExceptionType.Message);
                return result;
            }
            catch (Exception e)
            {
                new OMCLLog("[Login_Web_GET]出现错误：" + e, OMCLExceptionClass.DLL, OMCLExceptionType.Error);
                throw new Exception("Get时出现错误！");
            }
        }
    }
}