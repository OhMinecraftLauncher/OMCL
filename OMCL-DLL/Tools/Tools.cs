using ICSharpCode.SharpZipLib.Zip;
using Newtonsoft.Json.Linq;
using OMCL_DLL.Tools.Login.MicrosoftLogin;
using OMCL_DLL.Tools.Login.Result;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
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
    public enum DownloadSource
    {
        BMCLAPI, MCBBS,
    }
    public enum LoginType
    {
        Microsoft, Mojang
    }
    public enum JavaType
    {
        jre, jdk, unknown
    }
    public class Tools
    {
        public static bool IsIsolation = true;
        public static int MaxMem = 2048, MinMem = 1024;
        public static string jvm = "", otherArguments = "";
        public static string NativesPath = "";
        public static List<string> LaunchNoFile = new List<string>();
        public static List<string> LaunchNoFileNatives = new List<string>();
        public static string DownloadMinecraftFileUrl = "https://bmclapi2.bangbang93.com/libraries/";
        private static readonly string OMCLver = "0.0.0.1";
        private static readonly string Dir = OMCL_DLL.Tools.Dir.GetCurrentDirectory();
        public static List<Task> task = new List<Task>();
        [DllImport("kernel32.dll", EntryPoint = "SetProcessWorkingSetSize")]
        private static extern int SetProcessWorkingSetSize(IntPtr process, int minSize, int maxSize);
        public static void ClearMemory()
        {
            while (true)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                {
                    SetProcessWorkingSetSize(Process.GetCurrentProcess().Handle, -1, -1);
                }
            }
        }
        public static CrashMessage LaunchGame(string java, string version, string refresh_token, out string new_refresh_token, LoginType loginType)
        {
            try
            {
                LoginResult result;
                string playerName;
                string uuid;
                string token;
                if (loginType == LoginType.Microsoft)
                {
                    result = MicrosoftLogin.RefreshLogin(refresh_token);
                }
                else
                {
                    result = null;
                }
                playerName = result.name;
                uuid = result.uuid;
                token = result.access_token;
                new_refresh_token = result.refresh_token;
                LaunchNoFile = new List<string>();
                LaunchNoFileNatives = new List<string>();
                Version ver = ReadVersionJson(version);
                string GameCMD = "", ClientPath = Dir + @"\.minecraft\versions\" + version + @"\" + version + ".jar";
                DownloadMissFiles(version);
                string _NativesPath;
                if (NativesPath == "") _NativesPath = UnzipNatives(version); else _NativesPath = NativesPath;
                GameCMD += "-server -Dminecraft.client.jar=\"{Client_Path}\" -Xverify:none -XX:+UseParallelOldGC -XX:MaxInlineSize=420 -Xms{Min_Mem}m -Xmx{Max_Mem}m -Xmn256m -Djava.library.path=\"{Natives_Path}\" -Dminecraft.launcher.brand=OMCL -Dminecraft.launcher.version={OMCL_Version} ".Replace("{OMCL_Version}", OMCLver).Replace("{Natives_Path}", _NativesPath).Replace("{Max_Mem}", MaxMem.ToString()).Replace("{Min_Mem}", MinMem.ToString()).Replace("{Client_Path}", ClientPath);
                if (jvm != "") GameCMD += jvm + " ";
                GameCMD += "-cp \"";
                string tempCP = "";
                List<string> libraries = ver.libraries.ToList();
                foreach (string No_path in LaunchNoFile) libraries.Remove(No_path);
                for (int i = 0; i < libraries.Count; i++)
                {
                    if (i == libraries.Count - 1) tempCP += Dir + @"\.minecraft\libraries\" + libraries[i];
                    else tempCP += Dir + @"\.minecraft\libraries\" + libraries[i] + ';';
                }
                GameCMD += tempCP + ";" + Dir + @"\.minecraft\versions\" + version + "\\" + version + ".jar\" ";
                GameCMD += ver.mainClass + ' ';
                for (int i = 0; i < ver.arguments.Length; i++)
                {
                    GameCMD += ver.arguments[i].name + ' ';
                    switch (ver.arguments[i].value)
                    {
                        case "${auth_player_name}":
                            GameCMD += playerName + ' ';
                            break;
                        case "${version_name}":
                            GameCMD += version + ' ';
                            break;
                        case "${game_directory}":
                            if (IsIsolation) GameCMD += '\"' + Dir + @"\.minecraft\versions\" + version + '\"' + ' ';
                            else GameCMD += '\"' + Dir + @"\.minecraft" + '\"' + ' ';
                            break;
                        case "${assets_root}":
                            GameCMD += '\"' + Dir + @"\.minecraft\assets" + '\"' + ' ';
                            break;
                        case "${assets_index_name}":
                            GameCMD += ver.assets + ' ';
                            break;
                        case "${auth_uuid}":
                            GameCMD += uuid + ' ';
                            break;
                        case "${auth_access_token}":
                            GameCMD += token + ' ';
                            break;
                        case "${user_type}":
                            GameCMD += "Mojang" + ' ';
                            break;
                        case "${version_type}":
                            GameCMD += "\"OMCL " + OMCLver + "\" ";
                            break;
                        case "${user_properties}":
                            GameCMD += "{} ";
                            break;
                        default:
                            GameCMD += ver.arguments[i].value + ' ';
                            break;
                    }
                }
                if (otherArguments != "") GameCMD += otherArguments;
                else GameCMD.Remove(GameCMD.Length - 2, 1);
                new OMCLLog("[Tools_LaunchGame]启动脚本如下：" + "\"" + java + "\" " + GameCMD + "。", OMCLExceptionClass.DLL, OMCLExceptionType.Message);
                if (IsIsolation)
                {
                    if (!File.Exists(Dir + @"\.minecraft\versions\" + version + @"\options.txt"))
                    {
                        File.WriteAllText(Dir + @"\.minecraft\versions\" + version + @"\options.txt", "lang:zh_cn");
                    }
                }
                else
                {
                    if (!File.Exists(Dir + @"\.minecraft\options.txt"))
                    {
                        File.WriteAllText(Dir + @"\.minecraft\options.txt", "lang:zh_cn");
                    }
                }
                Process process = new Process();
                process.StartInfo.Arguments = GameCMD;
                process.StartInfo.FileName = java;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                if (!IsIsolation) process.StartInfo.WorkingDirectory = Dir + @"\.minecraft";
                else process.StartInfo.WorkingDirectory = Dir + @"\.minecraft\versions\" + version;
                process.Start();
                return WaitMinecraft(process, version);
            }
            catch (Exception e)
            {
                new OMCLLog("[Tools_LaunchGame]" + @e.Message, OMCLExceptionClass.DLL, OMCLExceptionType.Error);
                throw;
            }
        }
        public static CrashMessage LaunchGame(string java, string version, string playerName, string uuid, string token, string user_properties = "{}")
        {
            try
            {
                LaunchNoFile = new List<string>();
                LaunchNoFileNatives = new List<string>();
                Version ver = ReadVersionJson(version);
                string GameCMD = "", ClientPath = Dir + @"\.minecraft\versions\" + version + @"\" + version + ".jar";
                DownloadMissFiles(version);
                string _NativesPath;
                if (NativesPath == "") _NativesPath = UnzipNatives(version); else _NativesPath = NativesPath;
                GameCMD += "-server -Dminecraft.client.jar=\"{Client_Path}\" -Xverify:none -XX:+UseParallelOldGC -XX:MaxInlineSize=420 -Xms{Min_Mem}m -Xmx{Max_Mem}m -Xmn256m -Djava.library.path=\"{Natives_Path}\" -Dminecraft.launcher.brand=OMCL -Dminecraft.launcher.version={OMCL_Version} ".Replace("{OMCL_Version}", OMCLver).Replace("{Natives_Path}", _NativesPath).Replace("{Max_Mem}", MaxMem.ToString()).Replace("{Min_Mem}", MinMem.ToString()).Replace("{Client_Path}", ClientPath);
                if (jvm != "") GameCMD += jvm + " ";
                GameCMD += "-cp \"";
                string tempCP = "";
                List<string> libraries = ver.libraries.ToList();
                foreach (string No_path in LaunchNoFile) libraries.Remove(No_path);
                for (int i = 0; i < libraries.Count; i++)
                {
                    if (i == libraries.Count - 1) tempCP += Dir + @"\.minecraft\libraries\" + libraries[i];
                    else tempCP += Dir + @"\.minecraft\libraries\" + libraries[i] + ';';
                }
                GameCMD += tempCP + ";" + Dir + @"\.minecraft\versions\" + version + "\\" + version + ".jar\" ";
                GameCMD += ver.mainClass + ' ';
                for (int i = 0; i < ver.arguments.Length; i++)
                {
                    GameCMD += ver.arguments[i].name + ' ';
                    switch (ver.arguments[i].value)
                    {
                        case "${auth_player_name}":
                            GameCMD += playerName + ' ';
                            break;
                        case "${version_name}":
                            GameCMD += version + ' ';
                            break;
                        case "${game_directory}":
                            if (IsIsolation) GameCMD += '\"' + Dir + @"\.minecraft\versions\" + version + '\"' + ' ';
                            else GameCMD += '\"' + Dir + @"\.minecraft" + '\"' + ' ';
                            break;
                        case "${assets_root}":
                            GameCMD += '\"' + Dir + @"\.minecraft\assets" + '\"' + ' ';
                            break;
                        case "${assets_index_name}":
                            GameCMD += ver.assets + ' ';
                            break;
                        case "${auth_uuid}":
                            GameCMD += uuid + ' ';
                            break;
                        case "${auth_access_token}":
                            GameCMD += token + ' ';
                            break;
                        case "${user_type}":
                            GameCMD += "Mojang" + ' ';
                            break;
                        case "${version_type}":
                            GameCMD += "\"OMCL " + OMCLver + "\" ";
                            break;
                        case "${user_properties}":
                            GameCMD += user_properties + ' ';
                            break;
                        default:
                            GameCMD += ver.arguments[i].value + ' ';
                            break;
                    }
                }
                if (otherArguments != "") GameCMD += otherArguments;
                else GameCMD.Remove(GameCMD.Length - 2, 1);
                new OMCLLog("[Tools_LaunchGame]启动脚本如下：" + "\"" + java + "\" " + GameCMD + "。", OMCLExceptionClass.DLL, OMCLExceptionType.Message);
                if (IsIsolation)
                {
                    if (!File.Exists(Dir + @"\.minecraft\versions\" + version + @"\options.txt"))
                    {
                        File.WriteAllText(Dir + @"\.minecraft\versions\" + version + @"\options.txt", "lang:zh_cn");
                    }
                }
                else
                {
                    if (!File.Exists(Dir + @"\.minecraft\options.txt"))
                    {
                        File.WriteAllText(Dir + @"\.minecraft\options.txt", "lang:zh_cn");
                    }
                }
                Process process = new Process();
                process.StartInfo.Arguments = GameCMD;
                process.StartInfo.FileName = java;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                if (!IsIsolation) process.StartInfo.WorkingDirectory = Dir + @"\.minecraft";
                else process.StartInfo.WorkingDirectory = Dir + @"\.minecraft\versions\" + version;
                process.Start();
                return WaitMinecraft(process, version);
            }
            catch (Exception e)
            {
                new OMCLLog("[Tools_LaunchGame]" + @e.Message, OMCLExceptionClass.DLL, OMCLExceptionType.Error);
                throw;
            }
        }
        public static void UnZipFile(string file, string dir)
        {
            try
            {
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                ZipInputStream s = new ZipInputStream(File.OpenRead(file));
                ZipEntry theEntry;
                while ((theEntry = s.GetNextEntry()) != null)
                {
                    string directoryName = Path.GetDirectoryName(theEntry.Name);
                    string fileName = Path.GetFileName(theEntry.Name);
                    if (directoryName != string.Empty)
                    {
                        Directory.CreateDirectory(dir + directoryName);
                    }
                    if (fileName != string.Empty)
                    {
                        FileStream streamWriter = File.Create(dir + theEntry.Name);
                        int size = 2048;
                        byte[] data = new byte[2048];
                        while (true)
                        {
                            size = s.Read(data, 0, data.Length);
                            if (size > 0)
                            {
                                streamWriter.Write(data, 0, size);
                            }
                            else
                            {
                                break;
                            }
                        }
                        streamWriter.Close();
                    }
                }
                s.Close();
            }
            catch (Exception e)
            {
                new OMCLLog("[Tools_UnZipFile]" + @e.Message, OMCLExceptionClass.DLL, OMCLExceptionType.Error);
                throw;
            }
        }
        public static void SearchDirThread(string path, string filename, string[] onlysearches, string[] pathins, string[] pathsames, List<FindOutDir> result)
        {
            string[] dirs = Directory.GetDirectories(path, "*", SearchOption.TopDirectoryOnly);
            foreach (string dir in dirs)
            {
                bool cansearch = true;
                foreach (string onlysearch in onlysearches)
                {
                    if (dir.Contains(onlysearch)) break;
                    if (onlysearch == onlysearches.Last()) cansearch = false;
                }
                if (cansearch)
                {
                    string GetPainin = null;
                    bool findout = false;
                    foreach (string pathin in pathins)
                    {
                        if (dir.Contains(pathin))
                        {
                            GetPainin = pathin;
                            break;
                        }
                    }
                    int HasNum = 0;
                    foreach (string pathsame in pathsames)
                    {
                        if (dir.Contains(pathsame))
                        {
                            HasNum++;
                        }
                    }
                    if (HasNum == pathsames.Length && GetPainin != null)
                    {
                        string[] files;
                        try
                        {
                            files = Directory.GetFiles(dir, "*", SearchOption.TopDirectoryOnly);
                        }
                        catch
                        {
                            continue;
                        }
                        foreach (string file in files)
                        {
                            if (Path.GetFileName(file) == filename)
                            {
                                result.Add(new FindOutDir
                                {
                                    path = dir,
                                    PathinGet = GetPainin,
                                });
                                findout = true;
                                break;
                            }
                        }
                    }
                    try
                    {
                        if (findout) continue;
                        else if (Directory.GetDirectories(dir).Length != 0)
                        {
                            task.Add(Task.Run(() => SearchDirThread(dir, filename, onlysearches, pathins, pathsames, result)));
                        }
                    }
                    catch { }
                }
            }
        }
        public static void SearchFileThread(string path, string filename, string[] onlysearches, string[] pathins, string[] pathsames, List<FindOutFile> result)
        {
            string[] dirs = Directory.GetDirectories(path, "*", SearchOption.TopDirectoryOnly);
            foreach (string dir in dirs)
            {
                bool cansearch = true;
                foreach (string onlysearch in onlysearches)
                {
                    if (dir.Contains(onlysearch)) break;
                    if (onlysearch == onlysearches.Last()) cansearch = false;
                }
                if (cansearch)
                {
                    string GetPainin = null;
                    bool findout = false;
                    foreach (string pathin in pathins)
                    {
                        if (dir.Contains(pathin))
                        {
                            GetPainin = pathin;
                            break;
                        }
                    }
                    int HasNum = 0;
                    foreach (string pathsame in pathsames)
                    {
                        if (dir.Contains(pathsame))
                        {
                            HasNum++;
                        }
                    }
                    if (HasNum == pathsames.Length && GetPainin != null)
                    {
                        string[] files;
                        try
                        {
                            files = Directory.GetFiles(dir, "*", SearchOption.TopDirectoryOnly);
                        }
                        catch
                        {
                            continue;
                        }
                        foreach (string file in files)
                        {
                            if (Path.GetFileName(file) == filename)
                            {
                                result.Add(new FindOutFile
                                {
                                    path_and_filename = file,
                                    PathinGet = GetPainin,
                                });
                                findout = true;
                                break;
                            }
                        }
                    }
                    try
                    {
                        if (findout) continue;
                        else if (Directory.GetDirectories(dir).Length != 0)
                        {
                            task.Add(Task.Run(() => SearchFileThread(dir, filename, onlysearches, pathins, pathsames, result)));
                        }
                    }
                    catch { }
                }
            }
        }
        /*public static void SearchFile(string path, string filename, string[] onlysearches, string[] pathins, string[] pathsames, List<FindOutDir> result)
        {
            string[] dirs = Directory.GetDirectories(path, "*", SearchOption.TopDirectoryOnly);
            foreach (string dir in dirs)
            {
                bool cansearch = true;
                foreach (string onlysearch in onlysearches)
                {
                    if (dir.Contains(onlysearch)) break;
                    if (onlysearch == onlysearches.Last()) cansearch = false;
                }
                if (cansearch)
                {
                    string GetPainin = null;
                    bool findout = false;
                    foreach (string pathin in pathins)
                    {
                        if (dir.Contains(pathin))
                        {
                            GetPainin = pathin;
                            break;
                        }
                    }
                    int HasNum = 0;
                    foreach (string pathsame in pathsames)
                    {
                        if (dir.Contains(pathsame))
                        {
                            HasNum++;
                        }
                    }
                    if (HasNum == pathsames.Length && GetPainin != null)
                    {
                        string[] files;
                        try
                        {
                            files = Directory.GetFiles(dir, "*", SearchOption.TopDirectoryOnly);
                        }
                        catch
                        {
                            continue;
                        }
                        foreach (string file in files)
                        {
                            if (Path.GetFileName(file) == filename)
                            {
                                result.Add(new FindOutDir
                                {
                                    path = dir,
                                    PathinGet = GetPainin,
                                });
                                findout = true;
                                break;
                            }
                        }
                    }
                    try
                    {
                        if (findout) continue;
                        else if (Directory.GetDirectories(dir).Length != 0) SearchFile(dir, filename, onlysearches, pathins, pathsames, result);
                    }
                    catch { }
                }
            }
        }*/
        public static JavaVersion[] GetJavaList()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            task = new List<Task>();
            string[] pathins = { "jre", "jdk", "java", "Java" };
            string[] pathsames = { "bin" };
            string[] onlysearchs = { Environment.Is64BitOperatingSystem ? @"Program Files" : @"Program Files (x86)", "XboxGames", "MCLDownload" };
            List<FindOutDir> result = new List<FindOutDir>();
            List<JavaVersion> javas = new List<JavaVersion>();
            List<string> drives = new List<string>();
            foreach (DriveInfo info in DriveInfo.GetDrives()) if (info.DriveType == DriveType.Fixed) drives.Add(info.Name);
            foreach (string drive in drives) SearchDirThread(@drive, "java.exe", onlysearchs, pathins, pathsames, result);
            while (true)
            {
                if (stopwatch.ElapsedMilliseconds <= 1200)
                {
                    try
                    {
                        Task.WaitAll(task.ToArray());
                    }
                    catch { }
                }
                else break;
            }
            foreach (FindOutDir temp in result)
            {
                if (temp.PathinGet == "java" || temp.PathinGet == "Java")
                {
                    javas.Add(new JavaVersion
                    {
                        path = temp.path,
                        type = JavaType.unknown,
                    });
                }
                else if (temp.PathinGet == "jre")
                {
                    javas.Add(new JavaVersion
                    {
                        path = temp.path, 
                        type = JavaType.jre,
                    });
                }
                else if (temp.PathinGet == "jdk")
                {
                    javas.Add(new JavaVersion
                    {
                        path = temp.path,
                        type = JavaType.jdk,
                    });
                }
            }
            Console.WriteLine("耗时：" + (double)stopwatch.ElapsedMilliseconds / 1000 + " s");
            return javas.ToArray();
        }
        public static void DownloadMissFiles(string version, bool IsLaunch = true)
        {
            try
            {
                Version ver = ReadVersionJson(version);
                for (int i = 0; i < ver.libraries.Length; i++)
                {
                    if (!File.Exists(Dir + @"\.minecraft\libraries\" + ver.libraries[i]))
                    {
                        if (ver.libraries[i].Contains("optifine"))
                        {
                            try
                            {
                                string[] temppath1 = ver.libraries[i].Replace("optifine\\OptiFine\\", "").Split('\\');
                                string[] temppath2 = temppath1[0].Split('_');
                                Download.HttpFile.Start(5, DownloadMinecraftFileUrl.Replace("/libraries", "") + @"optifine/" + temppath2[0] + "/" + temppath1[0].Replace(temppath2[0] + "_", "").Replace("_" + temppath2.Last(), "") + "/" + temppath2.Last(), Dir + @"\.minecraft\libraries\" + ver.libraries[i]);
                            }
                            catch
                            {
                                if (IsLaunch)
                                {
                                    new OMCLLog("[Tools_DownloadMissFiles]将尝试跳过 " + Dir + @"\.minecraft\libraries\" + ver.libraries[i] + " 启动Minecraft！", OMCLExceptionClass.DLL, OMCLExceptionType.Warning);
                                    LaunchNoFile.Add(ver.libraries[i]);
                                }
                            }
                        }
                        else
                        {
                            try
                            {
                                Download.HttpFile.Start(5, DownloadMinecraftFileUrl + ver.libraries[i].Replace('\\', '/'), Dir + @"\.minecraft\libraries\" + ver.libraries[i]);
                            }
                            catch
                            {
                                if (IsLaunch)
                                {
                                    new OMCLLog("[Tools_DownloadMissFiles]将尝试跳过 " + Dir + @"\.minecraft\libraries\" + ver.libraries[i] + " 启动Minecraft！", OMCLExceptionClass.DLL, OMCLExceptionType.Warning);
                                    LaunchNoFile.Add(ver.libraries[i]);
                                }
                            }
                        }
                    }
                }
                for (int i = 0; i < ver.natives.Length; i++)
                {
                    if (!File.Exists(Dir + @"\.minecraft\libraries\" + ver.natives[i]))
                    {
                        try
                        {
                            Download.HttpFile.Start(5, DownloadMinecraftFileUrl + ver.natives[i].Replace('\\', '/'), Dir + @"\.minecraft\libraries\" + ver.natives[i]);
                        }
                        catch
                        {
                            if (IsLaunch)
                            {
                                new OMCLLog("[Tools_DownloadMissFiles]将尝试跳过 " + Dir + @"\.minecraft\libraries\" + ver.natives[i] + " 启动Minecraft！", OMCLExceptionClass.DLL, OMCLExceptionType.Warning);
                                LaunchNoFileNatives.Add(ver.natives[i]);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                new OMCLLog("[Tools_DownloadMissFile]" + @e.Message, OMCLExceptionClass.DLL, OMCLExceptionType.Error);
                throw;
            }
        }
        public static void ChangeNativesPath(string path)
        {
            NativesPath = path;
        }
        public static void ChangeMinecraftPath(string path)
        {
            if (path == null || path == "") OMCL_DLL.Tools.Dir.SetCurrentDirectory(Directory.GetCurrentDirectory());
            else OMCL_DLL.Tools.Dir.SetCurrentDirectory(path);
        }
        public static void ChangeDownloadSource(DownloadSource downloadSource)
        {
            if (downloadSource == DownloadSource.BMCLAPI) DownloadMinecraftFileUrl = "https://bmclapi2.bangbang93.com/libraries/";
            else DownloadMinecraftFileUrl = "https://download.mcbbs.net/libraries/";
        }
        private static Version ReadVersionJson(string version)
        {
            try
            {
                List<string> Natives_List = new List<string>(20);
                List<string> Path = new List<string>(60);
                JObject o = JObject.Parse(File.ReadAllText(Dir + @"\.minecraft\versions\" + version + @"\" + version + @".json"));
                for (int i = 0; i < ((JArray)o.SelectToken("libraries")).Count(); i++)
                {
                    string path = (string)o.SelectToken("libraries[" + i + "].name");
                    path = path.Replace(':', '\\');
                    string[] paths = path.Split('\\');
                    paths[0] = paths.First().Replace('.', '\\');
                    path = "";
                    for (int j = 0; j < paths.Length; j++)
                    {
                        path += paths[j] + '\\';
                    }
                    if (path.Contains("natives"))
                    {
                        if (path.Contains("natives-windows"))
                        {
                            if (Environment.Is64BitOperatingSystem == true)
                            {
                                if (path.Contains("x86")) continue;
                            }
                            else
                            {
                                if (!path.Contains("x86")) continue;
                            }
                            Natives_List.Add(path + paths[paths.Length - 3] + @"-" + paths[paths.Length - 2] + @"-" + paths[paths.Length - 1] + ".jar");
                        }
                        continue;
                    }
                    JObject natives_windows = (JObject)o.SelectToken("libraries[" + i + "].downloads.classifiers.natives-windows");
                    if (natives_windows != null)
                    {
                        Natives_List.Add(path + paths[paths.Length - 2] + @"-" + paths[paths.Length - 1] + "-natives-windows.jar");
                    }
                    string native = (string)o.SelectToken("libraries[" + i + "].natives.windows");
                    if (native != null)
                    {
                        native = native.Replace("${arch}", Environment.Is64BitOperatingSystem ? "64" : "32");
                        Natives_List.Add(path + paths[paths.Length - 2] + @"-" + paths[paths.Length - 1] + "-" + native + ".jar");
                    }
                    path = path + paths[paths.Length - 2] + @"-" + paths[paths.Length - 1] + ".jar";
                    Path.Add(path);
                }
                Natives_List = new HashSet<string>(Natives_List).ToList();
                Path = new HashSet<string>(Path).ToList();
                List<Argument> arguments = new List<Argument>(15);
                for (int i = 1; i <= 999; i += 2)
                {
                    Argument argument;
                    try
                    {
                        string name = (string)o.SelectToken("arguments.game[" + (i - 1) + "]");
                        string value = (string)o.SelectToken("arguments.game[" + i + "]");
                        if (name == null && value == null)
                        {
                            string[] minecraftArguments = ((string)o.SelectToken("minecraftArguments")).Split(' ');
                            if (minecraftArguments[0].Contains("--"))
                            {
                                for (int j = 1; j < minecraftArguments.Length; j += 2)
                                {
                                    name = minecraftArguments[j - 1];
                                    value = minecraftArguments[j];
                                    argument = new Argument
                                    {
                                        name = name,
                                        value = value,
                                    };
                                    arguments.Add(argument);
                                }
                            }
                            else
                            {
                                for (int j = 0; j < minecraftArguments.Length; j++)
                                {
                                    name = null;
                                    value = minecraftArguments[j];
                                    argument = new Argument
                                    {
                                        name = name,
                                        value = value,
                                    };
                                    arguments.Add(argument);
                                }
                            }
                            break;
                        }
                        argument = new Argument
                        {
                            name = name,
                            value = value,
                        };
                        arguments.Add(argument);
                    }
                    catch
                    {
                        break;
                    }
                }
                for (int i = 0; i < arguments.Count; i++)
                {
                    if (arguments[i].value.Contains("optifine"))
                    {
                        arguments.Add(arguments[i]);
                        arguments.RemoveAt(i);
                    }
                }
                arguments.TrimExcess();
                List<string> Na_temp = new List<string>(20);
                List<string> Li_temp = new List<string>(20);
                bool IsLwjgl_3_2_2 = false;
                for (int i = 0; i < Path.Count; i++)
                {
                    if (Path[i].Contains(@"lwjgl\3.2.2\"))
                    {
                        IsLwjgl_3_2_2 = true;
                        break;
                    }
                }
                if (IsLwjgl_3_2_2)
                {
                    for (int i = 0; i < Natives_List.Count; i++)
                    {
                        if (Regex.IsMatch(Natives_List[i], @"\\lwjgl(.*)\\3.2.1\\lwjgl(.*)-3.2.1(.*).jar"))
                        {
                            Na_temp.Add(Natives_List[i]);
                        }
                    }
                    for (int i = 0; i < Path.Count; i++)
                    {
                        if (Regex.IsMatch(Path[i], @"\\lwjgl(.*)\\3.2.1\\lwjgl(.*)-3.2.1.jar"))
                        {
                            Li_temp.Add(Path[i]);
                        }
                    }
                }
                bool IsLwjgl_Nightly_2_9_4 = false;
                for (int i = 0; i < Path.Count; i++)
                {
                    if (Path[i].Contains("2.9.4-nightly"))
                    {
                        IsLwjgl_Nightly_2_9_4 = true;
                        break;
                    }
                }
                if (IsLwjgl_Nightly_2_9_4)
                {
                    for (int i = 0; i < Natives_List.Count; i++)
                    {
                        if (Natives_List[i].Contains("2.9.2-nightly"))
                        {
                            Na_temp.Add(Natives_List[i]);
                        }
                    }
                    for (int i = 0; i < Path.Count; i++)
                    {
                        if (Path[i].Contains("2.9.2-nightly"))
                        {
                            Li_temp.Add(Path[i]);
                        }
                    }
                }
                bool IsLog4j_2_15_0 = false;
                for (int i = 0; i < Path.Count; i++)
                {
                    if (Path[i].Contains(@"\2.15.0\log4j"))
                    {
                        IsLog4j_2_15_0 = true;
                        break;
                    }
                }
                if (IsLog4j_2_15_0)
                {
                    for (int i = 0; i < Natives_List.Count; i++)
                    {
                        if (Natives_List[i].Contains(@"\2.8.1\log4j"))
                        {
                            Na_temp.Add(Natives_List[i]);
                        }
                    }
                    for (int i = 0; i < Path.Count; i++)
                    {
                        if (Path[i].Contains(@"\2.8.1\log4j"))
                        {
                            Li_temp.Add(Path[i]);
                        }
                    }
                }
                for (int i = 0; i < Natives_List.Count; i++)
                {
                    if (Natives_List[i].Contains(@"java-objc-bridge"))
                    {
                        Na_temp.Add(Natives_List[i]);
                    }
                    else if (Natives_List[i].Contains(@"lwjgl-platform\2.9.4-nightly"))
                    {
                        Na_temp.Add(Natives_List[i]);
                    }
                }
                for (int i = 0; i < Path.Count; i++)
                {
                    if (Path[i].Contains(@"java-objc-bridge"))
                    {
                        Li_temp.Add(Path[i]);
                    }
                    else if (Path[i].Contains(@"lwjgl-platform\2.9.4-nightly"))
                    {
                        Li_temp.Add(Path[i]);
                    }
                }
                for (int i = 0; i < Path.Count; i++)
                {
                    if (Path[i].Contains(@"optifine"))
                    {
                        Li_temp.Add(Path[i]);
                        Path.Add(Path[i]);
                        break;
                    }
                }
                Na_temp = new HashSet<string>(Na_temp).ToList();
                Li_temp = new HashSet<string>(Li_temp).ToList();
                Na_temp.TrimExcess();
                Li_temp.TrimExcess();
                foreach (var str in Na_temp) Natives_List.Remove(str);
                foreach (var str in Li_temp) Path.Remove(str);
                Na_temp.Clear();
                Li_temp.Clear();
                Natives_List = new HashSet<string>(Natives_List).ToList();
                Path = new HashSet<string>(Path).ToList();
                Natives_List.TrimExcess();
                Path.TrimExcess();
                Version ver = new Version
                {
                    libraries = Path.ToArray(),
                    natives = Natives_List.ToArray(),
                    mainClass = (string)o.SelectToken("mainClass"),
                    arguments = arguments.ToArray(),
                    assets = (string)o.SelectToken("assets"),
                };
                Natives_List.Clear();
                Path.Clear();
                return ver;
            }
            catch (Exception e)
            {
                new OMCLLog("[Tools_ReadVersionJson]" + @e.Message, OMCLExceptionClass.DLL, OMCLExceptionType.Error);
                throw;
            }
        }
        private static string UnzipNatives(string version)
        {
            try
            {
                Version ver = ReadVersionJson(version);
                List<string> natives = ver.natives.ToList();
                for (int i = 0; i < LaunchNoFileNatives.Count; i++) natives.Remove(LaunchNoFileNatives[i]);
                for (int i = 0; i < natives.Count; i++)
                {
                    UnZipFile(Dir + @"\.minecraft\libraries\" + natives[i], Dir + @"\.minecraft\versions\" + version + '\\' + version + "-natives\\");
                }
                return Dir + @"\.minecraft\versions\" + version + '\\' + version + "-natives";
            }
            catch (Exception e)
            {
                new OMCLLog("[Tools_UnzipNatives]" + @e.Message, OMCLExceptionClass.DLL, OMCLExceptionType.Error);
                throw;
            }
        }
        private static CrashMessage WaitMinecraft(Process process, string version)
        {
            try
            {
                //Process process = (Process)_Process;
                while (true) if (process.HasExited) break;
                if (process.ExitCode != 0)
                {
                    new OMCLLog("检测到Minecraft退出异常，分析已开始！", OMCLExceptionClass.DLL, OMCLExceptionType.Message);
                    string[] files;
                    try
                    {
                        if (!IsIsolation) files = Directory.GetFiles(Dir + @"\.minecraft\crash-reports", "crash-????-??-??_??.??.??-??????.txt");
                        else files = Directory.GetFiles(Dir + @"\.minecraft\versions\" + version + @"\crash-reports", "crash-????-??-??_??.??.??-??????.txt");
                    }
                    catch
                    {
                        return CrashMessageGet(null, process.ExitCode, version);
                    }
                    if (files == null) return CrashMessageGet(null, process.ExitCode, version);
                    else return CrashMessageGet(files.Last(), process.ExitCode, version);
                }
                return null;
            }
            catch (Exception e)
            {
                new OMCLLog("[Tools_WaitMinecraft]出现错误：" + @e.Message, OMCLExceptionClass.DLL, OMCLExceptionType.Error);
                throw;
            }
        }
        private static CrashMessage CrashMessageGet(string reportPath, int ExitCode, string version)
        {
            try
            {
                CrashMessage crashMessage = new CrashMessage();
                crashMessage.VersionName = version;
                crashMessage.ExitCode = ExitCode;
                if (ExitCode == 1)
                {
                    new OMCLLog("[CrashMessageGet]检测到Minecraft退出代码为1，不会产生崩溃日志，断定为：Minecraft被强制终止！", OMCLExceptionClass.DLL, OMCLExceptionType.Message);
                    crashMessage.Message = "您的Minecraft可能被强制停止！";
                    crashMessage.Solution = "1、请检查是否有杀毒软件杀掉了Minecraft的进程。\n" +
                                            "2、请检查您是否使用 任务管理器 结束了Minecraft的进程。\n" +
                                            "3、请检查其他启动器是否能够正常启动该版本。如果能，这可能是OMCL的问题，请将该问题详细地汇报给OMCL的开发者。";
                    new OMCLLog("[CrashMessageGet]结果：\n" + crashMessage.Message + "\n\n可能的解决方法：\n" + crashMessage.Solution + "\n\nMinecraft退出代码：" + crashMessage.ExitCode + "\n版本名称：" + crashMessage.VersionName + "。", OMCLExceptionClass.DLL, OMCLExceptionType.Message);
                    return crashMessage;
                }
                if (reportPath == null)
                {
                    new OMCLLog("[CrashMessageGet]找不到合适的Minecraft崩溃报告！", OMCLExceptionClass.DLL, OMCLExceptionType.Message);
                    crashMessage.Message = "未找到任何可能的原因，这次崩溃可能只是MC莫名抽风了而已！";
                    crashMessage.Solution = "请尝试重启该Minecraft版本，一般可以直接解决。\n如果重启后依然崩溃，可能是您的版本出现了问题或这是OMCL问题。";
                    new OMCLLog("[CrashMessageGet]结果：\n" + crashMessage.Message + "\n\n可能的解决方法：\n" + crashMessage.Solution + "\n\nMinecraft退出代码：" + crashMessage.ExitCode + "\n版本名称：" + crashMessage.VersionName + "。", OMCLExceptionClass.DLL, OMCLExceptionType.Message);
                    return crashMessage;
                }
                new OMCLLog("[CrashMessageGet]查找到 " + reportPath + " 作为Minecraft崩溃报告！", OMCLExceptionClass.DLL, OMCLExceptionType.Message);
                string path = Regex.Replace(reportPath, @"\\crash-reports\\crash(.*).txt", "") + @"\mods";
                string[] lines = File.ReadLines(reportPath).ToArray();
                List<string> GetList = new List<string>();
                List<ModInfo> mods = new List<ModInfo>();
                bool IsFinded = false;
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Contains("debug crash"))
                    {
                        new OMCLLog("[CrashMessageGet]检测到Minecraft崩溃日志中有关键词：debug crash，断定为：手动触发的F3+C的调试崩溃！", OMCLExceptionClass.DLL, OMCLExceptionType.Message);
                        crashMessage.Message = "嗯……这是您自己造成的崩溃！";
                        crashMessage.Solution = "请不要闲着没事（划掉）\n请尝试重启您的游戏，且不要再按下F3+C！";
                        new OMCLLog("[CrashMessageGet]结果：\n" + crashMessage.Message + "\n\n可能的解决方法：\n" + crashMessage.Solution + "\n\nMinecraft退出代码：" + crashMessage.ExitCode + "\n版本名称：" + crashMessage.VersionName + "。", OMCLExceptionClass.DLL, OMCLExceptionType.Message);
                        return crashMessage;
                    }
                    if (lines[i].Contains("\tat "))
                    {
                        string[] temp = Regex.Replace(lines[i], @"\((.*)\)", "").Replace("\tat ", "").Split('.');
                        GetList.Add(temp[0] + '\\' + temp[1]);
                    }
                    else if (lines[i].Contains("\t| "))
                    {
                        List<string> temp = lines[i].Replace(" ", "").Replace("\t", "").Split('|').ToList();
                        temp = new HashSet<string>(temp).ToList();
                        temp.Remove("");
                        if (temp[0] == "State") continue;
                        if (temp[3] == "minecraft.jar") continue;
                        if (temp[3].Contains("forge")) continue;
                        if (temp[0].Contains("E"))
                        {
                            new OMCLLog("[CrashMessageGet]查找到有模组加载时出现状态“E”，断定为：该模组加载时错误，是无效mod！", OMCLExceptionClass.DLL, OMCLExceptionType.Message);
                            crashMessage.Message = "找到可能无效的mod文件！";
                            crashMessage.Solution = "请尝试禁用或删除 " + path + @"\" + temp[3] + " mod，并尝试重启Minecraft！";
                            new OMCLLog("[CrashMessageGet]结果：\n" + crashMessage.Message + "\n\n可能的解决方法：\n" + crashMessage.Solution + "\n\nMinecraft退出代码：" + crashMessage.ExitCode + "\n版本名称：" + crashMessage.VersionName + "。", OMCLExceptionClass.DLL, OMCLExceptionType.Message);
                            return crashMessage;
                        }
                        mods.Add(new ModInfo
                        {
                            IdNamespace = temp[1],
                            FileName = temp[3],
                        });
                        IsFinded = true;
                    }
                    else if (IsFinded) break;
                }
                for (int i = 0; i < GetList.Count; i++)
                {
                    for (int j = 0; j < mods.Count; j++)
                    {
                        if (GetList[i].Contains(mods[j].IdNamespace))
                        {
                            new OMCLLog("[CrashMessageGet]查找到堆栈中的关键模组Id： " + mods[j].IdNamespace + " ，断定为：该模组导致Minecraft崩溃！", OMCLExceptionClass.DLL, OMCLExceptionType.Message);
                            crashMessage.Message = "找到可能导致Minecraft崩溃的mod文件！";
                            crashMessage.Solution = "请尝试禁用或删除 " + path + @"\" + mods[j].FileName + " mod，并尝试重启Minecraft！";
                            new OMCLLog("[CrashMessageGet]结果：\n" + crashMessage.Message + "\n\n可能的解决方法：\n" + crashMessage.Solution + "\n\nMinecraft退出代码：" + crashMessage.ExitCode + "\n版本名称：" + crashMessage.VersionName + "。", OMCLExceptionClass.DLL, OMCLExceptionType.Message);
                            return crashMessage;
                        }
                    }
                }
                crashMessage.Message = "未找到任何可能的原因，这次崩溃可能只是MC莫名抽风了而已！";
                crashMessage.Solution = "请尝试重启该Minecraft版本，一般可以直接解决。\n如果重启后依然崩溃，可能是您的版本出现了问题或这是OMCL问题。";
                new OMCLLog("[CrashMessageGet]结果：\n" + crashMessage.Message + "\n\n可能的解决方法：\n" + crashMessage.Solution + "\n\nMinecraft退出代码：" + crashMessage.ExitCode + "\n版本名称：" + crashMessage.VersionName + "。", OMCLExceptionClass.DLL, OMCLExceptionType.Message);
                return crashMessage;
            }
            catch (Exception e)
            {
                new OMCLLog("[Tools_CrashMessageGet]分析崩溃时出现错误：" + @e.Message, OMCLExceptionClass.DLL, OMCLExceptionType.Error);
                CrashMessage crashMessage = new CrashMessage();
                crashMessage.VersionName = version;
                crashMessage.ExitCode = ExitCode;
                crashMessage.Message = "未找到任何可能的原因，且崩溃分析运行时出现错误！";
                crashMessage.Solution = "请尝试重启该Minecraft版本。\n\nOMCL错误：" + @e.Message;
                return crashMessage;
            }
        }
    }
    public class GetLogin
    {
        public static LoginResult MicrosoftLogin(bool IsAuto = false)
        {
            try
            {
                return Login.MicrosoftLogin.MicrosoftLogin.Login(IsAuto);
            }
            catch
            {
                new OMCLLog("[Tools_GetLogin_MicrosoftLogin]登录失败！", OMCLExceptionClass.DLL, OMCLExceptionType.Error);
                throw;
            }
        }
    }
}
