using System;
using System.Windows;

namespace RDPInterceptor
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        public static MainWindow? CurrentMainWindow { get; private set; }
        
        public static bool WebMode { get; private set; }

        protected override void OnStartup(StartupEventArgs ev)
        {
            base.OnStartup(ev);

            if (ev.Args.Length > 0)
            {
                string command = ev.Args[0];

                if (command == "--WebOnly")
                {
                    WebMode = true;
                    
                    RDPInterceptor.MainWindow.Init(true);

                    MessageBox.Show($"Web service started at http://localhost:{RDPInterceptor.MainWindow.WebPort}.");
                }
                else
                {
                    MessageBox.Show("Invalid command.\n Use --WebOnly for start the WebService only.");
                    Environment.Exit(0);
                }
            }
            else
            {
                WebMode = false;
                
                CurrentMainWindow = new MainWindow();
                CurrentMainWindow.Show();
                
                RDPInterceptor.MainWindow.Init(true);
            }
        }
    }
}