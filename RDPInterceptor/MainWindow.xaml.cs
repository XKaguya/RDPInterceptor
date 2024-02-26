using System;
using System.ComponentModel;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using RDPInterceptor.API;

namespace RDPInterceptor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        static LogWindow logWindow = LogWindow.Instance;
        
        private CancellationTokenSource cancellationTokenSource;
        
        public MainWindow()
        {
            logWindow.Hide();
            InitializeComponent();
            ComboBox.ItemsSource = NetworkInterceptor.IpAddrList;

            cancellationTokenSource = new CancellationTokenSource();
            
            Task.Run(async () =>
            {
                try
                {
                    var host = new WebHostBuilder()
                        .UseKestrel()
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
                    
                    await host.RunAsync(cancellationTokenSource.Token);
                }
                catch (OperationCanceledException)
                {
                    Logger.Log($"Task WebService stopped.");
                }
            });
        }
        
        protected override void OnClosing(CancelEventArgs ev)
        {
            base.OnClosing(ev);
            cancellationTokenSource?.Cancel();
            
            Application.Current.Shutdown(0);
        }
        
        private async void StartCaptureClick(object sender, RoutedEventArgs e)
        {
            await NetworkInterceptor.StartCapture();
        }

        private async void StopCaptureClick(object sender, RoutedEventArgs e)
        {
            await NetworkInterceptor.StopCapture();
        }
        
        private void ConfirmClick(object sender, RoutedEventArgs e)
        {
            string input = InputTextBox.Text;
            if (!NetworkInterceptor.AddIpIntoList(input))
            {
                MessageBox.Show($"Invalid IP/Domain.");
            }
        }

        private void LogButtonClick(object sender, RoutedEventArgs e)
        {
            if (logWindow.IsVisible)
            {
                logWindow.Hide();
            }
            else
            {
                logWindow.Show();
            }
        }
        
        private void DeleteClick(object sender, RoutedEventArgs e)
        {
            if (ComboBox.SelectedItem != null)
            {
                NetworkInterceptor.IpAddrList.Remove(IPAddress.Parse(ComboBox.SelectedItem.ToString()));
            }
            else
            {
                Logger.Error($"Selected item is null.");
            }
        }
    }
}