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
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
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
    public class Server
    {
        public string server_url_or_ip { get; internal set; }
        public int server_port { get; internal set; }
    }
    public enum DownloadSource
    {
        BMCLAPI, MCBBS,
    }
    public enum JavaType
    {
        jre, jdk, unknown
    }
    public class JavaComp: IComparer<JavaVersion>
    {
        public int Compare(JavaVersion a, JavaVersion b)
        {
            return a.type.CompareTo(b.type);
        }
    }
    public class Tools
    {
        public static string DownloadMinecraftFileUrl = "https://bmclapi2.bangbang93.com/";
        private static readonly string OMCLver = "0.0.0.3";
        public static string Dir = Directory.GetCurrentDirectory();
        public static List<string> LaunchNoFile = new List<string>();
        public static List<string> LaunchNoFileNatives = new List<string>();
        /// <summary>
        /// 尝试获取操作系统的名称
        /// </summary>
        /// <returns>操作系统的名称，一个string值（"windows"、"linux"、"osx"或null）</returns>
        public static string GetSystemName()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                OMCLLog.WriteLog("[Tools_GetSystemName]成功获取到系统名称：windows", OMCLExceptionClass.DLL, OMCLExceptionType.Message);
                return "windows";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                OMCLLog.WriteLog("[Tools_GetSystemName]成功获取到系统名称：linux", OMCLExceptionClass.DLL, OMCLExceptionType.Message);
                return "linux";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                OMCLLog.WriteLog("[Tools_GetSystemName]成功获取到系统名称：osx", OMCLExceptionClass.DLL, OMCLExceptionType.Message);
                return "osx";
            }
            return null;
        }
        /// <summary>
        /// 解压一个zip文件
        /// </summary>
        /// <param name="file">zip文件的位置</param>
        /// <param name="dir">要将zip文件解压到的目录（必须指向一个文件夹）</param>
        /// <param name="CanOver">是否可以覆盖文件</param>
        public static void UnZipFile(string file, string dir, bool CanOver = false)
        {
            try
            {
                if (!(dir.EndsWith("\\") || dir.EndsWith("/"))) 
                {
                    if (GetSystemName() == "windows")
                    {
                        dir += '\\';
                    }
                    else
                    {
                        dir += '/';
                    }
                }
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
                        if ((!File.Exists(dir + theEntry.Name)) || CanOver) 
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
                }
                s.Close();
            }
            catch (Exception e)
            {
                OMCLLog.WriteLog("[Tools_UnZipFile]解压 " + file + " 时出现错误：" + @e.Message, OMCLExceptionClass.DLL, OMCLExceptionType.Error);
                throw new Exception("解压 " + file + " 时出现错误！");
            }
        }
        /// <summary>
        /// 扫描所有盘符中存在的Java（Task异步）
        /// </summary>
        /// <returns>一个数组，里面包含所有的JavaVersion</returns>
        public static async Task<JavaVersion[]> GetJavaListAsync()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            FDTools.search_task = new List<Task>();
            string[] pathins = { "jdk", "jre", "java", "Java" };
            string[] pathsames = { "bin" };
            string[] onlysearchs = { Environment.Is64BitOperatingSystem ? @"Program Files" : @"Program Files (x86)", "XboxGames", "MCLDownload" };
            List<FindOutDir> result = new List<FindOutDir>();
            List<JavaVersion> javas = new List<JavaVersion>();
            List<string> drives = new List<string>();
            foreach (DriveInfo info in DriveInfo.GetDrives()) if (info.DriveType == DriveType.Fixed) drives.Add(info.Name);
            foreach (string drive in drives) FDTools.search_task.Add(Task.Run(() => FDTools.SearchDirTask(@drive, "java.exe", onlysearchs, pathins, pathsames, result)));
            FDTools.search_task.Add(Task.Run(() => FDTools.SearchDirTask(Directory.GetCurrentDirectory(), "java.exe", onlysearchs, pathins, pathsames, result)));
            while (true)
            {
                if (stopwatch.ElapsedMilliseconds <= 1000)
                {
                    try
                    {
                        await Task.WhenAll(FDTools.search_task.ToArray());
                    }
                    catch { }
                }
                else break;
            }
            foreach (FindOutDir temp in result)
            {
                if (temp.PathinGet == "jre")
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
                else
                {
                    javas.Add(new JavaVersion
                    {
                        path = temp.path,
                        type = JavaType.unknown,
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
            OMCLLog.WriteLog("[Tools_GetJavaListAsync]耗时：" + (double)stopwatch.ElapsedMilliseconds / 1000 + " s，找到Java " + new_javas.Count + " 个！", OMCLExceptionClass.DLL, OMCLExceptionType.Message);
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
                char fp;
                if (GetSystemName() == "windows")
                {
                    fp = '\\';
                }
                else
                {
                    fp = '/';
                }
                for (int i = 0; i < ver.libraries.Length; i++)
                {
                    if (!File.Exists(Path.Combine(new string[] { Dir, ".minecraft", "libraries", ver.libraries[i] }))) 
                    {
                        if (ver.libraries[i].Contains("optifine"))
                        {
                            try
                            {
                                string[] temppath1 = ver.libraries[i].Replace($"optifine{fp}OptiFine{fp}", "").Split(fp);
                                string[] temppath2 = temppath1[0].Split('_');
                                HttpFile.Start(5, DownloadMinecraftFileUrl + @"optifine/" + temppath2[0] + "/" + temppath1[0].Replace(temppath2[0] + "_", "").Replace("_" + temppath2.Last(), "") + "/" + temppath2.Last(), Path.Combine(new string[] { Dir, ".minecraft", "libraries", ver.libraries[i] }));
                            }
                            catch (Exception e)
                            {
                                if (IsLaunch && e.Message.Contains("404"))
                                {
                                    OMCLLog.WriteLog("[Tools_DownloadMissFiles]将尝试跳过 " + Path.Combine(new string[] { Dir, ".minecraft", "libraries", ver.libraries[i] }) + " 启动Minecraft！", OMCLExceptionClass.DLL, OMCLExceptionType.Warning);
                                    LaunchNoFile.Add(ver.libraries[i]);
                                }
                                else if (IsLaunch)
                                {
                                    throw e;
                                }
                            }
                        }
                        else
                        {
                            try
                            {
                                HttpFile.Start(5, DownloadMinecraftFileUrl + "libraries/" + ver.libraries[i].Replace('\\', '/'), Path.Combine(new string[] { Dir, ".minecraft", "libraries", ver.libraries[i] }));
                            }
                            catch (Exception e)
                            {
                                if (IsLaunch && e.Message.Contains("404")) 
                                {
                                    OMCLLog.WriteLog("[Tools_DownloadMissFiles]将尝试跳过 " + Path.Combine(new string[] { Dir, ".minecraft", "libraries", ver.libraries[i] }) + " 启动Minecraft！", OMCLExceptionClass.DLL, OMCLExceptionType.Warning);
                                    LaunchNoFile.Add(ver.libraries[i]);
                                }
                                else if (IsLaunch)
                                {
                                    throw e;
                                }
                            }
                        }
                    }
                }
                for (int i = 0; i < ver.natives.Length; i++)
                {
                    if (!File.Exists(Path.Combine(new string[] { Dir, ".minecraft", "libraries", ver.natives[i] })))
                    {
                        try
                        {
                            HttpFile.Start(5, DownloadMinecraftFileUrl + "libraries/" + ver.natives[i].Replace('\\', '/'), Path.Combine(new string[] { Dir, ".minecraft", "libraries", ver.natives[i] }));
                        }
                        catch (Exception e)
                        {
                            if (IsLaunch && e.Message.Contains("404")) 
                            {
                                OMCLLog.WriteLog("[Tools_DownloadMissFiles]将尝试跳过 " + Path.Combine(new string[] { Dir, ".minecraft", "libraries", ver.natives[i] }) + " 启动Minecraft！", OMCLExceptionClass.DLL, OMCLExceptionType.Warning);
                                LaunchNoFileNatives.Add(ver.natives[i]);
                            }
                            else if (IsLaunch)
                            {
                                throw e;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                OMCLLog.WriteLog("[Tools_DownloadMissFile]" + @e.Message, OMCLExceptionClass.DLL, OMCLExceptionType.Error);
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
                if (!File.Exists(Path.Combine(new string[] { Dir, ".minecraft", "assets", "indexes", ver.assets + ".json" }))) 
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
                    versionAssetIndex = JObject.Parse(File.ReadAllText(Path.Combine(new string[] { Dir, ".minecraft", "versions", version, version + ".json" })));
                }
                catch
                {
                    OMCLLog.WriteLog("[Tools_DownloadMissAssets]获取版本json时出现错误，请检查您的版本是否正确！", OMCLExceptionClass.DLL, OMCLExceptionType.Error);
                    throw new Exception("[Tools_DownloadMissAssets]获取版本json时出现错误，请检查您的版本是否正确！");
                }
                if (versionAssetIndex.SelectToken("assetIndex.url") == null)
                {
                    string versionjson = Web.Get(DownloadMinecraftFileUrl + "version/" + ver.assets + "/json");
                    if (versionjson == null || versionjson == "" || versionjson == string.Empty)
                    {
                        OMCLLog.WriteLog("[Tools_DownloadMissAssets]获取版本json时出现错误，请检查您的网络！", OMCLExceptionClass.DLL, OMCLExceptionType.Error);
                        throw new Exception("[Tools_DownloadMissAssets]获取版本json时出现错误，请检查您的网络！");
                    }
                    versionAssetIndex = JObject.Parse(versionjson);
                }
                assets = Web.Get(DownloadMinecraftFileUrl + versionAssetIndex.SelectToken("assetIndex.url").ToString().Replace("https://", "").Replace("http://", "").Replace(versionAssetIndex.SelectToken("assetIndex.url").ToString().Replace("https://", "").Replace("http://", "").Split('/')[0] + '/', ""));
                try
                {
                    Directory.CreateDirectory(Path.Combine(new string[] { Dir, ".minecraft", "assets", "indexes" }));
                }
                catch { }
                File.WriteAllText(Path.Combine(new string[] { Dir, ".minecraft", "assets", "indexes", ver.assets + ".json" }), assets);
            }
            else
            {
                assets = File.ReadAllText(Path.Combine(new string[] { Dir, ".minecraft", "assets", "indexes", ver.assets + ".json" }));
            }
            JObject o = JObject.Parse(assets).Value<JObject>("objects");
            JObject json = (JObject)JsonConvert.DeserializeObject(o.ToString());
            string sysname = GetSystemName();
            string pn;
            foreach (JProperty property in o.Properties())
            {
                string hash = json[property.Name]["hash"].ToString();
                if (sysname == "windows")
                {
                    pn = property.Name.Replace('/', '\\');
                }
                else
                {
                    pn = property.Name;
                }
                if (ver.assets == "pre-1.6" || ver.assets == "legacy")
                {
                    if (!File.Exists(Path.Combine(new string[] { Dir, ".minecraft", "resources", pn }))) 
                    {
                        if (!File.Exists(Path.Combine(new string[] { Dir, ".minecraft", "assets", "objects", hash[0].ToString() + hash[1].ToString(), hash }))) 
                        {
                            try
                            {
                                HttpFile.Start(50, DownloadMinecraftFileUrl + "assets/" + hash[0] + hash[1] + '/' + hash, Path.Combine(new string[] { Dir, ".minecraft", "resources", pn }));
                            }
                            catch
                            {
                                try
                                {
                                    HttpFile.Start(50, DownloadMinecraftFileUrl + "assets/" + hash[0] + hash[1] + '/' + hash, Path.Combine(new string[] { Dir + @"\.minecraft\resources\" + pn }));
                                }
                                catch (Exception e)
                                {
                                    OMCLLog.WriteLog("[Tools_DownloadMissAssets]下载文件 " + hash + "<" + Path.Combine(new string[] { Dir, ".minecraft", "resources", pn }) + ">" + " 时出现错误！" + @e.Message, OMCLExceptionClass.DLL, OMCLExceptionType.Error);
                                    throw new Exception("[Tools_DownloadMissAssets]下载文件 " + Path.GetFileName(pn) + " 时出现错误！");
                                }
                            }
                        }
                        else
                        {
                            try
                            {
                                Directory.CreateDirectory(Path.Combine(new string[] { Dir, ".minecraft", "resources", pn.Replace(Path.GetFileName(pn), "") }));
                            }
                            catch { }
                            File.Copy(Path.Combine(new string[] { Dir, ".minecraft", "assets", "objects", hash[0].ToString() + hash[1].ToString(), hash }), Path.Combine(new string[] { Dir, ".minecraft", "resources", pn }), true);
                        }
                    }
                    Directory.CreateDirectory(Path.Combine(new string[] { Dir, ".minecraft", "versions", version, "resources", Path.GetDirectoryName(pn) }));
                    if (!File.Exists(Path.Combine(new string[] { Dir, ".minecraft", "versions", version, "resources", pn }))) 
                    {
                        File.Copy(Path.Combine(new string[] { Dir, ".minecraft", "resources", pn }), Path.Combine(new string[] { Dir, ".minecraft", "versions", version, "resources", pn }), true);
                    }
                    Directory.CreateDirectory(Path.Combine(new string[] { Dir, ".minecraft", "assets", "virtual", ver.assets, Path.GetDirectoryName(pn) }));
                    if (!File.Exists(Path.Combine(new string[] { Dir, ".minecraft", "assets", "virtual", ver.assets, pn }))) 
                    {
                        File.Copy(Path.Combine(new string[] { Dir, ".minecraft", "resources", pn }), Path.Combine(new string[] { Dir, ".minecraft", "assets", "virtual", ver.assets, pn }), true);
                    }
                }
                else
                {
                    if (!File.Exists(Path.Combine(new string[] { Dir, ".minecraft", "assets", "objects", hash[0].ToString() + hash[1].ToString(), hash })))
                    {
                        try
                        {
                            HttpFile.Start(50, DownloadMinecraftFileUrl + "assets/" + hash[0] + hash[1] + '/' + hash, Path.Combine(new string[] { Dir, ".minecraft", "assets", "objects", hash[0].ToString() + hash[1].ToString(), hash }));
                        }
                        catch
                        {
                            try
                            {
                                HttpFile.Start(50, DownloadMinecraftFileUrl + "assets/" + hash[0] + hash[1] + '/' + hash, Path.Combine(new string[] { Dir, ".minecraft", "assets", "objects", hash[0].ToString() + hash[1].ToString(), hash }));
                            }
                            catch (Exception e)
                            {
                                OMCLLog.WriteLog("[Tools_DownloadMissAssets]下载文件 " + hash + " 时出现错误！" + @e.Message, OMCLExceptionClass.DLL, OMCLExceptionType.Error);
                                throw new Exception("[Tools_DownloadMissAssets]下载文件 " + hash + " 时出现错误！");
                            }
                        }
                    }
                }
            }
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
                List<string> paths = Directory.GetDirectories(Path.Combine(new string[] { Dir, ".minecraft", "versions" }), "*", SearchOption.TopDirectoryOnly).ToList();
                for (int i = 0; i < paths.Count; i++)
                {
                    paths[i] = paths[i].Replace(Path.Combine(new string[] { Dir, ".minecraft", "versions" }), "");
                }
                foreach (string path in paths)
                {
                    if (File.Exists(Path.Combine(new string[] { Dir, ".minecraft", "versions", path, path + ".jar" })) && File.Exists(Path.Combine(new string[] { Dir, ".minecraft", "versions", path, path + ".json" }))) 
                    {
                        result.Add(path);
                    }
                }
                return result.ToArray();
            }
            catch (Exception e)
            {
                OMCLLog.WriteLog("[Tools_GetVersionList]错误：" + @e.Message, OMCLExceptionClass.DLL, OMCLExceptionType.Error);
                throw new Exception("获取Java列表时出现错误！");
            }
        }
        private static Version ReadVersionJson(string version)
        {
            return ReadVersionJson(Path.Combine(new string[] { Dir, ".minecraft", "versions", version, version + ".json" }), out _, out _, out _, out _, false);
        }
        internal static Version ReadVersionJson(string FilePathOrText, out JArray arguments, out string minecraftArguments, out JArray libraries, out JArray jvm, bool IsText)
        {
            try
            {
                string systemname = GetSystemName();
                if (systemname == null)
                {
                    OMCLLog.WriteLog("[Tools_ReadVersionJson]错误：操作系统不受支持！不是<Windows、Linux、MacOS>其中之一！", OMCLExceptionClass.DLL, OMCLExceptionType.Error);
                    throw new Exception("操作系统不受支持！");
                }
                List<string> Natives_List = new List<string>(20);
                List<string> Path = new List<string>(60);
                JObject o;
                if (!IsText) o = JObject.Parse(File.ReadAllText(FilePathOrText));
                else o = JObject.Parse(FilePathOrText);
                libraries = (JArray)o.SelectToken("libraries");
                char filesp;
                if (systemname == "windows") filesp = '\\';
                else filesp = '/';
                bool haslw = false;
                string lw_ver = "";
                for (int i = 0; i < libraries.Count(); i++)
                {
                    string path = (string)o.SelectToken("libraries[" + i + "].name");
                    if (path.Contains("@"))
                    {
                        path = path.Replace(':', filesp);
                        string[] t = path.Split(filesp);
                        string[] a = t.Last().Split('@');
                        t[0] = t.First().Replace('.', filesp);
                        path = "";
                        for (int j = 0; j < t.Length - 1; j++)
                        {
                            path += t[j] + filesp;
                        }
                        path += a[0] + filesp + t[t.Length - 2] + '-' + a[0] + '.' + a[1];
                        Path.Add(path);
                        continue;
                    }
                    path = path.Replace(':', filesp);
                    string[] paths = path.Split(filesp);
                    if (path.Contains("lwjgl") && !haslw)
                    {
                        haslw = true;
                        lw_ver = paths[paths.Length - 1];
                    }
                    else if (haslw)
                    {
                        if (path.Contains("lwjgl"))
                        {
                            if (!path.Contains(lw_ver))
                            {
                                continue;
                            }
                        }
                    }
                    paths[0] = paths.First().Replace('.', filesp);
                    path = "";
                    for (int j = 0; j < paths.Length; j++)
                    {
                        path += paths[j] + filesp;
                    }
                    if (path.Contains("natives"))
                    {
                        if (path.Contains($"natives-{systemname}"))
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
                        Path.Add(path.Replace($"{filesp}universal", "") + paths[paths.Length - 3] + @"-" + paths[paths.Length - 2] + @"-" + paths[paths.Length - 1] + ".jar");
                        continue;
                    }
                    JObject natives_windows = (JObject)o.SelectToken("libraries[" + i + $"].downloads.classifiers.natives-{systemname}");
                    if (natives_windows != null)
                    {
                        Natives_List.Add(path + paths[paths.Length - 2] + @"-" + paths[paths.Length - 1] + $"-natives-{systemname}.jar");
                    }
                    string native = (string)o.SelectToken("libraries[" + i + $"].natives.{systemname}");
                    if (native != null)
                    {
                        native = native.Replace("${arch}", Environment.Is64BitOperatingSystem ? "64" : "32");
                        Natives_List.Add(path + paths[paths.Length - 2] + @"-" + paths[paths.Length - 1] + "-" + native + ".jar");
                    }
                    path = path + paths[paths.Length - 2] + @"-" + paths[paths.Length - 1] + ".jar";
                    /*if (path.Contains($"launchwrapper{filesp}1.5"))
                    {
                        path = path.Replace("1.5", "1.6");
                    }*/
                    Path.Add(path);
                }
                Natives_List = new HashSet<string>(Natives_List).ToList();
                Path = new HashSet<string>(Path).ToList();
                List<Argument> arguments_List = new List<Argument>(15);
                JArray argumentso = (JArray)o.SelectToken("arguments.game");
                arguments = argumentso;
                bool IsVaule = false;
                string name = "";
                if (argumentso == null || argumentso.Count == 0)
                {
                    minecraftArguments = ((string)o.SelectToken("minecraftArguments"));
                    string[] minecraftArguments_sp = minecraftArguments.Split(' ');
                    if (minecraftArguments_sp[0].Contains("--"))
                    {
                        for (int j = 1; j < minecraftArguments_sp.Length; j += 2)
                        {
                            string tmp = minecraftArguments_sp[j - 1];
                            string value = minecraftArguments_sp[j];
                            arguments_List.Add(new Argument() { name = tmp, value = value });
                        }
                    }
                    else
                    {
                        for (int j = 0; j < minecraftArguments_sp.Length; j++)
                        {
                            string value = minecraftArguments_sp[j];
                            arguments_List.Add(new Argument() { name = "", value = value });
                        }
                    }
                }
                else
                {
                    minecraftArguments = "";
                    for (int i = 0; i < argumentso.Count; i++)
                    {
                        try
                        {
                            string vaule = (string)o.SelectToken("arguments.game[" + i + "]");
                            if (IsVaule)
                            {
                                IsVaule = false;
                                arguments_List.Add(new Argument() { name = name, value = vaule });
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
                for (int i = 0; i < arguments_List.Count; i++)
                {
                    if (arguments_List[i].value.Contains("optifine"))
                    {
                        arguments_List.Add(arguments_List[i]);
                        arguments_List.RemoveAt(i);
                    }
                }
                arguments_List.TrimExcess();
                List<string> Na_temp = new List<string>(20);
                List<string> Li_temp = new List<string>(20);
                /*bool IsLwjgl_3_2_2 = false;
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
                }*/
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
                jvm = jvms;
                List<string> jvms_List = new List<string>();
                string jvm_s = "";
                if (jvms != null)
                {
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
                    for (int i = 0; i < jvms_List.Count; i++)
                    {
                        jvm_s += jvms_List[i];
                        if (i != (jvms_List.Count - 1)) jvm_s += ' ';
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
                    arguments = arguments_List.ToArray(),
                    assets = (string)o.SelectToken("assets"),
                    jvm = jvm_s,
                };
                Natives_List.Clear();
                Path.Clear();
                return ver;
            }
            catch (Exception e)
            {
                OMCLLog.WriteLog("[Tools_ReadVersionJson]" + @e.Message, OMCLExceptionClass.DLL, OMCLExceptionType.Error);
                throw new Exception("读取版本Json时出现错误，请检查你的版本是否正常！");
            }
        }

        public class LaunchMinecraft
        {
            public bool IsIsolation = true;
            public int MaxMem = 2048, MinMem = 1024;
            private string NativesPath = "";
            public string jvm = "", otherArguments = "";
            public delegate void MinecraftCrashedDelegate(CrashMessage crashMessage);
            public event MinecraftCrashedDelegate OnMinecraftCrash;
            public Process process = new Process();
            public Server server = null;
            private string version = "";
            private List<string> OutputLines = new List<string>();
            private bool IsMinecraftCrashed = true;
            /// <summary>
            /// 更改Natives路径（注意：如果更改Natives的路径，OMCL将不会再帮您解压Natives库，因此，如果该文件夹不存在或该文件夹中的Natives文件不完整，将导致您的Minecraft无法启动）
            /// </summary>
            /// <param name="path">一个路径，指向一个文件夹，其中包含游戏运行所需要的Natives</param>
            public void ChangeNativesPath(string path)
            {
                if (path == null || path == "" || path == string.Empty) NativesPath = "";
                else if (Directory.Exists(path) && (Directory.GetFiles(path, "*.dll", SearchOption.TopDirectoryOnly).Length != 0)) NativesPath = path;
                else NativesPath = "";
            }
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
                    OMCLLog.WriteLog("[Tools_LaunchGame]" + @e.Message, OMCLExceptionClass.DLL, OMCLExceptionType.Error);
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
                            OMCLLog.WriteLog("[Tools_LaunchGame]错误：下载authlib-injector-1.2.1.jar时出现错误，请检查你的网络是否正常！", OMCLExceptionClass.DLL, OMCLExceptionType.Error);
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
                        OMCLLog.WriteLog("[Tools_LaunchGame]错误：获取信息时出现错误，请检查你的网络是否正常！", OMCLExceptionClass.DLL, OMCLExceptionType.Error);
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
                    OMCLLog.WriteLog("[Tools_LaunchGame]" + @e.Message, OMCLExceptionClass.DLL, OMCLExceptionType.Error);
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
                            OMCLLog.WriteLog("[Tools_LaunchGame]启动失败，下载nide8auth.jar文件时出现错误，请检查您的网络！", OMCLExceptionClass.DLL, OMCLExceptionType.Error);
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
                    OMCLLog.WriteLog("[Tools_LaunchGame]" + @e.Message, OMCLExceptionClass.DLL, OMCLExceptionType.Error);
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
                    OMCLLog.WriteLog("[Tools_LaunchGame]" + @e.Message, OMCLExceptionClass.DLL, OMCLExceptionType.Error);
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
                    OMCLLog.WriteLog("[Tools_LaunchGame]" + @e.Message, OMCLExceptionClass.DLL, OMCLExceptionType.Error);
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
                    OutputLines = new List<string>();
                    IsMinecraftCrashed = true;
                    this.version = version;
                    if (!File.Exists(Path.Combine(new string[] { Dir, ".minecraft", "versions", version, version + ".jar" })) || !File.Exists(Path.Combine(new string[] { Dir, ".minecraft", "versions", version, version + ".json" }))) 
                    {
                        OMCLLog.WriteLog("[Tools_LaunchGame]版本 " + version + " 有问题，请重新下载！", OMCLExceptionClass.DLL, OMCLExceptionType.Error);
                        throw new Exception("版本有问题！请重新下载！");
                    }
                    LaunchNoFile = new List<string>();
                    LaunchNoFileNatives = new List<string>();
                    Version ver = ReadVersionJson(version);
                    string GameCMD = "", ClientPath = Path.Combine(new string[] { ".minecraft", "versions", version, version + ".jar" });
                    DownloadMissFiles(version);
                    string _NativesPath;
                    UnzipNatives(version);
                    if (NativesPath == "") _NativesPath = Path.Combine(new string[] { Dir, ".minecraft", "versions", version, version + "-natives" }); else _NativesPath = NativesPath;
                    GameCMD += "-Dfile.encoding=GB18030 -Dsun.stdout.encoding=GB18030 -Dsun.stderr.encoding=GB18030 -Djava.rmi.server.useCodebaseOnly=true -Dcom.sun.jndi.rmi.object.trustURLCodebase=false -Dcom.sun.jndi.cosnaming.object.trustURLCodebase=false -Dlog4j2.formatMsgNoLookups=true -Dminecraft.client.jar={Client_Path} -XX:+UnlockExperimentalVMOptions -XX:+UseG1GC -XX:G1NewSizePercent=20 -XX:G1ReservePercent=20 -XX:MaxGCPauseMillis=50 -XX:G1HeapRegionSize=32m -XX:-UseAdaptiveSizePolicy -XX:-OmitStackTraceInFastThrow -XX:-DontCompileHugeMethods -Dfml.ignoreInvalidMinecraftCertificates=true -Dfml.ignorePatchDiscrepancies=true -XX:HeapDumpPath=MojangTricksIntelDriversForPerformance_javaw.exe_minecraft.exe.heapdump -Xms{Min_Mem}m -Xmx{Max_Mem}m -Djava.library.path=\"{Natives_Path}\" -Dminecraft.launcher.brand=OMCL -Dminecraft.launcher.version={OMCL_Version} ".Replace("{OMCL_Version}", OMCLver).Replace("{Natives_Path}", _NativesPath).Replace("{Max_Mem}", MaxMem.ToString()).Replace("{Min_Mem}", MinMem.ToString()).Replace("{Client_Path}", ClientPath);
                    if (ver.jvm != "") GameCMD += ver.jvm + " ";
                    if (jvm != "") GameCMD += jvm + " ";
                    GameCMD += "-cp \"";
                    string tempCP = "";
                    List<string> libraries = ver.libraries.ToList();
                    char libssp;
                    if (GetSystemName() == "windows")
                    {
                        libssp = ';';
                    }
                    else
                    {
                        libssp = ':';
                    }
                    foreach (string No_path in LaunchNoFile) libraries.Remove(No_path);
                    for (int i = 0; i < libraries.Count; i++)
                    {
                        if (i == libraries.Count - 1) tempCP += Path.Combine(new string[] { Dir, ".minecraft", "libraries", libraries[i] });
                        else tempCP += Path.Combine(new string[] { Dir, ".minecraft", "libraries", libraries[i] }) + libssp;
                    }
                    GameCMD += tempCP + ";" + Path.Combine(new string[] { Dir, ".minecraft", "versions", version, version + ".jar" }) + "\" ";
                    GameCMD += ver.mainClass + ' ';
                    for (int i = 0; i < ver.arguments.Length; i++)
                    {
                        if (ver.arguments[i].name != null && ver.arguments[i].name != "") GameCMD += ver.arguments[i].name + ' ';
                        switch (ver.arguments[i].value)
                        {
                            case "${auth_player_name}":
                                GameCMD += playerName + ' ';
                                break;
                            case "${version_name}":
                                GameCMD += version + ' ';
                                break;
                            case "${game_directory}":
                                if (IsIsolation) GameCMD += '\"' + Path.Combine(new string[] { Dir, ".minecraft", "versions", version }) + '\"' + ' ';
                                else GameCMD += '\"' + Path.Combine(new string[] { Dir, ".minecraft" }) + '\"' + ' ';
                                break;
                            case "${assets_root}":
                                GameCMD += '\"' + Path.Combine(new string[] { Dir, ".minecraft", "assets" }) + '\"' + ' ';
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
                                GameCMD += "mojang" + ' ';
                                break;
                            case "${version_type}":
                                GameCMD += "\"OMCL " + OMCLver + "\" ";
                                break;
                            case "${user_properties}":
                                GameCMD += "{} ";
                                break;
                            case "${game_assets}":
                                GameCMD += '\"' + Path.Combine(new string[] { Dir, ".minecraft", "assets", "virtual", ver.assets }) + '\"' + ' ';
                                /*if (!IsIsolation) GameCMD += '\"' + Path.Combine(new string[] { Dir, ".minecraft", "versions", version, "resources" }) + '\"' + ' ';
                                else GameCMD += '\"' + Path.Combine(new string[] { Dir, ".minecraft", "resources" }) + '\"' + ' ';*/
                                break;
                            default:
                                if (ver.arguments[i].value != null && ver.arguments[i].value != "" && !ver.arguments[i].value.Contains("$")) GameCMD += ver.arguments[i].value + ' ';
                                else GameCMD.Replace(ver.arguments[i].name + ' ', "");
                                break;
                        }
                    }
                    if (otherArguments != "") GameCMD += otherArguments;
                    else if (server != null && server.server_url_or_ip != "" && server.server_url_or_ip != null && server.server_url_or_ip != string.Empty) GameCMD += "--server " + server.server_url_or_ip + " --port " + server.server_port;
                    else GameCMD.Remove(GameCMD.Length - 2, 1);
                    Console.WriteLine("启动脚本如下：\n" + '\"' + java + '\"' + ' ' + GameCMD);
                    OMCLLog.WriteLog("[Tools_LaunchGame]启动脚本如下：" + ("\"" + java + "\" " + GameCMD).Replace(token, "access_token隐藏").Replace(uuid, "uuid隐藏").Replace(playerName, "玩家名称隐藏") + "。", OMCLExceptionClass.DLL, OMCLExceptionType.Message);
                    if (IsIsolation)
                    {
                        if (!File.Exists(Path.Combine(new string[] { Dir, ".minecraft", "versions", version, "options.txt" }))) 
                        {
                            File.WriteAllText(Path.Combine(new string[] { Dir, ".minecraft", "versions", version, "options.txt" }), "lang:zh_CN");
                        }
                    }
                    else
                    {
                        if (!File.Exists(Path.Combine(new string[] { Dir, ".minecraft", "options.txt" }))) 
                        {
                            File.WriteAllText(Path.Combine(new string[] { Dir, ".minecraft", "options.txt" }), "lang:zh_CN");
                        }
                    }
                    process = new Process();
                    process.StartInfo.Arguments = GameCMD;
                    process.StartInfo.FileName = java;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                    process.OutputDataReceived += Process_OutputDataReceived;
                    process.ErrorDataReceived += Process_OutputDataReceived;
                    if (!IsIsolation) process.StartInfo.WorkingDirectory = Path.Combine(new string[] { Dir, ".minecraft" });
                    else process.StartInfo.WorkingDirectory = Path.Combine(new string[] { Dir, ".minecraft", "versions", version });
                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    new Thread(new ThreadStart(WaitMinecraft))
                    {
                        IsBackground = false
                    }.Start();
                }
                catch (Exception e)
                {
                    OMCLLog.WriteLog("[Tools_LaunchGame]" + @e.Message, OMCLExceptionClass.DLL, OMCLExceptionType.Error);
                    throw new Exception("启动时出现错误！");
                }
            }
            private void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
            {
                try
                {
                    Console.WriteLine(e.Data);
                    OMCLLog.WriteLog("[MinecraftProcess]接收到Minecraft进程的控制台输出信息：" + e.Data, OMCLExceptionClass.DLL, OMCLExceptionType.Message);
                    OutputLines.Add(e.Data);
                    if (e.Data != null && e.Data.Contains("Stopping!")) IsMinecraftCrashed = false;
                }
                catch { }
            }
            private void WaitMinecraft()
            {
                try
                {
                    process.WaitForExit();
                    CrashMessage CrashMessage = new CrashMessage();
                    OMCLLog.WriteLog("[Tools_WaitMinecraft]当前Minecraft版本 " + version + " 已退出，检查中……", OMCLExceptionClass.DLL, OMCLExceptionType.Message);
                    if (process.ExitCode != 0)
                    {
                        OMCLLog.WriteLog("检测到Minecraft退出异常（退出代码不为0），分析已开始！", OMCLExceptionClass.DLL, OMCLExceptionType.Message);
                        string[] files;
                        try
                        {
                            if (!IsIsolation) files = Directory.GetFiles(Path.Combine(new string[] { Dir, ".minecraft", "crash-reports" }), "crash-????-??-??_??.??.??-??????.txt");
                            else files = Directory.GetFiles(Path.Combine(new string[] { Dir, ".minecraft", "versions", version, "crash-reports" }), "crash-????-??-??_??.??.??-??????.txt");
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
                        try
                        {
                            OnMinecraftCrash(CrashMessage);
                        }
                        catch { }
                        return;
                    }
                    else if (IsMinecraftCrashed)
                    {
                        OMCLLog.WriteLog("[CrashMessageGet]警告：在Minecraft的日志中找不到<Stopping！>，开始检查！", OMCLExceptionClass.DLL, OMCLExceptionType.Message);
                        CrashMessage.ExitCode = 0;
                        CrashMessage.VersionName = version;
                        for (int i = OutputLines.Count - 1; i >= 0; i--)
                        {
                            if (OutputLines[i] != null && OutputLines[i].Contains("Exception"))
                            {
                                OMCLLog.WriteLog("[CrashMessageGet]Minecraft报出了这个错误：<" + OutputLines[i] + ">。", OMCLExceptionClass.DLL, OMCLExceptionType.Message);
                                CrashMessage.Solution = "这个是您的Minecraft报出的错误：" + OutputLines[i] + '\n';
                                switch(OutputLines[i])
                                {
                                    case "java.lang.NullPointerException":
                                        CrashMessage.Message = "您的Minecraft可能因为找不到语言文件或Assets资源文件而崩溃！";
                                        CrashMessage.Solution += "建议您补全Assets文件后再尝试启动Minecraft。";
                                        break;
                                    case "java.io.FileNotFoundException: level.dat (系统找不到指定的文件。)":
                                        CrashMessage.Message = null;
                                        CrashMessage.Solution = null;
                                        break;
                                    default:
                                        CrashMessage.Message = "您的Minecraft因为一些未知错误而崩溃！";
                                        CrashMessage.Solution += "我们无法确定Minecraft是否正常，建议您自行检查。\n可以尝试重新下载该Minecraft版本并尝试启动，如果问题仍然存在，请与OMCL作者联系！";
                                        break;
                                }
                                break;
                            }
                            else if (OutputLines[i] != null && !OutputLines[i].Contains("at") && i >= 4)
                            {
                                CrashMessage = null;
                                OMCLLog.WriteLog("[Tools_WaitMinecraft]版本 " + version + " 的Minecraft进程未报出关键错误或异常，断定为：Minecraft并没有崩溃，可能是老版本！", OMCLExceptionClass.DLL, OMCLExceptionType.Message);
                                return;
                            }
                        }
                        if (CrashMessage.Message == "" || CrashMessage.Message == null || CrashMessage.Solution == "" || CrashMessage.Solution == null)
                        {
                            CrashMessage = null;
                            OMCLLog.WriteLog("[Tools_WaitMinecraft]版本 " + version + " 的Minecraft进程未报出关键错误或异常，断定为：Minecraft并没有崩溃，可能是老版本！", OMCLExceptionClass.DLL, OMCLExceptionType.Message);
                            return;
                        }
                        OMCLLog.WriteLog("[CrashMessageGet]结果：\n" + CrashMessage.Message + "\n\n可能的解决方法：\n" + CrashMessage.Solution + "\n\nMinecraft退出代码：" + CrashMessage.ExitCode + "\n版本名称：" + CrashMessage.VersionName + "。", OMCLExceptionClass.DLL, OMCLExceptionType.Message);
                        try
                        {
                            OnMinecraftCrash(CrashMessage);
                        }
                        catch { }
                        return;
                    }
                    OMCLLog.WriteLog("[Tools_WaitMinecraft]版本 " + version + " 退出正常，返回代码：0，将不做任何分析处理！", OMCLExceptionClass.DLL, OMCLExceptionType.Message);
                    CrashMessage = null;
                    return;
                }
                catch (Exception e)
                {
                    OMCLLog.WriteLog("[Tools_WaitMinecraft]出现错误：" + @e.Message, OMCLExceptionClass.DLL, OMCLExceptionType.Error);
                    throw new Exception("日志检测出现错误！");
                }
            }
            private CrashMessage CrashMessageGet(string reportPath, int ExitCode, string version)
            {
                try
                {
                    CrashMessage crashMessage = new CrashMessage();
                    crashMessage.VersionName = version;
                    crashMessage.ExitCode = ExitCode;
                    string ex = "";
                    for (int i = OutputLines.Count - 1; i >= 0; i--)
                    {
                        if (OutputLines[i] != null && (OutputLines[i].Contains("Exception") || OutputLines[i].Contains("错误") || OutputLines[i].Contains("Error") || OutputLines[i].ToLower().Contains("unable"))) 
                        {
                            OMCLLog.WriteLog("[CrashMessageGet]Minecraft报出了这个错误：<" + OutputLines[i] + ">。", OMCLExceptionClass.DLL, OMCLExceptionType.Message);
                            ex = "这个是您的Minecraft报出的错误：" + OutputLines[i];
                            if (OutputLines[i].Contains("java.lang.StackOverflowError")) ex += '\n' + "可能的原因：栈内存溢出！";
                            break;
                        }
                    }
                    if (ExitCode == 1)
                    {
                        OMCLLog.WriteLog("[CrashMessageGet]检测到Minecraft退出代码为1，不会产生崩溃日志，断定为：Minecraft被强制终止！", OMCLExceptionClass.DLL, OMCLExceptionType.Message);
                        crashMessage.Message = "您的Minecraft可能被强制停止！";
                        crashMessage.Solution = "1、请检查是否有杀毒软件杀掉了Minecraft的进程。\n" +
                                                "2、请检查您是否使用 任务管理器 结束了Minecraft的进程。\n" +
                                                "3、请检查其他启动器是否能够正常启动该版本。如果能，这可能是OMCL的问题，请将该问题详细地汇报给OMCL的开发者。" +
                                                ((ex != "" && ex != null) ? "\n" + ex : "");
                        OMCLLog.WriteLog("[CrashMessageGet]结果：\n" + crashMessage.Message + "\n\n可能的解决方法：\n" + crashMessage.Solution + "\n\nMinecraft退出代码：" + crashMessage.ExitCode + "\n版本名称：" + crashMessage.VersionName + "。", OMCLExceptionClass.DLL, OMCLExceptionType.Message);
                        return crashMessage;
                    }
                    else if (ExitCode == -805306369)
                    {
                        OMCLLog.WriteLog("[CrashMessageGet]检测到Minecraft退出代码为-805306369，不会产生崩溃日志，断定为：Minecraft因未响应被操作系统强制终止！", OMCLExceptionClass.DLL, OMCLExceptionType.Message);
                        crashMessage.Message = "您的Minecraft可能因未响应而闪退或被操作系统强制终止！";
                        crashMessage.Solution = "1、请检查Minecraft的进程是否正常，或您是否在Minecraft启动时多次操作Minecraft导致未响应。\n" +
                                                "2、请确认您刚刚是否在Minecraft中炸图（放置超过Minecraft或CPU或显卡极限的数量的tnt）或做实验，导致Minecraft未响应或崩溃（雾）。\n" +
                                                "3、请确认您刚刚是否在Minecraft中做了其他会超过Minecraft或CPU或显卡极限的事情，如果做了，请下次不要再这样做（雾）。\n" +
                                                "4、请检查其他启动器是否能够正常启动及游玩该版本。如果能，这可能是OMCL的问题，请将该问题详细地汇报给OMCL的开发者。" +
                                                ((ex != "" && ex != null) ? "\n" + ex : "");
                        OMCLLog.WriteLog("[CrashMessageGet]结果：\n" + crashMessage.Message + "\n\n可能的解决方法：\n" + crashMessage.Solution + "\n\nMinecraft退出代码：" + crashMessage.ExitCode + "\n版本名称：" + crashMessage.VersionName + "。", OMCLExceptionClass.DLL, OMCLExceptionType.Message);
                        return crashMessage;
                    }
                    if (reportPath == null)
                    {
                        OMCLLog.WriteLog("[CrashMessageGet]找不到合适的Minecraft崩溃报告！", OMCLExceptionClass.DLL, OMCLExceptionType.Message);
                        crashMessage.Message = "未找到任何可能的原因，这次崩溃可能只是MC莫名抽风了而已！";
                        crashMessage.Solution = "请尝试重启该Minecraft版本，一般可以直接解决。\n如果重启后依然崩溃，可能是您的版本出现了问题或这是OMCL问题。" +
                                                ((ex != "" && ex != null) ? "\n" + ex : "");
                        OMCLLog.WriteLog("[CrashMessageGet]结果：\n" + crashMessage.Message + "\n\n可能的解决方法：\n" + crashMessage.Solution + "\n\nMinecraft退出代码：" + crashMessage.ExitCode + "\n版本名称：" + crashMessage.VersionName + "。", OMCLExceptionClass.DLL, OMCLExceptionType.Message);
                        return crashMessage;
                    }
                    OMCLLog.WriteLog("[CrashMessageGet]查找到 " + reportPath + " 作为Minecraft崩溃报告！", OMCLExceptionClass.DLL, OMCLExceptionType.Message);
                    char filesp;
                    if (GetSystemName() == "windows")
                    {
                        filesp = '\\';
                    }
                    else
                    {
                        filesp = '/';
                    }
                    string path = Regex.Replace(reportPath, $@"{filesp}{filesp}crash-reports{filesp}{filesp}crash(.*?)\.txt", "") + @"\mods";
                    OMCLLog.WriteLog("[CrashMessageGet]查找到 " + path + " 作为模组文件放置路径！", OMCLExceptionClass.DLL, OMCLExceptionType.Message);
                    string[] lines = File.ReadLines(reportPath).ToArray();
                    List<string> GetList = new List<string>();
                    List<ModInfo> mods = new List<ModInfo>();
                    bool IsFinded = false;
                    for (int i = 0; i < lines.Length; i++)
                    {
                        if (lines[i].Contains("debug crash"))
                        {
                            OMCLLog.WriteLog("[CrashMessageGet]检测到Minecraft崩溃日志中的第" + (i + 1).ToString() + "行有关键词：debug crash，断定为：手动触发的F3+C的调试崩溃！", OMCLExceptionClass.DLL, OMCLExceptionType.Message);
                            crashMessage.Message = "嗯……这是您自己造成的崩溃！";
                            crashMessage.Solution = "请不要闲着没事（划掉）\n请尝试重启您的游戏，且不要再按下F3+C！" +
                                                ((ex != "" && ex != null) ? "\n" + ex : "");
                            OMCLLog.WriteLog("[CrashMessageGet]结果：\n" + crashMessage.Message + "\n\n可能的解决方法：\n" + crashMessage.Solution + "\n\nMinecraft退出代码：" + crashMessage.ExitCode + "\n版本名称：" + crashMessage.VersionName + "。", OMCLExceptionClass.DLL, OMCLExceptionType.Message);
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
                                OMCLLog.WriteLog("[CrashMessageGet]找到资源文件为 minecraft.jar ，默认跳过！", OMCLExceptionClass.DLL, OMCLExceptionType.Message);
                                continue;
                            }
                            if (temp[3].Contains("forge"))
                            {
                                OMCLLog.WriteLog("[CrashMessageGet]找到资源文件名称中含有 forge ，默认跳过！", OMCLExceptionClass.DLL, OMCLExceptionType.Message);
                                continue;
                            }
                            if (temp[0].Contains("E"))
                            {
                                OMCLLog.WriteLog("[CrashMessageGet]查找到模组 " + path + @"\" + temp[3] + " 加载时出现状态“E”，断定为：该模组加载时错误，是无效mod！", OMCLExceptionClass.DLL, OMCLExceptionType.Message);
                                crashMessage.Message = "找到可能无效的mod文件！";
                                crashMessage.Solution = "请尝试禁用或删除 " + path + @"\" + temp[3] + " mod，并尝试重启Minecraft！" +
                                                ((ex != "" && ex != null) ? "\n" + ex : "");
                                OMCLLog.WriteLog("[CrashMessageGet]结果：\n" + crashMessage.Message + "\n\n可能的解决方法：\n" + crashMessage.Solution + "\n\nMinecraft退出代码：" + crashMessage.ExitCode + "\n版本名称：" + crashMessage.VersionName + "。", OMCLExceptionClass.DLL, OMCLExceptionType.Message);
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
                        OMCLLog.WriteLog("[CrashMessageGet]查找到以下模组关键Id： " + mods[i].IdNamespace + " 。", OMCLExceptionClass.DLL, OMCLExceptionType.Message);
                    }
                    for (int i = 0; i < GetList.Count; i++)
                    {
                        for (int j = 0; j < mods.Count; j++)
                        {
                            if (GetList[i].Contains(mods[j].IdNamespace))
                            {
                                OMCLLog.WriteLog("[CrashMessageGet]查找到堆栈中的关键模组Id： " + mods[j].IdNamespace + " ，断定为：该模组导致Minecraft崩溃！", OMCLExceptionClass.DLL, OMCLExceptionType.Message);
                                OMCLLog.WriteLog("[CrashMessageGet]匹配成功！ " + GetList[i] + "对" + mods[j] + " 。断定 " + path + @"\" + mods[j].FileName + " 导致Minecraft崩溃！", OMCLExceptionClass.DLL, OMCLExceptionType.Message);
                                crashMessage.Message = "找到可能导致Minecraft崩溃的mod文件！";
                                crashMessage.Solution = "请尝试禁用或删除 " + path + @"\" + mods[j].FileName + " mod，并尝试重启Minecraft！" +
                                                ((ex != "" && ex != null) ? "\n" + ex : "");
                                OMCLLog.WriteLog("[CrashMessageGet]结果：\n" + crashMessage.Message + "\n\n可能的解决方法：\n" + crashMessage.Solution + "\n\nMinecraft退出代码：" + crashMessage.ExitCode + "\n版本名称：" + crashMessage.VersionName + "。", OMCLExceptionClass.DLL, OMCLExceptionType.Message);
                                return crashMessage;
                            }
                        }
                    }
                    crashMessage.Message = "未找到任何可能的原因，这次崩溃可能只是MC莫名抽风了而已！";
                    crashMessage.Solution = "请尝试重启该Minecraft版本，一般可以直接解决。\n如果重启后依然崩溃，可能是您的版本出现了问题或这是OMCL问题。" +
                                                ((ex != "" && ex != null) ? "\n" + ex : "");
                    OMCLLog.WriteLog("[CrashMessageGet]结果：\n" + crashMessage.Message + "\n\n可能的解决方法：\n" + crashMessage.Solution + "\n\nMinecraft退出代码：" + crashMessage.ExitCode + "\n版本名称：" + crashMessage.VersionName + "。", OMCLExceptionClass.DLL, OMCLExceptionType.Message);
                    return crashMessage;
                }
                catch (Exception e)
                {
                    OMCLLog.WriteLog("[Tools_CrashMessageGet]分析崩溃时出现错误：" + @e.Message, OMCLExceptionClass.DLL, OMCLExceptionType.Error);
                    CrashMessage crashMessage = new CrashMessage();
                    crashMessage.VersionName = version;
                    crashMessage.ExitCode = ExitCode;
                    crashMessage.Message = "未找到任何可能的原因，且崩溃分析运行时出现错误！";
                    crashMessage.Solution = "请尝试重启该Minecraft版本。\n\nOMCL错误：" + @e.Message;
                    return crashMessage;
                }
            }
            private void UnzipNatives(string version)
            {
                try
                {
                    Version ver = ReadVersionJson(version);
                    List<string> natives = ver.natives.ToList();
                    for (int i = 0; i < LaunchNoFileNatives.Count; i++) natives.Remove(LaunchNoFileNatives[i]);
                    for (int i = 0; i < natives.Count; i++)
                    {
                        UnZipFile(Path.Combine(new string[] { Dir, ".minecraft", "libraries", natives[i] }), Path.Combine(Dir, ".minecraft", "versions", version, version + "-natives/"), false);
                    }
                }
                catch (Exception e)
                {
                    OMCLLog.WriteLog("[Tools_UnzipNatives]" + @e.Message, OMCLExceptionClass.DLL, OMCLExceptionType.Error);
                    throw new Exception("解压Natives文件时出现问题！");
                }
            }
        }

        public class FDTools
        {
            public static List<Task> search_task = new List<Task>();
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
                                search_task.Add(Task.Run(() => SearchDirTask(dir, filename, onlysearches, pathins, pathsames, result)));
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
                                search_task.Add(Task.Run(() => SearchFileTask(dir, filename, onlysearches, pathins, pathsames, result)));
                            }
                        }
                        catch { }
                    }
                }
            }
            /// <summary>
            /// 将一个文件夹下的子文件（夹）复制到一个可以不为空的文件夹。
            /// </summary>
            /// <param name="sourceDirName">要复制到destDirName的文件夹（一定是一个文件夹，且可以不为空）</param>
            /// <param name="destDirName">要将sourceDirName文件夹复制到的文件夹（一定是一个文件夹，且可以不为空，即会直接将sourceDirName下的子文件（夹）复制到destDirName）</param>
            /// <param name="CanJump">是否可以跳过已经复制过的文件</param>
            /// <exception cref="Exception"></exception>
            public static void DirectoryCopy(string sourceDirName, string destDirName, bool CanJump = true)
            {
                if (!Directory.Exists(sourceDirName))
                {
                    OMCLLog.WriteLog("[Tools_FDTools_DirectoryCopy]错误：复制文件夹时出现错误！找不到文件夹<" + sourceDirName + ">", OMCLExceptionClass.DLL, OMCLExceptionType.Error);
                    throw new Exception("错误：复制文件夹时出现错误！找不到文件夹<" + sourceDirName + ">");
                }
                if (!Directory.Exists(destDirName)) Directory.CreateDirectory(destDirName);
                string[] floders = Directory.GetDirectories(sourceDirName);
                if (floders != null || floders.Length != 0)
                {
                    foreach (string f in floders)
                    {
                        DirectoryCopy(f, destDirName + f.Replace(sourceDirName, ""));
                    }
                }
                string[] files = Directory.GetFiles(sourceDirName);
                if (files != null || files.Length != 0)
                {
                    foreach (string f in files)
                    {
                        if (File.Exists(destDirName + f.Replace(sourceDirName, ""))) 
                        {
                            if (CanJump)
                            {
                                continue;
                            }
                            else
                            {
                                OMCLLog.WriteLog("[Tools_FDTools_DirectoryCopy]错误：复制文件夹时出现错误！文件夹<" + destDirName + ">中已经存在文件<" + f + ">，且不允许跳过文件！", OMCLExceptionClass.DLL, OMCLExceptionType.Error);
                                throw new Exception("错误：复制文件夹时出现错误！文件夹<" + destDirName + ">中已经存在文件<" + Path.GetFileName(f) + ">");
                            }
                        }
                        File.Copy(f, destDirName + f.Replace(sourceDirName, ""));
                    }
                }
            }
        }
    }
    public class Link
    {
        public static List<Process> LinkProcess = new List<Process>();
        public static readonly string[] servers = { "frp.104300.xyz", "la.afrps.cn" };
        public static readonly string[] server_tokens = { "www.126126.xyz", "afrps.cn" };
        public static string FrpcIni = "[common]\nserver_addr = {server_url}\nserver_port = {server_port}\ntoken = {server_token}\n\n[{link_name}]\ntype = tcp\nlocal_ip = 127.0.0.1\nlocal_port = {client_port}\nremote_port = {random_num}";
        public static string StartLink(int client_port)
        {
            int ser = 0;
            long pingms = 10005;
            for (int i = 0; i < servers.Length; i++)
            {
                PingReply reply;
                try
                {
                    reply = new Ping().Send(servers[i], (int)pingms);
                }
                catch
                {
                    reply = null;
                }
                if (reply != null)
                {
                    if (reply.RoundtripTime < pingms)
                    {
                        pingms = reply.RoundtripTime;
                        ser = i;
                    }
                }
            }
            return StartLink(client_port, ser);
        }
        public static string StartLink(int client_port, int server_id)
        {
            return StartLink(client_port, servers[server_id], 7000, server_tokens[server_id]);
        }
        [STAThread]
        public static string StartLink(int client_port, string server_url, int server_port, string server_token)
        {
            PingReply reply;
            int trynum = 0;
        ping:
            try
            {
                reply = new Ping().Send(server_url, 5000);
            }
            catch (Exception e)
            {
                OMCLLog.WriteLog("[Tools_Link_StartLink]错误：请求服务器<" + server_url + ">响应时出现问题！<" + e.Message + ">", OMCLExceptionClass.DLL, OMCLExceptionType.Warning);
                throw new Exception("错误：请求服务器<" + server_url + ">响应时出现未知问题！请检查服务器ip或url是否正确！");
            }
            if (reply.Status == IPStatus.Success)
            {
                OMCLLog.WriteLog("[Tools_Link_StartLink]联机-server status：Got server result at " + DateTime.Now.ToString() + " by " + reply.RoundtripTime + " ms", OMCLExceptionClass.DLL, OMCLExceptionType.Message);
                try
                {
                    Directory.CreateDirectory(Tools.Dir + @"\OMCL\Link");
                }
                catch { }
                if (!File.Exists(Path.Combine(new string[] { Tools.Dir, "OMCL", "Link", "LICENSE" }))) File.WriteAllBytes(Path.Combine(new string[] { Tools.Dir, "OMCL", "Link", "LICENSE" }), Properties.Resources.link_LICENSE);
                if (!File.Exists(Path.Combine(new string[] { Tools.Dir, "OMCL", "Link", "frpc.exe" }))) File.WriteAllBytes(Path.Combine(new string[] { Tools.Dir, "OMCL", "Link", "frpc.exe" }), Properties.Resources.link_frpc);
                Random random = new Random();
                int r;
                while (true)
                {
                    try
                    {
                        r = random.Next(10005, 59005);
                        File.WriteAllText(Path.Combine(new string[] { Tools.Dir, "OMCL", "Link", "frpc.ini" }), FrpcIni.Replace("{server_url}", server_url).Replace("{server_port}", server_port.ToString()).Replace("{server_token}", server_token).Replace("{link_name}", $"OMCL_Link_{Process.GetProcessesByName("frpc").Length}").Replace("{client_port}", client_port.ToString()).Replace("{random_num}", r.ToString()));
                        using (Process process = new Process
                        {
                            StartInfo = new ProcessStartInfo
                            {
                                FileName = Path.Combine(new string[] { Tools.Dir, "OMCL", "Link", "frpc.exe" }),
                                Arguments = "-c \"" + Path.Combine(new string[] { Tools.Dir, "OMCL", "Link", "frpc.ini" }) + '\"',
                                CreateNoWindow = true,
                                UseShellExecute = false,
                                RedirectStandardOutput = true,
                            },
                        })
                        {
                            LinkProcess.Add(process);
                            process.Start();
                            using (StreamReader reader = process.StandardOutput)
                            {
                                bool IsOK = false;
                                while (true)
                                {
                                    string s = reader.ReadLine();
                                    if (s == null && process.HasExited)
                                    {
                                        OMCLLog.WriteLog("[Tools_Link_StartLink]错误：启动内网穿透（frpc.exe）服务时出现问题！进程已退出！", OMCLExceptionClass.DLL, OMCLExceptionType.Warning);
                                        throw new Exception("");
                                    }
                                    else if (s == null)
                                    {
                                        StopLink();
                                        OMCLLog.WriteLog("[Tools_Link_StartLink]错误：启动内网穿透（frpc.exe）服务时出现未知问题！", OMCLExceptionClass.DLL, OMCLExceptionType.Warning);
                                        throw new Exception("");
                                    }
                                    else if (s.Contains("control.go:172") && s.Contains("start proxy success"))
                                    {
                                        IsOK = true;
                                        break;
                                    }
                                    else if (s.Contains("control.go"))
                                    {
                                        break;
                                    }
                                }
                                if (IsOK) break;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        OMCLLog.WriteLog("[Tools_Link_StartLink]错误：启动内网穿透（frpc.exe）服务时出现问题<" + e.Message + ">！请检查！", OMCLExceptionClass.DLL, OMCLExceptionType.Warning);
                        throw new Exception("错误：启动内网穿透（frpc.exe）服务时出现问题！请检查！");
                    }
                }
                try
                {
                    File.Delete(Path.Combine(new string[] { Tools.Dir, "OMCL", "Link", "frpc.ini" }));
                }
                catch { }
                bool flag = false;
                int server_id = -1;
                for (int i = 0;i < servers.Length;i++)
                {
                    if (server_url == servers[i])
                    {
                        flag = true;
                        server_id = i;
                    }
                }
                string result;
                if (flag)
                {
                    result = server_id.ToString() + r.ToString();
                }
                else
                {
                    result = Convert.ToBase64String(Encoding.Default.GetBytes(server_url)) + "!" + Convert.ToBase64String(Encoding.Default.GetBytes(r.ToString()));
                }
                Thread thr = new Thread(new ThreadStart(() => Clipboard.SetText(result)));
                thr.SetApartmentState(ApartmentState.STA);
                thr.IsBackground = true;
                thr.Start();
                return result;
            }
            else if (trynum <= 3)
            {
                trynum++;
                goto ping;
            }
            else
            {
                OMCLLog.WriteLog("[Tools_Link_StartLink]错误：请求服务器<" + server_url + ">响应时出现问题！<" + reply.Status.ToString() + ">", OMCLExceptionClass.DLL, OMCLExceptionType.Warning);
                throw new Exception("错误：请求服务器<" + server_url + ">响应时出现问题！<" + reply.Status.ToString() + ">");
            }
        }
        public static void StopLink()
        {
            foreach (Process p in LinkProcess)
            {
                try
                {
                    p.Kill();
                }
                catch { }
            }
            LinkProcess = new List<Process>();
        }
        public static Server JoinLink(string code)
        {
            try
            {
                string[] sp = code.Split('!');
                if (sp.Length == 1)
                {
                    return new Server
                    {
                        server_url_or_ip = servers[int.Parse(sp[0][0].ToString())],
                        server_port = int.Parse(sp[0].Remove(0,1)),// int.Parse(sp[0].Replace(sp[0][0].ToString(),"")),
                    };
                }
                else
                {
                    return new Server
                    {
                        server_url_or_ip = Encoding.Default.GetString(Convert.FromBase64String(sp[0])),
                        server_port = int.Parse(Encoding.Default.GetString(Convert.FromBase64String(sp[1]))),
                    };
                }
            }
            catch (Exception e)
            {
                OMCLLog.WriteLog("[Tools_Link_JoinLink]错误：尝试加入联机<" + code + ">时出现错误！<" + e.Message + ">", OMCLExceptionClass.DLL, OMCLExceptionType.Warning);
                throw new Exception("错误：尝试加入联机<" + code + ">时出现错误！请检查您的联机码是否正确！");
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
                    OMCLLog.WriteLog("[Tools_GetLogin_MicrosoftLogin]登录失败！", OMCLExceptionClass.DLL, OMCLExceptionType.Error);
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
                    OMCLLog.WriteLog("[Tools_GetLogin_MicrosoftLogin]登录失败！", OMCLExceptionClass.DLL, OMCLExceptionType.Error);
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
                OMCLLog.WriteLog("[GetLogin_MojangLogin]失败！该账号可能不拥有Minecraft！", OMCLExceptionClass.DLL, OMCLExceptionType.Error);
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
                OMCLLog.WriteLog("[GetLogin_MojangLogin]失败！该账号可能不拥有Minecraft！", OMCLExceptionClass.DLL, OMCLExceptionType.Error);
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
                OMCLLog.WriteLog("[GetLogin_UnifiedPassLogin]失败！该账号可能错误！", OMCLExceptionClass.DLL, OMCLExceptionType.Error);
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
                OMCLLog.WriteLog("[GetLogin_RefreshUnifiedPassLogin]失败！该账号可能错误！", OMCLExceptionClass.DLL, OMCLExceptionType.Error);
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
                        uuid = MD5UUID(name),
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
                    uuid = MD5UUID(name),
                };
            }
        }
        private static string MD5UUID(string name)
        {
            /*
            //Cause by RandomUUID
            string result = "";
            char[] canuse = { '1', '2', '3', '4', '5', '6', '7', '8', '9', '0', 'a', 'b', 'c', 'd', 'e', 'f' };
            while (true)
            {
                if (result.Length == 31) break;
                Random random = new Random();
                result += canuse[random.Next(0, 17)];
            }
            return result;
            */
            using (MD5 md5 = MD5.Create())
            {
                return new Guid(md5.ComputeHash(Encoding.Default.GetBytes(name))).ToString().Replace("-", "");
            }
        }
    }
    public class InstallMinecraft
    {
        public class ForgeInstall
        {
            public static void InstallForge(string version, string java, string forgeFile)
            {
                try
                {
                    Directory.Delete(Path.Combine(new string[] { Path.GetTempPath(), "OMCL", "temp" }), true);
                }
                catch { }
                Directory.CreateDirectory(Path.Combine(new string[] { Path.GetTempPath(), "OMCL", "temp" }));
                Tools.UnZipFile(forgeFile, Path.Combine(new string[] { Path.GetTempPath(), "OMCL", "temp/" }), true);
                JArray arguments;
                string minecraftArguments;
                JArray libraries;
                JArray jvm;
                if (File.Exists(Path.Combine(new string[] { Path.GetTempPath(), "OMCL", "temp", "version.json" }))) Tools.ReadVersionJson(Path.Combine(new string[] { Path.GetTempPath(), "OMCL", "temp", "version.json" }), out arguments, out minecraftArguments, out libraries, out jvm, false);
                else if (File.Exists(Path.Combine(new string[] { Path.GetTempPath(), "OMCL", "temp", "install_profile.json" }))) Tools.ReadVersionJson(JObject.Parse(File.ReadAllText(Path.Combine(new string[] { Path.GetTempPath(), "OMCL", "temp", "install_profile.json" }))).SelectToken("versionInfo").ToString(), out arguments, out minecraftArguments, out libraries, out jvm, true);
                else
                {

                }

            }
        }
        public class MinecraftInstall
        {
            public static void InstallMinecraftVersion(string versionName, string installName, bool IsDownloadLibraries = true, bool IsDownloadAssets = false)
            {
                Directory.CreateDirectory(Path.Combine(new string[] { Tools.Dir, ".minecraft", "versions", versionName }));
                if (!File.Exists(Path.Combine(new string[] { Tools.Dir, ".minecraft", "versions", versionName, versionName + ".json" }))) File.WriteAllText(Path.Combine(new string[] { Tools.Dir, ".minecraft", "versions", versionName, versionName + ".json" }), Web.Get(Tools.DownloadMinecraftFileUrl + @"version/" + installName + @"/json"));
                if (!File.Exists(Path.Combine(new string[] { Tools.Dir, ".minecraft", "versions", versionName, versionName + ".jar" }))) HttpFile.Start(10, Tools.DownloadMinecraftFileUrl + @"version/" + installName + @"/client", Path.Combine(new string[] { Tools.Dir, ".minecraft", "versions", versionName, versionName + ".jar" }));
                if (IsDownloadLibraries) Tools.DownloadMissFiles(versionName, false);
                if (IsDownloadAssets) Tools.DownloadMissAsstes(versionName);
            }
        }
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
                Tools.UnZipFile(ForgePath, Path.GetTempPath() + @"\OMCL\temp\", true);
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
                    OMCLLog.WriteLog("[InstallMinecraft_InstallForge]安装Forge时出现错误：找不到版本json，该版本可能有问题！", OMCLExceptionClass.DLL, OMCLExceptionType.Error);
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
                                OMCLLog.WriteLog("[InstallMinecraft_InstallForge]安装Forge时出现错误：下载文件 " + Tools.Dir + @"\.minecraft\libraries\" + path + " 时出现错误！" + @e.Message, OMCLExceptionClass.DLL, OMCLExceptionType.Error);
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
                                    /*OMCLLog.WriteLog("[InstallMinecraft_InstallForge]安装Forge时出现错误：下载文件 " + Tools.Dir + @"\.minecraft\libraries\" + temp + " 时出现错误！" + @e.Message, OMCLExceptionClass.DLL, OMCLExceptionType.Error);
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