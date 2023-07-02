using OMCL_DLL.Tools;
using OMCL_DLL.Tools.Login.MicrosoftLogin;
using OMCL_DLL.Tools.Login.Result;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace demo
{
    internal class Program
    {
        private static async Task Main(string[] args)
        //static async Task Main(string[] args)
        {
            /*
            //（循环）清理内存
            Thread thread = new Thread(new ThreadStart(Tools.ClearMemory));
            thread.IsBackground = true;
            thread.Start();
            */
            //Tools.DownloadMissAsstes("1.16.5");
            /*UnifiedPassLoginResult result = GetLogin.UnifiedPassLogin("0ef7f5a2050811ed9cdc00163e095b49", "Ocean123", "Myy100524");
            Console.WriteLine(result.access_token);
            Console.WriteLine(result.name);
            Console.WriteLine(result.uuid);
            Console.WriteLine(result.serverid);
            Console.WriteLine(result.client_token);
            CrashMessage crashMessage = Tools.LaunchGame("C:\\Program Files\\Java\\jre1.8.0_331\\bin\\java.exe", "1.16.5", result, out _);
            if (crashMessage == null) return;
            Console.WriteLine(crashMessage.Message);
            Console.WriteLine("可能的解决方法：\n" + crashMessage.Solution);
            Console.WriteLine("\n\n详细信息：\n退出代码：" + crashMessage.ExitCode + "\n版本名称：" + crashMessage.VersionName);*/
            /*CrashMessage crashMessage = Tools.LaunchGame("C:\\Program Files\\Java\\jre1.8.0_331\\bin\\java.exe", "1.16.5", "A", "A", "A");
            if (crashMessage == null) return;
            Console.WriteLine(crashMessage.Message);
            Console.WriteLine("可能的解决方法：\n" + crashMessage.Solution);
            Console.WriteLine("\n\n详细信息：\n退出代码：" + crashMessage.ExitCode + "\n版本名称：" + crashMessage.VersionName);*/
            //foreach (var java in await Tools.GetJavaListAsync()) Console.WriteLine(java.type.ToString() + ':' + java.path);
            //foreach (var java in SettingsAndRegistry.GetJavaListInRegistry()) Console.WriteLine(java.type.ToString() + ':' + java.path);
            //foreach (string version in Tools.GetVersionList()) Console.WriteLine(version);
            try
            {
                MicrosoftLoginResult loginResult = await GetLogin.MicrosoftLogin.NewLogin(true);
                if (loginResult == null)
                {
                    Console.WriteLine("用户取消登录！");
                    return;
                }
                Console.WriteLine(loginResult.name);
                Console.WriteLine(loginResult.uuid);
                Console.WriteLine(loginResult.refresh_token);
                Console.WriteLine(loginResult.access_token);
            }
            catch (NotAnException)
            {
                Console.Write("请输入网页返回后的url，什么都不输入则取消：");
                string result = Console.ReadLine();
                if (result == "" || result == null)
                {
                    Console.WriteLine("用户取消登录！");
                    return;
                }
                MicrosoftLoginResult login = GetLogin.MicrosoftLogin.LoginByWebSite(result);
                if (login == null)
                {
                    Console.WriteLine("错误的url！");
                    return;
                }
                Console.WriteLine(login.name);
                Console.WriteLine(login.uuid);
                Console.WriteLine(login.refresh_token);
                Console.WriteLine(login.access_token);
            }
            /*GetLogin.MicrosoftLogin.oauth2_OnGetCode += MicrosoftLogin2_oauth2_OnGetCode;
            MicrosoftLoginResult result = GetLogin.MicrosoftLogin.DCLogin();
            if (result == null)
            {
                Console.WriteLine("用户取消登录！");
                return;
            }
            Console.WriteLine(result.name);
            Console.WriteLine(result.uuid);
            Console.WriteLine(result.access_token);
            Console.WriteLine(result.refresh_token);*/
            /*CrashMessage crashMessage = Tools.LaunchGame("C:\\Program Files\\Java\\jre1.8.0_331\\bin\\java.exe", "1.16.5-demo-Forge", loginResult, out _);
            if (crashMessage == null) return;
            Console.WriteLine(crashMessage.Message);
            Console.WriteLine("可能的解决方法：\n" + crashMessage.Solution);
            Console.WriteLine("\n\n详细信息：\n退出代码：" + crashMessage.ExitCode + "\n版本名称：" + crashMessage.VersionName);*/
            /*InstallMinecraft.ForgeInstall.InstallForge(@"C:\Program Files\Java\jre1.8.0_331\bin\javaw.exe", "1.16.5-1-Forge", @"C:\Users\麻熠洋\Desktop\forge-1.16.5-36.2.39-installer.jar");
            Tools.DownloadMissFiles("1.16.5-1-Forge", false);
            Console.WriteLine("Forge安装成功！");*/

            //Form1 form1 = new Form1();
            //form1.ShowDialog();
            //Tools.LaunchMinecraft launch = new Tools.LaunchMinecraft();
            //launch.OnMinecraftCrash += Launch_OnMinecraftCrash;
            //launch.LaunchGame("C:\\Program Files\\Java\\jre1.8.0_331\\bin\\java.exe", "1.16.5", "AAA");
            //launch.process.WaitForExit();

            /*
            Tools.MaxMem = 4096;
            Tools.MinMem = 2048;
            Tools.IsIsolation = false;
            CrashMessage crashMessage = Tools.LaunchGame("C:\\Program Files\\Java\\jre1.8.0_331\\bin\\java.exe", "1.12.2-Forge-Computer", "A", "A", "A");
            */
        }

        private static void MicrosoftLogin2_oauth2_OnGetCode(string code)
        {
            Console.WriteLine(code);
        }
        /*private static void Launch_OnMinecraftCrash(CrashMessage crashMessage)
        {
            Console.WriteLine(crashMessage.Message);
            Console.WriteLine("可能的解决方法：\n" + crashMessage.Solution);
            Console.WriteLine("\n\n详细信息：\n退出代码：" + crashMessage.ExitCode + "\n版本名称：" + crashMessage.VersionName);
        }*/
    }
}