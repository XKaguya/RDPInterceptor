using System.Collections.ObjectModel;
using System.Net;
using System.Threading.Tasks;
using WindivertDotnet;

namespace RDPInterceptor.API
{
    public class NetworkInterceptor
    {
        public static bool IpWhitelistMode { get; set; } = true;

        public static ObservableCollection<IPAddress> IpAddrList { get; set; } = new();

        public static bool IsCapture { get; set; } = true;
        
        private static WinDivert Divert { get; set; }

        private static WinDivertPacket Packet { get; set; }
        
        private static WinDivertAddress Addr { get; set; }

        public static bool AddIpIntoList(string Ip)
        {
            if (Ip == null)
            {
                return false;
            }

            IPAddress IpAddr = null;

            if (IPAddress.TryParse(Ip, out IpAddr))
            {
                IpAddrList.Add(IpAddr);
            }
            else
            {
                Logger.Error($"Invalid IP/Domain.");
                return false;
            }
            
            return true;
        }
        
        public static async Task StartCapture()
        {
            Logger.Log("Start Interceptor.");
            
            var filter = Filter.True.And(f => f.Tcp.DstPort == 3389);
            
            Divert = new WinDivert(filter, WinDivertLayer.Network);
            Addr = new();
            Packet = new();

            IsCapture = true;

            while (IsCapture)
            {
                while (IsCapture)
                {
                    if (Divert != null)
                    {
                        try
                        {
                            await Divert.RecvAsync(Packet, Addr);

                            if (ProcessPacket(Packet, Addr))
                            {
                                await Divert.SendAsync(Packet, Addr);
                            }
                        }
                        catch (TaskCanceledException ex)
                        {
                            Logger.Log($"Stop Interceptor.");
                            IsCapture = false;
                        }
                    }
                }
            }
        }

        public static async Task StopCapture()
        {
            IsCapture = false;
            
            if (Divert != null)
            {
                Divert.Dispose();
                Packet.Dispose();
                Addr.Dispose();
            }
            
            Logger.Log("Capture has now stopped.");
        }

        private unsafe static bool ProcessPacket(WinDivertPacket Packet, WinDivertAddress Address)
        {
            if (Packet != null)
            {
                if (!IpWhitelistMode)
                {
                    return true;
                }
                else
                {
                    var result = Packet.GetParseResult();
                    IPAddress SrcIpAddr = result.IPV4Header->SrcAddr;
                    IPAddress DstIpAddr = result.IPV4Header->DstAddr;
                    
                    if (IpAddrList.Contains(SrcIpAddr))
                    {
                        Logger.Debug($"Incomming RDP Connection from {SrcIpAddr} has been accepted.");
                        Packet.CalcChecksums(Address);
                        return true;
                    }
                    else if (IpAddrList.Contains(DstIpAddr))
                    {
                        Logger.Debug($"Outgoing RDP Connection to {DstIpAddr} has been accepted.");
                        Packet.CalcChecksums(Address);
                        return true;
                    }
                    else
                    {
                        Logger.Debug($"Incomming RDP Connection from {SrcIpAddr} has been refused.");
                        return false;
                    }
                }
            }

            return false;
        }
    }
}


