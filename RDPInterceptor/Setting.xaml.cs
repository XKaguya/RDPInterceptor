﻿using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using RDPInterceptor.API;

namespace RDPInterceptor;

public partial class Setting : Window
{
    private static Setting? instance;

    private static string settingFilePath = "Setting.xml";
    private static readonly object lockObject = new object();
    
    public static Setting Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new Setting();
                instance.InitializeComponent();
            }

            return instance;
        }
    }

    private void WhiteListModeEvent(object sender, RoutedEventArgs ev)
    {
        NetworkInterceptor.IpWhitelistMode = WhiteListModeCheck.IsChecked.Value;

        Logger.Debug($"IpWhitelistMode: {WhiteListModeCheck.IsChecked.Value}");

        WriteIntoSettingFile();
    }

    private void WriteIpLogEvent(object sender, RoutedEventArgs ev)
    {
        NetworkInterceptor.WriteIntoLog = IpLogCheck.IsChecked.Value;

        Logger.Debug($"WriteIntoLog: {IpLogCheck.IsChecked.Value}");

        WriteIntoSettingFile();
    }

    private void LogDebug(object sender, RoutedEventArgs ev)
    {
        if (DebugLog.IsChecked.Value)
        {
            Logger.SetLogLevel("DEBUG");
        }
        else
        {
            Logger.SetLogLevel("INFO");
        }

        Logger.Log($"Log Level is now: {Logger.LogLevel}");

        WriteIntoSettingFile();
    }

    private void RdpPortEvent(object sender, TextChangedEventArgs e)
    {
        ushort port;
        if (UInt16.TryParse(RdpPort.Text, out port))
        {
            NetworkInterceptor.Port = port;

            Logger.Debug($"RdpPort: {NetworkInterceptor.Port}");

            WriteIntoSettingFile();
        }
    }

    private void WebUIPortEvent(object sender, TextChangedEventArgs e)
    {
        ushort port;
        if (UInt16.TryParse(WebPort.Text, out port))
        {
            MainWindow.WebPort = port;

            Logger.Debug($"WebUI Port: {MainWindow.WebPort}");

            WriteIntoSettingFile();
        }
    }

    private void WriteIntoSettingFile()
    {
        lock (lockObject)
        {
            using (StreamWriter writer = new StreamWriter(settingFilePath, false))
            {
                Logger.Log("Called WriteSet");
                
                writer.WriteLine("<Settings>");

                writer.WriteLine($"  <IpWhitelistMode>{NetworkInterceptor.IpWhitelistMode}</IpWhitelistMode>");
                writer.WriteLine($"  <WriteIntoLog>{NetworkInterceptor.WriteIntoLog}</WriteIntoLog>");
                writer.WriteLine($"  <LogLevel>{Logger.LogLevel}</LogLevel>");
                writer.WriteLine($"  <RdpPort>{NetworkInterceptor.Port}</RdpPort>");
                writer.WriteLine($"  <WebPort>{MainWindow.WebPort}</WebPort>");
                writer.WriteLine($"  <ConnectionLog>{NetworkInterceptor.IsLogConnection}</ConnectionLog>");

                writer.WriteLine("</Settings>");
            }
        }
    }
    
    public static void WriteIntoSettingFile(App.Argument argument)
    {
        using (StreamWriter writer = new StreamWriter(settingFilePath, false))
        {
            Logger.Log("Called WriteSet");
                
            writer.WriteLine("<Settings>");

            writer.WriteLine($"  <IpWhitelistMode>{argument.WhiteList}</IpWhitelistMode>");
            writer.WriteLine($"  <WriteIntoLog>{NetworkInterceptor.WriteIntoLog}</WriteIntoLog>");
            writer.WriteLine($"  <LogLevel>{argument.LogLevel}</LogLevel>");
            writer.WriteLine($"  <RdpPort>{argument.Port}</RdpPort>");
            writer.WriteLine($"  <WebPort>{argument.UiPort}</WebPort>");
            writer.WriteLine($"  <ConnectionLog>{argument.LogConnection}</ConnectionLog>");

            writer.WriteLine("</Settings>");
        }
    }
    
    public void ReadFromSettingFileSync()
    {
        if (File.Exists(settingFilePath))
        {
            Logger.Debug("Called ReadSet");
                
            Logger.Log($"Trying reading settings from file {settingFilePath}");

            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var doc = XDocument.Load(settingFilePath);
                    var settingsElement = doc.Element("Settings");

                    if (settingsElement != null)
                    {
                        NetworkInterceptor.IpWhitelistMode = bool.Parse(settingsElement.Element("IpWhitelistMode").Value);
                        WhiteListModeCheck.IsChecked = NetworkInterceptor.IpWhitelistMode;

                        NetworkInterceptor.WriteIntoLog = bool.Parse(settingsElement.Element("WriteIntoLog").Value);
                        IpLogCheck.IsChecked = NetworkInterceptor.WriteIntoLog;

                        Logger.SetLogLevel(settingsElement.Element("LogLevel").Value);
                        DebugLog.IsChecked = Logger.LogLevel == "DEBUG";

                        NetworkInterceptor.Port = ushort.Parse(settingsElement.Element("RdpPort").Value);
                        RdpPort.Text = NetworkInterceptor.Port.ToString();

                        MainWindow.WebPort = ushort.Parse(settingsElement.Element("WebPort").Value);
                        WebPort.Text = MainWindow.WebPort.ToString();
                        
                        NetworkInterceptor.IsLogConnection = bool.Parse(settingsElement.Element("ConnectionLog").Value);
                        ConnectionLog.IsChecked = NetworkInterceptor.IsLogConnection;
                    }
                });
            }
            catch (Exception ex)
            {
                Logger.Error($"Error reading setting file: {ex.Message}");
            }
        }
        else
        {
            Logger.Error($"Setting file not found: {settingFilePath}");
        }
    }

    public void ReadFromSettingFile()
    {
        lock (lockObject)
        {
            if (File.Exists(settingFilePath))
            {
                Logger.Debug("Called ReadSet");
                
                Logger.Log($"Trying reading settings from file {settingFilePath}");

                try
                {
                    var doc = XDocument.Load(settingFilePath);
                    var settingsElement = doc.Element("Settings");

                    if (settingsElement != null)
                    {
                        NetworkInterceptor.IpWhitelistMode = bool.Parse(settingsElement.Element("IpWhitelistMode").Value);
                        WhiteListModeCheck.IsChecked = NetworkInterceptor.IpWhitelistMode;

                        NetworkInterceptor.WriteIntoLog = bool.Parse(settingsElement.Element("WriteIntoLog").Value);
                        IpLogCheck.IsChecked = NetworkInterceptor.WriteIntoLog;

                        Logger.SetLogLevel(settingsElement.Element("LogLevel").Value);
                        DebugLog.IsChecked = Logger.LogLevel == "DEBUG";

                        NetworkInterceptor.Port = ushort.Parse(settingsElement.Element("RdpPort").Value);
                        RdpPort.Text = NetworkInterceptor.Port.ToString();

                        MainWindow.WebPort = ushort.Parse(settingsElement.Element("WebPort").Value);
                        WebPort.Text = MainWindow.WebPort.ToString();
                        
                        NetworkInterceptor.IsLogConnection = bool.Parse(settingsElement.Element("ConnectionLog").Value);
                        ConnectionLog.IsChecked = NetworkInterceptor.IsLogConnection;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"Error reading setting file: {ex.Message}");
                }
            }
            else
            {
                Logger.Error($"Setting file not found: {settingFilePath}");
            }
        }
    }

    protected override void OnClosing(CancelEventArgs ev)
    {
        ev.Cancel = true;
        this.Hide();
    }

    private void ConnectionLogEvent(object sender, RoutedEventArgs e)
    {
        NetworkInterceptor.IsLogConnection = ConnectionLog.IsChecked.Value;
        
        WriteIntoSettingFile();
    }
}