using System;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace OMCL_DLL.Tools.Download
{
    public class DownloadFile
    {
        private static bool[] threadw; //每个线程结束标志  
        private static string[] filenamew;//每个线程接收文件的文件名  
        private static long[] filestartw;//每个线程接收文件的起始位置  
        private static long[] filesizew;//每个线程接收文件的大小  
        private static string strurl;//接受文件的URL  
        private static bool hb;//文件合并标志  
        private static int thread;//进程数  
        private static string _path;
        private static int CanTry = 3;
        private static int TryNum = 0;

        public delegate void FinishDownloadDelegate();
        public event FinishDownloadDelegate FinishDownload;

        public DownloadFile(string url, string path, int thread = 3)
        {
            strurl = url;
            _path = path;
            if (thread < 1)
            {
                DownloadFile.thread = 1;
            }
            else if (thread > 2048)
            {

                DownloadFile.thread = 2048;
            }
            else
            {
                DownloadFile.thread = thread;
            }
        }
        public void StartDownload()
        {
            try
            {
                DateTime dt = DateTime.Now;//开始接收时间  
                //if (TryNum == 0) //new OMCLLog("[DownloadFile_Start]开始下载文件：" + strurl + "，保存位置：" + path + "，线程数：" + _thread + "个。", OMCLExceptionClass.DLL, OMCLExceptionType.Message);
                HttpWebRequest request;
                long filesize = 0;
                request = (HttpWebRequest)HttpWebRequest.Create(strurl);
                filesize = request.GetResponse().ContentLength;//取得目标文件的长度  
                request.Abort();

                Console.WriteLine("File size:" + filesize / (1024 * 1024 * 1.0) + " M");

                if (filesize <= 1024 * 1024 * 10)
                {
                    thread = 1;
                }

                //根据线程数初始化数组  
                threadw = new bool[thread];
                filenamew = new string[thread];
                filestartw = new long[thread];
                filesizew = new long[thread];

                //计算每个线程应该接收文件的大小  
                long filethread = filesize / thread;//平均分配  
                long filethreade = filethread + filesize % thread;//剩余部分由最后一个线程完成  
                                                                      //为数组赋值  
                for (int i = 0; i < thread; i++)
                {
                    threadw[i] = false;//每个线程状态的初始值为假  
                    filenamew[i] = _path + '[' + i.ToString() + ']' + ".odtmp";//每个线程接收文件的临时文件名  
                    if (i < thread - 1)
                    {
                        filestartw[i] = filethread * i;//每个线程接收文件的起始点  
                        filesizew[i] = filethread - 1;//每个线程接收文件的长度  
                    }
                    else
                    {
                        filestartw[i] = filethread * i;
                        filesizew[i] = filethreade - 1;
                    }
                }
                //定义线程数组，启动接收线程  
                Thread[] threadk = new Thread[thread];
                HttpFile[] httpfile = new HttpFile[thread];
                for (int j = 0; j < thread; j++)
                {
                    httpfile[j] = new HttpFile(j, strurl, _path);
                    threadk[j] = new Thread(new ThreadStart(httpfile[j].receive));
                    threadk[j].Start();
                }
                //启动合并各线程接收的文件线程  
                hbfile();
            }
            catch (Exception e)
            {
                if (TryNum == CanTry)
                {
                    TryNum = 0;
                    //new OMCLLog("[DownloadFile_Start]请求下载文件时出现错误：" + @e.Message, OMCLExceptionClass.DLL, OMCLExceptionType.Error);
                    //new OMCLLog("已达到重试次数上限：" + CanTry + "次！", OMCLExceptionClass.DLL, OMCLExceptionType.Warning);
                    throw new Exception("下载失败！" + e);
                }
                else
                {
                    TryNum++;
                    //new OMCLLog("[DownloadFile_Start]请求下载文件时出现错误：" + @e.Message, OMCLExceptionClass.DLL, OMCLExceptionType.Error);
                    //new OMCLLog("[DownloadFile_Start]下载重启，进行第" + TryNum + "/" + CanTry + "次重试。", OMCLExceptionClass.DLL, OMCLExceptionType.Message);
                    StartDownload();
                }
            }
        }
        public async Task StartDownloadAsync()
        {
            try
            {
                DateTime dt = DateTime.Now;//开始接收时间  
                //if (TryNum == 0) //new OMCLLog("[DownloadFile_Start]开始下载文件：" + strurl + "，保存位置：" + path + "，线程数：" + _thread + "个。", OMCLExceptionClass.DLL, OMCLExceptionType.Message);
                HttpWebRequest request;
                long filesize = 0;
                request = (HttpWebRequest)HttpWebRequest.Create(strurl);
                filesize = request.GetResponse().ContentLength;//取得目标文件的长度  
                request.Abort();

                if (filesize <= 1024 * 1024 * 10)
                {
                    thread = 1;
                }

                //根据线程数初始化数组  
                threadw = new bool[thread];
                filenamew = new string[thread];
                filestartw = new long[thread];
                filesizew = new long[thread];

                //计算每个线程应该接收文件的大小  
                long filethread = filesize / thread;//平均分配  
                long filethreade = filethread + filesize % thread;//剩余部分由最后一个线程完成  
                                                                      //为数组赋值  
                for (int i = 0; i < thread; i++)
                {
                    threadw[i] = false;//每个线程状态的初始值为假  
                    //filenamew[i] = Path.GetTempPath() + @"\" + Path.GetFileName(_path) + i.ToString() + ".dat";//每个线程接收文件的临时文件名  
                    filenamew[i] = _path + '[' + i.ToString() + ']' + ".odtmp";//每个线程接收文件的临时文件名  
                    if (i < thread - 1)
                    {
                        filestartw[i] = filethread * i;//每个线程接收文件的起始点  
                        filesizew[i] = filethread - 1;//每个线程接收文件的长度  
                    }
                    else
                    {
                        filestartw[i] = filethread * i;
                        filesizew[i] = filethreade - 1;
                    }
                }
                //定义线程数组，启动接收线程  
                Thread[] threadk = new Thread[thread];
                HttpFile[] httpfile = new HttpFile[thread];
                for (int j = 0; j < thread; j++)
                {
                    httpfile[j] = new HttpFile(j, strurl, _path);
                    threadk[j] = new Thread(new ThreadStart(httpfile[j].receive));
                    threadk[j].Start();
                }
                //启动合并各线程接收的文件线程  
                Task start = new Task(() => hbfile());
                start.Start();
                await Task.WhenAll(start);
            }
            catch (Exception e)
            {
                if (TryNum == CanTry)
                {
                    TryNum = 0;
                    //new OMCLLog("[DownloadFile_Start]请求下载文件时出现错误：" + @e.Message, OMCLExceptionClass.DLL, OMCLExceptionType.Error);
                    //new OMCLLog("已达到重试次数上限：" + CanTry + "次！", OMCLExceptionClass.DLL, OMCLExceptionType.Warning);
                    throw new Exception("下载失败！");
                }
                else
                {
                    TryNum++;
                    //new OMCLLog("[DownloadFile_Start]请求下载文件时出现错误：" + @e.Message, OMCLExceptionClass.DLL, OMCLExceptionType.Error);
                    //new OMCLLog("[DownloadFile_Start]下载重启，进行第" + TryNum + "/" + CanTry + "次重试。", OMCLExceptionClass.DLL, OMCLExceptionType.Message);
                    await StartDownloadAsync();
                }
            }
        }
        public void StartDownloadNoWait()
        {
            try
            {
                DateTime dt = DateTime.Now;//开始接收时间  
                //if (TryNum == 0) //new OMCLLog("[DownloadFile_Start]开始下载文件：" + strurl + "，保存位置：" + path + "，线程数：" + _thread + "个。", OMCLExceptionClass.DLL, OMCLExceptionType.Message);
                HttpWebRequest request;
                long filesize = 0;
                request = (HttpWebRequest)HttpWebRequest.Create(strurl);
                filesize = request.GetResponse().ContentLength;//取得目标文件的长度  
                request.Abort();

                if (filesize <= 1024 * 1024 * 10)
                {
                    thread = 1;
                }

                //根据线程数初始化数组  
                threadw = new bool[thread];
                filenamew = new string[thread];
                filestartw = new long[thread];
                filesizew = new long[thread];

                //计算每个线程应该接收文件的大小  
                long filethread = filesize / thread;//平均分配  
                long filethreade = filethread + filesize % thread;//剩余部分由最后一个线程完成  
                                                                      //为数组赋值  
                for (int i = 0; i < thread; i++)
                {
                    threadw[i] = false;//每个线程状态的初始值为假  
                    filenamew[i] = _path + '[' + i.ToString() + ']' + ".odtmp";//每个线程接收文件的临时文件名  
                    if (i < thread - 1)
                    {
                        filestartw[i] = filethread * i;//每个线程接收文件的起始点  
                        filesizew[i] = filethread - 1;//每个线程接收文件的长度  
                    }
                    else
                    {
                        filestartw[i] = filethread * i;
                        filesizew[i] = filethreade - 1;
                    }
                }
                //定义线程数组，启动接收线程  
                Thread[] threadk = new Thread[thread];
                HttpFile[] httpfile = new HttpFile[thread];
                for (int j = 0; j < thread; j++)
                {
                    httpfile[j] = new HttpFile(j, strurl, _path);
                    threadk[j] = new Thread(new ThreadStart(httpfile[j].receive));
                    threadk[j].Start();
                }
                //启动合并各线程接收的文件线程  
                Task start = new Task(() => hbfile());
                start.Start();
            }
            catch (Exception e)
            {
                if (TryNum == CanTry)
                {
                    TryNum = 0;
                    //new OMCLLog("[DownloadFile_Start]请求下载文件时出现错误：" + @e.Message, OMCLExceptionClass.DLL, OMCLExceptionType.Error);
                    //new OMCLLog("已达到重试次数上限：" + CanTry + "次！", OMCLExceptionClass.DLL, OMCLExceptionType.Warning);
                    throw new Exception("下载失败！");
                }
                else
                {
                    TryNum++;
                    //new OMCLLog("[DownloadFile_Start]请求下载文件时出现错误：" + @e.Message, OMCLExceptionClass.DLL, OMCLExceptionType.Error);
                    //new OMCLLog("[DownloadFile_Start]下载重启，进行第" + TryNum + "/" + CanTry + "次重试。", OMCLExceptionClass.DLL, OMCLExceptionType.Message);
                    StartDownloadNoWait();
                }
            }
        }
        private void hbfile()
        {
            try
            {
                while (true)//等待  
                {
                    hb = true;
                    for (int i = 0; i < thread; i++)
                    {
                        if (threadw[i] == false)//有未结束线程，等待  
                        {
                            hb = false;
                            Thread.Sleep(100);
                            break;
                        }
                    }
                    if (hb == true)//所有线程均已结束，停止等待，  
                    {
                        break;
                    }
                }
                FileStream fs;//开始合并  
                FileStream fstemp;
                int readfile;
                byte[] bytes = new byte[512];
                string dir = Path.GetDirectoryName(_path);
                if (dir != string.Empty)
                {
                    if (!Directory.Exists(dir)) Directory.CreateDirectory(Path.GetDirectoryName(_path));
                }
                fs = new FileStream(_path, System.IO.FileMode.Create);
                for (int k = 0; k < thread; k++)
                {
                    fstemp = new FileStream(filenamew[k], System.IO.FileMode.Open);
                    while (true)
                    {
                        readfile = fstemp.Read(bytes, 0, 512);
                        if (readfile > 0)
                        {
                            fs.Write(bytes, 0, readfile);
                        }
                        else
                        {
                            break;
                        }
                    }
                    fstemp.Close();
                }
                fs.Close();
                DateTime dt = DateTime.Now;
                for (int i = 0; i < thread; i++)
                {
                    File.Delete(filenamew[i]);
                }
                try
                {
                    FinishDownload();
                }
                catch { }
            }
            catch (Exception e)
            {
                //new OMCLLog("[DownloadFile_hbfile]多线程下载出现错误，合并文件时出现错误：" + @e.Message, OMCLExceptionClass.DLL, OMCLExceptionType.Error);
                throw new Exception("下载文件时出现错误！");
            }
        }

        private class HttpFile
        {
            private readonly int threadh;//线程代号  
            private string filename;//文件名  
            private string strUrl;//接收文件的URL  
            private FileStream fs;
            private HttpWebRequest request;
            private Stream ns;
            private byte[] nbytes;//接收缓冲区  
            private int nreadsize;//接收字节数  
            public HttpFile(int thread, string url, string path)//构造方法  
            {
                threadh = thread;
                strUrl = url;
                _path = path;
            }
            public void receive()//接收线程  
            {
                while (true)
                {
                    try
                    {
                        filename = filenamew[threadh];
                        strUrl = strurl;
                        ns = null;
                        nbytes = new byte[512];
                        nreadsize = 0;
                        //Console.WriteLine("线程" + threadh.ToString() + "开始接收");
                        fs = new FileStream(filename, System.IO.FileMode.Create);
                        request = (HttpWebRequest)HttpWebRequest.Create(strUrl);
                        //接收的起始位置及接收的长度   
                        request.AddRange(filestartw[threadh],
                        filestartw[threadh] + filesizew[threadh]);
                        ns = request.GetResponse().GetResponseStream();//获得接收流  
                        nreadsize = ns.Read(nbytes, 0, 512);
                        while (nreadsize > 0)
                        {
                            fs.Write(nbytes, 0, nreadsize);
                            nreadsize = ns.Read(nbytes, 0, 512);
                            //Console.WriteLine("线程" + threadh.ToString() + "正在接收");
                        }
                        fs.Close();
                        fs.Dispose();
                        request.Abort();
                        ns.Close();
                        ns.Dispose();
                        Console.WriteLine("线程" + threadh.ToString() + "接收完毕!");
                        threadw[threadh] = true;
                        break;
                    }
                    catch (Exception e)
                    {
                        //new OMCLLog("[DownloadFile_Receive]" + @e.Message, OMCLExceptionClass.DLL, OMCLExceptionType.Error);
                        //throw new Exception("下载文件时出现错误！");
                        try
                        {
                            fs.Close();
                            fs.Dispose();
                            ns.Close();
                            ns.Dispose();
                            request.Abort();
                        }
                        catch { }
                        Console.WriteLine("线程" + threadh.ToString() + "下载失败，正在重启!---->" + @e.Message);
                    }
                }
            }
        }
    }
}