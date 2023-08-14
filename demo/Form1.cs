using OMCL_DLL.Tools;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace demo
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {

            Tools.LaunchMinecraft launch = new Tools.LaunchMinecraft();
            launch.OnMinecraftCrash += Launch_OnMinecraftCrash;
            launch.LaunchGame("C:\\Program Files\\Java\\jre1.8.0_331\\bin\\java.exe", "1.16.5", "AAA");
        }
        private static void Launch_OnMinecraftCrash(CrashMessage crashMessage)
        {
            Console.WriteLine(crashMessage.Message);
            Console.WriteLine("可能的解决方法：\n" + crashMessage.Solution);
            Console.WriteLine("\n\n详细信息：\n退出代码：" + crashMessage.ExitCode + "\n版本名称：" + crashMessage.VersionName);
        }
    }
}
