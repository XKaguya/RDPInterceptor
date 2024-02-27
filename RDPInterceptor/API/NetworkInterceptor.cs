using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using WindivertDotnet;

namespace RDPInterceptor.API
{
    public class NetworkInterceptor
    {
        public static CancellationTokenSource? CaptureCancellationTokenSource = new();

        public static bool IpWhitelistMode { get; set; } = true;

        public static ObservableCollection<IPAddress> IpAddrList { get; set; } = new();

        public static bool IsLogConnection { get; set; } = false;

        public static bool WriteIntoLog { get; set; } = true;

        public static ushort Port { get; set; } = 3389;

        private static WinDivert? Divert { get; set; }
        
        private static readonly SemaphoreSlim semaphore = new(1);

        private static WinDivertPacket? Packet { get; set; }

        private static WinDivertAddress? Addr { get; set; }

        public static async Task<bool> AddIpIntoList(string Ip)
        {
            if (Ip == null)
            {
                return false;
            }

            IPAddress IpAddr;

            if (IPAddress.TryParse(Ip, out IpAddr))
            {
                if (!IpAddrList.Contains(IpAddr))
                {
                    IpAddrList.Add(IpAddr);
                    await AddIpIntoWhitelistFile(IpAddr);
                }
                else
                {
                    Logger.Error($"There's already a {IpAddr}");
                    return false;
                }
            }
            else
            {
                Logger.Error($"Invalid IP/Domain.");
                return false;
            }

            return true;
        }

        public static async Task StartCapture(CancellationToken cancellationToken)
        {
            Logger.Log("Start Interceptor.");

            if (cancellationToken.IsCancellationRequested)
            {
                CaptureCancellationTokenSource = new CancellationTokenSource();
            }

            try
            {
                await RunCapture(CaptureCancellationTokenSource.Token);
            }
            catch (OperationCanceledException e)
            {
                Logger.Error(e.Message + e.StackTrace);
            }
        }

