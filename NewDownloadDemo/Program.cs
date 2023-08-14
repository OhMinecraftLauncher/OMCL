//using OMCL_DLL.Tools.Download;
using OMCL_DLL.Tools.Download;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewDownloadDemo
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            //MultiThreadedDownloader downloader = new MultiThreadedDownloader("https://6010f16780d7c5acec1b4134.openbmclapi.933.moe:4000/download/37fd3c903861eeff3bc24b71eed48f828b5269c8?name=client.jar", @"1.jar");
            //MultiThreadedDownloader downloader = new MultiThreadedDownloader();
            //await MultiThreadedDownloader.DownloadFile("https://6010f16780d7c5acec1b4134.openbmclapi.933.moe:4000/download/37fd3c903861eeff3bc24b71eed48f828b5269c8?name=client.jar", @"1.jar");
            DownloadFile download = new DownloadFile("https://software.download.prss.microsoft.com/dbazure/Win10_22H2_Chinese_Simplified_x32v1.iso?t=7b55feff-20d8-47e6-bf98-80b443609e84&e=1686448423&h=9d1f7ad1c030647eb6ccdfa817e2f1cbc570da8b887f9fbb8aea2f50e80bc77f", @"F:\ABC\1.iso", 4096);
            download.FinishDownload += Download_FinishDownload;
            download.StartDownload();

            Console.WriteLine("Time used " + stopwatch.ElapsedMilliseconds / 1000.0 + " s");
            stopwatch.Stop();
        }

        private static void Download_FinishDownload()
        {
            Console.WriteLine("Download OK!");
        }
    }
}
