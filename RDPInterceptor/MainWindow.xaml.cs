﻿using System;
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

        private static CancellationTokenSource? WebCancellactionTokenSource = new();

        public static ushort WebPort { get; set; } = 5000;

        public MainWindow()
        {
            logWindow.Hide();
            InitializeComponent();
            ComboBox.ItemsSource = NetworkInterceptor.IpAddrList;
        }

        public static void Init(bool OnlyWeb)
        {
            Setting.Instance.ReadFromSettingFile();
            NetworkInterceptor.ReadLinesFromFileAsync();

            if (!OnlyWeb)
            {
                App.CurrentMainWindow.ChangeStatus(false);
            }
            
            InitWebServer();
        }

        private static async Task InitWebServer()
        {
            await Task.Run(async () =>
            {
                try
                {
                    var host = new WebHostBuilder()
                        .UseKestrel(options => { options.Listen(IPAddress.Any, WebPort); })
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

                    await host.RunAsync(WebCancellactionTokenSource.Token);
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
            WebCancellactionTokenSource?.Cancel();

            Application.Current.Shutdown(0);
        }

        private async void StartCaptureClick(object sender, RoutedEventArgs e)
        {
            await NetworkInterceptor.StartCapture(NetworkInterceptor.CaptureCancellationTokenSource.Token);
        }

        private async void StopCaptureClick(object sender, RoutedEventArgs e)
        {
            await NetworkInterceptor.StopCapture();
        }

        public void ChangeStatus(bool IsStarted)
        {
            if (IsStarted)
            {
                StartCapture.IsEnabled = false;
                StopCapture.IsEnabled = true;
            }
            else
            {
                StartCapture.IsEnabled = true;
                StopCapture.IsEnabled = false;
            }
        }

        private async void ConfirmClick(object sender, RoutedEventArgs e)
        {
            string input = InputTextBox.Text;
            bool result = await NetworkInterceptor.AddIpIntoList(input);

            if (!result)
            {
                MessageBox.Show($"Invalid IP/Domain. Check log for more detail.");
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
                NetworkInterceptor.RemoveIpFromList(IPAddress.Parse(ComboBox.SelectedItem.ToString()));
            }
            else
            {
                Logger.Error($"Selected item is null.");
            }
        }

        private void SettingClick(object sender, RoutedEventArgs e)
        {
            if (Setting.Instance.IsVisible)
            {
                Setting.Instance.Hide();
            }
            else
            {
                Setting.Instance.Show();
            }
        }
    }
}