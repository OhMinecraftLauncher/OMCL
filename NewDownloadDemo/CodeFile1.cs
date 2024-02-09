using NewDownloadDemo;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

public class FileDownloader
{
    private readonly string _url, _fileName, _floderName;
    private readonly int _threadCount;
    private DownloadProgress progress;

    private int UA_NUM = 0;

    private static readonly string[] UAs = { "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/115.0.0.0 Safari/537.36 Edg/115.0.1901.188", "OMCL/0.0.0.1", "curl/7.64.1" };

    public Task FileDownloadingTask { get; private set; }
    public bool IsDownloadCompleted { get; private set; }
    public bool IsFileMerging { get; private set; }
    public static int TimeoutTime = 30000;
    public static int WaitToRestart = 5000;

    public Task[] tasks;

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
        IsFileMerging = false;
        UA_NUM = 0;
        tasks = new Task[threadCount];
    }

    public async Task StartDownloadAsync(bool waitForCompletion = false)
    {
        IsDownloadCompleted = false;
        IsFileMerging = false;
        string fileName = _fileName;
        string name = Path.GetFileName(fileName);
        string url = _url;
        string floderName = _floderName;

        int threadCount = _threadCount;

        long fileSize = GetFileSize();

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

        try
        {
            OnDownloadStatusChanged($"读取文件[{Path.Combine(floderName, $"temp_{name}[thread].tmp")}]中……");
        }
        catch { }

        int thfilerd = 0;
        try
        {
            thfilerd = int.Parse(File.ReadAllText(Path.Combine(floderName, $"temp_{name}[thread].tmp")));
        }
        catch (FileNotFoundException)
        {

        }
        catch
        {
            Cancle();
        }

        if (thfilerd != 0)
        {
            try
            {
                OnDownloadStatusChanged($"文件[{Path.Combine(floderName, $"temp_{name}[thread].tmp")}]读取完成！值：[{thfilerd}]");
            }
            catch { }
        }

        if (thfilerd != 0 && thfilerd != threadCount)
        {
            try
            {
                OnDownloadStatusChanged($"由于原下载任务线程数[{thfilerd}]与当前下载任务线程数[{threadCount}]不匹配，正在重新开始下载……");
            }
            catch { }
            Cancle();
            try
            {
                OnDownloadStatusChanged($"已取消原下载，正在重新开始下载……");
            }
            catch { }
            /*
            MergeTempFiles(thfilerd, fileName);
            try
            {
                OnDownloadStatusChanged($"原下载任务下载文件合并完成！分割文件中……");
            }
            catch { }
            using (FileStream f = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                
                Task[] fts = new Task[threadCount];
                for (int i = 0; i < threadCount; i++)
                {
                    fts[i] = new Task(() =>
                    {
                        byte[] bytes = new byte[ranges[i].End - ranges[i].Start];
                        int nu = f.Read(bytes, (int)ranges[i].Start, bytes.Length);
                        using (FileStream ft = new FileStream(Path.Combine(floderName, $"temp_{name}[{i}].tmp"), FileMode.Create, FileAccess.Write, FileShare.Read))
                        {
                            ft.Write(bytes, 0, bytes.Length);
                        }
                    });
                    fts[i].Start();
                }
                await Task.WhenAll(fts);
            }
            try
            {
                OnDownloadStatusChanged($"文件分割完成！删除临时整合文件[{fileName}]中……");
            }
            catch { }
            File.Delete(fileName);
            try
            {
                OnDownloadStatusChanged($"临时整合文件[{fileName}]删除完成！转换完成！正在尝试开始下载当前下载任务……");
            }
            catch { }
            */
        }

        try
        {
            OnDownloadStatusChanged($"写入文件[{Path.Combine(floderName, $"temp_{name}[thread].tmp")}]中……");
        }
        catch { }

        File.WriteAllText(Path.Combine(floderName, $"temp_{name}[thread].tmp"), threadCount.ToString());

        try
        {
            OnDownloadStatusChanged($"文件[{Path.Combine(floderName, $"temp_{name}[thread].tmp")}]写入完成！");
        }
        catch { }

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

    public void Pause()
    {
        try
        {
            OnDownloadStatusChanged($"正在暂停下载……");
        }
        catch { }
        for (int i = 0; i < _threadCount; i++)
        {
            try
            {
                OnDownloadStatusChanged($"正在停止下载任务（{i + 1}/{_threadCount}）");
            }
            catch { }
            try
            {
                tasks[i].Dispose();
            }
            catch { }
            try
            {
                OnDownloadStatusChanged($"已停止下载任务（{i + 1}/{_threadCount}）！");
            }
            catch { }
        }
        try
        {
            OnDownloadStatusChanged($"已暂停下载");
        }
        catch { }
    }

    public void Cancle()
    {
        try
        {
            OnDownloadStatusChanged($"正在取消下载……");
        }
        catch { }
        Pause();
        for (int i = 0; i < _threadCount; i++)
        {
            string tempFileName = Path.Combine(Path.GetDirectoryName(_fileName), $"temp_{Path.GetFileName(_fileName)}[{i}].tmp");
            try
            {
                OnDownloadStatusChanged($"删除临时文件[{i + 1}/{_threadCount}]中……");
            }
            catch { }

            try
            {
                File.Delete(tempFileName);
            }
            catch { }

            try
            {
                OnDownloadStatusChanged($"临时文件[{i + 1}/{_threadCount}]删除完成！");
            }
            catch { }
        }

        try
        {
            OnDownloadStatusChanged($"删除临时文件[{Path.Combine(_floderName, $"temp_{Path.GetFileName(_fileName)}[thread].tmp")}]中……");
        }
        catch { }

        File.Delete(Path.Combine(_floderName, $"temp_{Path.GetFileName(_fileName)}[thread].tmp"));

        try
        {
            OnDownloadStatusChanged($"临时文件[{Path.Combine(_floderName, $"temp_{Path.GetFileName(_fileName)}[thread].tmp")}]删除完成！");
        }
        catch { }

        try
        {
            OnDownloadStatusChanged($"已取消下载！");
        }
        catch { }
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

    private long GetFileSize()
    {
        string url = _url;
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
        /*else
        {
            using (FileStream f = File.Open(tempFileName, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                f.SetLength(range.End - range.Start);
            }
        }*/

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
        IsFileMerging = true;
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
            OnDownloadStatusChanged($"删除临时文件[{Path.Combine(_floderName, $"temp_{Path.GetFileName(fileName)}[thread].tmp")}]中……");
        }
        catch { }

        File.Delete(Path.Combine(_floderName, $"temp_{Path.GetFileName(fileName)}[thread].tmp"));

        try
        {
            OnDownloadStatusChanged($"临时文件[{Path.Combine(_floderName, $"temp_{Path.GetFileName(fileName)}[thread].tmp")}]删除完成！");
        }
        catch { }

        try
        {
            OnDownloadStatusChanged("下载完成！");
        }
        catch { }
    }
}

