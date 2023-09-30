using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OMCL_DLL.Tools
{
    public class Version
    {
        public string[] libraries { get; internal set; }
        public string[] natives { get; internal set; }
        public string mainClass { get; internal set; }
        public Argument[] arguments { get; internal set; }
        public string assets { get; internal set; }
        public string jvm { get; internal set; }
    }
    public class Argument
    {
        public string name { get; internal set; }
        public string value { get; internal set; }
    }
    public class JavaVersion
    {
        public JavaType type { get; internal set; }
        public string path { get; internal set; }
    }
    public class CrashMessage
    {
        public string Message { get; internal set; }
        public int ExitCode { get; internal set; }
        public string VersionName { get; internal set; }
        public string Solution { get; internal set; }
    }
    public class ModInfo
    {
        public string IdNamespace { get; internal set; }
        public string FileName { get; internal set; }
    }
    public class FindOutDir
    {
        public string path { get; internal set; }
        public string PathinGet { get; internal set; }
    }
    public class FindOutFile
    {
        public string path_and_filename { get; internal set; }
        public string PathinGet { get; internal set; }
    }
    public class Server
    {
        public string server_url_or_ip { get; internal set; }
        public int server_port { get; internal set; }
    }
    public class JavaComp : IComparer<JavaVersion>
    {
        public int Compare(JavaVersion a, JavaVersion b)
        {
            return a.type.CompareTo(b.type);
        }
    }
}
