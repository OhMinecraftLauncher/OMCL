using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OMCL_DLL.Tools.Download;
using OMCL_DLL.Tools.LocalException;
using OMCL_DLL.Tools.Login.MicrosoftLogin;
using OMCL_DLL.Tools.Login.Result;
using OMCL_DLL.Tools.Login.Web;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.WebSockets;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using TextCopy;
using Path = System.IO.Path;

namespace OMCL_DLL.Tools
{
    public class Tools
    {
        public static int DownloadThreadNum = 1;
        public static DownloadServer MCFileDownloadServer = DownloadServers.MCFileDownloadServers[0];
        public static DownloadSource Source = DownloadSource.Minecraft;
        private static readonly string OMCLver = "0.0.0.3";
        public static string Dir = Directory.GetCurrentDirectory();
        public readonly static Clipboard clipboard = new();
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
                if (!(dir.EndsWith('\\') || !dir.EndsWith('/')))
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
                ZipInputStream s = new(File.OpenRead(file));
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
                throw new OMCLException("解压 " + file + " 时出现错误！", e);
            }
        }
        /// <summary>
        /// 为一个版本补全Libraries和Natives
        /// </summary>
        /// <param name="version">版本名称</param>
        /// <param name="IsLaunch">是否是启动时地补全文件（如果是，下载失败的文件将自动列入LaunchNoFile或LaunchNoFileNatives，进行启动时跳过操作。如果否，文件下载失败将不会有任何反应，直接跳过下载下一个文件。）</param>
        public static void DownloadMissFiles(Version ver)
        {
            try
            {
                int threadNum = DownloadThreadNum;
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
                                HttpFile.Start(threadNum, MCFileDownloadServer.Optifine + '/' + temppath2[0] + "/" + temppath1[0].Replace(temppath2[0] + "_", "").Replace("_" + temppath2.Last(), "") + "/" + temppath2.Last(), Path.Combine(new string[] { Dir, ".minecraft", "libraries", ver.libraries[i] }));
                            }
                            catch (Exception e)
                            {
                                if (e.Message.Contains("404"))
                                {
                                    OMCLLog.WriteLog("[Tools_DownloadMissFiles]跳过文件 " + Path.Combine(new string[] { Dir, ".minecraft", "libraries", ver.libraries[i] }) + " 的下载！", OMCLExceptionClass.DLL, OMCLExceptionType.Warning);
                                }
                            }
                        }
                        else
                        {
                            try
                            {
                                HttpFile.Start(threadNum, MCFileDownloadServer.Libraries + '/' + ver.libraries[i].Replace('\\', '/'), Path.Combine(new string[] { Dir, ".minecraft", "libraries", ver.libraries[i] }));
                            }
                            catch (Exception e)
                            {
                                if (e.Message.Contains("404"))
                                {
                                    OMCLLog.WriteLog("[Tools_DownloadMissFiles]跳过文件 " + Path.Combine(new string[] { Dir, ".minecraft", "libraries", ver.libraries[i] }) + " 的下载！", OMCLExceptionClass.DLL, OMCLExceptionType.Warning);
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
                            HttpFile.Start(threadNum, MCFileDownloadServer.Libraries + '/' + ver.natives[i].Replace('\\', '/'), Path.Combine(new string[] { Dir, ".minecraft", "libraries", ver.natives[i] }));
                        }
                        catch (Exception e)
                        {
                            if (e.Message.Contains("404"))
                            {
                                OMCLLog.WriteLog("[Tools_DownloadMissFiles]跳过文件 " + Path.Combine(new string[] { Dir, ".minecraft", "libraries", ver.natives[i] }) + " 的下载！", OMCLExceptionClass.DLL, OMCLExceptionType.Warning);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                OMCLLog.WriteLog("[Tools_DownloadMissFile]" + @e.Message, OMCLExceptionClass.DLL, OMCLExceptionType.Error);
                throw new OMCLException("补全文件时出现错误！", e);
            }
        }
        /// <summary>
        /// 为一个版本补全Assets
        /// </summary>
        /// <param name="version">版本名称</param>
        public static async void DownloadMissAsstes(string version)
        {
            int threadNum = DownloadThreadNum;
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
                    throw new OMCLException("[Tools_DownloadMissAssets]获取版本json时出现错误，请检查您的版本是否正确！");
                }
                if (versionAssetIndex.SelectToken("assetIndex.url") == null)
                {
                    JObject vi = JObject.Parse(await Web.Get(Tools.MCFileDownloadServer.VersionInfo[1]));
                    JArray vis = (JArray)vi.SelectToken("versions");
                    string vjurl = null;
                    foreach (JToken vip in vis)
                    {
                        if (vip.SelectToken("id").ToString() == ver.assets || vip.SelectToken("id").ToString() == ver.id) vjurl = vip.SelectToken("url").ToString();
                    }
                    string versionjson = await Web.Get(vjurl);
                    if (versionjson == null || versionjson == "" || versionjson == string.Empty)
                    {
                        OMCLLog.WriteLog("[Tools_DownloadMissAssets]获取版本json时出现错误，请检查您的网络！", OMCLExceptionClass.DLL, OMCLExceptionType.Error);
                        throw new OMCLException("[Tools_DownloadMissAssets]获取版本json时出现错误，请检查您的网络！");
                    }
                    versionAssetIndex = JObject.Parse(versionjson);
                }
                assets = await Web.Get(versionAssetIndex.SelectToken("assetIndex.url").ToString());
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
                                HttpFile.Start(threadNum, MCFileDownloadServer.Assets + '/' + hash[0] + hash[1] + '/' + hash, Path.Combine(new string[] { Dir, ".minecraft", "resources", pn }));
                            }
                            catch
                            {
                                try
                                {
                                    HttpFile.Start(threadNum, MCFileDownloadServer.Assets + '/' + hash[0] + hash[1] + '/' + hash, Path.Combine(new string[] { Dir, ".minecraft", "resources", pn }));
                                }
                                catch (Exception e)
                                {
                                    OMCLLog.WriteLog("[Tools_DownloadMissAssets]下载文件 " + hash + "<" + Path.Combine(new string[] { Dir, ".minecraft", "resources", pn }) + ">" + " 时出现错误！" + @e.Message, OMCLExceptionClass.DLL, OMCLExceptionType.Error);
                                    throw new OMCLException("[Tools_DownloadMissAssets]下载文件 " + Path.GetFileName(pn) + " 时出现错误！", e);
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
                            HttpFile.Start(threadNum, MCFileDownloadServer.Assets + '/' + hash[0] + hash[1] + '/' + hash, Path.Combine(new string[] { Dir, ".minecraft", "assets", "objects", hash[0].ToString() + hash[1].ToString(), hash }));
                        }
                        catch
                        {
                            try
                            {
                                HttpFile.Start(threadNum, MCFileDownloadServer.Assets + '/' + hash[0] + hash[1] + '/' + hash, Path.Combine(new string[] { Dir, ".minecraft", "assets", "objects", hash[0].ToString() + hash[1].ToString(), hash }));
                            }
                            catch (Exception e)
                            {
                                OMCLLog.WriteLog("[Tools_DownloadMissAssets]下载文件 " + hash + " 时出现错误！" + @e.Message, OMCLExceptionClass.DLL, OMCLExceptionType.Error);
                                throw new OMCLException("[Tools_DownloadMissAssets]下载文件 " + hash + " 时出现错误！", e);
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
            Source = downloadSource;
            if (downloadSource == DownloadSource.Minecraft)
            {
                MCFileDownloadServer = DownloadServers.MCFileDownloadServers[0];
            }
            else if (downloadSource == DownloadSource.BMCLAPI)
            {
                MCFileDownloadServer = DownloadServers.MCFileDownloadServers[1];
            }
            else if (downloadSource == DownloadSource.MCBBS)
            {
                MCFileDownloadServer = DownloadServers.MCFileDownloadServers[2];
            }
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
                throw new OMCLException("获取Java列表时出现错误！", e);
            }
        }
        private static void UnzipNatives(Version version)
        {
            try
            {
                List<string> natives = version.natives.ToList();
                for (int i = 0; i < natives.Count; i++)
                {
                    if (!File.Exists(Path.Combine(new string[] { Dir, ".minecraft", "libraries", natives[i] }))) continue;
                    UnZipFile(Path.Combine(new string[] { Dir, ".minecraft", "libraries", natives[i] }), Path.Combine(Dir, ".minecraft", "versions", version.version, version.version + "-natives/"), false);
                }
            }
            catch (Exception e)
            {
                OMCLLog.WriteLog("[Tools_UnzipNatives]" + @e.Message, OMCLExceptionClass.DLL, OMCLExceptionType.Error);
                throw new OMCLException("解压Natives文件时出现问题！", e);
            }
        }
        public static Version ReadVersionJson(string version)
        {
            Version result = ReadVersionJson(Path.Combine(new string[] { Dir, ".minecraft", "versions", version, version + ".json" }), out _, out _, out _);
            result.version = version;
            return result;
        }
        public static Version ReadVersionJson(string FilePathOrText, out JArray arguments, out JArray libraries, out JArray jvm)
        {
            string text = FilePathOrText;
            try
            {
                if (File.Exists(text))
                {
                    text = File.ReadAllText(text);
                }
            }
            catch { }
            JToken json = JToken.Parse(text);
            string sysname = GetSystemName();
            string arg = json.SelectToken("minecraftArguments")?.ToString();
            if (arg == null)
            {
                arguments = (JArray)json.SelectToken("arguments.game");
                arg = "";
                foreach (JToken arg_game in arguments)
                {
                    bool cr = true;
                    try
                    {
                        JArray rules = (JArray)arg_game.SelectToken("rules", true);
                        cr = CheckRuleInJson(rules, sysname);
                    }
                    catch { }
                    if (cr)
                    {
                        try
                        {
                            JArray values = (JArray)arg_game.SelectToken("value", true);
                            foreach (JToken value in values)
                            {
                                arg += value.ToString() + ' ';
                            }
                        }
                        catch
                        {
                            try
                            {
                                arg += arg_game.SelectToken("value", true).ToString() + ' ';
                            }
                            catch
                            {
                                arg += arg_game.ToString() + ' ';
                            }
                        }
                    }
                }
                arg = arg.Remove(arg.Length - 1);
            }
            else
            {
                arguments = null;
            }
            libraries = (JArray)json.SelectToken("libraries");
            char filesp = '/';
            if (sysname == "windows") filesp = '\\';
            List<string> L_libraries = new();
            List<string> L_natives = new();
            foreach (JToken lib in libraries)
            {
                string name = lib.SelectToken("name").ToString();
                string[] sp_name = name.Split(':');
                string path = sp_name[0].Replace('.',filesp);
                for (int i = 1;i < sp_name.Length;i++)
                {
                    path += filesp + sp_name[i];
                }
                string path_noafter = path + filesp + sp_name[^2] + '-' + sp_name[^1];
                path = path_noafter + ".jar";
                bool cr = true;
                try
                {
                    JArray rules = (JArray)lib.SelectToken("rules", true);
                    cr = CheckRuleInJson(rules, sysname);
                }
                catch { }
                if (sp_name.Last() == "natives-" + sysname + (RuntimeInformation.OSArchitecture == Architecture.X64 ? "" : ('-' + RuntimeInformation.OSArchitecture.ToString().ToLower())))//To Support the version above 1.19.3
                {
                    L_natives.Add(path);
                }
                else if (sp_name.Last().StartsWith("natives-")) continue;
                else if (cr)
                {
                    L_libraries.Add(path);
                }
                JToken natives_sysname = lib.SelectToken("downloads.classifiers");
                if (natives_sysname != null)
                {
                    try
                    {
                        _ = natives_sysname.SelectToken("natives-" + sysname, true);
                        L_natives.Add(path_noafter + "-natives-" + sysname + ".jar");
                    }
                    catch { }
                }
                natives_sysname = lib.SelectToken("natives." + sysname);
                if (natives_sysname != null)
                {
                    string na = natives_sysname.ToString();
                    L_natives.Add((path_noafter + '-' + na + ".jar").Replace("${arch}", Environment.Is64BitOperatingSystem ? "64" : "32"));
                }
            }
            L_libraries = L_libraries.GroupBy(x => x).Select(y => y.First()).ToList();
            L_natives = L_natives.GroupBy(x => x).Select(y => y.First()).ToList();
            string s_jvm = "";
            jvm = (JArray)json.SelectToken("arguments.jvm");
            if (jvm != null)
            {
                foreach (JToken f_jvm in jvm)
                {
                    bool cr = true;
                    try
                    {
                        JArray rules = (JArray)f_jvm.SelectToken("rules", true);
                        cr = CheckRuleInJson(rules, sysname);
                    }
                    catch { }
                    if (cr)
                    {
                        try
                        {
                            JArray values = (JArray)f_jvm.SelectToken("value", true);
                            foreach (JToken value in values)
                            {
                                s_jvm += '\"' + value.ToString().Replace(@"\u003d", " ") + '\"' + ' ';
                            }
                        }
                        catch
                        {
                            try
                            {
                                s_jvm += '\"' + f_jvm.SelectToken("value", true).ToString().Replace(@"\u003d", " ") + '\"' + ' ';
                            }
                            catch
                            {
                                s_jvm += '\"' + f_jvm.ToString().Replace(@"\u003d", " ") + '\"' + ' ';
                            }
                        }
                    }
                }
                s_jvm = s_jvm.Remove(s_jvm.Length - 1);
            }
            return new Version
            {
                arguments = arg,
                jvm = s_jvm,
                libraries = L_libraries.ToArray(),
                natives = L_natives.ToArray(),
                mainClass = json.SelectToken("mainClass").ToString(),
                assets = json.SelectToken("assets").ToString(),
                loggingFile = new LoggingFile
                {
                    FileName = json.SelectToken("logging.client.file.id")?.ToString(),
                    FileUrl = json.SelectToken("logging.client.file.url")?.ToString(),
                },
                version = null,
                id = json.SelectToken("id")?.ToString(),
                type = json.SelectToken("type")?.ToString(),
            };
        }
        public static bool CheckRuleInJson(JArray rules, string sysname = null)
        {
            sysname ??= GetSystemName();
            foreach (JToken rule in rules)
            {
                JToken t_name = rule.SelectToken("os.name");
                JToken t_arch = rule.SelectToken("os.arch");
                JToken t_version = rule.SelectToken("os.version");
                string action = rule.SelectToken("action").ToString();
                bool r_name = true; 
                bool r_arch = true;
                bool r_version = true;
                if (t_name != null)
                {
                    string name = t_name.ToString();
                    if (name != sysname)
                    {
                        r_name = false;
                    }
                }
                if (t_arch != null)
                {
                    string arch = t_arch.ToString();
                    if (RuntimeInformation.OSArchitecture.ToString().ToLower() != arch)
                    {
                        r_arch = false;
                    }
                }
                if (t_version != null)
                {
                    string version = t_version.ToString().Replace("\\\\", "\\");
                    string sysver = Environment.OSVersion.Version.ToString();
                    if (!Regex.Match(sysver, version).Success) 
                    {
                        t_version = false;
                    }
                }
                if (r_name && r_arch && r_version)
                {
                    if (t_name == null && t_arch == null && t_version == null)
                    {
                        continue;
                    }
                    else
                    {
                        if (action == "allow")
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
                else
                {
                    if (action == "allow")
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public class LaunchMinecraft
        {
            public bool IsIsolation = true;
            public int MaxMem = 2048;
            private string NativesPath = "";
            public string jvm = "", otherArguments = "";
            public delegate void MinecraftCrashedDelegate(CrashMessage crashMessage);
            public event MinecraftCrashedDelegate OnMinecraftCrash;
            public Process process = new();
            public Server server = null;
            private string version = "";
            private List<string> OutputLines = new();
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
            public async Task LaunchGame(string java, string version, string playerName)
            {
                try
                {
                    OfflineLoginResult result = await GetLogin.OfflineLogin(playerName);
                    LaunchGame(java, version, result.name, result.uuid, result.uuid);
                    return;
                }
                catch (Exception e)
                {
                    OMCLLog.WriteLog("[Tools_LaunchGame]" + @e.Message, OMCLExceptionClass.DLL, OMCLExceptionType.Error);
                    throw new OMCLException("启动失败！", e);
                }
            }
            /// <summary>
            /// 使用AuthlibInjector登录启动一个Minecraft版本
            /// </summary>
            /// <param name="java">java.exe或javaw.exe的路径</param>
            /// <param name="version">版本名称</param>
            /// <param name="login">提供一个AuthlibInjectorLoginResult用于登录</param>
            /// <param name="result">返回一个新的AuthlibInjectorLoginResult</param>
            public async Task<AuthlibInjectorLoginResult> LaunchGame(string java, string version, AuthlibInjectorLoginResult login, int user = -1)
            {
                try
                {
                    AuthlibInjectorLoginResult result = await GetLogin.RefreshAuthlibInjectorLogin(login);
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
                        if (!File.Exists(Path.Combine(new string[] { Dir, "OMCL", "Login", "authlib-injector.jar" }))) HttpFile.Start(DownloadThreadNum, MCFileDownloadServer.authlib_injector + "/artifact/52/authlib-injector-1.2.4.jar", Path.Combine(new string[] { Dir, "OMCL", "Login", "authlib-injector.jar" }));
                    }
                    catch
                    {
                        try
                        {
                            Directory.CreateDirectory(Path.Combine(new string[] { Dir, "OMCL", "Login" }));
                            HttpFile.Start(DownloadThreadNum, MCFileDownloadServer.authlib_injector + "/artifact/52/authlib-injector-1.2.4.jar", Path.Combine(new string[] { Dir, "OMCL", "Login", "authlib-injector.jar" }));
                        }
                        catch (Exception e)
                        {
                            OMCLLog.WriteLog("[Tools_LaunchGame]错误：下载authlib-injector.jar时出现错误，请检查你的网络是否正常！", OMCLExceptionClass.DLL, OMCLExceptionType.Error);
                            throw new OMCLException("启动失败，下载authlib-injector.jar时出现错误，请检查你的网络是否正常！", e);
                        }
                    }
                    string Base64;
                    try
                    {
                        Base64 = Convert.ToBase64String(Encoding.Default.GetBytes(await Web.Get(login.server)));
                    }
                    catch (Exception e)
                    {
                        OMCLLog.WriteLog("[Tools_LaunchGame]错误：获取信息时出现错误，请检查你的网络是否正常！", OMCLExceptionClass.DLL, OMCLExceptionType.Error);
                        throw new OMCLException("启动失败，获取信息时出现错误，请检查你的网络是否正常！", e);
                    }
                    try
                    {
                        if (jvm.Last() == ' ') jvm += "-javaagent:" + Path.Combine(new string[] { Dir, "OMCL", "Login", "authlib-injector.jar" }) + "=" + login.server + " -Dauthlibinjector.yggdrasil.prefetched=" + Base64;
                        else jvm += " -javaagent:" + Path.Combine(new string[] { Dir, "OMCL", "Login", "authlib-injector.jar" }) + "=" + login.server + " -Dauthlibinjector.yggdrasil.prefetched=" + Base64;
                    }
                    catch
                    {
                        jvm += "-javaagent:" + Path.Combine(new string[] { Dir, "OMCL", "Login", "authlib-injector.jar" }) + "=" + login.server + " -Dauthlibinjector.yggdrasil.prefetched=" + Base64;
                    }
                    LaunchGame(java, version, playerName, uuid, token);
                    return result;
                }
                catch (Exception e)
                {
                    OMCLLog.WriteLog("[Tools_LaunchGame]" + @e.Message, OMCLExceptionClass.DLL, OMCLExceptionType.Error);
                    throw new OMCLException("启动失败！", e);
                }
            }
            /// <summary>
            /// 使用统一通行证登录启动一个Minecraft版本
            /// </summary>
            /// <param name="java">java.exe或javaw.exe的路径</param>
            /// <param name="version">版本名称</param>
            /// <param name="login">提供一个UnifiedPassLoginResult用于登录</param>
            /// <param name="result">返回一个新的UnifiedPassLoginResult</param>
            public async Task<UnifiedPassLoginResult> LaunchGame(string java, string version, UnifiedPassLoginResult login)
            {
                try
                {
                    UnifiedPassLoginResult result = await GetLogin.RefreshUnifiedPassLogin(login.serverid, login.client_token, login.access_token);
                    string playerName;
                    string uuid;
                    string token;
                    playerName = result.name;
                    uuid = result.uuid;
                    token = result.access_token;
                    try
                    {
                        if (!File.Exists(Path.Combine(new string[] { Dir, "OMCL", "Login", "nide8auth.jar" }))) HttpFile.Start(DownloadThreadNum, "https://login.mc-user.com:233/index/jar", Path.Combine(new string[] { Dir, "OMCL", "Login", "nide8auth.jar" }));
                    }
                    catch
                    {
                        try
                        {
                            Directory.CreateDirectory(Path.Combine(new string[] { Dir, "OMCL", "Login" }));
                            HttpFile.Start(DownloadThreadNum, "https://login.mc-user.com:233/index/jar", Path.Combine(new string[] { Dir, "OMCL", "Login", "nide8auth.jar" }));
                        }
                        catch (Exception e)
                        {
                            OMCLLog.WriteLog("[Tools_LaunchGame]启动失败，下载nide8auth.jar文件时出现错误，请检查您的网络！", OMCLExceptionClass.DLL, OMCLExceptionType.Error);
                            throw new OMCLException("启动失败，下载nide8auth.jar文件时出现错误，请检查您的网络！", e);
                        }
                    }
                    try
                    {
                        if (jvm.Last() == ' ') jvm += "-javaagent:" + Path.Combine(new string[] { Dir, "OMCL", "Login", "nide8auth.jar" }) + "=" + login.serverid + " -Dnide8auth.client=true";
                        else jvm += " -javaagent:" + Path.Combine(new string[] { Dir, "OMCL", "Login", "nide8auth.jar" }) + "=" + login.serverid + " -Dnide8auth.client=true";
                    }
                    catch
                    {
                        jvm = "-javaagent:" + Path.Combine(new string[] { Dir, "OMCL", "Login", "nide8auth.jar" }) + "=" + result.serverid + " -Dnide8auth.client=true";
                    }
                    LaunchGame(java, version, playerName, uuid, token);
                    return result;
                }
                catch (Exception e)
                {
                    OMCLLog.WriteLog("[Tools_LaunchGame]" + @e.Message, OMCLExceptionClass.DLL, OMCLExceptionType.Error);
                    throw new OMCLException("启动失败！", e);
                }
            }
            /// <summary>
            /// 使用Microsoft登录启动一个Minecraft版本
            /// </summary>
            /// <param name="java">java.exe或javaw.exe的路径</param>
            /// <param name="version">版本名称</param>
            /// <param name="login">提供一个MicrosoftLoginResult以进行登录</param>
            /// <param name="result">返回一个新的MicrosoftLoginResult</param>
            public async Task<MicrosoftLoginResult> LaunchGame(string java, string version, MicrosoftLoginResult login)
            {
                try
                {
                    MicrosoftLoginResult result = await MicrosoftLogin.RefreshLogin(login);
                    string playerName;
                    string uuid;
                    string token;
                    playerName = result.name;
                    uuid = result.uuid;
                    token = result.access_token;
                    LaunchGame(java, version, playerName, uuid, token, LoginType.Microsoft);
                    return result;
                }
                catch (Exception e)
                {
                    OMCLLog.WriteLog("[Tools_LaunchGame]" + @e.Message, OMCLExceptionClass.DLL, OMCLExceptionType.Error);
                    throw new OMCLException("启动失败！", e);
                }
            }
            /// <summary>
            /// 使用Mojang登录启动一个Minecraft版本
            /// </summary>
            /// <param name="java">java.exe或javaw.exe的路径</param>
            /// <param name="version">版本名称</param>
            /// <param name="login">提供一个MojangLoginResult以进行登录</param>
            /// <param name="result">返回一个新的MojangLoginResult</param>
            public async Task<MojangLoginResult> LaunchGame(string java, string version, MojangLoginResult login)
            {
                try
                {
                    MojangLoginResult result = await GetLogin.RefreshMojangLogin(login.access_token);
                    string playerName;
                    string uuid;
                    string token;
                    playerName = result.name;
                    uuid = result.uuid;
                    token = result.access_token;
                    LaunchGame(java, version, playerName, uuid, token);
                    return result;
                }
                catch (Exception e)
                {
                    OMCLLog.WriteLog("[Tools_LaunchGame]" + @e.Message, OMCLExceptionClass.DLL, OMCLExceptionType.Error);
                    throw new OMCLException("启动失败！", e);
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
            /// <param name="loginType">一个LoginType枚举类型，指示登录方式是否是微软登录</param>
            public void LaunchGame(string java, string version, string playerName, string uuid, string token, LoginType loginType = LoginType.Other)
            {
                string sysname = GetSystemName();
                char pathsp = ':';
                if (sysname == "windows") pathsp = ';';
                OutputLines = new List<string>();
                IsMinecraftCrashed = true;
                this.version = version;
                string VersionJarFile = Path.Combine(new string[] { Dir, ".minecraft", "versions", version, version + ".jar" });
                if (!File.Exists(VersionJarFile) || !File.Exists(Path.Combine(new string[] { Dir, ".minecraft", "versions", version, version + ".json" })))
                {
                    OMCLLog.WriteLog("[Tools_LaunchGame]版本 " + version + " 有问题，请重新下载！", OMCLExceptionClass.DLL, OMCLExceptionType.Error);
                    throw new OMCLException("版本有问题！请重新下载！");
                }
                Version ver = ReadVersionJson(version);
                DownloadMissFiles(ver);
                string GameCMD = "";
                if (NativesPath == "")
                {
                    NativesPath = Path.Combine(new string[] { Dir, ".minecraft", "versions", version, version + "-natives" });
                    UnzipNatives(ver);
                }
                string classpath = "";
                foreach (string lib in ver.libraries)
                {
                    classpath += Path.Combine(new string[] { Dir, ".minecraft", "libraries", lib }) + pathsp;
                }
                classpath += VersionJarFile;
                string ver_jvm = ver.jvm;
                /*
                bool IsLoggingFileOK = true;
                try
                {
                    if (ver.loggingFile.FileUrl != null && ver.loggingFile.FileName != null)
                    {
                        if (!File.Exists(Path.Combine(new string[] { Dir, ".minecraft", ver.loggingFile.FileName })))
                        {
                            HttpFile.Start(DownloadThreadNum, ver.loggingFile.FileUrl, Path.Combine(new string[] { Dir, ".minecraft", ver.loggingFile.FileName }));
                        }
                    }
                    else
                    {
                        IsLoggingFileOK = false;
                    }
                }
                catch
                {
                    IsLoggingFileOK = false;
                }
                */
                if (ver_jvm != null)
                {
                    GameCMD += (MaxMem > 0 ? ("-Xmx" + MaxMem + "m ") : "") + "-Dlog4j2.formatMsgNoLookups=true " /*+ (IsLoggingFileOK ? "\"-Dlog4j.configurationFile=${path}\" ".Replace("${path}", Path.Combine(new string[] { Dir, ".minecraft", ver.loggingFile.FileName })) : "")*/ + ver_jvm.Replace("${natives_directory}", NativesPath).Replace("${launcher_name}", "OMCL").Replace("${launcher_version}", OMCLver).Replace("${classpath}", classpath) + ' ';
                }
                else
                {
                    GameCMD += (MaxMem > 0 ? ("-Xmx" + MaxMem + "m ") : "") + "-Dlog4j2.formatMsgNoLookups=true " /*+ (IsLoggingFileOK ? "\"-Dlog4j.configurationFile=${path}\" ".Replace("${path}", Path.Combine(new string[] { Dir, ".minecraft", ver.loggingFile.FileName })) : "")*/ + "-XX:HeapDumpPath=MojangTricksIntelDriversForPerformance_javaw.exe_minecraft.exe.heapdump -Djava.library.path=\"" + NativesPath + "\" " + ((sysname == "windows" && Regex.IsMatch(Environment.OSVersion.Version.ToString(), "^10\\.")) ? "\"-Dos.name=Windows 10\" -Dos.version=10.0 " : "") + (sysname == "osx" ? "-XstartOnFirstThread " : "") + "-Dminecraft.launcher.brand=OMCL " + "-Dminecraft.launcher.version=" + OMCLver + ' ' + "-cp " + classpath + ' ';
                }
                if (jvm != "")
                {
                    GameCMD += jvm + ' ';
                }
                GameCMD += ver.mainClass + ' ';
                GameCMD += (ver.arguments + (otherArguments == "" ? "" : ' ' + otherArguments) + (server == null ? "" : (" --server " + server.server_url_or_ip + " --port " + server.server_port))).Replace("${auth_player_name}", playerName).Replace("${version_name}", '\"' + version + '\"').Replace("${game_directory}", '\"' + (IsIsolation ? Path.Combine(new string[] { Dir, ".minecraft", "versions", version }) : Path.Combine(new string[] { Dir, ".minecraft" })) + '\"').Replace("${assets_root}", '\"' + Path.Combine(new string[] { Dir, ".minecraft", "asstes" }) + '\"').Replace("${assets_index_name}", ver.assets).Replace("${auth_uuid}", uuid).Replace("${auth_access_token}", token).Replace("${user_type}", loginType == LoginType.Microsoft ? "msa" : "mojang").Replace("${version_type}", '\"' + ver.type + "/OMCL " + OMCLver + '\"').Replace("${auth_session}", "token:" + token);
                Console.WriteLine("启动脚本如下：\n" + '\"' + java + '\"' + ' ' + GameCMD);
                OMCLLog.WriteLog("[Tools_LaunchGame]启动脚本如下：" + ("\"" + java + "\" " + GameCMD).Replace(token, "access_token隐藏").Replace(uuid, "uuid隐藏").Replace(playerName, "玩家名称隐藏") + "。", OMCLExceptionClass.DLL, OMCLExceptionType.Message);
                process = new()
                {
                    StartInfo = new()
                    {
                        FileName = java,
                        Arguments = GameCMD,
                        RedirectStandardError = true,
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WorkingDirectory = IsIsolation ? Path.Combine(new string[] { Dir, ".minecraft", "versions", version }) : Path.Combine(new string[] { Dir, ".minecraft" }),
                    },
                };
                process.OutputDataReceived += Process_OutputDataReceived;
                process.ErrorDataReceived += Process_OutputDataReceived;
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                new Thread(new ThreadStart(WaitMinecraft))
                {
                    IsBackground = false
                }.Start();
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
                            string s = OutputLines[i];
                            if (s != null && (s.Contains("Exception") || s.Contains("unable")) && !s.Contains("at") && s.Contains("["))
                            {
                                OMCLLog.WriteLog("[CrashMessageGet]Minecraft报出了这个错误：<" + s + ">。", OMCLExceptionClass.DLL, OMCLExceptionType.Message);
                                CrashMessage.Solution = "这个是您的Minecraft报出的错误：" + s + '\n';
                                switch (s)
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
                                if (s.Contains("fmlclient") || s.Contains("fml") || s.Contains("forge"))
                                {
                                    CrashMessage.Message = "您的Minecraft可能因为forge安装失败或不完整而崩溃！";
                                    CrashMessage.Solution += "建议您卸载重新安装forge并补全文件后再尝试启动Minecraft。";
                                }
                                break;
                            }
                            else if (s != null && !s.Contains("at") && i >= 4)
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
                    throw new OMCLException("日志检测出现错误！", e);
                }
            }
            private CrashMessage CrashMessageGet(string reportPath, int ExitCode, string version)
            {
                try
                {
                    CrashMessage crashMessage = new CrashMessage
                    {
                        VersionName = version,
                        ExitCode = ExitCode
                    };
                    string ex = "";
                    for (int i = OutputLines.Count - 1; i >= 0; i--)
                    {
                        string s = OutputLines[i];
                        if (s != null && (s.Contains("Exception") || s.Contains("unable")))
                        {
                            //OMCLLog.WriteLog("[CrashMessageGet]Minecraft报出了这个错误：<" + s + ">。", OMCLExceptionClass.DLL, OMCLExceptionType.Message);
                            if (ex != "") ex += "\n\n";
                            else ex += "\n这个（些）是您的Minecraft报出的错误：\n\n";
                            ex += s;
                            if (s.Contains("StackOverflowError")) ex += '\n' + "可能的原因：栈内存溢出！";
                            if (s.Contains("fmlclient") || s.Contains("fml") || s.Contains("forge")) ex += '\n' + "可能的原因：forge安装失败或不完整！";
                            if (s.Contains("Cannot find launch target")) ex += '\n' + "可能的原因：Minecraft安装错误！";
                            if (s.Contains('[')) break;
                        }
                    }
                    OMCLLog.WriteLog("[CrashMessageGet]Minecraft报出的错误：<" + ex + ">。", OMCLExceptionClass.DLL, OMCLExceptionType.Message);
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
                    string path = Regex.Replace(reportPath, $@"{filesp}{filesp}crash-reports{filesp}{filesp}crash(.*?)\.txt", "") + filesp + "mods";
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
                                OMCLLog.WriteLog("[CrashMessageGet]查找到模组 " + path + filesp + temp[3] + " 加载时出现状态“E”，断定为：该模组加载时错误，是无效mod！", OMCLExceptionClass.DLL, OMCLExceptionType.Message);
                                crashMessage.Message = "找到可能无效的mod文件！";
                                crashMessage.Solution = "请尝试禁用或删除 " + path + filesp + temp[3] + " mod，并尝试重启Minecraft！" +
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
                                OMCLLog.WriteLog("[CrashMessageGet]匹配成功！ " + GetList[i] + "对" + mods[j] + " 。断定 " + path + filesp + mods[j].FileName + " 导致Minecraft崩溃！", OMCLExceptionClass.DLL, OMCLExceptionType.Message);
                                crashMessage.Message = "找到可能导致Minecraft崩溃的mod文件！";
                                crashMessage.Solution = "请尝试禁用或删除 " + path + filesp + mods[j].FileName + " mod，并尝试重启Minecraft！" +
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
                    CrashMessage crashMessage = new CrashMessage
                    {
                        VersionName = version,
                        ExitCode = ExitCode,
                        Message = "未找到任何可能的原因，且崩溃分析运行时出现错误！",
                        Solution = "请尝试重启该Minecraft版本。\n\nOMCL错误：" + @e.Message
                    };
                    return crashMessage;
                }
            }
        }
        public class FDTools
        {
            public delegate void FDFoundDelegate(string FDPath);
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
                    throw new OMCLException("错误：复制文件夹时出现错误！找不到文件夹<" + sourceDirName + ">");
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
                                throw new OMCLException("错误：复制文件夹时出现错误！文件夹<" + destDirName + ">中已经存在文件<" + Path.GetFileName(f) + ">");
                            }
                        }
                        File.Copy(f, destDirName + f.Replace(sourceDirName, ""));
                    }
                }
            }
            /// <summary>
            /// 在一个特定的位置通过一定的条件获取文件夹（查找文件夹）
            /// </summary>
            /// <param name="folder">一个string，表示要在哪进行查找</param>
            /// <param name="keyword">一个string，表示关键词，即路径中要有的词（可以用‘|’表示路径分隔符），默认null，表示不限定</param>
            /// <param name="endwith">一个string，表示路径的结尾（可以用‘|’表示路径分隔符），默认null，表示不限定</param>
            /// <param name="fileName">一个string，表示文件夹中应有什么文件（文件名的关键词），默认null，表示不限定</param>
            /// <param name="filesp">[不填]路径分隔符，递归用</param>
            /// <returns>一个string的List，包含满足上述所有条件的文件夹路径</returns>
            public static async Task<List<string>> SearchFoldersAsync(string folder, string keyword = null, string endwith = null, string fileName = null, FDFoundDelegate OnFDFound = null, char filesp = '\0')
            {
                //OMCLLog.WriteLog("Searching:" + folder, OMCLExceptionClass.DLL, OMCLExceptionType.Message);
                List<string> result = new();
                if (filesp == '\0')
                {
                    if (GetSystemName() == "windows")
                    {
                        filesp = '\\';
                    }
                    else
                    {
                        filesp = '/';
                    }
                }
                try
                {
                    if (new DirectoryInfo(folder).Attributes.HasFlag(FileAttributes.ReparsePoint)) return new();
                    string[] subFolders = Directory.GetDirectories(folder);
                    IEnumerable<Task<List<string>>> tasks = subFolders.Select(subFolder => Task.Run(() => SearchFoldersAsync(subFolder, keyword, endwith, fileName, OnFDFound, filesp)));

                    List<string>[] results = await Task.WhenAll(tasks);

                    foreach (List<string> subResult in results)
                    {
                        result.AddRange(subResult);
                    }

                    if ((endwith == null || folder.EndsWith(endwith.Replace('|', filesp))) && (keyword == null || folder.Contains(keyword.Replace('|', filesp))))
                    {
                        if (fileName != null || fileName != "")
                        {
                            string[] files = Directory.GetFiles(folder, "*", SearchOption.TopDirectoryOnly);
                            bool IsOK = false;
                            foreach (string file in files) if (file.Contains(fileName)) IsOK = true;
                            if (IsOK)
                            {
                                result.Add(folder);
                                _ = Task.Run(() =>
                                {
                                    try
                                    {
                                        OnFDFound(folder);
                                    }
                                    catch { }
                                });
                            }
                        }
                        else
                        {
                            result.Add(folder);
                            _ = Task.Run(() =>
                            {
                                try
                                {
                                    OnFDFound(folder);
                                }
                                catch { }
                            });
                        }
                    }
                }
                catch (Exception e)
                {
                    OMCLLog.WriteLog("[Tools_FDTools_SearchDir]Error on searching [" + folder + "],keyword: [" + keyword + "],filename: [" + fileName + "]: " + e.Message, OMCLExceptionClass.DLL, OMCLExceptionType.Warning);
                    //OMCLLog.WriteLog("[Tools_FDTools_SearchDir]Error on searching [" + folder + "],keyword: [" + keyword + "],filename: [" + fileName + "]: " + e, OMCLExceptionClass.DLL, OMCLExceptionType.Warning);
                }

                return result;
            }
            /// <summary>
            /// 在所有的驱动器中通过一定的条件获取文件夹（查找文件夹）
            /// </summary>
            /// <param name="keyword">一个string，表示关键词，即路径中要有的词（可以用‘|’表示路径分隔符），默认null，表示不限定</param>
            /// <param name="endwith">一个string，表示路径的结尾（可以用‘|’表示路径分隔符），默认null，表示不限定</param>
            /// <param name="fileName">一个string，表示文件夹中应有什么文件（文件名的关键词），默认null，表示不限定</param>
            /// <returns>一个string数组，包含满足上述所有条件的文件夹路径</returns>
            public static async Task<string[]> SearchFoldersInDrivers(string keyword = null, string endwith = null, string fileName = null, FDFoundDelegate OnFDFound = null)
            {
                DriveInfo[] drives = DriveInfo.GetDrives();
                List<Task<List<string>>> tasks = new();
                List<string> pre_results = new();
                char filesp;
                if (GetSystemName() == "windows")
                {
                    filesp = '\\';
                }
                else
                {
                    filesp = '/';
                }
                foreach (DriveInfo drive in drives)
                {
                    tasks.Add(Task.Run(async () =>
                    {
                        return await SearchFoldersAsync(drive.RootDirectory.FullName, keyword, endwith, fileName, OnFDFound, filesp);
                    }));
                }
                await Task.WhenAll(tasks);
                foreach (Task<List<string>> task in tasks)
                {
                    pre_results.AddRange(task.Result);
                }
                return pre_results.ToArray();
            }
        }
        public class FindJava
        {
            private static List<Task> tasks;
            private static List<JavaVersion> javas;
            private static string javafile;
            /// <summary>
            /// 获取所有驱动器中的Java
            /// </summary>
            /// <returns>一个JavaVersion数组，包含每个Java的类型（jre、jdk）、版本和路径（以bin结尾，即*/bin或*\bin）</returns>
            public static async Task<JavaVersion[]> GetJavas()
            {
                javas = new();
                tasks = new();
                string system = GetSystemName();
                if (system == "windows")
                {
                    javafile = "java.exe";
                    _ = await FDTools.SearchFoldersInDrivers(null, "|bin", "javaw.exe", OnFDFound);
                    await Task.WhenAll(tasks);
                }
                else if (system == "linux")
                {
                    javafile = "java";
                    _ = await FDTools.SearchFoldersAsync("/", null, "|bin", "java", OnFDFound, '/');
                    await Task.WhenAll(tasks);
                }
                else
                {

                }
                return javas.GroupBy(x => x.path).Select(y => y.First()).ToArray();
                //return javas.ToArray();
            }
            private static void OnFDFound(string FDPath)
            {
                tasks.Add(Task.Run(() =>
                {
                    try
                    {
                        OMCLLog.WriteLog("[Tools_FindJava]找到一个类似Java的文件夹：" + FDPath, OMCLExceptionClass.DLL, OMCLExceptionType.Message);
                        string filename = Path.Combine(new string[] { FDPath, javafile });
                        if (!File.Exists(filename))
                        {
                            OMCLLog.WriteLog("[Tools_FindJava]Warning: An exception appeared when trying to check java [" + FDPath + "]: Cannot find file [" + filename + "]!", OMCLExceptionClass.DLL, OMCLExceptionType.Warning);
                            return;
                        }
                        Process process = new()
                        {
                            StartInfo = new ProcessStartInfo
                            {
                                FileName = filename,
                                Arguments = "-version",
                                WorkingDirectory = FDPath,
                                UseShellExecute = false,
                                CreateNoWindow = true,
                                //RedirectStandardOutput = true,
                                RedirectStandardError = true,
                            },
                        };
                        List<string> console = new();
                        /*
                        process.OutputDataReceived += new DataReceivedEventHandler((object sender, DataReceivedEventArgs e) =>
                        {
                            console.Add(e.Data);
                        });
                        */
                        process.ErrorDataReceived += new DataReceivedEventHandler((object sender, DataReceivedEventArgs e) =>
                        {
                            //OMCLLog.WriteLog("[JAVA:" + FDPath + "]:" + (e.Data == null ? "空" : e.Data), OMCLExceptionClass.DLL, OMCLExceptionType.Message);
                            console.Add(e.Data);
                        });
                        if (!process.Start())
                        {
                            OMCLLog.WriteLog("[Tools_FindJava]Warning: An exception appeared when trying to check java [" + FDPath + "]: The process cannot start!", OMCLExceptionClass.DLL, OMCLExceptionType.Warning);
                            return;
                        }
                        /*try
                        {*/
                            process.BeginErrorReadLine();
                            //process.BeginOutputReadLine();
                            process.WaitForExit();
                        /*}
                        catch { }*/
                        string version = console[0].Split('\"')[1];
                        JavaType type;
                        if (console[1].Contains("JDK")) type = JavaType.jdk;
                        else if (console[1].Contains("Java") && console[1].Contains("Runtime")) type = JavaType.jre;
                        else type = JavaType.unknown;
                        if (version == "" || version == null) return;
                        javas.Add(new JavaVersion
                        {
                            path = FDPath,
                            type = type,
                            version = version,
                        });
                    }
                    catch (Exception e)
                    {
                        OMCLLog.WriteLog("[Tools_FindJava]Warning: An exception appeared when trying to check java [" + FDPath + "]: " + e, OMCLExceptionClass.DLL, OMCLExceptionType.Warning);
                        throw new OMCLException("Error: " + e.Message, e);
                    }
                }));
            }
        }
    }
    public class Link
    {
        public static List<Process> LinkProcess = new();
        public static readonly string[] servers = { "la.afrps.cn" };
        public static readonly string[] server_tokens = { "afrps.cn" };
        public static string FrpcIni = "[common]\nserver_addr = {server_url}\nserver_port = {server_port}\ntoken = {server_token}\n\n[{link_name}]\ntype = tcp\nlocal_ip = 127.0.0.1\nlocal_port = {client_port}\nremote_port = {random_num}";
        public static async Task<string> StartLink(int client_port)
        {
            return await StartLink(client_port, 0);
        }
        public static async Task<string> StartLink(int client_port, int server_id)
        {
            return await StartLink(client_port, servers[server_id], 7000, server_tokens[server_id]);
        }
        [STAThread]
        public static async Task<string> StartLink(int client_port, string server_url, int server_port, string server_token)
        {
            try
            {
                Directory.CreateDirectory(Path.Combine(new string[] { Tools.Dir, "OMCL", "Link" }));
            }
            catch { }
            //if (!File.Exists(Path.Combine(new string[] { Tools.Dir, "OMCL", "Link", "LICENSE" }))) File.WriteAllBytes(Path.Combine(new string[] { Tools.Dir, "OMCL", "Link", "LICENSE" }), Properties.Resources.link_LICENSE);
            if (!File.Exists(Path.Combine(new string[] { Tools.Dir, "OMCL", "Link", "frpc.exe" }))) throw new NoClassFileException("联机", "frpc.exe", "https://github.com/fatedier/frp/releases"); //File.WriteAllBytes(Path.Combine(new string[] { Tools.Dir, "OMCL", "Link", "frpc.exe" }), Properties.Resources.link_frpc);
            Random random = new();
            int r;
            while (true)
            {
                r = random.Next(10005, 59005);
                File.WriteAllText(Path.Combine(new string[] { Tools.Dir, "OMCL", "Link", "frpc.ini" }), FrpcIni.Replace("{server_url}", server_url).Replace("{server_port}", server_port.ToString()).Replace("{server_token}", server_token).Replace("{link_name}", $"OMCL_Link_{Process.GetProcessesByName("frpc").Length}").Replace("{client_port}", client_port.ToString()).Replace("{random_num}", r.ToString()));
                Process p = new()
                {
                    StartInfo = new()
                    {
                        FileName = Path.Combine(new string[] { Tools.Dir, "OMCL", "Link", "frpc.exe" }),
                        Arguments = "-c \"" + Path.Combine(new string[] { Tools.Dir, "OMCL", "Link", "frpc.ini" }) + '\"',
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        WorkingDirectory = Path.Combine(new string[] { Tools.Dir, "OMCL", "Link" }),
                    },
                };
                bool IsOK = false;
                bool ShouldReset = false;
                p.OutputDataReceived += new DataReceivedEventHandler((object sender, DataReceivedEventArgs e) =>
                {
                    OMCLLog.WriteLog("[Tools_Link_StartLink]捕获到内网穿透（frpc.exe）的控制台输出：" + e.Data, OMCLExceptionClass.DLL, OMCLExceptionType.Message);
                    if (e.Data.Contains("start proxy success"))
                    {
                        IsOK = true;
                    }
                    else if (e.Data.Contains("control.go"))
                    {
                        ShouldReset = true;
                    }
                    else if (e.Data.Contains("connect to server error"))
                    {
                        OMCLLog.WriteLog("[Tools_Link_StartLink]错误：启动内网穿透（frpc.exe）服务时出现错误！无法连接到服务器！", OMCLExceptionClass.DLL, OMCLExceptionType.Error);
                        throw new OMCLException("启动内网穿透（frpc.exe）服务时出现错误！无法连接到服务器！");
                    }
                });
                p.ErrorDataReceived += new DataReceivedEventHandler((object sender, DataReceivedEventArgs e) =>
                {
                    OMCLLog.WriteLog("[Tools_Link_StartLink]捕获到内网穿透（frpc.exe）的控制台输出：" + e.Data, OMCLExceptionClass.DLL, OMCLExceptionType.Message);
                    if (e.Data.Contains("start proxy success"))
                    {
                        IsOK = true;
                    }
                    else if (e.Data.Contains("control.go"))
                    {
                        ShouldReset = true;
                    }
                    else if (e.Data.Contains("connect to server error"))
                    {
                        OMCLLog.WriteLog("[Tools_Link_StartLink]错误：启动内网穿透（frpc.exe）服务时出现错误！无法连接到服务器！", OMCLExceptionClass.DLL, OMCLExceptionType.Error);
                        throw new OMCLException("启动内网穿透（frpc.exe）服务时出现错误！无法连接到服务器！");
                    }
                });
                p.Start();
                p.BeginErrorReadLine();
                p.BeginOutputReadLine();
                await Task.WhenAny(Task.Run(() =>
                {
                    while (!IsOK && !ShouldReset) ;
                }));
                if (IsOK) break;
                /*
                try
                {
                    r = random.Next(10005, 59005);
                    File.WriteAllText(Path.Combine(new string[] { Tools.Dir, "OMCL", "Link", "frpc.ini" }), FrpcIni.Replace("{server_url}", server_url).Replace("{server_port}", server_port.ToString()).Replace("{server_token}", server_token).Replace("{link_name}", $"OMCL_Link_{Process.GetProcessesByName("frpc").Length}").Replace("{client_port}", client_port.ToString()).Replace("{random_num}", r.ToString()));
                    using (Process process = new()
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
                                OMCLLog.WriteLog("[Tools_Link_StartLink]捕获到内网穿透（frpc.exe）的控制台输出：" + s, OMCLExceptionClass.DLL, OMCLExceptionType.Message);
                                if (s == null && process.HasExited)
                                {
                                    OMCLLog.WriteLog("[Tools_Link_StartLink]错误：启动内网穿透（frpc.exe）服务时出现问题！进程已退出！", OMCLExceptionClass.DLL, OMCLExceptionType.Error);
                                    throw new OMCLException("启动内网穿透（frpc.exe）服务时出现问题！进程已退出！");
                                }
                                else if (s == null)
                                {
                                    StopLink();
                                    OMCLLog.WriteLog("[Tools_Link_StartLink]错误：启动内网穿透（frpc.exe）服务时出现未知问题！", OMCLExceptionClass.DLL, OMCLExceptionType.Error);
                                    throw new OMCLException("启动内网穿透（frpc.exe）服务时出现未知问题！");
                                }
                                else if (s.Contains("connect to server error"))
                                {
                                    OMCLLog.WriteLog("[Tools_Link_StartLink]错误：启动内网穿透（frpc.exe）服务时出现错误！无法连接到服务器！", OMCLExceptionClass.DLL, OMCLExceptionType.Error);
                                    throw new OMCLException("启动内网穿透（frpc.exe）服务时出现错误！无法连接到服务器！");
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
                    OMCLLog.WriteLog("[Tools_Link_StartLink]错误：启动内网穿透（frpc.exe）服务时出现问题<" + e.Message + ">！请检查！", OMCLExceptionClass.DLL, OMCLExceptionType.Error);
                    throw new OMCLException("错误：启动内网穿透（frpc.exe）服务时出现问题！请检查！", e);
                }
                */
            }
            try
            {
                File.Delete(Path.Combine(new string[] { Tools.Dir, "OMCL", "Link", "frpc.ini" }));
            }
            catch { }
            bool flag = false;
            int server_id = -1;
            for (int i = 0; i < servers.Length; i++)
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
            await Tools.clipboard.SetTextAsync(result);
            /*
            Thread thread = new(() =>
            {
                try
                {
                    Clipboard.SetText(result);
                }
                catch (Exception e)
                {
                    OMCLLog.WriteLog("Error on setting text into the chipboard:" + e, OMCLExceptionClass.DLL, OMCLExceptionType.Warning);
                }
            })
            {
                IsBackground = true,
            };
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            */
            return result;
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
                        server_port = int.Parse(sp[0].Remove(0, 1)),// int.Parse(sp[0].Replace(sp[0][0].ToString(),"")),
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
                throw new OMCLException("错误：尝试加入联机<" + code + ">时出现错误！请检查您的联机码是否正确！", e);
            }
        }
    }
    public class SettingsAndRegistry
    {
        /*
        /// <summary>
        /// 从注册表中获取已安装的Java列表
        /// </summary>
        /// <returns>一个JavaVersion数组，其中包含所有注册表中登记过的Java的信息</returns>
        public static JavaVersion[] GetJavaListInRegistry()
        {
            char filesp;
            if (Tools.GetSystemName() == "windows")
            {
                filesp = '\\';
            }
            else
            {
                filesp = '/';
            }
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
                    string get = temp.GetValue("JavaHome").ToString() + filesp + "bin";
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
                    string get = temp.GetValue("JavaHome").ToString() + filesp + @"\bin";
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
        */
    }
    public class GetLogin
    {
        public class MicrosoftLogin
        {
            public static event MicrosoftLogin2_oauth2.GetCodeDelegate Oauth2_OnGetCode;
            public class NewLogin
            {
                public static void OpenLoginUrl(bool IsAuto = true)
                {
                    Login.MicrosoftLogin.MicrosoftLogin.OpenLoginUrl(IsAuto);
                }
                /// <summary>
                /// （新方法）使用在浏览器中打开的方法登录一个Microsoft账号
                /// </summary>
                /// <param name="url">从用户浏览器中获取的返回url</param>
                /// <returns>一个MicrosoftLoginResult，其中包含玩家的名字，uuid，refresh_token等信息</returns>
                public static async Task<MicrosoftLoginResult> LoginByCode(string url)
                {
                    return await Login.MicrosoftLogin.MicrosoftLogin.Login(url);
                }
            }
            /// <summary>
            /// （旧方法）使用oauth2的device_code登录一个Microsoft账号
            /// </summary>
            /// <returns>一个MicrosoftLoginResult，其中包含玩家的名字，uuid，refresh_token等信息</returns>
            public static async Task<MicrosoftLoginResult> DCLogin()
            {
                MicrosoftLogin2_oauth2.OnGetCode += MicrosoftLogin2_oauth2_OnGetCode;
                return await MicrosoftLogin2_oauth2.Login();
            }
            /// <summary>
            /// 刷新MicrosoftLogin
            /// </summary>
            /// <param name="login">一个MicrosoftLoginResult，其中包含玩家的名字，uuid，refresh_token等信息</param>
            /// <returns>一个新的MicrosoftLoginResult，其中包含刷新过的玩家的名字，uuid，refresh_token等信息</returns>
            public static async Task<MicrosoftLoginResult> RefreshLogin(MicrosoftLoginResult login)
            {
                return await Login.MicrosoftLogin.MicrosoftLogin.RefreshLogin(login);
            }
            /// <summary>
            /// 刷新MicrosoftLogin
            /// </summary>
            /// <param name="token">即refresh_token，用来刷新登录，一般存储在MicrosoftLoginResult中</param>
            /// <param name="IsNew">指示该登录使用的是LoginByCode还是DCLogin，前者为true</param>
            /// <returns>一个新的MicrosoftLoginResult，其中包含刷新过的玩家的名字，uuid，refresh_token等信息</returns>
            public static async Task<MicrosoftLoginResult> RefreshLogin(string token, bool IsNew)
            {
                return await Login.MicrosoftLogin.MicrosoftLogin.RefreshLogin(new()
                {
                    refresh_token = token,
                    IsNew = IsNew,
                });
            }
            private static void MicrosoftLogin2_oauth2_OnGetCode(string code, string verification_uri, string message)
            {
                Oauth2_OnGetCode(code, verification_uri, message);
            }
        }
        /// <summary>
        /// 登录mojang账号
        /// </summary>
        /// <param name="user">拥有Minecraft的电子邮箱地址或玩家名称</param>
        /// <param name="password">密码</param>
        /// <returns>一个MojangLoginResult，其中包含玩家的名字，uuid，access_token等信息</returns>
        public static async Task<MojangLoginResult> MojangLogin(string user, string password)
        {
            JObject o = JObject.Parse(await Web.Post("https://authserver.mojang.com/authenticate", "{\"agent\": {\"name\": \"Minecraft\",\"version\": 1},\"username\": \"" + user + "\",\"password\": \"" + password + "\"}", "application/json"));
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
        public static async Task<MojangLoginResult> RefreshMojangLogin(string access_token)
        {
            JObject o = JObject.Parse(await Web.Post("https://authserver.mojang.com/refresh", "{\"accessToken\": \"" + access_token + "\"}", "application/json"));
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
        public static async Task<UnifiedPassLoginResult> UnifiedPassLogin(string serverid, string user, string password)
        {
            JObject o = JObject.Parse(await Web.Post("https://auth.mc-user.com:233/" + serverid + "/authserver/authenticate", "{\"username\": \"" + user + "\",\"password\": \"" + password + "\"}", "application/json"));
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
        public static async Task<UnifiedPassLoginResult> RefreshUnifiedPassLogin(string serverid, string clientToken, string access_token)
        {
            JObject o = JObject.Parse(await Web.Post("https://auth.mc-user.com:233/" + serverid + "/authserver/refresh", "{\"accessToken\": \"" + access_token + "\",\"clientToken\": \"" + clientToken + "\"}", "application/json"));
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
        public static async Task<AuthlibInjectorLoginResult> AuthlibInjectorLogin(string server, string user, string password)
        {
            JObject o = JObject.Parse(await Web.Post(server + "/authserver/authenticate", "{\"username\":\"" + user + "\",\"password\":\"" + password + "\",\"agent\":{\"name\":\"Minecraft\",\"version\":1}}", "application/json"));
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
        public static async Task<AuthlibInjectorLoginResult> RefreshAuthlibInjectorLogin(AuthlibInjectorLoginResult login)
        {
            JObject o = JObject.Parse(await Web.Post(login.server + "/authserver/refresh", "{\"accessToken\":\"" + login.access_token + "\"}", "application/json"));
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
        public static async Task<OfflineLoginResult> OfflineLogin(string name)
        {
            try
            {
                string result = await Web.Get("https://api.mojang.com/users/profiles/minecraft/" + name);
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
            //Coding,don't use or forget it!!!!!!!!!
            public static void InstallForge(string version, string forgeFile)
            {
                char filesp;
                if (Tools.GetSystemName() == "windows")
                {
                    filesp = '\\';
                }
                else
                {
                    filesp = '/';
                }
                try
                {
                    Directory.Delete(Path.Combine(new string[] { Path.GetTempPath(), "OMCL", "temp" }), true);
                }
                catch { }
                Directory.CreateDirectory(Path.Combine(new string[] { Path.GetTempPath(), "OMCL", "temp" }));
                Tools.UnZipFile(forgeFile, Path.Combine(new string[] { Path.GetTempPath(), "OMCL", "temp" }) + filesp, true);
                JArray Narguments;
                //string NminecraftArguments;
                JArray Nlibraries;
                JArray Njvm;
                Version forge_version_json;
                if (File.Exists(Path.Combine(new string[] { Path.GetTempPath(), "OMCL", "temp", "version.json" }))) forge_version_json = Tools.ReadVersionJson(Path.Combine(new string[] { Path.GetTempPath(), "OMCL", "temp", "version.json" }), out Narguments, out Nlibraries, out Njvm);
                else if (File.Exists(Path.Combine(new string[] { Path.GetTempPath(), "OMCL", "temp", "install_profile.json" }))) forge_version_json = Tools.ReadVersionJson(JObject.Parse(File.ReadAllText(Path.Combine(new string[] { Path.GetTempPath(), "OMCL", "temp", "install_profile.json" }))).SelectToken("versionInfo").ToString(), out Narguments, out Nlibraries, out Njvm);
                else
                {
                    return;
                }
                _ = Tools.ReadVersionJson(Path.Combine(new string[] { Tools.Dir, ".minecraft", "versions", version, version + ".json" }), out JArray arguments, out JArray libraries, out JArray jvm);
                JObject o = JObject.Parse(File.ReadAllText(Path.Combine(new string[] { Tools.Dir, ".minecraft", "versions", version, version + ".json" })));
                /*
                if (NminecraftArguments == "" || NminecraftArguments == null)
                {
                    arguments.Merge(Narguments);
                    o.SelectToken("arguments.game").Replace(arguments);
                }
                else
                {
                    o["minecraftArguments"] = NminecraftArguments;
                }
                */
                if (jvm != null && Njvm != null)
                {
                    jvm.Merge(Njvm);
                    o.SelectToken("arguments.jvm").Replace(jvm);
                }
                libraries.Merge(Nlibraries);
                o.SelectToken("libraries").Replace(libraries);
                o["mainClass"] = forge_version_json.mainClass;
                File.WriteAllText(Path.Combine(new string[] { Tools.Dir, ".minecraft", "versions", version, version + ".json" }), o.ToString());
                try
                {
                    Directory.Delete(Path.Combine(new string[] { Path.GetTempPath(), "OMCL", "temp" }), true);
                }
                catch { }
            }
        }
        public class MinecraftInstall
        {
            public static async Task InstallMinecraftVersion(string versionName, string installName, bool IsDownloadLibraries = true, bool IsDownloadAssets = false)
            {
                Directory.CreateDirectory(Path.Combine(new string[] { Tools.Dir, ".minecraft", "versions", versionName }));
                JObject vi = JObject.Parse(await Web.Get(Tools.MCFileDownloadServer.VersionInfo[1]));
                JArray vis = (JArray)vi.SelectToken("versions");
                string vjurl = null;
                foreach (JToken vip in vis)
                {
                    if (vip.SelectToken("id").ToString() == installName) vjurl = vip.SelectToken("url").ToString();
                    //if (vip.Value.SelectToken("id").ToString() == installName) vjurl = vip.Value.SelectToken("url").ToString();
                }
                string json = await Web.Get(vjurl);
                if (!File.Exists(Path.Combine(new string[] { Tools.Dir, ".minecraft", "versions", versionName, versionName + ".json" }))) File.WriteAllText(Path.Combine(new string[] { Tools.Dir, ".minecraft", "versions", versionName, versionName + ".json" }), json);
                string ujar = JObject.Parse(json).SelectToken("downloads.client.url").ToString();
                if (!File.Exists(Path.Combine(new string[] { Tools.Dir, ".minecraft", "versions", versionName, versionName + ".jar" }))) HttpFile.Start(Tools.DownloadThreadNum, ujar, Path.Combine(new string[] { Tools.Dir, ".minecraft", "versions", versionName, versionName + ".jar" }));
                if (IsDownloadLibraries) Tools.DownloadMissFiles(Tools.ReadVersionJson(versionName));
                if (IsDownloadAssets) Tools.DownloadMissAsstes(versionName);
            }
        }
    }
}