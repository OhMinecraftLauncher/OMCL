using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WinDivertSharp;

namespace WLANPackageGet
{
    internal class Program
    {
        static void Main(string[] args)
        {
            IntPtr handle = WinDivert.WinDivertOpen("outbound and tcp.PayloadLength > 0 and tcp.PayloadLength < 50", WinDivertLayer.Network, 0, WinDivertOpenFlags.None);

            int err = Marshal.GetLastWin32Error();
            if (err != 0 || handle == IntPtr.Zero)
            {
                Console.WriteLine("无法打开 WinDivert 句柄：" + err);
                return;
            }

            WinDivertBuffer buffer = new WinDivertBuffer();
            WinDivertAddress address = new WinDivertAddress();
            uint readLen = 0;
            while (true)
            {
                NativeOverlapped native = new NativeOverlapped();
                WinDivert.WinDivertRecvEx(handle, buffer, 0, ref address, ref readLen, ref native);
                WinDivertParseResult result = WinDivert.WinDivertHelperParsePacket(buffer, readLen);
                unsafe
                {
                    byte[] bytes = new byte[result.PacketPayloadLength];
                    if (result.PacketPayload != null)
                    {
                        Marshal.Copy((IntPtr)result.PacketPayload, bytes, 0, (int)result.PacketPayloadLength);
                        for (int i = 0; i < result.PacketPayloadLength; i++)
                        {
                            Console.Write((bytes[i] >= 33 && bytes[i] <= 126) ? Encoding.ASCII.GetString(new byte[1] { bytes[i] }) : ".");
                        }
                    }
                }
                /*for (int i = 0; i < readLen; i++)
                {
                    Console.Write((buffer[i] >= 33 && buffer[i] <= 126) ? Encoding.ASCII.GetString(new byte[1] { buffer[i] }) : ".");
                }
                Console.WriteLine();*/
                Console.WriteLine();
                if (!WinDivert.WinDivertSendEx(handle, buffer, readLen, 0, ref address)) 
                {
                    Console.WriteLine("Send Error:" + Marshal.GetLastWin32Error());
                }
            }
        }
    }
}
