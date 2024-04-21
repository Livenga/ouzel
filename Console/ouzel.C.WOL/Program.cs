namespace ouzel.C.WOL
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Net.NetworkInformation;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;


    /// <summay></summay>
    static class Program
    {
        /// <summary></summary>
        static async Task Main(string[] args)
        {
            var interfaces = NetworkInterface
                .GetAllNetworkInterfaces()
                .OrderBy(ifr => ifr.Id)
                .Where(ifr => ifr.GetIPProperties().UnicastAddresses.Count(u => u.Address.AddressFamily == AddressFamily.InterNetwork) == 1)
                .Select((ifr, idx) => (Index: idx, Interface: ifr))
                .ToArray();

            Console.WriteLine("ネットワークインターフェース一覧");
            foreach(var o in interfaces)
            {
                Console.WriteLine($"\t#{o.Index}\t{o.Interface.Name}");
                var unicast = o.Interface
                    .GetIPProperties()
                    .UnicastAddresses
                    .First(u => u.Address.AddressFamily == AddressFamily.InterNetwork);

                var addr = string.Join(".", unicast.Address.GetAddressBytes().Select(b_ => $"{b_}"));
                var mask  = string.Join(":", unicast.IPv4Mask.GetAddressBytes().Select(b_ => $"{b_:x2}"));
                var broadcast = GetBroadcastAddress(unicast);

                Console.WriteLine($"\t\t{addr} / {mask}\t{broadcast.ToString()}");
                Console.WriteLine();
            }

            Console.Write("Inferface Number: ");
            var sInputNumber = Console.ReadLine();

            NetworkInterface targetInterface;
            IPAddress? ownAddress = null;
            try
            {
                var targetNumber = Convert.ToInt32(sInputNumber, 10);
                targetInterface = interfaces[targetNumber].Interface;

                ownAddress = targetInterface.GetIPProperties()
                    .UnicastAddresses
                    .FirstOrDefault(u => u.Address.AddressFamily == AddressFamily.InterNetwork)?
                    .Address;

#if DEBUG
                Console.WriteLine($"\t{targetInterface.Id} {targetInterface.Name}");
#endif
            }
            catch
            {
                return;
            }

           
            PhysicalAddress physicalAddress;
            try
            {
                Console.Write("対象物理アドレス: ");
                physicalAddress = PhysicalAddress.Parse(Console.ReadLine() ?? throw new InvalidDataException());
            }
            catch
            {
                Console.Error.WriteLine($"不明なMACアドレスが入力されました.");
                return;
            }

            var targetAddress = GetBroadcastAddress(targetInterface.GetIPProperties().UnicastAddresses.First(u => u.Address.AddressFamily == AddressFamily.InterNetwork));
            var magicPacket = CreateMagicPacket(physicalAddress);

            int port = 9;
            try
            {
                Console.Write("ポート番号: ");
                port = Convert.ToInt32(Console.ReadLine(), 10);
            }
            catch
            {
                Console.Error.WriteLine("\t不正なポート番号が入力された可能性があります. 既定の 9 を使用します.");
            }


            Console.WriteLine($"送信先: {targetAddress.ToString()}:{port}");

            IPAddress? pingTargetAddress = null;
            int pingAttemptCount = 30;
            try
            {
                Console.WriteLine("起動確認");
                Console.Write("\tPING 送信先: ");
                pingTargetAddress = IPAddress.Parse(Console.ReadLine() ?? throw new InvalidDataException());

                Console.Write("\t試行回数: ");
                pingAttemptCount = Convert.ToInt32(Console.ReadLine() ?? throw new InvalidDataException());
            }
            catch { }

            // Magic Packet の送信
            await SendMagicPacketAsync(
                    localEP:           (ownAddress != null)
                        ? new IPEndPoint(ownAddress, 0)
                        : null,
                    endPoint:          new IPEndPoint(address: targetAddress, port: port),
                    data:              magicPacket,
                    cancellationToken: CancellationToken.None);

            // PING テスト
            if(pingTargetAddress != null)
            {
                Console.Error.WriteLine("\tPING 銅通確認...");
                for(var i = 0; i < pingAttemptCount; ++i)
                {
                    try
                    {
                        Console.WriteLine($"\t\t{DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss")} #{i} PING({pingTargetAddress.ToString()})");
                        var isConnected = await DoSendPingAsyng(pingTargetAddress, 5_000);
                        if(isConnected)
                        {
                            break;
                        }
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine($"[Error] {ex.GetType().FullName} {ex.Message}");
                    }
                }
            }
        }


        /// <summary></summary>
        static async Task<bool> DoSendPingAsyng(
                IPAddress address,
                int       timeout = 1_000)
        {
            var buffer = new byte[32];
            var dummy = Guid.NewGuid().ToByteArray();
            Array.Copy(dummy, 0, buffer, 0, dummy.Length);


            using var ping = new Ping();
            var reply = await ping.SendPingAsync(
                    address: address,
                    timeout: timeout,
                    buffer:  buffer,
                    options: new PingOptions() );

            return (reply.Status == IPStatus.Success);
        }


        /// <summary></summary>
        static async Task SendMagicPacketAsync(
                IPEndPoint?       localEP,
                IPEndPoint        endPoint,
                byte[]            data,
                CancellationToken cancellationToken = default(CancellationToken))
        {
            var udp = (localEP != null)
                ? new UdpClient(localEP)
                : new UdpClient();

            using(udp)
            {
                //udp.Connect(endPoint: endPoint);

                await udp.SendAsync(
                        datagram:          data,
                        endPoint:          endPoint,
                        cancellationToken: cancellationToken);
            }
        }


        /// <summary></summary>
        static IPAddress GetBroadcastAddress(UnicastIPAddressInformation info)
        {
            var v4 = info.Address.GetAddressBytes();
            var netmask = info.IPv4Mask.GetAddressBytes();

            if(v4.Length != netmask.Length)
                throw new InvalidDataException();

            var ret = new byte[v4.Length];
            for(var i = 0; i < v4.Length; ++i)
            {
                ret[i] = (byte)((v4[i] & netmask[i]) | ~netmask[i]);
            }

            return new IPAddress(ret);
        }


        /// <summary>
        /// </summary>
        static byte[] CreateMagicPacket(PhysicalAddress physicalAddress)
        {
            var mac = physicalAddress.GetAddressBytes();
            if(mac.Length != 6)
            {
                throw new ArgumentException("無効なMACアドレスの指定.", nameof(physicalAddress));
            }

            return CreateMagicPacket(mac);
        }


        /// <summary>
        /// </summary>
        static byte[] CreateMagicPacket(byte[] mac)
        {
            if(mac.Length != 6)
            {
                throw new ArgumentException("", nameof(mac));
            }

            byte[] magicPacket = new byte[6 + (6 * 16)];

            var header = new byte[] { 0xff, 0xff, 0xff, 0xff, 0xff, 0xff };
            Array.Copy(header, 0, magicPacket, 0, header.Length);

            for(var i = 0; i < 16; ++i)
            {
                Array.Copy(mac, 0, magicPacket, (i + 1) * 6, mac.Length);
            }

#if DEBUG
            var rowCount_ = ((magicPacket.Length % 16) != 0)
                ? (magicPacket.Length / 16) + 1
                : magicPacket.Length / 16;

            Console.Error.WriteLine("[Debug] Magic Packet");
            Console.Error.WriteLine("        >>>");
            for(var row_ = 0; row_ < rowCount_; ++row_)
            {
                Console.Write($"\t{row_ * 16:X8} | {magicPacket[row_ * 16]:X2}");
                for(var col_ = 1; col_ < 16; ++col_)
                {
                    var offset_ = (row_ * 16) + col_;

                    if(offset_ < magicPacket.Length)
                    {
                        Console.Error.Write($" {magicPacket[offset_]:X2}");
                    }
                    else
                    {
                        Console.Error.Write(" XX");
                    }
                }
                Console.Error.WriteLine();
            }
            Console.Error.WriteLine("        <<<");
#endif

            return magicPacket;
        }
    }
}
