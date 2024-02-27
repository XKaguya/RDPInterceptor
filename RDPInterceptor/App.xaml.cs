using System;
using System.Net;
using System.Windows;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using RDPInterceptor.API;

namespace RDPInterceptor
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        public static MainWindow? CurrentMainWindow { get; private set; }
        
        protected override void OnStartup(StartupEventArgs ev)
        {
            base.OnStartup(ev);

            if (ev.Args.Length > 0)
            {
                string command = ev.Args[0];

                if (command == "--WebOnly")
                {
                    var host = new WebHostBuilder()
                        .UseKestrel(options =>
                        {
                            options.Listen(IPAddress.Any, RDPInterceptor.MainWindow.WebPort);
                        })
                        .ConfigureServices(services =>
                        {
                            var webService = new Startup();
                            webService.ConfigureServices(services);
                        })
                        .Configure(app =>
                        {
                            var env = app.ApplicationServices.GetRequiredService<IWebHostEnvironment>();
                            var webService = new Startup();
                            webService.Configure(app, env);
                        })
                        .Build();

                    host.RunAsync();
                }
                else
                {
                    MessageBox.Show("Invalid command.\n Use --WebOnly for start the WebService only.");
                    Environment.Exit(0);
                }
            }
            else
            {
                CurrentMainWindow = new MainWindow();
                CurrentMainWindow.Show();
            }
        }
    }
}