using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

public class FileDownloader
{
    private readonly string _url, _fileName, _floderName;
    private readonly int _threadCount;
    private DownloadProgress progress;

    private int UA_NUM = 0;

    private static readonly string[] UAs = { "OMCL/0.0.0.1", "curl/7.64.1", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/115.0.0.0 Safari/537.36 Edg/115.0.1901.188", "", null };

    public Task FileDownloadingTask { get; private set; }
    public bool IsDownloadCompleted { get; private set; }
    public static int TimeoutTime = 10000;
    public static int WaitToRestart = 5000;

    public delegate void DownloadCompletedDelegate(string filename);
    public event DownloadCompletedDelegate OnDownloadCompleted;
    public delegate void DownloadStatusChangeDelegate(string status);
    public event DownloadStatusChangeDelegate OnDownloadStatusChanged;

    public FileDownloader(string url, string savePath, int threadCount = 5)
    {
        _url = url;
        _fileName = savePath;
        _threadCount = threadCount;
        _floderName = Path.GetDirectoryName(savePath);
        try
        {
            Directory.CreateDirectory(_floderName);
        }
        catch { }
        IsDownloadCompleted = false;
        UA_NUM = 0;
    }

    public async Task StartDownloadAsync(bool waitForCompletion)
    {
        string fileName = _fileName;
        string name = Path.GetFileName(fileName);
        string url = _url;
        string floderName = _floderName;

        int threadCount = _threadCount;

        long fileSize = GetFileSize(url);

        Console.WriteLine("File size:" + fileSize + " (" + fileSize / (1024 * 1024 * 1.0) + " MB)");

        if (fileSize <= 0)
        {
            throw new Exception("获取文件大小失败");
        }

        try
        {
            OnDownloadStatusChanged($"成功获取到文件大小：{fileSize} bytes  ({fileSize / (1024 * 1024 * 1.0):00} MB)！");
        }
        catch { }

        progress = new DownloadProgress(fileSize);

        Range[] ranges = CalculateRanges(threadCount, fileSize);

        Task[] tasks = new Task[threadCount];
        for (int i = 0; i < threadCount; i++)
        {
            try
            {
                OnDownloadStatusChanged($"启动线程{i + 1}/{threadCount}中……");
            }
            catch { }
            int index = i;
            tasks[i] = DownloadRangeAsync(url, Path.Combine(floderName, $"temp_{name}[{index}].tmp"), ranges[index], progress);
            try
            {
                OnDownloadStatusChanged($"线程{i + 1}/{threadCount}启动完成！");
            }
            catch { }
        }

        try
        {
            OnDownloadStatusChanged("正在下载……");
        }
        catch { }

        if (waitForCompletion)
        {
            await Task.WhenAll(tasks);

            MergeTempFiles(threadCount, fileName);

            IsDownloadCompleted = true;

            try
            {
                OnDownloadCompleted(name);
            }
            catch { }
        }
        else
        {
            FileDownloadingTask = new Task(async () =>
            {
                await Task.WhenAll(tasks);

                MergeTempFiles(threadCount, fileName);

                IsDownloadCompleted = true;

                try
                {
                    OnDownloadCompleted(name);
                }
                catch { }
            });

            FileDownloadingTask.Start();
        }
    }

    public long GetDownloadedBytes()
    {
        return progress.downloadedSize;
    }

    public long GetTotalBytes()
    {
        return progress.totalSize;
    }

    public double GetProgressPercentage()
    {
        return progress.percentage;
    }

    public double GetDownloadSpeed()
    {
        return progress.DownloadSpeed;
    }

    public DownloadRemainingTime GetDownloadRemainingTime()
    {
        return progress.DownloadRemainingTime;
    }

    private readonly struct Range
    {
        public long Start { get; }
        public long End { get; }

        public Range(long start, long end)
        {
            Start = start;
            End = end;
        }
    }

    public struct DownloadRemainingTime
    {
        public int Hour { get; internal set; }
        public int Minute { get; internal set; }
        public int Second { get; internal set; }
    }

    private class DownloadProgress
    {
        public readonly long totalSize;
        public long downloadedSize;
        public double percentage;

        private readonly Timer timer;

        private long DownloadSpeed_NUM;

        public double DownloadSpeed;

        public DownloadRemainingTime DownloadRemainingTime;

        public DownloadProgress(long totalSize)
        {
            this.totalSize = totalSize;
            downloadedSize = 0;
            percentage = 0.0;
            DownloadSpeed = 0.0;
            DownloadSpeed_NUM = 0;
            timer = new Timer(new TimerCallback((object sender) =>
            {
                DownloadSpeed = DownloadSpeed_NUM / (1000 * 1000 * 1.0) / 3;
                Interlocked.Exchange(ref DownloadSpeed_NUM, 0);
                if (DownloadSpeed > 0)
                {
                    double rt = (this.totalSize - downloadedSize) / 3 / (1000 * 1000 * 1.0) / DownloadSpeed;
                    DownloadRemainingTime = new DownloadRemainingTime()
                    {
                        Hour = (int)(rt / 60 / 60),
                        Minute = (int)(rt / 60 % 60),
                        Second = (int)(rt % 60),
                    };
                }
                else
                {
                    DownloadRemainingTime = new DownloadRemainingTime()
                    {
                        Hour = 0,
                        Minute = 0,
                        Second = 0,
                    };
                }
            }), null, 0, 3000);
        }

        public void Increment(long size)
        {
            Interlocked.Add(ref downloadedSize, size);
            Interlocked.Add(ref DownloadSpeed_NUM, size);
            percentage = (double)downloadedSize / totalSize * 100;
        }
    }

    private long GetFileSize(string url)
    {
        try
        {
            OnDownloadStatusChanged("获取文件大小（测试UA及链接）中……");
        }
        catch { }
        Exception e = null;
        for (int i = 0; i < UAs.Length; i++)
        {
            try
            {
                try
                {
                    OnDownloadStatusChanged($"测试UA[{i + 1}]{UAs[i]}中……");
                }
                catch { }
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.UserAgent = UAs[i];
                UA_NUM = i;
                request.Timeout = TimeoutTime;
                try
                {
                    OnDownloadStatusChanged("尝试获取请求及目标文件大小……");
                }
                catch { }
                long len = request.GetResponse().ContentLength;//取得目标文件的长度
                if (len > 0)
                {
                    return len;
                }
            }
            catch (Exception ex)
            {
                e = ex;
                try
                {
                    OnDownloadStatusChanged($"UA[{i + 1}]{UAs[i]}连接失败！");
                }
                catch { }
            }
        }
        try
        {
            OnDownloadStatusChanged("获取文件大小失败！");
        }
        catch { }
        throw new Exception("获取文件大小失败！", e);
    }

    private static Range[] CalculateRanges(int threadCount, long fileSize)
    {
        Range[] ranges = new Range[threadCount];
        long chunkSize = fileSize / threadCount;
        long remainingBytes = fileSize % threadCount;

        long startPosition = 0;
        for (int i = 0; i < threadCount; i++)
        {
            long endPosition = startPosition + chunkSize - 1;
            if (i == threadCount - 1)
            {
                endPosition += remainingBytes;
            }

            ranges[i] = new Range(startPosition, endPosition);
            startPosition = endPosition + 1;
        }

        return ranges;
    }

    private async Task DownloadRangeAsync(string url, string tempFileName, Range range, DownloadProgress progress)
    {
        long startPosition = range.Start;
        long endPosition = range.End;

        // 检查临时文件是否存在，如果存在则更新起始位置
        if (File.Exists(tempFileName))
        {
            long downloadedBytes = new FileInfo(tempFileName).Length;
            startPosition += downloadedBytes;
            progress.Increment(downloadedBytes);
        }

        while (true)
        {
            try
            {
                if (startPosition <= endPosition)
                {
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                    request.UserAgent = UAs[UA_NUM];
                    request.AddRange(startPosition, endPosition);
                    request.Timeout = TimeoutTime;
                    using (var response = request.GetResponse())
                    {
                        using (var contentStream = response.GetResponseStream())
                        {
                            contentStream.ReadTimeout = TimeoutTime;
                            using (var fileStream = new FileStream(tempFileName, FileMode.Append, FileAccess.Write, FileShare.Read))
                            {
                                byte[] buffer = new byte[2048];
                                int bytesRead;

                                while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                                {
                                    await fileStream.WriteAsync(buffer, 0, bytesRead);
                                    progress.Increment(bytesRead);
                                    startPosition += bytesRead;
                                }
                            }
                        }
                    }
                    request.Abort();
                }
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"线程 {range.Start}-{range.End} 下载失败: {ex.Message}");
                Console.WriteLine($"等待 {WaitToRestart * 1.0 / 1000} 秒后重试...");
                await Task.Delay(WaitToRestart);
            }
        }
        return;
    }

