using System;
using System.Collections.ObjectModel;
using System.Diagnostics.Eventing.Reader;
using System.Net;
using System.Text;
using PacketDotNet;
using SharpPcap;
using SharpPcap.LibPcap;
using SharpPcap.WinPcap;

namespace WLANPackageGet
{
    internal class Program
    {
        static void Main(string[] args)
        {
            for (int i = 0; i < CaptureDeviceList.Instance.Count; i++)
            {
                string[] sps = CaptureDeviceList.Instance[i].ToString().Split('\n');
                for (int j = 0; j < sps.Length; j++)
                {
                    if (sps[j].Contains("FriendlyName: "))
                    {
                        Console.WriteLine($"[{i}] " + sps[j].Replace("FriendlyName: ", ""));
                    }
                }
            }
            string con = Console.ReadLine();
            int num = int.Parse(con);

            ICaptureDevice device = CaptureDeviceList.Instance[num];

            device.OnPacketArrival += Device_OnPacketArrival;

            device.Open(DeviceMode.Promiscuous);

            device.StartCapture();

            while (true) ;
        }

        private static void Device_OnPacketArrival(object sender, CaptureEventArgs e)
        {
            RawCapture capture = e.Packet;
            Packet packet = Packet.ParsePacket(capture.LinkLayerType, capture.Data);
            IpPacket ippacket = IpPacket.GetEncapsulated(packet);
            if (ippacket != null)
            {
                TcpPacket tcpPacket = TcpPacket.GetEncapsulated(packet);
                UdpPacket udpPacket = UdpPacket.GetEncapsulated(packet);
                if (tcpPacket != null)
                {
                    Console.WriteLine("协议：Tcp");
                    Console.WriteLine("源地址：" + ippacket.SourceAddress.ToString() + " [" + tcpPacket.SourcePort.ToString() + ']');
                    Console.WriteLine("目标地址：" + ippacket.DestinationAddress.ToString() + " [" + tcpPacket.DestinationPort.ToString() + ']');
                }
                else if (udpPacket != null)
                {
                    Console.WriteLine("协议：Udp");
                    Console.WriteLine("源地址：" + ippacket.SourceAddress.ToString() + " [" + udpPacket.SourcePort.ToString() + ']');
                    Console.WriteLine("目标地址：" + ippacket.DestinationAddress.ToString() + " [" + udpPacket.DestinationPort.ToString() + ']');
                }
                else
                {
                    Console.WriteLine("协议：Ip");
                    Console.WriteLine("源地址：" + ippacket.SourceAddress.ToString());
                    Console.WriteLine("目标地址：" + ippacket.DestinationAddress.ToString());
                }
                byte[] bytes = ippacket.BytesHighPerformance.Bytes;
                Console.Write("报文信息：");
                for (int i = 0;i < bytes.Length;i++)
                {
                    Console.Write((bytes[i] >= 33 && bytes[i] <= 126) ? Encoding.ASCII.GetString(new byte[1] { bytes[i] }) : ".");
                }
                Console.WriteLine();
                bool flag_s = false;
                bool flag_d = false;
                ReadOnlyCollection<PcapAddress> ans = ((WinPcapDevice)e.Device).Addresses;
                foreach (PcapAddress an in ans)
                {
                    if (an != null && an.Addr != null && an.Addr.ipAddress != null)
                    {
                        if (ippacket.SourceAddress.ToString() == an.Addr.ipAddress.ToString())
                        {
                            flag_s = true;
                        }
                        if (ippacket.DestinationAddress.ToString() == an.Addr.ipAddress.ToString())
                        {
                            flag_d = true;
                        }
                    }
                }
                if (flag_s)
                {
                    Console.WriteLine("方向：发出");
                }
                else if (flag_d)
                {
                    Console.WriteLine("方向：收入");
                }
                else
                {
                    Console.WriteLine("方向：未知");
                }
            }
            Console.WriteLine();
        }

        /*private static void Program_OnPacketArrival(object sender, PacketCapture e)
        {
            try
            {
                RawCapture capture = e.GetPacket();
                Packet packet = Packet.ParsePacket(capture.LinkLayerType, capture.Data);
                TcpPacket tcp = (TcpPacket)packet;
                IpPacket iPPacket = (IpPacket)packet;
                Console.WriteLine("源地址：" + iPPacket.SourceAddress);
                Console.WriteLine("目标地址" + iPPacket.DestinationAddress);
                Console.WriteLine();
                //Console.WriteLine(Encoding.Default.GetString(packet.BytesSegment.ActualBytes()));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }*/
    }
}
