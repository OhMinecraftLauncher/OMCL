using System;
using System.IO;

namespace OMCL_DLL.Tools
{
    public class OMCLLog
    {
        public OMCLLog(string message, OMCLExceptionClass Class, OMCLExceptionType type)
        {
            string dir = Dir.GetCurrentDirectory();
            if (!Directory.Exists(dir + @"\OMCL")) Directory.CreateDirectory(dir + @"\OMCL");
            if (!Directory.Exists(dir + @"\OMCL\Log")) Directory.CreateDirectory(dir + @"\OMCL\Log");
            if (!File.Exists(dir + @"\OMCL\Log\" + DateTime.Now.Year + '-' + DateTime.Now.Month + '-' + DateTime.Now.Day + "-Log.Log"))
            {
                FileStream fileStream = File.Create(dir + @"\OMCL\Log\" + DateTime.Now.Year + '-' + DateTime.Now.Month + '-' + DateTime.Now.Day + "-Log.Log");
                fileStream.Close();
            }
            StreamWriter streamWriter = File.AppendText(dir + @"\OMCL\Log\" + DateTime.Now.Year + '-' + DateTime.Now.Month + '-' + DateTime.Now.Day + "-Log.Log");
            streamWriter.WriteLine("[" + DateTime.Now.ToString() + "][" + Class.ToString() + "][" + type.ToString() + "]:" + message);
            streamWriter.Flush();
            streamWriter.Dispose();
            streamWriter.Close();
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