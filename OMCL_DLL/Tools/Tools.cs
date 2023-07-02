using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OMCL_DLL.Tools.Download;
using OMCL_DLL.Tools.Login.MicrosoftLogin;
using OMCL_DLL.Tools.Login.Result;
using OMCL_DLL.Tools.Login.Web;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Path = System.IO.Path;

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
    public enum DownloadSource
    {
        BMCLAPI, MCBBS,
    }
    public enum JavaType
    {
        jre, jdk, unknown
    }
    public class JavaComp : IComparer<JavaVersion>
    {
        public int Compare(JavaVersion a, JavaVersion b)
        {
            return a.type.CompareTo(b.type);
        }
    }
    public class Tools
    {
        public static string DownloadMinecraftFileUrl = "https://bmclapi2.bangbang93.com/";
        private static readonly string OMCLver = "0.0.0.1";
        public static string Dir = Directory.GetCurrentDirectory();
        public static List<Task> task = new List<Task>();
        public static string NativesPath = "";
        public static List<string> LaunchNoFile = new List<string>();
        public static List<string> LaunchNoFileNatives = new List<string>();
        /*
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
        */
        /// <summary>
        /// 解压一个zip文件
        /// </summary>
        /// <param name="file">zip文件的位置</param>
        /// <param name="dir">要将zip文件解压到的目录（必须指向一个文件夹）</param>
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
                new OMCLLog("[Tools_UnZipFile]解压 " + file + " 时出现错误：" + @e.Message, OMCLExceptionClass.DLL, OMCLExceptionType.Error);
                throw new Exception("解压 " + file + " 时出现错误！");
            }
        }
        /// <summary>
        /// 提供高级、模糊、高效搜索文件夹的方法（递归、Task方式异步）。
        /// </summary>
        /// <param name="path">要查找的文件夹所在的根目录
        /// 如C:\</param>
        /// <param name="filename">要查找的文件夹中有的文件
        /// 如java.exe</param>
        /// <param name="onlysearches">仅在根目录中的哪个目录查找（筛选二级目录）
        /// 如：Program Files</param>
        /// <param name="pathins">要查找的文件夹的目录中包含的元素（或）
        /// 如：jre，jdk，java</param>
        /// <param name="pathsames">与“pathins”基本相同，但是要求同时存在（与）
        /// 如：bin，与“pathins”结合后得到三种可能的方法（条件）：jre+bin，jdk+bin，java+bin</param>
        /// <param name="result">结果列表，要求必须提供一个正确的FindOutDir类型的List</param>
        public static void SearchDirTask(string path, string filename, string[] onlysearches, string[] pathins, string[] pathsames, List<FindOutDir> result)
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
                            task.Add(Task.Run(() => SearchDirTask(dir, filename, onlysearches, pathins, pathsames, result)));
                        }
                    }
                    catch { }
                }
            }
        }
        /// <summary>
        /// 提供高级、模糊、高效搜索文件的方法（递归、Task方式异步）。与“SearchDirTask”基本相同，但最后result增加的是FindOutFile类型的List。
        /// </summary>
        /// <param name="path">要查找的文件所在的根目录
        /// 如C:\</param>
        /// <param name="filename">要查找的文件名
        /// 如java.exe</param>
        /// <param name="onlysearches">仅在根目录中的哪个目录查找（筛选二级目录）
        /// 如：Program Files</param>
        /// <param name="pathins">要查找的文件所在的文件夹的目录中包含的元素（或）
        /// 如：jre，jdk，java</param>
        /// <param name="pathsames">与“pathins”基本相同，但是要求同时存在（与）
        /// 如：bin，与“pathins”结合后得到三种可能的方法（条件）：jre+bin，jdk+bin，java+bin</param>
        /// <param name="result">结果列表，要求必须提供一个正确的FindOutFile类型的List</param>
        public static void SearchFileTask(string path, string filename, string[] onlysearches, string[] pathins, string[] pathsames, List<FindOutFile> result)
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
                            task.Add(Task.Run(() => SearchFileTask(dir, filename, onlysearches, pathins, pathsames, result)));
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
        /// <summary>
        /// 扫描所有盘符中存在的Java（Task异步）
        /// </summary>
        /// <returns>一个数组，里面包含所有的JavaVersion</returns>
        public static async Task<JavaVersion[]> GetJavaListAsync()
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
            foreach (string drive in drives) task.Add(Task.Run(() => SearchDirTask(@drive, "java.exe", onlysearchs, pathins, pathsames, result)));
            while (true)
            {
                if (stopwatch.ElapsedMilliseconds <= 1000)
                {
                    try
                    {
                        await Task.WhenAll(task.ToArray());
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
            JavaVersion[] javas_in_reg = SettingsAndRegistry.GetJavaListInRegistry();
            foreach (JavaVersion java in javas_in_reg)
            {
                javas.Add(java);
            }
            List<JavaVersion> new_javas = new List<JavaVersion>();
            for (int i = 0; i < javas.Count; i++)
            {
                bool IsOK = true;
                for (int j = 0; j < new_javas.Count; j++)
                {
                    if (javas[i].path == new_javas[j].path)
                    {
                        IsOK = false;
                        break;
                    }
                }
                if (IsOK)
                {
                    new_javas.Add(javas[i]);
                }
            }
            new_javas.Sort(new JavaComp());
            new OMCLLog("[Tools_GetJavaListAsync]耗时：" + (double)stopwatch.ElapsedMilliseconds / 1000 + " s，找到Java " + new_javas.Count + " 个！", OMCLExceptionClass.DLL, OMCLExceptionType.Message);
            return new_javas.ToArray();
        }
        /// <summary>
        /// 为一个版本补全Libraries和Natives
        /// </summary>
        /// <param name="version">版本名称</param>
        /// <param name="IsLaunch">是否是启动时地补全文件（如果是，下载失败的文件将自动列入LaunchNoFile或LaunchNoFileNatives，进行启动时跳过操作。如果否，文件下载失败将不会有任何反应，直接跳过下载下一个文件。）</param>
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
                                HttpFile.Start(5, DownloadMinecraftFileUrl + @"optifine/" + temppath2[0] + "/" + temppath1[0].Replace(temppath2[0] + "_", "").Replace("_" + temppath2.Last(), "") + "/" + temppath2.Last(), Dir + @"\.minecraft\libraries\" + ver.libraries[i]);
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
                                HttpFile.Start(5, DownloadMinecraftFileUrl + "libraries/" + ver.libraries[i].Replace('\\', '/'), Dir + @"\.minecraft\libraries\" + ver.libraries[i]);
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
                            HttpFile.Start(5, DownloadMinecraftFileUrl + "libraries/" + ver.natives[i].Replace('\\', '/'), Dir + @"\.minecraft\libraries\" + ver.natives[i]);
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
                throw new Exception("补全文件时出现错误！");
            }
        }
        /// <summary>
        /// 为一个版本补全Assets
        /// </summary>
        /// <param name="version">版本名称</param>
        public static void DownloadMissAsstes(string version)
        {
            Version ver = ReadVersionJson(version);
            bool DownJson = false;
            try
            {
                if (!File.Exists(Dir + @"\.minecraft\assets\indexes\" + ver.assets + ".json"))
                {
                    DownJson = true;
                }
            }
            catch
            {
                DownJson = true;
            }
            string assets;
            if (DownJson)
            {
                JObject versionAssetIndex;
                try
                {
                    versionAssetIndex = JObject.Parse(File.ReadAllText(Dir + @"\.minecraft\versions\" + version + @"\" + version + ".json"));
                }
                catch
                {
                    new OMCLLog("[Tools_DownloadMissAssets]获取版本json时出现错误，请检查您的版本是否正确！", OMCLExceptionClass.DLL, OMCLExceptionType.Error);
                    throw new Exception("[Tools_DownloadMissAssets]获取版本json时出现错误，请检查您的版本是否正确！");
                }
                if (versionAssetIndex.SelectToken("assetIndex.url") == null)
                {
                    string versionjson = Web.Get(DownloadMinecraftFileUrl + "version/" + ver.assets + "/json");
                    if (versionjson == null || versionjson == "" || versionjson == string.Empty)
                    {
                        new OMCLLog("[Tools_DownloadMissAssets]获取版本json时出现错误，请检查您的网络！", OMCLExceptionClass.DLL, OMCLExceptionType.Error);
                        throw new Exception("[Tools_DownloadMissAssets]获取版本json时出现错误，请检查您的网络！");
                    }
                    versionAssetIndex = JObject.Parse(versionjson);
                }
                assets = Web.Get(DownloadMinecraftFileUrl + versionAssetIndex.SelectToken("assetIndex.url").ToString().Replace("https://", "").Replace("http://", "").Replace(versionAssetIndex.SelectToken("assetIndex.url").ToString().Replace("https://", "").Replace("http://", "").Split('/')[0] + '/', ""));
                try
                {
                    Directory.CreateDirectory(Dir + @"\.minecraft\assets\indexes");
                }
                catch { }
                File.WriteAllText(Dir + @"\.minecraft\assets\indexes\" + ver.assets + ".json", assets);
            }
            else
            {
                assets = File.ReadAllText(Dir + @"\.minecraft\assets\indexes\" + ver.assets + ".json");
            }
            JObject o = JObject.Parse(assets).Value<JObject>("objects");
            JObject json = (JObject)JsonConvert.DeserializeObject(o.ToString());
            foreach (JProperty property in o.Properties())
            {
                string hash = json[property.Name]["hash"].ToString();
                if (!File.Exists(Dir + @"\.minecraft\assets\objects\" + hash[0] + hash[1] + '\\' + hash))
                {
                    try
                    {
                        HttpFile.Start(50, DownloadMinecraftFileUrl + "assets/" + hash[0] + hash[1] + '/' + hash, Dir + @"\.minecraft\assets\objects\" + hash[0] + hash[1] + '\\' + hash);
                    }
                    catch
                    {
                        try
                        {
                            HttpFile.Start(50, DownloadMinecraftFileUrl + "assets/" + hash[0] + hash[1] + '/' + hash, Dir + @"\.minecraft\assets\objects\" + hash[0] + hash[1] + '\\' + hash);
                        }
                        catch (Exception e)
                        {
                            new OMCLLog("[Tools_DownloadMissAssets]下载文件 " + hash + " 时出现错误！" + @e.Message, OMCLExceptionClass.DLL, OMCLExceptionType.Error);
                            throw new Exception("[Tools_DownloadMissAssets]下载文件 " + hash + " 时出现错误！");
                        }
                    }
                }
            }
        }
        /// <summary>
        /// 更改Natives路径（注意：如果更改Natives的路径，OMCL将不会再帮您解压Natives库，因此，如果该文件夹不存在或该文件夹中的Natives文件不完整，将导致您的Minecraft无法启动）
        /// </summary>
        /// <param name="path">一个路径，指向一个文件夹，其中包含游戏运行所需要的Natives</param>
        public static void ChangeNativesPath(string path)
        {
            if (path == null || path == "" || path == string.Empty) NativesPath = "";
            else if (Directory.Exists(path) && (Directory.GetFiles(path, "*.dll", SearchOption.TopDirectoryOnly).Length == 0)) NativesPath = path;
            else NativesPath = "";
        }
        /// <summary>
        /// 更改.minecraft文件夹的路径
        /// </summary>
        /// <param name="path">一个路径，指向一个文件夹，其中包含assets、libraries及versions子文件夹</param>
        public static void ChangeMinecraftPath(string path)
        {
            if (path == null || path == "" || path == string.Empty) Dir = Directory.GetCurrentDirectory();
            else Dir = path;
        }
        /// <summary>
        /// 更改下载源
        /// </summary>
        /// <param name="downloadSource">下载源，有BMCLAPI和MCBBS</param>
        public static void ChangeDownloadSource(DownloadSource downloadSource)
        {
            if (downloadSource == DownloadSource.BMCLAPI) DownloadMinecraftFileUrl = "https://bmclapi2.bangbang93.com/";
            else DownloadMinecraftFileUrl = "https://download.mcbbs.net/";
        }
        /// <summary>
        /// 获取当前.minecraft文件夹中所有的版本
        /// </summary>
        /// <returns>一个string数组，包含所有的版本名</returns>
        public static string[] GetVersionList()
        {
            try
            {
                List<string> result = new List<string>();
                List<string> paths = Directory.GetDirectories(Dir + @"\.minecraft\versions", "*", SearchOption.TopDirectoryOnly).ToList();
                for (int i = 0; i < paths.Count; i++)
                {
                    paths[i] = paths[i].Replace(Dir + @"\.minecraft\versions\", "");
                }
                foreach (string path in paths)
                {
                    if (File.Exists(Dir + @"\.minecraft\versions\" + path + @"\" + path + ".jar") && File.Exists(Dir + @"\.minecraft\versions\" + path + @"\" + path + ".json"))
                    {
                        result.Add(path);
                    }
                }
                return result.ToArray();
            }
            catch (Exception e)
            {
                new OMCLLog("[Tools_GetVersionList]错误：" + @e.Message, OMCLExceptionClass.DLL, OMCLExceptionType.Error);
                throw new Exception("获取Java列表时出现错误！");
            }
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
                    if (path.Contains("@"))
                    {
                        path = path.Replace(':', '\\');
                        string[] t = path.Split('\\');
                        string[] a = t.Last().Split('@');
                        t[0] = t.First().Replace('.', '\\');
                        path = "";
                        for (int j = 0; j < t.Length - 1; j++)
                        {
                            path += t[j] + '\\';
                        }
                        path += a[0] + "\\" + t[t.Length-2] + '-' + a[0] + '.' + a[1];
                        Path.Add(path);
                        continue;
                    }
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
                    if (path.Contains("universal"))
                    {
                        Path.Add(path.Replace("\\universal","") + paths[paths.Length - 3] + @"-" + paths[paths.Length - 2] + @"-" + paths[paths.Length - 1] + ".jar");
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
                JArray argumentso = (JArray)o.SelectToken("arguments.game");
                bool IsVaule = false;
                string name = "";
                if (argumentso.Count == 0)
                {
                    string[] minecraftArguments = ((string)o.SelectToken("minecraftArguments")).Split(' ');
                    if (minecraftArguments[0].Contains("--"))
                    {
                        for (int j = 1; j < minecraftArguments.Length; j += 2)
                        {
                            string tmp = minecraftArguments[j - 1];
                            string value = minecraftArguments[j];
                            arguments.Add(new Argument() { name = tmp, value = value });
                        }
                    }
                    else
                    {
                        for (int j = 0; j < minecraftArguments.Length; j++)
                        {
                            string tmp = null;
                            string value = minecraftArguments[j];
                            arguments.Add(new Argument() { name = tmp, value = value });
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < argumentso.Count; i++)
                    {
                        try
                        {
                            string vaule = (string)o.SelectToken("arguments.game[" + i + "]");
                            if (IsVaule)
                            {
                                IsVaule = false;
                                arguments.Add(new Argument() { name = name, value = vaule });
                            }
                            else
                            {
                                IsVaule = true;
                                name = vaule;
                            }
                        }
                        catch { }
                    }
                }
                /*for (int i = 1; i <= 999; i += 2)
                {
                    Argument argument;
                    try
                    {
                        string name = (string)o.SelectToken("arguments.game[" + (i - 1) + "]");
                        string value = (string)o.SelectToken("arguments.game[" + i + "]");
                        if (!name.Contains("--"))
                        {
                            name = (string)o.SelectToken("arguments.game[" + (i - 2) + "]");
                            value = (string)o.SelectToken("arguments.game[" + (i - 1) + "]");
                        }
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
                    catch { }
                }*/
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
                JArray jvms = (JArray)o.SelectToken("arguments.jvm");
                List<string> jvms_List = new List<string>();
                string jvm = "";
                for (int i = 0; i < jvms.Count; i++)
                {
                    try
                    {
                        string tjvm_1 = (string)o.SelectToken("arguments.jvm[" + i + "]");
                        if (tjvm_1 == "-Djava.library.path=${natives_directory}") continue;
                        else if (tjvm_1 == "-Dminecraft.launcher.brand=${launcher_name}") continue;
                        else if (tjvm_1 == "-Dminecraft.launcher.version=${launcher_version}") continue;
                        else if (tjvm_1 == "-cp") continue;
                        else if (tjvm_1 == "${classpath}") continue;
                        jvms_List.Add(tjvm_1);
                    }
                    catch { }
                    
                }
                for (int i = 0;i < jvms_List.Count;i++)
                {
                    jvm += jvms_List[i];
                    if (i != (jvms_List.Count - 1)) jvm += ' ';
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
                    jvm = jvm,
                };
                Natives_List.Clear();
                Path.Clear();
                return ver;
            }
            catch (Exception e)
            {
                new OMCLLog("[Tools_ReadVersionJson]" + @e.Message, OMCLExceptionClass.DLL, OMCLExceptionType.Error);
                throw new Exception("读取版本Json时出现错误，请检查你的版本是否正常！");
            }
        }

        public class LaunchMinecraft
        {
            private bool IsIsolation = true;
            private int MaxMem = 2048, MinMem = 1024;
            private string jvm = "", otherArguments = "";
            public delegate void MinecraftCrashedDelegate(CrashMessage crashMessage);
            public event MinecraftCrashedDelegate OnMinecraftCrash;
            public Process process = new Process();
            private string version = "";
            /// <summary>
            /// 使用离线登录启动一个Minecraft版本
            /// </summary>
            /// <param name="java">java.exe或javaw.exe的路径</param>
            /// <param name="version">版本名称</param>
            /// <param name="playerName">离线玩家名称</param>
            public void LaunchGame(string java, string version, string playerName)
            {
                try
                {
                    OfflineLoginResult result = GetLogin.OfflineLogin(playerName);
                    LaunchGame(java, version, result.name, result.uuid, result.uuid);
                    return;
                }
                catch (Exception e)
                {
                    new OMCLLog("[Tools_LaunchGame]" + @e.Message, OMCLExceptionClass.DLL, OMCLExceptionType.Error);
                    throw new Exception("启动失败！");
                }
            }
            /// <summary>
            /// 使用AuthlibInjector登录启动一个Minecraft版本
            /// </summary>
            /// <param name="java">java.exe或javaw.exe的路径</param>
            /// <param name="version">版本名称</param>
            /// <param name="login">提供一个AuthlibInjectorLoginResult用于登录</param>
            /// <param name="result">返回一个新的AuthlibInjectorLoginResult</param>
            public void LaunchGame(string java, string version, AuthlibInjectorLoginResult login, out AuthlibInjectorLoginResult result, int user = -1)
            {
                try
                {
                    result = GetLogin.RefreshAuthlibInjectorLogin(login);
                    string playerName;
                    string uuid;
                    string token = result.access_token; ;
                    if (user == -1)
                    {
                        playerName = result.name;
                        uuid = result.uuid;
                    }
                    else
                    {
                        playerName = result.users[user].name;
                        uuid = result.users[user].uuid;
                    }
                    try
                    {
                        if (!File.Exists(Dir + @"\OMCL\Login\authlib-injector-1.2.1.jar")) HttpFile.Start(100, "https://bmclapi2.bangbang93.com/mirrors/authlib-injector/artifact/49/authlib-injector-1.2.1.jar", Dir + @"\OMCL\Login\authlib-injector-1.2.1.jar");
                    }
                    catch
                    {
                        try
                        {
                            Directory.CreateDirectory(Dir + @"\OMCL\Login");
                            HttpFile.Start(100, "https://bmclapi2.bangbang93.com/mirrors/authlib-injector/artifact/49/authlib-injector-1.2.1.jar", Dir + @"\OMCL\Login\authlib-injector-1.2.1.jar");
                        }
                        catch
                        {
                            new OMCLLog("[Tools_LaunchGame]错误：下载authlib-injector-1.2.1.jar时出现错误，请检查你的网络是否正常！", OMCLExceptionClass.DLL, OMCLExceptionType.Error);
                            throw new Exception("启动失败，下载authlib-injector-1.2.1.jar时出现错误，请检查你的网络是否正常！");
                        }
                    }
                    string Base64;
                    try
                    {
                        Base64 = Convert.ToBase64String(Encoding.Default.GetBytes(Web.Get(login.server)));
                    }
                    catch
                    {
                        new OMCLLog("[Tools_LaunchGame]错误：获取信息时出现错误，请检查你的网络是否正常！", OMCLExceptionClass.DLL, OMCLExceptionType.Error);
                        throw new Exception("启动失败，获取信息时出现错误，请检查你的网络是否正常！");
                    }
                    try
                    {
                        if (jvm.Last() == ' ') jvm += "-javaagent:" + Dir + @"\OMCL\Login\authlib-injector-1.2.1.jar" + "=" + login.server + " -Dauthlibinjector.yggdrasil.prefetched=" + Base64;
                        else jvm += " -javaagent:" + Dir + @"\OMCL\Login\authlib-injector-1.2.1.jar" + "=" + login.server + " -Dauthlibinjector.yggdrasil.prefetched=" + Base64;
                    }
                    catch
                    {
                        jvm += "-javaagent:" + Dir + @"\OMCL\Login\authlib-injector-1.2.1.jar" + "=" + login.server + " -Dauthlibinjector.yggdrasil.prefetched=" + Base64;
                    }
                    LaunchGame(java, version, playerName, uuid, token);
                    return;
                }
                catch (Exception e)
                {
                    new OMCLLog("[Tools_LaunchGame]" + @e.Message, OMCLExceptionClass.DLL, OMCLExceptionType.Error);
                    throw new Exception("启动失败！");
                }
            }
            /// <summary>
            /// 使用统一通行证登录启动一个Minecraft版本
            /// </summary>
            /// <param name="java">java.exe或javaw.exe的路径</param>
            /// <param name="version">版本名称</param>
            /// <param name="login">提供一个UnifiedPassLoginResult用于登录</param>
            /// <param name="result">返回一个新的UnifiedPassLoginResult</param>
            public void LaunchGame(string java, string version, UnifiedPassLoginResult login, out UnifiedPassLoginResult result)
            {
                try
                {
                    result = GetLogin.RefreshUnifiedPassLogin(login.serverid, login.client_token, login.access_token);
                    string playerName;
                    string uuid;
                    string token;
                    playerName = result.name;
                    uuid = result.uuid;
                    token = result.access_token;
                    try
                    {
                        if (!File.Exists(Dir + @"\OMCL\Login\nide8auth.jar")) HttpFile.Start(100, "https://login.mc-user.com:233/index/jar", Dir + @"\OMCL\Login\nide8auth.jar");
                    }
                    catch
                    {
                        try
                        {
                            Directory.CreateDirectory(Dir + @"\OMCL\Login");
                            HttpFile.Start(100, "https://login.mc-user.com:233/index/jar", Dir + @"\OMCL\Login\nide8auth.jar");
                        }
                        catch
                        {
                            new OMCLLog("[Tools_LaunchGame]启动失败，下载nide8auth.jar文件时出现错误，请检查您的网络！", OMCLExceptionClass.DLL, OMCLExceptionType.Error);
                            throw new Exception("启动失败，下载nide8auth.jar文件时出现错误，请检查您的网络！");
                        }
                    }
                    try
                    {
                        if (jvm.Last() == ' ') jvm += "-javaagent:" + Dir + @"\OMCL\Login\nide8auth.jar" + "=" + login.serverid + " -Dnide8auth.client=true";
                        else jvm += " -javaagent:" + Dir + @"\OMCL\Login\nide8auth.jar" + "=" + login.serverid + " -Dnide8auth.client=true";
                    }
                    catch
                    {
                        jvm = "-javaagent:" + Dir + @"\OMCL\Login\nide8auth.jar" + "=" + result.serverid + " -Dnide8auth.client=true";
                    }
                    LaunchGame(java, version, playerName, uuid, token); 
                    return;
                }
                catch (Exception e)
                {
                    new OMCLLog("[Tools_LaunchGame]" + @e.Message, OMCLExceptionClass.DLL, OMCLExceptionType.Error);
                    throw new Exception("启动失败！");
                }
            }
            /// <summary>
            /// 使用Microsoft登录启动一个Minecraft版本
            /// </summary>
            /// <param name="java">java.exe或javaw.exe的路径</param>
            /// <param name="version">版本名称</param>
            /// <param name="login">提供一个MicrosoftLoginResult以进行登录</param>
            /// <param name="result">返回一个新的MicrosoftLoginResult</param>
            public void LaunchGame(string java, string version, MicrosoftLoginResult login, out MicrosoftLoginResult result)
            {
                try
                {
                    result = MicrosoftLogin.RefreshLogin(login);
                    string playerName;
                    string uuid;
                    string token;
                    playerName = result.name;
                    uuid = result.uuid;
                    token = result.access_token;
                    LaunchGame(java, version, playerName, uuid, token);
                    return;
                }
                catch (Exception e)
                {
                    new OMCLLog("[Tools_LaunchGame]" + @e.Message, OMCLExceptionClass.DLL, OMCLExceptionType.Error);
                    throw new Exception("启动失败！");
                }
            }
            /// <summary>
            /// 使用Mojang登录启动一个Minecraft版本
            /// </summary>
            /// <param name="java">java.exe或javaw.exe的路径</param>
            /// <param name="version">版本名称</param>
            /// <param name="login">提供一个MojangLoginResult以进行登录</param>
            /// <param name="result">返回一个新的MojangLoginResult</param>
            public void LaunchGame(string java, string version, MojangLoginResult login, out MojangLoginResult result)
            {
                try
                {
                    result = GetLogin.RefreshMojangLogin(login.access_token);
                    string playerName;
                    string uuid;
                    string token;
                    playerName = result.name;
                    uuid = result.uuid;
                    token = result.access_token;
                    LaunchGame(java, version, playerName, uuid, token);
                    return;
                }
                catch (Exception e)
                {
                    new OMCLLog("[Tools_LaunchGame]" + @e.Message, OMCLExceptionClass.DLL, OMCLExceptionType.Error);
                    throw new Exception("启动失败！");
                }
            }
            /// <summary>
            /// 启动一个Minecraft版本
            /// </summary>
            /// <param name="java">java.exe或javaw.exe的路径</param>
            /// <param name="version">版本名称</param>
            /// <param name="playerName">玩家名称</param>
            /// <param name="uuid">玩家uuid</param>
            /// <param name="token">玩家登录的access_token</param>
            public void LaunchGame(string java, string version, string playerName, string uuid, string token)
            {
                try
                {
                    this.version = version;
                    if (!File.Exists(Dir + @"\.minecraft\versions\" + version + @"\" + version + ".jar") || !File.Exists(Dir + @"\.minecraft\versions\" + version + @"\" + version + ".json"))
                    {
                        new OMCLLog("[Tools_LaunchGame]版本 " + version + " 有问题，请重新下载！", OMCLExceptionClass.DLL, OMCLExceptionType.Error);
                        throw new Exception("版本有问题！请重新下载！");
                    }
                    LaunchNoFile = new List<string>();
                    LaunchNoFileNatives = new List<string>();
                    Version ver = ReadVersionJson(version);
                    string GameCMD = "", ClientPath = Dir + @"\.minecraft\versions\" + version + @"\" + version + ".jar";
                    DownloadMissFiles(version);
                    string _NativesPath;
                    if (NativesPath == "") _NativesPath = UnzipNatives(version); else _NativesPath = NativesPath;
                    GameCMD += "-server -Dminecraft.client.jar=\"{Client_Path}\" -Xverify:none -XX:+UseParallelOldGC -XX:MaxInlineSize=420 -XX:HeapDumpPath=MojangTricksIntelDriversForPerformance_javaw.exe_minecraft.exe.heapdump -Xms{Min_Mem}m -Xmx{Max_Mem}m -Xmn256m -Djava.library.path=\"{Natives_Path}\" -Dminecraft.launcher.brand=OMCL -Dminecraft.launcher.version={OMCL_Version} ".Replace("{OMCL_Version}", OMCLver).Replace("{Natives_Path}", _NativesPath).Replace("{Max_Mem}", MaxMem.ToString()).Replace("{Min_Mem}", MinMem.ToString()).Replace("{Client_Path}", ClientPath);
                    if (ver.jvm != "") GameCMD += ver.jvm + " ";
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
                            case "${auth_session}":
                                GameCMD += token + ' ';
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
                    new OMCLLog("[Tools_LaunchGame]启动脚本如下：" + ("\"" + java + "\" " + GameCMD).Replace(token, "access_token隐藏").Replace(uuid, "uuid隐藏").Replace(playerName, "玩家名称隐藏") + "。", OMCLExceptionClass.DLL, OMCLExceptionType.Message);
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
                    process.StartInfo.Arguments = GameCMD;
                    process.StartInfo.FileName = java;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.CreateNoWindow = true;
                    if (!IsIsolation) process.StartInfo.WorkingDirectory = Dir + @"\.minecraft";
                    else process.StartInfo.WorkingDirectory = Dir + @"\.minecraft\versions\" + version;
                    process.Exited += Process_Exited;
                    process.Start();
                    return;
                }
                catch (Exception e)
                {
                    new OMCLLog("[Tools_LaunchGame]" + @e.Message, OMCLExceptionClass.DLL, OMCLExceptionType.Error);
                    throw new Exception("启动时出现错误！");
                }
            }
            private void Process_Exited(object sender, EventArgs ex)
            {
                try
                {
                    CrashMessage CrashMessage = new CrashMessage();
                    new OMCLLog("[Tools_WaitMinecraft]当前Minecraft版本 " + version + " 已退出，检查中……", OMCLExceptionClass.DLL, OMCLExceptionType.Message);
                    if (process.ExitCode != 0)
                    {
                        new OMCLLog("检测到Minecraft退出异常（退出代码不为0），分析已开始！", OMCLExceptionClass.DLL, OMCLExceptionType.Message);
                        string[] files;
                        try
                        {
                            if (!IsIsolation) files = Directory.GetFiles(Dir + @"\.minecraft\crash-reports", "crash-????-??-??_??.??.??-??????.txt");
                            else files = Directory.GetFiles(Dir + @"\.minecraft\versions\" + version + @"\crash-reports", "crash-????-??-??_??.??.??-??????.txt");
                        }
                        catch
                        {
                            CrashMessage = CrashMessageGet(null, process.ExitCode, version);
                            try
                            {
                                OnMinecraftCrash(CrashMessage);
                            }
                            catch { }
                            return;
                        }
                        if (files == null) CrashMessage = CrashMessageGet(null, process.ExitCode, version);
                        else CrashMessage = CrashMessageGet(files.Last(), process.ExitCode, version);
                        OnMinecraftCrash(CrashMessage);
                        return;
                    }
                    new OMCLLog("[Tools_WaitMinecraft]版本 " + version + " 退出正常，返回代码：0，将不做任何分析处理！", OMCLExceptionClass.DLL, OMCLExceptionType.Message);
                    CrashMessage = null;
                    return;
                }
                catch (Exception e)
                {
                    new OMCLLog("[Tools_WaitMinecraft]出现错误：" + @e.Message, OMCLExceptionClass.DLL, OMCLExceptionType.Error);
                    throw new Exception("日志检测出现错误！");
                }
            }
            private string UnzipNatives(string version)
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
                    throw new Exception("解压Natives文件时出现问题！");
                }
            }
            private CrashMessage CrashMessageGet(string reportPath, int ExitCode, string version)
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
                    new OMCLLog("[CrashMessageGet]查找到 " + path + " 作为模组文件放置路径！", OMCLExceptionClass.DLL, OMCLExceptionType.Message);
                    string[] lines = File.ReadLines(reportPath).ToArray();
                    List<string> GetList = new List<string>();
                    List<ModInfo> mods = new List<ModInfo>();
                    bool IsFinded = false;
                    for (int i = 0; i < lines.Length; i++)
                    {
                        if (lines[i].Contains("debug crash"))
                        {
                            new OMCLLog("[CrashMessageGet]检测到Minecraft崩溃日志中的第" + (i + 1).ToString() + "行有关键词：debug crash，断定为：手动触发的F3+C的调试崩溃！", OMCLExceptionClass.DLL, OMCLExceptionType.Message);
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
                            if (temp[3] == "minecraft.jar")
                            {
                                new OMCLLog("[CrashMessageGet]找到资源文件为 minecraft.jar ，默认跳过！", OMCLExceptionClass.DLL, OMCLExceptionType.Message);
                                continue;
                            }
                            if (temp[3].Contains("forge"))
                            {
                                new OMCLLog("[CrashMessageGet]找到资源文件名称中含有 forge ，默认跳过！", OMCLExceptionClass.DLL, OMCLExceptionType.Message);
                                continue;
                            }
                            if (temp[0].Contains("E"))
                            {
                                new OMCLLog("[CrashMessageGet]查找到模组 " + path + @"\" + temp[3] + " 加载时出现状态“E”，断定为：该模组加载时错误，是无效mod！", OMCLExceptionClass.DLL, OMCLExceptionType.Message);
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
                    for (int i = 0; i < mods.Count; i++)
                    {
                        new OMCLLog("[CrashMessageGet]查找到以下模组关键Id： " + mods[i].IdNamespace + " 。", OMCLExceptionClass.DLL, OMCLExceptionType.Message);
                    }
                    for (int i = 0; i < GetList.Count; i++)
                    {
                        for (int j = 0; j < mods.Count; j++)
                        {
                            if (GetList[i].Contains(mods[j].IdNamespace))
                            {
                                new OMCLLog("[CrashMessageGet]查找到堆栈中的关键模组Id： " + mods[j].IdNamespace + " ，断定为：该模组导致Minecraft崩溃！", OMCLExceptionClass.DLL, OMCLExceptionType.Message);
                                new OMCLLog("[CrashMessageGet]匹配成功！ " + GetList[i] + "对" + mods[j] + " 。断定 " + path + @"\" + mods[j].FileName + " 导致Minecraft崩溃！", OMCLExceptionClass.DLL, OMCLExceptionType.Message);
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
    }
    public class SettingsAndRegistry
    {
        /// <summary>
        /// 从注册表中获取已安装的Java列表
        /// </summary>
        /// <returns>一个JavaVersion数组，其中包含所有注册表中登记过的Java的信息</returns>
        public static JavaVersion[] GetJavaListInRegistry()
        {
            RegistryKey key = Environment.Is64BitOperatingSystem ? RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64) : Registry.LocalMachine;
            key = key.OpenSubKey(@"SOFTWARE\JavaSoft", RegistryKeyPermissionCheck.ReadSubTree);
            if (key == null) return null;
            List<JavaVersion> javas = new List<JavaVersion>();
            key = key.OpenSubKey(@"Java Runtime Environment", RegistryKeyPermissionCheck.ReadSubTree);
            if (key != null)
            {
                string[] names = key.GetSubKeyNames();
                foreach (string name in names)
                {
                    RegistryKey temp = key.OpenSubKey(name);
                    if (temp == null) continue;
                    string get = temp.GetValue("JavaHome").ToString() + @"\bin";
                    if (get == null) continue;
                    javas.Add(new JavaVersion
                    {
                        path = get,
                        type = JavaType.jre,
                    });
                }
            }
            key = Environment.Is64BitOperatingSystem ? RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64) : Registry.LocalMachine;
            key = key.OpenSubKey(@"SOFTWARE\JavaSoft");
            key = key.OpenSubKey("JDK");
            if (key != null)
            {
                string[] names = key.GetSubKeyNames();
                foreach (string name in names)
                {
                    RegistryKey temp = key.OpenSubKey(name);
                    if (temp == null) continue;
                    string get = temp.GetValue("JavaHome").ToString() + @"\bin";
                    if (get == null) continue;
                    javas.Add(new JavaVersion
                    {
                        path = get,
                        type = JavaType.jdk,
                    });
                }
            }
            return javas.ToArray();
        }
    }
    public class GetLogin
    {
        public class MicrosoftLogin
        {
            public delegate void GetCodeDelegate(string code);
            public static event GetCodeDelegate oauth2_OnGetCode;
            /// <summary>
            /// （新方法）登录一个Microsoft账号
            /// </summary>
            /// <param name="IsAuto">是否是自动登录，如果是，则将使用本地账号快捷登录，不需要账号和密码。如果否，则不管您是否登录了本地账号，一律要求重新输入账号和密码</param>
            /// <returns>一个MicrosoftLoginResult，其中包含玩家的名字，uuid，refresh_token等信息</returns>
            public static async Task<MicrosoftLoginResult> NewLogin(bool IsAuto = false)
            {
                try
                {
                    return await Login.MicrosoftLogin.MicrosoftLogin.LoginAsync(IsAuto);
                }
                catch (NotAnException e)
                {
                    throw e;
                }
                catch
                {
                    new OMCLLog("[Tools_GetLogin_MicrosoftLogin]登录失败！", OMCLExceptionClass.DLL, OMCLExceptionType.Error);
                    throw new Exception("微软登录时出现错误！请检查您的网络及您是否拥有Minecraft！");
                }
            }
            /// <summary>
            /// （新方法）使用在浏览器中打开的方法登录一个Microsoft账号
            /// </summary>
            /// <param name="url">从用户浏览器中获取的返回url</param>
            /// <returns>一个MicrosoftLoginResult，其中包含玩家的名字，uuid，refresh_token等信息</returns>
            public static MicrosoftLoginResult LoginByWebSite(string url)
            {
                return Login.MicrosoftLogin.MicrosoftLogin.LoginByWebSite(url);
            }
            /// <summary>
            /// （旧方法）使用oauth2的device_code登录一个Microsoft账号
            /// </summary>
            /// <returns>一个MicrosoftLoginResult，其中包含玩家的名字，uuid，refresh_token等信息</returns>
            public static MicrosoftLoginResult DCLogin()
            {
                try
                {
                    MicrosoftLogin2_oauth2.OnGetCode += MicrosoftLogin2_oauth2_OnGetCode;
                    return MicrosoftLogin2_oauth2.Login();
                }
                catch
                {
                    new OMCLLog("[Tools_GetLogin_MicrosoftLogin]登录失败！", OMCLExceptionClass.DLL, OMCLExceptionType.Error);
                    throw new Exception("微软登录时出现错误！请检查您的网络及您是否拥有Minecraft！");
                }
            }
            /// <summary>
            /// 刷新MicrosoftLogin
            /// </summary>
            /// <param name="login">一个MicrosoftLoginResult，其中包含玩家的名字，uuid，refresh_token等信息</param>
            /// <returns>一个新的MicrosoftLoginResult，其中包含刷新过的玩家的名字，uuid，refresh_token等信息</returns>
            public static MicrosoftLoginResult RefreshLogin(MicrosoftLoginResult login)
            {
                return Login.MicrosoftLogin.MicrosoftLogin.RefreshLogin(login);
            }
            private static void MicrosoftLogin2_oauth2_OnGetCode(string code)
            {
                oauth2_OnGetCode(code);
            }
        }
        /// <summary>
        /// 登录mojang账号
        /// </summary>
        /// <param name="user">拥有Minecraft的电子邮箱地址或玩家名称</param>
        /// <param name="password">密码</param>
        /// <returns>一个MojangLoginResult，其中包含玩家的名字，uuid，access_token等信息</returns>
        public static MojangLoginResult MojangLogin(string user, string password)
        {
            JObject o = JObject.Parse(Web.Post("https://authserver.mojang.com/authenticate", "{\"agent\": {\"name\": \"Minecraft\",\"version\": 1},\"username\": \"" + user + "\",\"password\": \"" + password + "\"}", "application/json"));
            if (o.SelectToken("selectedProfile.id") == null || o.SelectToken("selectedProfile.name") == null)
            {
                new OMCLLog("[GetLogin_MojangLogin]失败！该账号可能不拥有Minecraft！", OMCLExceptionClass.DLL, OMCLExceptionType.Error);
                return null;
            }
            return new MojangLoginResult
            {
                access_token = o.SelectToken("accessToken").ToString(),
                uuid = o.SelectToken("selectedProfile.id").ToString(),
                name = o.SelectToken("selectedProfile.name").ToString(),
            };
        }
        /// <summary>
        /// 刷新mojang账号的access_token
        /// </summary>
        /// <param name="access_token">一个从MojangLogin函数或上次调用该函数时的MojangLoginResult中的access_token</param>
        /// <returns>一个MojangLoginResult，其中包含玩家的名字，uuid，access_token等信息</returns>
        public static MojangLoginResult RefreshMojangLogin(string access_token)
        {
            JObject o = JObject.Parse(Web.Post("https://authserver.mojang.com/refresh", "{\"accessToken\": \"" + access_token + "\"}", "application/json"));
            if (o.SelectToken("selectedProfile.id") == null || o.SelectToken("selectedProfile.name") == null)
            {
                new OMCLLog("[GetLogin_MojangLogin]失败！该账号可能不拥有Minecraft！", OMCLExceptionClass.DLL, OMCLExceptionType.Error);
                return null;
            }
            return new MojangLoginResult
            {
                access_token = o.SelectToken("accessToken").ToString(),
                uuid = o.SelectToken("selectedProfile.id").ToString(),
                name = o.SelectToken("selectedProfile.name").ToString(),
            };
        }
        /// <summary>
        /// 登录UnifiedPass账号
        /// </summary>
        /// <param name="serverid">UnifiedPass服务器的id</param>
        /// <param name="user">电子邮箱地址或玩家名称</param>
        /// <param name="password">密码</param>
        /// <returns>一个UnifiedPassLoginResult，其中包含玩家的名字，uuid，access_token等信息</returns>
        public static UnifiedPassLoginResult UnifiedPassLogin(string serverid, string user, string password)
        {
            JObject o = JObject.Parse(Web.Post("https://auth.mc-user.com:233/" + serverid + "/authserver/authenticate", "{\"username\": \"" + user + "\",\"password\": \"" + password + "\"}", "application/json"));
            if (o.SelectToken("selectedProfile.id") == null || o.SelectToken("selectedProfile.name") == null)
            {
                new OMCLLog("[GetLogin_UnifiedPassLogin]失败！该账号可能错误！", OMCLExceptionClass.DLL, OMCLExceptionType.Error);
                return null;
            }
            return new UnifiedPassLoginResult
            {
                access_token = o.SelectToken("accessToken").ToString(),
                uuid = o.SelectToken("selectedProfile.id").ToString(),
                name = o.SelectToken("selectedProfile.name").ToString(),
                client_token = o.SelectToken("clientToken").ToString(),
                serverid = serverid,
            };
        }
        /// <summary>
        /// 刷新UnifiedPass账号的access_token
        /// </summary>
        /// <param name="serverid">UnifiedPass服务器的id</param>
        /// <param name="access_token">一个从UnifiedPassLogin函数或上次调用该函数时的UnifiedPassLoginResult中的access_token</param>
        /// <returns>一个UnifiedPassLoginResult，其中包含玩家的名字，uuid，access_token等信息</returns>
        public static UnifiedPassLoginResult RefreshUnifiedPassLogin(string serverid, string clientToken, string access_token)
        {
            JObject o = JObject.Parse(Web.Post("https://auth.mc-user.com:233/" + serverid + "/authserver/refresh", "{\"accessToken\": \"" + access_token + "\",\"clientToken\": \"" + clientToken + "\"}", "application/json"));
            if (o.SelectToken("selectedProfile.id") == null || o.SelectToken("selectedProfile.name") == null)
            {
                new OMCLLog("[GetLogin_RefreshUnifiedPassLogin]失败！该账号可能错误！", OMCLExceptionClass.DLL, OMCLExceptionType.Error);
                return null;
            }
            return new UnifiedPassLoginResult
            {
                access_token = o.SelectToken("accessToken").ToString(),
                uuid = o.SelectToken("selectedProfile.id").ToString(),
                name = o.SelectToken("selectedProfile.name").ToString(),
                client_token = o.SelectToken("clientToken").ToString(),
                serverid = serverid,
            };
        }
        /// <summary>
        /// 登录AuthlibInjector账号
        /// </summary>
        /// <param name="user">电子邮箱地址或玩家名称</param>
        /// <param name="password">密码</param>
        /// <returns>一个AuthlibInjectorLoginResult，其中包含玩家的名字，uuid，access_token等信息</returns>
        public static AuthlibInjectorLoginResult AuthlibInjectorLogin(string server, string user, string password)
        {
            JObject o = JObject.Parse(Web.Post(server + "/authserver/authenticate", "{\"username\":\"" + user + "\",\"password\":\"" + password + "\",\"agent\":{\"name\":\"Minecraft\",\"version\":1}}", "application/json"));
            if (o.SelectToken("selectedProfile.id") == null || o.SelectToken("selectedProfile.name") == null)
            {
                List<AuthlibInjectorUser> users = new List<AuthlibInjectorUser>();
                JArray array = (JArray)o.SelectToken("availableProfiles");
                for (int i = 0; i < array.Count; i++)
                {
                    users.Add(new AuthlibInjectorUser
                    {
                        uuid = o.SelectToken("availableProfiles[" + i + "].id").ToString(),
                        name = o.SelectToken("availableProfiles[" + i + "].name").ToString(),
                    });
                }
                return new AuthlibInjectorLoginResult
                {
                    access_token = o.SelectToken("accessToken").ToString(),
                    users = users.ToArray(),
                    server = server,
                    name = "",
                    uuid = "",
                };
            }
            else
            {
                List<AuthlibInjectorUser> users = new List<AuthlibInjectorUser>();
                JArray array = (JArray)o.SelectToken("availableProfiles");
                for (int i = 0; i < array.Count; i++)
                {
                    users.Add(new AuthlibInjectorUser
                    {
                        uuid = o.SelectToken("availableProfiles[" + i + "].id").ToString(),
                        name = o.SelectToken("availableProfiles[" + i + "].name").ToString(),
                    });
                }
                return new AuthlibInjectorLoginResult
                {
                    access_token = o.SelectToken("accessToken").ToString(),
                    uuid = o.SelectToken("selectedProfile.id").ToString(),
                    name = o.SelectToken("selectedProfile.name").ToString(),
                    server = server,
                    users = users.ToArray(),
                };
            }
        }
        /// <summary>
        /// 刷新AuthlibInjector账号的access_token
        /// </summary>
        /// <param name="login">一个AuthlibInjectorLoginResult</param>
        /// <returns>一个AuthlibInjectorLoginResult，其中包含玩家的名字，uuid，access_token等信息</returns>
        public static AuthlibInjectorLoginResult RefreshAuthlibInjectorLogin(AuthlibInjectorLoginResult login)
        {
            JObject o = JObject.Parse(Web.Post(login.server + "/authserver/refresh", "{\"accessToken\":\"" + login.access_token + "\"}", "application/json"));
            return new AuthlibInjectorLoginResult
            {
                access_token = o.SelectToken("accessToken").ToString(),
                server = login.server,
                users = login.users,
                name = login.name,
                uuid = login.uuid,
            };
        }
        /// <summary>
        /// 登录一个离线账号
        /// </summary>
        /// <param name="name">离线玩家名称</param>
        /// <returns>一个OfflineLoginResult，其中包含玩家的名字和uuid</returns>
        public static OfflineLoginResult OfflineLogin(string name)
        {
            try
            {
                string result = Web.Get("https://api.mojang.com/users/profiles/minecraft/" + name);
                if (result == "" || result == null || result == string.Empty)
                {
                    return new OfflineLoginResult
                    {
                        name = name,
                        uuid = RandomUUID(),
                    };
                }
                else
                {
                    JObject o = JObject.Parse(result);
                    return new OfflineLoginResult
                    {
                        name = o.SelectToken("name").ToString(),
                        uuid = o.SelectToken("id").ToString(),
                    };
                }
            }
            catch
            {
                return new OfflineLoginResult
                {
                    name = name,
                    uuid = RandomUUID(),
                };
            }
        }
        private static string RandomUUID()
        {
            string result = "";
            char[] canuse = { '1', '2', '3', '4', '5', '6', '7', '8', '9', '0', 'a', 'b', 'c', 'd', 'e', 'f' };
            while (true)
            {
                if (result.Length == 31) break;
                Random random = new Random();
                result += canuse[random.Next(0, 17)];
            }
            return result;
        }
    }
    public class InstallMinecraft
    {
        //不可使用（作废）
        /*public class ForgeInstall
        {
            private static string ForgeDataPathGet(string name)
            {
                string value = name;
                if (value.Contains("[") && value.Contains("]"))
                {
                    if (value.Contains("@"))
                    {
                        string[] arr = value.Split(':');
                        string arr0temp = arr[0].Replace("[", "");
                        string temp = arr0temp.Replace('.', '\\');
                        value = (temp + value.Replace("[" + arr0temp, "")).Replace("]", "").Replace(':', '\\').Replace('@', '.');
                        //value = value.Split(':')[0].Replace('.', '\\') + value.Replace(value.Split(':')[0], "").Replace(':', '\\').Replace('@', '.').Replace("[", "").Replace("]", "");
                    }
                    else
                    {
                        string[] arr = value.Split(':');
                        string arr0temp = arr[0].Replace("[", "");
                        string temp = arr0temp.Replace('.','\\');
                        value = temp + value.Replace("[" + arr0temp, "").Replace("]", "").Replace(':', '\\') + '\\' + arr[arr.Length - 2] + '-' + arr[arr.Length - 1].Replace("]","") + ".jar";
                    }
                    value = Tools.Dir + @"\.minecraft\libraries\" + value;
                }
                else if (value.Contains("/"))
                {
                    value = value.Replace('/', '\\');
                    value = Path.GetTempPath() + @"\OMCL\temp\" + value;
                }
                return value;
            }
            private static string ForgeFilePathGet(string name)
            {
                string path = name;
                string[] temppath = path.Split(':');
                string temp = temppath[0].Replace('.', '\\');
                path = temp + @"\" + path.Replace(temppath[0] + ":", "").Replace(':', '\\') + @"\" + temppath[temppath.Length - 2] + "-" + temppath[temppath.Length - 1] + ".jar";
                return path;
            }
            public class ForgeDATA
            {
                public string name { get; internal set; }
                public string value { get; internal set; }
            }
            public static void InstallForge(string java, string version, string ForgePath)
            {
                try
                {
                    if (Directory.Exists(Path.GetTempPath() + @"\OMCL\temp\")) Directory.Delete(Path.GetTempPath() + @"\OMCL\temp\", true);
                    Directory.CreateDirectory(Path.GetTempPath() + @"\OMCL\temp\");
                }
                catch { }
                Tools.UnZipFile(ForgePath, Path.GetTempPath() + @"\OMCL\temp\");
                JObject o = JObject.Parse(File.ReadAllText(Path.GetTempPath() + @"\OMCL\temp\install_profile.json"));
                JObject data = (JObject)JsonConvert.DeserializeObject(o.Value<JObject>("data").ToString());
                List<ForgeDATA> datas = new List<ForgeDATA>();
                foreach (JProperty property in data.Properties())
                {
                    datas.Add(new ForgeDATA
                    {
                        value = ForgeDataPathGet(data[property.Name]["client"].ToString()),
                        name = property.Name,
                    });
                }
                JArray libraries = (JArray)o.SelectToken("libraries");
                if (!File.Exists(Tools.Dir + @"\.minecraft\versions\" + version + @"\" + version + ".json"))
                {
                    new OMCLLog("[InstallMinecraft_InstallForge]安装Forge时出现错误：找不到版本json，该版本可能有问题！", OMCLExceptionClass.DLL, OMCLExceptionType.Error);
                    throw new Exception("安装Forge时出现错误：找不到版本json，该版本可能有问题！");
                }
                JObject versionjson = JObject.Parse(File.ReadAllText(Tools.Dir + @"\.minecraft\versions\" + version + @"\" + version + ".json"));
                JArray versionlibraries = (JArray)versionjson.SelectToken("libraries");
                versionlibraries.Merge(libraries);
                versionjson.Merge(versionlibraries);
                File.WriteAllText(Tools.Dir + @"\.minecraft\versions\" + version + @"\" + version + ".json", versionjson.ToString());
                JArray processors = (JArray)o.SelectToken("processors");
                for (int i = 0; i < processors.Count; i++)
                {
                    Process process = new Process();
                    JArray sides = (JArray)processors[i]["sides"];
                    if (sides.Count != 0)
                    {
                        if (sides[0].ToString() == "server")
                        {
                            continue;
                        }
                    }
                    string jar = processors[i]["jar"].ToString();
                    string path = ForgeFilePathGet(jar);
                    if (!File.Exists(Tools.Dir + @"\.minecraft\libraries\" + path))
                    {
                        try
                        {
                            HttpFile.Start(5, Tools.DownloadMinecraftFileUrl + "/libraries/" + path.Replace('\\', '/'), Tools.Dir + @"\.minecraft\libraries\" + path);
                        }
                        catch
                        {
                            try
                            {
                                HttpFile.Start(5, Tools.DownloadMinecraftFileUrl + "/libraries/" + path.Replace('\\', '/'), Tools.Dir + @"\.minecraft\libraries\" + path);
                            }
                            catch (Exception e)
                            {
                                new OMCLLog("[InstallMinecraft_InstallForge]安装Forge时出现错误：下载文件 " + Tools.Dir + @"\.minecraft\libraries\" + path + " 时出现错误！" + @e.Message, OMCLExceptionClass.DLL, OMCLExceptionType.Error);
                                throw new Exception("安装Forge时出现错误：下载文件 " + Tools.Dir + @"\.minecraft\libraries\" + path + " 时出现错误！");
                            }
                        }
                    }
                    string au = "";
                    au += "-cp \"" + Tools.Dir + @"\.minecraft\libraries\" + path;
                    JArray classpath = (JArray)processors[i].SelectToken("classpath");
                    for (int j = 0; j < classpath.Count; j++)
                    {
                        string temp = ForgeFilePathGet(classpath[j].ToString());
                        au += ';' + Tools.Dir + @"\.minecraft\libraries\" + temp;
                        if (!File.Exists(Tools.Dir + @"\.minecraft\libraries\" + temp))
                        {
                            try
                            {
                                HttpFile.Start(5, Tools.DownloadMinecraftFileUrl + "libraries/" + temp.Replace('\\', '/'), Tools.Dir + @"\.minecraft\libraries\" + temp);
                            }
                            catch
                            {
                                try
                                {
                                    HttpFile.Start(5, Tools.DownloadMinecraftFileUrl + "libraries/" + temp.Replace('\\', '/'), Tools.Dir + @"\.minecraft\libraries\" + temp);
                                }
                                catch// (Exception e)
                                {
                                    continue;
                                    /*new OMCLLog("[InstallMinecraft_InstallForge]安装Forge时出现错误：下载文件 " + Tools.Dir + @"\.minecraft\libraries\" + temp + " 时出现错误！" + @e.Message, OMCLExceptionClass.DLL, OMCLExceptionType.Error);
                                    throw new Exception("安装Forge时出现错误：下载文件 " + Tools.Dir + @"\.minecraft\libraries\" + temp + " 时出现错误！");*
                                }
                            }
                        }
                    }
                    au += "\" ";
                    if (path.Contains("SpecialSource"))
                    {
                        au += jar.Replace(':', '.').Replace('-', '_').ToLower() + ".SpecialSource ";
                    }
                    else
                    {
                        au += jar.Replace(':', '.') + ".ConsoleTool ";
                    }
                    JArray args = (JArray)processors[i].SelectToken("args");
                    for (int j = 0;j < args.Count;j++)
                    {
                        string temp = args[j].ToString();
                        if (temp.Contains("--"))
                        {
                            au += temp + " ";
                        }
                        else if (temp == "{MINECRAFT_JAR}")
                        {
                            au += "\"" + Tools.Dir + @"\.minecraft\versions\" + version + @"\" + version + ".jar" + "\" ";
                        }
                        else if (temp.Contains("{") && temp.Contains("}"))
                        {
                            string t = temp.Replace("{", "").Replace("}", "");
                            for (int k = 0;k < datas.Count;k++)
                            {
                                if (datas[i].name == t)
                                {
                                    au += datas[i].value + " ";
                                }
                            }
                        }
                        else if (temp.Contains("[") && temp.Contains(']'))
                        {
                            string t = ForgeDataPathGet(temp);
                            au += t + " ";
                        }
                    }
                    process.StartInfo.FileName = java;
                    process.StartInfo.Arguments = au;
                    process.StartInfo.UseShellExecute = true;
                    process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    process.Start();
                }
            }
        }*/
    }
}