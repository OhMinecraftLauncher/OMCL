using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OMCL_DLL.Tools
{
    public class Version
    {
        public string[] libraries { get; set; }
        public string[] natives { get; set; }
        public string mainClass { get; set; }
        //public Argument[] arguments { get; set; }
        public string arguments { get; set; }
        public string assets { get; set; }
        public string jvm { get; set; }
        public LoggingFile loggingFile { get; set; }
        public string version { get; set; }
        public string id { get; set; }
        public string type { get; set; }
    }
    public struct LoggingFile
    {
        public string FileName { get; set; }
        public string FileUrl { get; set; }
    }
    /*
    public class Argument
    {
        public string name { get; set; }
        public string value { get; set; }
    }
    */
    public class JavaVersion
    {
        public JavaType type { get; set; }
        public string path { get; set; }
        public string version { get; set; }
    }
    public class CrashMessage
    {
        public string Message { get; set; }
        public int ExitCode { get; set; }
        public string VersionName { get; set; }
        public string Solution { get; set; }
    }
    public class ModInfo
    {
        public string IdNamespace { get; set; }
        public string FileName { get; set; }
    }
    public class FindOutDir
    {
        public string path { get; set; }
        public string PathinGet { get; set; }
    }
    public class FindOutFile
    {
        public string path_and_filename { get; set; }
        public string PathinGet { get; set; }
    }
    public class Server
    {
        public string server_url_or_ip { get; set; }
        public int server_port { get; set; }
    }
    public class JavaComp : IComparer<JavaVersion>
    {
        public int Compare(JavaVersion a, JavaVersion b)
        {
            return a.type.CompareTo(b.type);
        }
    }
    public struct DownloadServer
    {
        public string[] VersionInfo = new string[2];
        public string[] VersionJsonAndAssetsIndex = new string[3];
        public string Assets = "";
        public string Libraries = "";
        public string Java = "";
        public string LiteLoader = "";
        public string Optifine = "";
        public string authlib_injector = "";
        public string[] Forge = new string[2];
        public string[] Fabric = new string[2];
        public string[] NeoForge = new string[2];
        public string[] quilt = new string[2];
        public DownloadServer() { }
    }
    public class DownloadServers
    {
        public static readonly DownloadServer[] MCFileDownloadServers = new DownloadServer[3]
        {
            new()
            {
                VersionInfo = new string[2]
                {
                    "https://launchermeta.mojang.com/mc/game/version_manifest.json",
                    "https://launchermeta.mojang.com/mc/game/version_manifest_v2.json",
                },
                VersionJsonAndAssetsIndex = new string[3]
                {
                    "https://piston-meta.mojang.com",
                    "https://launchermeta.mojang.com",
                    "https://launcher.mojang.com",
                },
                Assets = "https://resources.download.minecraft.net",
                Libraries = "https://libraries.minecraft.net",
                Java = "https://launchermeta.mojang.com/v1/products/java-runtime/2ec0cc96c44e5a76b9c8b7c39df7210883d12871/all.json",
                LiteLoader = "https://dl.liteloader.com/versions/versions.json",
                Optifine = "",
                authlib_injector = "https://authlib-injector.yushi.moe",
                Forge = new string[2]
                {
                    "https://maven.minecraftforge.net",
                    "https://files.minecraftforge.net/maven",
                },
                Fabric = new string[2]
                {
                    "https://meta.fabricmc.net",
                    "https://maven.fabricmc.net",
                },
                NeoForge = new string[2]
                {
                    "https://maven.neoforged.net/releases/net/neoforged/forge",
                    "https://maven.neoforged.net/releases/net/neoforged/neoforge",
                },
                quilt = new string[2]
                {
                    "https://maven.quiltmc.org/repository/release",
                    "https://meta.quiltmc.org",
                },
            },
            new()
            {
                VersionInfo = new string[2]
                {
                    "https://bmclapi2.bangbang93.com/mc/game/version_manifest.json",
                    "https://bmclapi2.bangbang93.com/mc/game/version_manifest_v2.json",
                },
                VersionJsonAndAssetsIndex = new string[3]
                {
                    "https://bmclapi2.bangbang93.com",
                    "https://bmclapi2.bangbang93.com",
                    "https://bmclapi2.bangbang93.com",
                },
                Assets = "https://bmclapi2.bangbang93.com/assets",
                Libraries = "https://bmclapi2.bangbang93.com/maven",
                Java = "https://bmclapi2.bangbang93.com/v1/products/java-runtime/2ec0cc96c44e5a76b9c8b7c39df7210883d12871/all.json",
                LiteLoader = "https://bmclapi2.bangbang93.com/maven/com/mumfrey/liteloader/versions.json",
                Optifine = "https://bmclapi2.bangbang93.com/optifine",
                authlib_injector = "https://bmclapi2.bangbang93.com/mirrors/authlib-injector",
                Forge = new string[2]
                {
                    "https://bmclapi2.bangbang93.com/maven",
                    "https://bmclapi2.bangbang93.com/maven",
                },
                Fabric = new string[2]
                {
                    "https://bmclapi2.bangbang93.com/fabric-meta",
                    "https://bmclapi2.bangbang93.com/maven",
                },
                NeoForge = new string[2]
                {
                    "https://bmclapi2.bangbang93.com/maven/net/neoforged/forge",
                    "https://bmclapi2.bangbang93.com/maven/net/neoforged/neoforge",
                },
                quilt = new string[2]
                {
                    "https://bmclapi2.bangbang93.com/maven",
                    "https://bmclapi2.bangbang93.com/quilt-meta",
                },
            },
            new()
            {
                VersionInfo = new string[2]
                {
                    "https://download.mcbbs.net/mc/game/version_manifest.json",
                    "https://download.mcbbs.net/mc/game/version_manifest_v2.json",
                },
                VersionJsonAndAssetsIndex = new string[3]
                {
                    "https://download.mcbbs.net",
                    "https://download.mcbbs.net",
                    "https://download.mcbbs.net",
                },
                Assets = "https://download.mcbbs.net/assets",
                Libraries = "https://download.mcbbs.net/maven",
                Java = "https://download.mcbbs.net/v1/products/java-runtime/2ec0cc96c44e5a76b9c8b7c39df7210883d12871/all.json",
                LiteLoader = "https://download.mcbbs.net/maven/com/mumfrey/liteloader/versions.json",
                Optifine = "https://download.mcbbs.net/optifine",
                authlib_injector = "https://download.mcbbs.net/mirrors/authlib-injector",
                Forge = new string[2]
                {
                    "https://download.mcbbs.net/maven",
                    "https://download.mcbbs.net/maven",
                },
                Fabric = new string[2]
                {
                    "https://download.mcbbs.net/fabric-meta",
                    "https://download.mcbbs.net/maven",
                },
                NeoForge = new string[2]
                {
                    "https://download.mcbbs.net/maven/net/neoforged/forge",
                    "https://download.mcbbs.net/maven/net/neoforged/neoforge",
                },
                quilt = new string[2]
                {
                    "https://download.mcbbs.net/maven",
                    "https://download.mcbbs.net/quilt-meta",
                },
            },
        };
    }
}
