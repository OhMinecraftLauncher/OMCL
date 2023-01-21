using OMCL_DLL.Tools.Login.Result;
using OMCL_DLL.Tools;
using System;
using System.Threading;
using OMCL_DLL.Tools.Login.MicrosoftLogin;
using System.Linq;

namespace demo
{
    internal class Program
    {
        static void Main(string[] args)
        {
            
            //（循环）清理内存
            Thread thread = new Thread(new ThreadStart(Tools.ClearMemory));
            thread.IsBackground = true;
            thread.Start();

            CrashMessage crashMessage = Tools.LaunchGame("C:\\Program Files\\Java\\jre1.8.0_331\\bin\\java.exe", "1.16.5", "A", "A", "A");
            if (crashMessage == null) return;
            Console.WriteLine(crashMessage.Message);
            Console.WriteLine("可能的解决方法：\n" + crashMessage.Solution);
            Console.WriteLine("\n\n详细信息：\n退出代码：" + crashMessage.ExitCode + "\n版本名称：" + crashMessage.VersionName);
            //foreach (var java in Tools.GetJavaList()) Console.WriteLine(java.type.ToString() + ':' + java.path);
            /*LoginResult loginResult = GetLogin.MicrosoftLogin(true);
            if (loginResult == null)
            {
                Console.WriteLine("用户取消登录！");
                return;
            }
            CrashMessage crashMessage = Tools.LaunchGame("C:\\Program Files\\Java\\jre1.8.0_331\\bin\\java.exe", "1.16.5", loginResult.refresh_token, out string new_refresh_token, LoginType.Microsoft);
            if (crashMessage == null) return;
            Console.WriteLine(crashMessage.Message);
            Console.WriteLine("可能的解决方法：\n" + crashMessage.Solution);
            Console.WriteLine("\n\n详细信息：\n退出代码：" + crashMessage.ExitCode + "\n版本名称：" + crashMessage.VersionName);*/
            /*
            Tools.MaxMem = 4096;
            Tools.MinMem = 2048;
            Tools.IsIsolation = false;
            CrashMessage crashMessage = Tools.LaunchGame("C:\\Program Files\\Java\\jre1.8.0_331\\bin\\java.exe", "1.12.2-Forge-Computer", "A", "A", "A");
            */
        }
    }
}