public class DownloadManager
{
    public readonly List<FileDownloader> ToRunTask;
    public readonly List<FileDownloader> RunningTask;
    private readonly int maxConcurrentDownloads;
    private long totalbytes = 0;

    public long GetFileSize(string url)
    {
        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
        request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/115.0.0.0 Safari/537.36 Edg/115.0.1901.188";
        long len = request.GetResponse().ContentLength;//取得目标文件的长度
        if (len > 0)
        {
            return len;
        }
        else
        {
            throw new Exception("Error:Get file size error!");
        }
    }

    public DownloadManager(int maxConcurrentDownloads)
    {
        RunningTask = new List<FileDownloader>();
        ToRunTask = new List<FileDownloader>();
        this.maxConcurrentDownloads = maxConcurrentDownloads;
    }

    public readonly struct DownloadTask
    {
        public string Url { get; }
        public string SavePath { get; }

        public DownloadTask(string url, string savePath)
        {
            Url = url;
            SavePath = savePath;
        }
    };

    public void Add(string url,string savePath,int threadcount = 3)
    {
        Task.Run(() =>
        {
            FileDownloader file = new FileDownloader(url, savePath, threadcount);
            if (RunningTask.Count < maxConcurrentDownloads)
            {
                Task.Run(async () => await file.StartDownloadAsync(false));
                RunningTask.Add(file);
            }
            else ToRunTask.Add(file);
            file.OnDownloadCompleted += File_OnDownloadCompleted;
            totalbytes += GetFileSize(url);
        });
    }

