using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Internet_Monitor
{
    public partial class Main : Form
    {
        ContextMenu contextMenu;
        MenuItem TestSpeed_Item;

        public DateTime begin, end;

        public Main()
        {
            InitializeComponent();

            HideWindow();

            contextMenu = new ContextMenu();
            TestSpeed_Item = new MenuItem("Test Speed", new System.EventHandler(TestSpeedItem_Click));
            TestSpeed_Item.Text = "Test Speed";

            contextMenu.MenuItems.Add(0, TestSpeed_Item);
            TrayIcon.ContextMenu = contextMenu;

        }

        struct connection
        {
            public bool speedtest_timeout;
            public double result;
            public connection(bool s, double r)
            {
                speedtest_timeout = s;
                result = r;
            }
            
        }

        private async void TestSpeedItem_Click(Object sender, System.EventArgs e)
        {
            TrayIcon.ShowBalloonTip(500, "Calculating Speed", "...", ToolTipIcon.Info);
            connection connection_status = await return_speed();
            string BalloonTipText = connection_status.result.ToString() + " kb/s\n" +
                                                                        (connection_status.result * 0.008).ToString() + " mb/s (megabits)\n" +
                                                                        ((connection_status.result * 0.008) * 0.125).ToString() + " mb/s (megabytes)";
            if (connection_status.speedtest_timeout)
            {
                Console.WriteLine("Timeout");
                TrayIcon.ShowBalloonTip(1000, "Connection Timeout (Slow Internet)", BalloonTipText, ToolTipIcon.Info);
            }else
            {
                Console.WriteLine("Success");
                TrayIcon.ShowBalloonTip(1000, "Finished!", BalloonTipText, ToolTipIcon.Info);
            }
            
        }

        private async Task<connection> return_speed()
        {
            WebClient wc = new WebClient();
            
            bool success = true;

            double final_value = 0;
            double final_time_elapsed = 0;


            byte[] data = null;
            begin = DateTime.Now;
            wc.DownloadDataCompleted += new DownloadDataCompletedEventHandler((s, e_param) =>
            {
                if(e_param.Cancelled)
                {
                    success = false;
                    return;
                }

                data = e_param.Result;
                end = DateTime.Now;
                return;
            });
            wc.DownloadProgressChanged += new DownloadProgressChangedEventHandler((s, e_param) =>
            {
                double time_elapsed = (DateTime.Now - begin).TotalSeconds;
                double current_speed = e_param.BytesReceived / 1024 / time_elapsed;
                TrayIcon.ShowBalloonTip(5, "Calculating Speed", current_speed.ToString() + "kb/s", ToolTipIcon.Info);
                //TrayIcon.Text = Math.Round(current_speed, 2).ToString() + "kb/s";
                
                if (e_param.ProgressPercentage <= 40 && ((DateTime.Now - begin).TotalSeconds) >= 13)
                {
                    final_value = Math.Round(current_speed, 2);
                    wc.CancelAsync();
                }

            });

            Console.WriteLine("Starting Download");
            await Task.Run(() => wc.DownloadDataAsync(new Uri("http://212.183.159.230/10MB.zip")));
            while (data == null)
            {
                if(!success)
                {
                    return new connection(true, final_value);
                }
                Thread.Sleep(100);
            }


            final_time_elapsed = ((end - begin).TotalSeconds);
            
            final_value = Math.Round(data.Length / 1024 / final_time_elapsed, 2);
            
            return new connection(false, final_value);
        }

        public void HideWindow()
        {
            this.WindowState = FormWindowState.Minimized;
            Hide();
            ShowInTaskbar = false;
        }



    }
}
