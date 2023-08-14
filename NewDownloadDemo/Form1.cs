using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NewDownloadDemo
{
    public partial class Form1 : Form
    {
        private SaveFileDialog saveFileDialog;
        private DialogResult result;
        private System.Threading.Timer timer;
        private Stopwatch stopwatch;
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            saveFileDialog = new SaveFileDialog();
            saveFileDialog.OverwritePrompt = true;
            saveFileDialog.CreatePrompt = false;
            saveFileDialog.CheckPathExists = false;
            saveFileDialog.CheckFileExists = false;
            saveFileDialog.Filter = "All Files(*.*)|*.*";
            Thread invokeThread = new Thread(new ThreadStart(InvokeMethod));
            invokeThread.SetApartmentState(ApartmentState.STA);
            invokeThread.Start();
            invokeThread.Join();
            if (result == DialogResult.OK)
            {
                textBox2.Text = saveFileDialog.FileName;
            }
        }

        private void InvokeMethod()
        {
            result = saveFileDialog.ShowDialog();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                Process.Start(textBox1.Text);
            }
            catch { }
        }

        private void TextBox3_KeyUp(object sender, KeyEventArgs e)
        {
            try
            {
                int a = int.Parse(textBox3.Text);
                if (a <= 0 || a > 25)
                {
                    MessageBox.Show("错误：无效的线程数！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    textBox3.Text = "3";
                }
            }
            catch
            {
                MessageBox.Show("错误：无效的线程数！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                textBox3.Text = "3";
            }
        }

        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string a = ((string[])e.Data.GetData(DataFormats.FileDrop))[0];
                if (File.Exists(a))
                {
                    if (MessageBox.Show("警告：文件<" + a + ">已经存在！是否要替换？", "警告", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.No) return;
                }
                if (Directory.Exists(a))
                {
                    MessageBox.Show("错误：这是一个文件夹！且已经存在！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                textBox2.Text = a;
            }
            else if (e.Data.GetDataPresent(DataFormats.Text))
            {
                textBox1.Text = e.Data.GetData(DataFormats.UnicodeText).ToString();
            }
        }

        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop) || e.Data.GetDataPresent(DataFormats.Text))
            {
                if (e.Data.GetData(DataFormats.FileDrop) != null && ((string[])e.Data.GetData(DataFormats.FileDrop)).Length == 1)
                {
                    e.Effect = DragDropEffects.All;
                    return;
                }
                try
                {
                    new Uri(e.Data.GetData(DataFormats.UnicodeText).ToString());
                    e.Effect = DragDropEffects.All;
                    return;
                }
                catch { }
                e.Effect = DragDropEffects.None;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private async void button3_Click(object sender, EventArgs e)
        {
            button3.Enabled = false;
            if (textBox1.Text == null || textBox1.Text == "" || textBox2.Text == null || textBox2.Text == "")
            {
                MessageBox.Show("错误：无效的输入数据！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                button3.Enabled = true;
                return;
            }
            try
            {
                new Uri(textBox1.Text);
            }
            catch
            {
                MessageBox.Show("错误：无效的url！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                button3.Enabled = true;
                return;
            }
            stopwatch = new Stopwatch();
            stopwatch.Start();
            progressBar1.Value = 0;
            label3.Text = "0.00 %";
            FileDownloader downloader = new FileDownloader(textBox1.Text, textBox2.Text, int.Parse(textBox3.Text));
            downloader.OnDownloadStatusChanged += Downloader_OnDownloadStatusChanged;
            downloader.OnDownloadCompleted += Downloader_OnDownloadCompleted;
            await Task.Run(async () => await downloader.StartDownloadAsync(false));

            timer = new System.Threading.Timer(new TimerCallback((object s) =>
            {
                double pro = downloader.GetProgressPercentage();
                progressBar1.Value = (int)pro;
                label3.Text = $"{pro:F2} %";
                FileDownloader.DownloadRemainingTime time = downloader.GetDownloadRemainingTime();
                label6.Text = $"{downloader.GetDownloadSpeed():F2} MB/s  {time.Hour:00}:{time.Minute:00}:{time.Second:00}";
            }), null, 0, 1000);
        }

        private void Downloader_OnDownloadCompleted(string filename)
        {
            timer.Dispose();
            progressBar1.Value = 100;
            label3.Text = "100.00 %";
            stopwatch.Stop();
            MessageBox.Show("文件<" + filename + ">下载完成！耗时：" + stopwatch.ElapsedMilliseconds * 1.0 / 1000 + " s。", "信息", MessageBoxButtons.OK, MessageBoxIcon.Information);
            stopwatch.Reset();
            button3.Enabled = true;
        }

        private void Downloader_OnDownloadStatusChanged(string status)
        {
            label1.Text = status;
        }
    }
}
