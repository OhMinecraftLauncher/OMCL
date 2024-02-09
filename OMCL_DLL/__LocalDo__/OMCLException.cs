using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OMCL_DLL.Tools
{
    public class OMCLLog
    {
        private static readonly object wlock = new object();
        private static readonly string dir = Tools.Dir;
        private static readonly DateTime dt = DateTime.Now;
        private static StreamWriter streamWriter = null;
        public static void WriteLog(string message, OMCLExceptionClass Class, OMCLExceptionType type)
        {
            Task.Run(() =>
            {
                lock (wlock)
                {
                    if (streamWriter == null)
                    {
                        Directory.CreateDirectory(Path.Combine(dir, "OMCL", "Log"));
                        streamWriter = File.AppendText(Path.Combine(new string[] { dir, "OMCL", "Log", dt.Year + '-'.ToString() + dt.Month + '-' + dt.Day + "-Log.Log" }));
                    }
                    if (!File.Exists(Path.Combine(new string[] { dir, "OMCL", "Log", dt.Year + '-'.ToString() + dt.Month + '-' + dt.Day + "-Log.Log" })))
                    {
                        File.Create(Path.Combine(new string[] { dir, "OMCL", "Log", dt.Year + '-'.ToString() + dt.Month + '-' + dt.Day + "-Log.Log" })).Dispose();
                    }
                    string s = "[" + dt.ToString() + "][" + Class.ToString() + "][" + type.ToString() + "]:" + Regex.Replace(message, @"\\Users\\(.*?)\\", @"\Users\隐藏用户名\");
#if DEBUG
                    //Console.WriteLine(s);
#endif
                    streamWriter.WriteLine(s);
                    streamWriter.Flush();
                }
            });
        }
    }
    public enum OMCLExceptionClass
    {
        OMCL, DLL, UI
    }
    public enum OMCLExceptionType
    {
        Message, Warning, Error
    }
}

namespace OMCL_DLL.Tools.LocalException
{
    public class NoClassFileException : Exception
    {
        public NoClassFileException(string className, string fileName, string url) : base("找不到 " + className + " 中必须的 " + fileName + " 文件！请去 " + url + " 中下载！")
        {
            OMCLLog.WriteLog("NoClassFileException: " + fileName + " in " + className + " , " + url + " 。\n\t" + Message + '\n' + ToString(), OMCLExceptionClass.DLL, OMCLExceptionType.Error);
        }
    }
    public class OMCLException : Exception
    {
        public OMCLException(string message) : base(message)
        {
            OMCLLog.WriteLog("OMCLException: " + Message  + '\n' + ToString(), OMCLExceptionClass.DLL, OMCLExceptionType.Error);
        }
        public OMCLException(string message, Exception e) : base(message, e)
        {
            OMCLLog.WriteLog("OMCLException: " + Message + '\n' + ToString(), OMCLExceptionClass.DLL, OMCLExceptionType.Error);
        }
    }
}