        private static async Task RunCapture(CancellationToken cancellationToken)
        {
            var filter = Filter.True.And(f => f.Tcp.DstPort == Port);

            Divert = new WinDivert(filter, WinDivertLayer.Network);
            Addr = new();
            Packet = new();

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (Divert != null)
                    {
                        await Divert.RecvAsync(Packet, Addr, cancellationToken);

                        if (await ProcessPacketAsync(Packet, Addr))
                        {
                            await Divert.SendAsync(Packet, Addr, cancellationToken);
                        }
                    }
                }
            }
            catch (OperationCanceledException ex)
            {
                Logger.Log($"Stop Interceptor.");
            }
            finally
            {
                Divert?.Dispose();
                Addr?.Dispose();
                Packet?.Dispose();
            }
        }

        public static async Task StopCapture()
        {
            if (CaptureCancellationTokenSource != null)
            {
                CaptureCancellationTokenSource.Cancel();
                await Task.Delay(100);
                Logger.Log("Capture has now stopped.");
            }
            else
            {
                Logger.Log("Capture is not running.");
            }
        }


        private static unsafe void GetIpAddresses(IPV4Header* header, out IPAddress srcIpAddr, out IPAddress dstIpAddr)
        {
            IPAddress srcIp = header->SrcAddr;
            IPAddress dstIp = header->DstAddr;
            srcIpAddr = srcIp;
            dstIpAddr = dstIp;
        }

        private static async Task<bool> ProcessPacketAsync(WinDivertPacket Packet, WinDivertAddress Address)
        {
            if (Packet != null)
            {
                try
                {
                    if (!IpWhitelistMode)
                    {
                        return true;
                    }
                    else
                    {
                        var result = Packet.GetParseResult();
                        IPAddress SrcIpAddr, DstIpAddr;
                        unsafe
                        {
                            GetIpAddresses(result.IPV4Header, out SrcIpAddr, out DstIpAddr);
                        }

                        if (IpAddrList.Contains(SrcIpAddr))
                        {
                            LogConnections(IsLogConnection, $"Incoming RDP Connection from {SrcIpAddr} has been accepted.");
                            Packet.CalcChecksums(Address);
                            return true;
                        }
                        else if (IpAddrList.Contains(DstIpAddr))
                        {
                            LogConnections(IsLogConnection, $"Outgoing RDP Connection to {DstIpAddr} has been accepted.");
                            Packet.CalcChecksums(Address);
                            return true;
                        }
                        else
                        {
                            LogConnections(IsLogConnection, $"Incoming RDP Connection from {SrcIpAddr} has been refused.");
                            await LogConnectionAsync(SrcIpAddr);

                            return false;
                        }
                    }
                }
                catch (Exception e)
                {
                    Logger.Error(e.Message + e.StackTrace);
                    throw;
                }
            }

            return false;
        }

        private static void LogConnections(bool isLogConnection, string content)
        {
            if (isLogConnection)
            {
                Logger.Debug(content);
            }
        }

        private static async Task LogConnectionAsync(IPAddress srcIpAddr)
        {
            try
            {
                string logFilePath = "Connectionlist.log";

                if (File.Exists(logFilePath))
                {
                    string[] lines = await File.ReadAllLinesAsync(logFilePath);
                    if (lines.Contains(srcIpAddr.ToString()))
                    {
                        return;
                    }
                }
                else
                {
                    FileStream fs = File.Create(logFilePath);
                    fs.Close();
                }

                using (StreamWriter writer = File.AppendText(logFilePath))
                {
                    await writer.WriteLineAsync(srcIpAddr.ToString());
                }
            }
            catch (Exception e)
            {
                Logger.Error(e.Message + e.StackTrace);
                throw;
            }
        }

        public static async void ReadLinesFromFileAsync()
        {
            await semaphore.WaitAsync();

            string WhitelistFilePath = "Whitelist.txt";

            try
            {
                if (File.Exists(WhitelistFilePath))
                {
                    IPAddress IpAddr;

                    foreach (string ip in await File.ReadAllLinesAsync(WhitelistFilePath))
                    {
                        if (IPAddress.TryParse(ip, out IpAddr))
                        {
                            IpAddrList.Add(IpAddr);

                            Logger.Log($"IP {ip} has been read into whitelist.");
                        }
                        else
                        {
                            Logger.Error($"ERROR! Failed to parse {ip}.");
                        }
                    }
                }
                else
                {
                    FileStream fs = File.Create(WhitelistFilePath);
                    fs.Close();
                }
            }
            finally
            {
                semaphore.Release();
            }
        }

        public static async Task AddIpIntoWhitelistFile(IPAddress ipAddress)
        {
            Logger.Debug($"Method AddIpIntoWhitelistFile called.");

            try
            {
                string WhitelistFilePath = "Whitelist.txt";

                if (File.Exists(WhitelistFilePath))
                {
                    Logger.Debug($"File {WhitelistFilePath} exists. Proceeding...");

                    await semaphore.WaitAsync();

                    string[] lines = await File.ReadAllLinesAsync(WhitelistFilePath);
                    if (Array.Exists(lines, line => line.Equals(ipAddress.ToString())))
                    {
                        Logger.Debug($"IP {ipAddress} already in {WhitelistFilePath}");
                    }
                    else
                    {
                        using (StreamWriter writer = File.AppendText(WhitelistFilePath))
                        {
                            await writer.WriteLineAsync(ipAddress.ToString());
                        }

                        Logger.Debug($"IP {ipAddress} has been written into {WhitelistFilePath}");
                    }
                }
                else
                {
                    FileStream fs = File.Create(WhitelistFilePath);
                    fs.Close();

                    Logger.Error($"File {WhitelistFilePath} doesn't exist. Now create file {WhitelistFilePath}.");

                    await AddIpIntoWhitelistFile(ipAddress);
                }
            }
            catch (Exception e)
            {
                Logger.Error(e.Message + e.Message);
                throw;
            }
            finally
            {
                semaphore.Release();
            }
        }
        
        public static async Task RemoveIpFromList(IPAddress ipAddress)
        {
            Logger.Debug($"Method RemoveIpFromWhitelist called.");

            try
            {
                if (IpAddrList.Contains(ipAddress))
                {
                    IpAddrList.Remove(ipAddress);
                }
                
                string WhitelistFilePath = "Whitelist.txt";

                if (File.Exists(WhitelistFilePath))
                {
                    Logger.Debug($"File {WhitelistFilePath} exists. Proceeding...");

                    await semaphore.WaitAsync();

                    string[] lines = await File.ReadAllLinesAsync(WhitelistFilePath);

                    if (lines.Contains(ipAddress.ToString()))
                    {
                        List<string> linesList = lines.ToList();
                        linesList.Remove(ipAddress.ToString());
                        
                        using (StreamWriter writer = new StreamWriter(WhitelistFilePath, false))
                        {
                            foreach (string ip in linesList)
                            {
                                await writer.WriteLineAsync(ip);
                            }
                        }
                    }
                }
                else
                {
                    FileStream fs = File.Create(WhitelistFilePath);
                    fs.Close();

                    Logger.Error($"File {WhitelistFilePath} doesn't exist. Now create file {WhitelistFilePath}.");
                }
            }
            catch (Exception e)
            {
                Logger.Error(e.Message + e.Message);
                throw;
            }
            finally
            {
                semaphore.Release();
            }
        }
    }
}