using System;
using System.IO;
using System.Net;
using System.Text;

namespace OMCL_DLL.Tools.Login.Web
{
    public class Web
    {
        internal static string Post(string url, string content, string MIMEType)
        {
            try
            {
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
                return result;
            }
            catch (Exception e)
            {
                new OMCLLog("[Login_Web_POST]出现错误：" + e, OMCLExceptionClass.DLL, OMCLExceptionType.Error);
                throw;
            }
        }
        internal static string Get(string url, string access_token)
        {
            try
            {
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
                req.Method = "GET";
                req.Headers.Add("Authorization: Bearer " + access_token);
                HttpWebResponse resp = (HttpWebResponse)req.GetResponse();
                Stream stream = resp.GetResponseStream();
                string result = "";
                using (StreamReader reader = new StreamReader(stream))
                {
                    result = reader.ReadToEnd();
                }
                return result;
            }
            catch (Exception e)
            {
                new OMCLLog("[Login_Web_GET]出现错误：" + e, OMCLExceptionClass.DLL, OMCLExceptionType.Error);
                throw;
            }
        }
    }
}