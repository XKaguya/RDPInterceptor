using System;
using System.Windows;
using RDPInterceptor.API;

namespace RDPInterceptor
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        public static MainWindow CurrentMainWindow { get; private set; }
        
        public static bool WebMode { get; private set; }

        private void Initialization(Argument argument)
        {
            if (argument.LogLevel == "Debug" || argument.LogLevel == "DEBUG")
            {
                Logger.SetLogLevel(argument.LogLevel);
            }

            if (argument.Port <= 65535)
            {
                NetworkInterceptor.Port = argument.Port.Value;
            }

            if (argument.WhiteList != null && argument.WhiteList.Value)
            {
                NetworkInterceptor.IpWhitelistMode = argument.WhiteList.Value;
            }

            if (argument.LogConnection != null && argument.LogConnection.Value)
            {
                NetworkInterceptor.IsLogConnection = argument.LogConnection.Value;
            }
            
            if (argument.UiPort <= 65535)
            {
                NetworkInterceptor.Port = argument.UiPort.Value;
            }
            
            if (argument.WebOnly != null && argument.WebOnly.Value)
            {
                WebMode = true;
                RDPInterceptor.MainWindow.Init(true);
                
                MessageBox.Show($"Web service started at http://localhost:{RDPInterceptor.MainWindow.WebPort}.");
            }
        }

        protected override void OnStartup(StartupEventArgs ev)
        {
            base.OnStartup(ev);

            if (ev.Args.Length > 0)
            {
                Command? currentCommand = null;
                string? currentValue = null;

                Argument argument = new();
                
                foreach (string arg in ev.Args)
                {
                    if (arg.StartsWith("--"))
                    {
                        if (Enum.TryParse(arg.Substring(2), out Command command))
                        {
                            currentCommand = command;
                        }
                        else
                        {
                            MessageBox.Show($"Invalid command: {arg}.");
                            Environment.Exit(0);
                        }
                    }
                    else
                    {
                        if (currentCommand != null)
                        {
                            currentValue = arg;

                            try
                            {
                                switch (currentCommand)
                                {
                                    case Command.WebOnly:
                                        argument.WebOnly = bool.Parse(currentValue);
                                        break;
                                
                                    case Command.LogLevel:
                                        argument.LogLevel = currentValue;
                                        break;
                                
                                    case Command.Port:
                                        argument.Port = ushort.Parse(currentValue);
                                        break;
                                
                                    case Command.UiPort:
                                        argument.UiPort = ushort.Parse(currentValue);
                                        break;
                                
                                    case Command.Whitelist:
                                        argument.WhiteList = bool.Parse(currentValue);
                                        break;
                                    
                                    case Command.LogConnection:
                                        argument.LogConnection = bool.Parse(currentValue);
                                        break;
                                
                                    default:
                                        MessageBox.Show($"Invalid arg: {currentValue}");
                                        break;
                                }
                            }
                            catch (Exception e)
                            {
                                MessageBox.Show(e.Message + e.StackTrace);
                                throw;
                            }
                            
                            currentCommand = null;
                        }
                        else
                        {
                            MessageBox.Show($"Invalid value without command: {arg}.");
                            Environment.Exit(0);
                        }
                    }
                }
                Initialization(argument);
            }
            else
            {
                WebMode = false;
                
                CurrentMainWindow = new MainWindow();
                CurrentMainWindow.Show();
                
                RDPInterceptor.MainWindow.Init(true);
            }
        }

        private enum Command
        {
            WebOnly,
            LogLevel,
            Port,
            LogConnection,
            Whitelist,
            UiPort,
        }

        public class Argument
        {
            public bool? WebOnly { get; set; }
        
            public string? LogLevel { get; set; }
        
            public ushort? Port { get; set; }
        
            public bool? WhiteList { get; set; }
        
            public bool? LogConnection { get; set; }

            public ushort? UiPort { get; set; }
            
            public override string ToString()
            {
                return $"WebOnly: {WebOnly}, LogLevel: {LogLevel}, Port: {Port}, WhiteList: {WhiteList}, LogConnection: {LogConnection}, UiPort: {UiPort}";
            }
        }
    }
}