    private void MergeTempFiles(int threadCount, string fileName)
    {
        try
        {
            OnDownloadStatusChanged("合并（整合）文件中……");
        }
        catch { }

        using (var mergedFileStream = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.Read))
        {
            for (int i = 0; i < threadCount; i++)
            {
                try
                {
                    OnDownloadStatusChanged($"合并（整合）文件[{i + 1}/{threadCount}]中……");
                }
                catch { }

                string tempFileName = Path.Combine(Path.GetDirectoryName(fileName), $"temp_{Path.GetFileName(fileName)}[{i}].tmp");
                using (var tempFileStream = new FileStream(tempFileName, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    tempFileStream.CopyTo(mergedFileStream);
                }

                try
                {
                    OnDownloadStatusChanged($"文件[{i + 1}/{threadCount}]合并（整合）完成！删除临时文件中……");
                }
                catch { }

                File.Delete(tempFileName);

                try
                {
                    OnDownloadStatusChanged($"临时文件[{i + 1}/{threadCount}]删除完成！");
                }
                catch { }
            }
        }

        try
        {
            OnDownloadStatusChanged("下载完成！");
        }
        catch { }
    }
}

public class Program
{
    static async Task Main(string[] args)
    {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();

        /*网络不稳定*/ //FileDownloader downloader = new FileDownloader("https://codeload.github.com/OhMinecraftLauncher/OMCL/zip/refs/heads/main", @"main.zip");
        /*小文件*/ //FileDownloader downloader = new FileDownloader("https://648538699da4abbeb38efa68.openbmclapi.933.moe:4000/download/d722504db9de2b47f46cc592b8528446272ae648?name=client.jar", "1.jar",1);
        /*大文件*/ FileDownloader downloader = new FileDownloader("https://mirrors.tuna.tsinghua.edu.cn/ubuntu-releases/23.04/ubuntu-23.04-desktop-amd64.iso", @"F:\ABC\2.iso", 10);
        downloader.OnDownloadCompleted += Downloader_OnDownloadCompleted;
        downloader.OnDownloadStatusChanged += Downloader_OnDownloadStatusChanged;
        await downloader.StartDownloadAsync(false);

        while (!downloader.IsDownloadCompleted)
        {
            FileDownloader.DownloadRemainingTime time = downloader.GetDownloadRemainingTime();
            Console.WriteLine($"{downloader.GetDownloadedBytes()}/{downloader.GetTotalBytes()}({downloader.GetProgressPercentage():F2} %)  {downloader.GetDownloadSpeed():F2} MB/s  {time.Hour:00}:{time.Minute:00}:{time.Second:00}");
            Thread.Sleep(1000);
        }

        stopwatch.Stop();

        Console.WriteLine("Time used : " + stopwatch.ElapsedMilliseconds * 1.0 / (1000 * 1.0) + "s");

        Console.ReadLine();
    }

    private static void Downloader_OnDownloadStatusChanged(string status)
    {
        Console.WriteLine(status);
    }

    private static void Downloader_OnDownloadCompleted(string filename)
    {
        Console.WriteLine($"文件[{filename}]下载完成！");
    }
}