    public void Add(DownloadTask[] tasks, int threadcount = 3)
    {
        if (tasks == null) throw new ArgumentNullException(nameof(tasks));
        for (int i = 0;i < tasks.Length;i++)
        {
            Add(tasks[i].Url, tasks[i].SavePath, threadcount);
        }
    }

    private void File_OnDownloadCompleted(string filename)
    {
        StartNext();
    }

    private void StartNext()
    {
        if (ToRunTask.Count > 0 && RunningTask.Count < maxConcurrentDownloads)
        {
            FileDownloader file = ToRunTask[0];
            ToRunTask.Remove(file);
            Task.Run(async () => await file.StartDownloadAsync(false));
            RunningTask.Add(file);
        }
    }

    public async Task WaitAll()
    {
        /*
        for (int i = 0;i < RunningTask.Count;i++)
        {
            await Task.WhenAny(RunningTask[i].FileDownloadingTask);
        }
        */
        await Task.Run(() =>
        {
            while (true)
            {
                while (RunningTask.Count > 0) ;
                if (ToRunTask.Count != 0) StartNext();
                else break;
            }
        });
    }

    public bool IsDownloadComplete()
    {
        if (RunningTask.Count == 0 && ToRunTask.Count == 0) return true;
        return false;
    }

    public double GetDowloadProgress()
    {
        if (totalbytes == 0) return 0;
        if (RunningTask.Count == 0 && ToRunTask.Count == 0) return 100;
        long downloadedbytes = 0;
        foreach (var item in RunningTask)
        {
            downloadedbytes += item.GetDownloadedBytes();
        }
        return (double)downloadedbytes / totalbytes * 100;
    }
}

public class Program
{
    static void Main()
    //static async Task Main()
    {
        ServicePointManager.ServerCertificateValidationCallback = (sender, cert, chain, error) =>
        {
            return true;
        };
        DownloadManager manager = new DownloadManager(2);
        manager.Add("http://speedtest-ny.turnkeyinternet.net/100mb.bin", "F:\\ABC2\\100M-1", 1);
        manager.Add("http://speedtest-ca.turnkeyinternet.net/100mb.bin", "F:\\ABC2\\100M-2", 1);
        manager.Add("http://speedtest.zju.edu.cn/1000M", "F:\\ABC2\\1000M", 1);
        while (!manager.IsDownloadComplete())
        {
            Console.WriteLine(manager.GetDowloadProgress().ToString("f2") + " %");
            Thread.Sleep(1000);
        }
    }
    /*
    static async Task Main()
    {
        FileDownloader file = new FileDownloader("https://software.download.prss.microsoft.com/dbazure/Win10_22H2_Chinese_Simplified_x32v1.iso?t=859c9de3-d914-4542-b45b-d1cd6765eb63&e=1696568994&h=61768a61ea49fc87907347790fb04a3e979ed641174a3a0995c8e18006ff809e", @"F:\ABC\1.iso", 4);
        file.OnDownloadStatusChanged += File_OnDownloadStatusChanged;
        await file.StartDownloadAsync(false);
        while (!file.IsFileMerging)
        {
            FileDownloader.DownloadRemainingTime time = file.GetDownloadRemainingTime();
            Console.WriteLine(file.GetDownloadedBytes() + "/" + file.GetTotalBytes() + "(" + file.GetProgressPercentage().ToString("f2") + "%) " + file.GetDownloadSpeed().ToString("f2") + $"MB/s {time.Hour:00}:{time.Minute:00}:{time.Second:00}");
            Thread.Sleep(2000);
            /*if (file.GetProgressPercentage() > 20.0)
            {
                file.Pause();
                Thread.Sleep(5000);
                await file.StartDownloadAsync(false);
            }*
        }
        while (!file.IsDownloadCompleted) { }
    }

    private static void File_OnDownloadStatusChanged(string status)
    {
        Console.WriteLine(status);
    }
    */
}