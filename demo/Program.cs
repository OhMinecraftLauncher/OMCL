using OMCL_DLL.Tools;
using OMCL_DLL.Tools.LocalException;
using OMCL_DLL.Tools.Login.MicrosoftLogin;
using OMCL_DLL.Tools.Login.Result;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace demo
{
    internal class Program
    {
        static async Task Main(string[] args)
        //static void Main(string[] args)
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
            /*try
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
                
                Tools.LaunchMinecraft launch = new Tools.LaunchMinecraft();
                launch.LaunchGame("C:\\Program Files\\Java\\jre1.8.0_331\\bin\\javaw.exe", "1.15", login, out _);
            }*/
            /*Tools.LaunchMinecraft launch = new Tools.LaunchMinecraft();
            launch.LaunchGame("C:\\Program Files\\Java\\jre1.8.0_331\\bin\\javaw.exe", "1.15", "Ocean");*/
            /*
            GetLogin.MicrosoftLogin.oauth2_OnGetCode += MicrosoftLogin2_oauth2_OnGetCode;
            MicrosoftLoginResult result = GetLogin.MicrosoftLogin.DCLogin();
            if (result == null)
            {
                Console.WriteLine("用户取消登录！");
                return;
            }
            Console.WriteLine(result.name);
            Console.WriteLine(result.uuid);
            Console.WriteLine(result.access_token);
            Console.WriteLine(result.refresh_token);
            */
            /*
            GetLogin.MicrosoftLogin.NewLogin.OpenLoginUrl(false);
            Console.Write("请输入网页返回后的url，什么都不输入则取消：");
            string result = Console.ReadLine();
            if (result == "" || result == null)
            {
                Console.WriteLine("用户取消登录！");
                return;
            }
            MicrosoftLoginResult login = GetLogin.MicrosoftLogin.NewLogin.LoginByCode(result);
            if (login == null)
            {
                Console.WriteLine("错误的url！");
                return;
            }
            Console.WriteLine(login.name);
            Console.WriteLine(login.uuid);
            Console.WriteLine(login.refresh_token);
            Console.WriteLine(login.access_token);
            */
            /*CrashMessage crashMessage = Tools.LaunchGame("C:\\Program Files\\Java\\jre1.8.0_331\\bin\\java.exe", "1.16.5-demo-Forge", loginResult, out _);
            if (crashMessage == null) return;
            Console.WriteLine(crashMessage.Message);
            Console.WriteLine("可能的解决方法：\n" + crashMessage.Solution);
            Console.WriteLine("\n\n详细信息：\n退出代码：" + crashMessage.ExitCode + "\n版本名称：" + crashMessage.VersionName);*/
            //InstallMinecraft.MinecraftInstall.InstallMinecraftVersion("1.8.9", "1.8.9");
            //InstallMinecraft.ForgeInstall.InstallForge("1.8.9", @"C:\Program Files\Java\jre1.8.0_331\bin\javaw.exe", @"C:\Users\麻熠洋\Desktop\Forge\forge-1.8.9-11.15.1.2318-1.8.9-installer.jar");
            /*Console.WriteLine(Tools.Dir);
            Tools.LaunchMinecraft launch = new Tools.LaunchMinecraft();
            launch.OnMinecraftCrash += Launch_OnMinecraftCrash;
            launch.LaunchGame(@"C:\Program Files\Java\jre1.8.0_331\bin\javaw.exe", "1.8.9", "AAA");*/
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            JavaVersion[] javas = await Tools.FindJava.GetJavas();
            //string[] s = (await Tools.FDTools.SearchFoldersInDrivers(null, "|bin", "javaw.exe")).ToArray();
            //foreach (string s2 in s)
            foreach (JavaVersion java in javas) 
            {
                //Console.WriteLine(s2);
                Console.WriteLine(java.type.ToString() + ':' + java.version + ':' + java.path);
            }
            Console.WriteLine("Time used: " + stopwatch.ElapsedMilliseconds / 1000.0 + " s");
            //Tools.DownloadMissFiles("1.16.5-1-Forge", false);
            //Console.WriteLine("Forge安装成功！");

            //InstallMinecraft.MinecraftInstall.InstallMinecraftVersion("rd-132211", "rd-132211", false, false);
            //InstallMinecraft.MinecraftInstall.InstallMinecraftVersion("1.6", "1.6", false, false);
            //InstallMinecraft.MinecraftInstall.InstallMinecraftVersion("1.8", "1.8", true, true);
            /*Tools.DownloadMissAsstes("1.1");
            Tools.LaunchMinecraft launch = new Tools.LaunchMinecraft();
            launch.OnMinecraftCrash += Launch_OnMinecraftCrash;
            launch.LaunchGame(@"C:\Program Files\Java\jre1.8.0_331\bin\java.exe", "1.1", "AAA");*/

            /*
            Tools.MaxMem = 4096;
            Tools.MinMem = 2048;
            Tools.IsIsolation = false;
            CrashMessage crashMessage = Tools.LaunchGame("C:\\Program Files\\Java\\jre1.8.0_331\\bin\\java.exe", "1.12.2-Forge-Computer", "A", "A", "A");
            */

            //Console.WriteLine(GetLogin.OfflineLogin("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaahbbbbbbbbbbbbbbbbbbbbbbbbbbbthdhddrhdgfhdfg").uuid); //918c59deab31be5bda9a153fc57e7582
            //联机
            /*
            Console.WriteLine("[0]加入一个联机");
            Console.WriteLine("[1]开启一个联机");
            Console.WriteLine("[2]关闭联机");
            Console.Write("请选择：");
            string con = Console.ReadLine();
            if (con == "0")
            {
                Console.Write("请输入联机码：");
                Server server = Link.JoinLink(Console.ReadLine());
                Console.WriteLine(server.server_url_or_ip + ':' + server.server_port);
            }
            else if (con == "1")
            {
                Console.Write("请启动一个Minecraft，并在Minecraft中进入一个单人游戏存档，然后选择<在局域网开放>，将聊天框中的数字（端口号）输入：");
                Console.WriteLine("这是您的联机码，将其发送给您的好友，畅快联机吧：" + Link.StartLink(int.Parse(Console.ReadLine())));
                Console.WriteLine("联机已经开启，按enter键结束联机！");
                Console.ReadLine();
                Link.StopLink();
            }
            else if (con == "2")
            {
                Link.StopLink();
            }
            */
        }

        private static void MicrosoftLogin2_oauth2_OnGetCode(string code)
        {
            Console.WriteLine(code);
        }
        private static void Launch_OnMinecraftCrash(CrashMessage crashMessage)
        {
            Console.WriteLine(crashMessage.Message);
            Console.WriteLine("可能的解决方法：\n" + crashMessage.Solution);
            Console.WriteLine("\n\n详细信息：\n退出代码：" + crashMessage.ExitCode + "\n版本名称：" + crashMessage.VersionName);
        }
    }
}