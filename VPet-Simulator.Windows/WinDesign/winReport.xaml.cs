using LinePutScript.Localization.WPF;
using Panuon.WPF.UI;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Web;
using System.Windows;
using VPet_Simulator.Core;

namespace VPet_Simulator.Windows
{
    /// <summary>
    /// </summary>
    public partial class winReport : WindowX
    {
        MainWindow mw;
        string save;
        public bool IsUniformSizeChanged => false;
        public bool StoreSize => false;
        public winReport(MainWindow mainw, string errmsg = null)
        {
            mainw.Windows.Add(this);
            InitializeComponent();
            mw = mainw;
            Title = "Feedback".Translate() + ' ' + mw.PrefixSave;
            save = mw?.Core?.Save?.ToLine().ToString() + mw.Set?.ToString();
            if (errmsg != null)
            {
                tType.SelectedIndex = 0;
                tContent.Text = errmsg;
                tContent.IsReadOnly = true;
            }
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void tUpload_Click(object sender, RoutedEventArgs e)
        {
            if (tUpload.IsChecked == true)
                save = mw.Core.Save.ToLine().ToString() + mw.Set.ToString();
            else
                save = "Player cancels uploading saved".Translate();
        }

        private void btn_upload(object sender, RoutedEventArgs e)
        {
            if (tDescription.Text == "" && tType.SelectedIndex != 0)
            {
                MessageBoxX.Show("Detailed description of the problem is feedback on the specific problem\\nLike how to get this errors, What is unreasonable about the game.".Translate(), "Please fill in a description of the problem".Translate());
                return;
            }
            MainGrid.IsEnabled = false;
            pgload.Value = 0;
            gridLoading.Visibility = Visibility.Visible;

#if DEBUG
            string _url = "http://localhost:5079/VPET/Report";
#else
                string _url = "https://report.exlb.net/VPET/Report";
#endif
            StringBuilder sb = new StringBuilder();
            sb.Append("action=error");
            sb.Append("&type=" + HttpUtility.UrlEncode(tType.Text));
            sb.Append("&description=" + HttpUtility.UrlEncode(tDescription.Text));
            sb.Append("&content=" + HttpUtility.UrlEncode(tContent.Text));
            sb.Append("&contact=" + HttpUtility.UrlEncode(tContact.Text));
            sb.Append($"&ver={mw.version}&repver=3&lang={LocalizeCore.CurrentCulture}");
            sb.Append("&save=");
            sb.Append(HttpUtility.UrlEncode(save));

            Task.Run(async () =>
            {
                try
                {
                    ShowTimeLoading();
                    using System.Net.Http.HttpClient client = new System.Net.Http.HttpClient();
                    client.Timeout = TimeSpan.FromSeconds(120);
                    byte[] byteData = Encoding.UTF8.GetBytes(sb.ToString());
                    using System.Net.Http.ByteArrayContent content = new System.Net.Http.ByteArrayContent(byteData);
                    content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/x-www-form-urlencoded");
                    System.Net.Http.HttpResponseMessage httpResponse = await client.PostAsync(_url, content);
                    string responseString = await httpResponse.Content.ReadAsStringAsync();

                    Dispatcher.Invoke(() =>
                    {
                        gridLoading.Visibility = Visibility.Collapsed;
                        if (responseString == "Report Error Success")
                        {
                            MessageBoxX.Show("Feedback success submitted. Thank you for your feedback and submit\\nVOS will process your feedback as soon as possible and do better".Translate(), "Thank you for your feedback and submit".Translate());
                            Close();
                        }
                        else if (responseString == "IP times Max")
                        {
                            mw.Set.DiagnosisDayEnable = false;
                            MessageBoxX.Show("You've reached your maximum number of feedback today, please come back tomorrow for feedback.\\nOr go the https://github.com/LorisYounger/VPet for feedback problem".Translate(), "You've reached your maximum number of feedback today".Translate(), MessageBoxButton.OK, MessageBoxIcon.Error);
                        }
                        else if (responseString.StartsWith("ReportMessage:"))
                        {
                            MessageBoxX.Show(responseString.Substring(14), "Thank you for your feedback and submit".Translate());
                            Close();
                        }
                        else
                        {
                            MessageBoxX.Show("Feedback submit failed\\nWelcome to the https://github.com/LorisYounger/VPet for feedback\\nServer Message".Translate() + responseString, "Feedback submit failed with error".Translate(), MessageBoxButton.OK, MessageBoxIcon.Error);
                        }
                        MainGrid.IsEnabled = true;
                    });
                }
                catch (Exception exp)
                {
                    Dispatcher.Invoke(() => MessageBoxX.Show("Feedback submit failed, may be a network or other issue preventing the upload\\nWelcome to the https://github.com/LorisYounger/VPet for feedback\\n".Translate() + exp.ToString(), "Feedback submit failed with error".Translate(), MessageBoxButton.OK, MessageBoxIcon.Error));
                }
                Dispatcher.Invoke(() => MainGrid.IsEnabled = true);
            });

        }
        public void ShowTimeLoading()
        {
            Dispatcher.Invoke(() =>
             {
                 if (gridLoading.Visibility == Visibility.Visible)
                 {
                     if (pgload.Value >= 117)
                     {
                         pgload.Value = 120;
                     }
                     else
                     {
                         pgload.Value += Function.Rnd.Next(5, 15) * 0.1;
                         Task.Run(() =>
                         {
                             Thread.Sleep(Function.Rnd.Next(500, 1500));
                             ShowTimeLoading();
                         });
                     }
                 }
             });

        }
        private void MainGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Height = MainGrid.ActualHeight + 50;
        }

        private void tType_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (tType.SelectedIndex == 5)
            {
                try
                {
                    StringBuilder sb = new StringBuilder();
                    foreach (var v in LocalizeCore.StoreTranslationList)
                    {
                        sb.AppendLine(v.Replace("\n", @"\n").Replace("\r", @"\r"));
                    }
                    tContent.Text = sb.ToString();
                    if (string.IsNullOrEmpty(tContent.Text))
                    {
                        tContent.Text = "There are no translations that need to be submitted".Translate();
                    }
                    tUpload.IsChecked = false;
                }
                catch
                {

                }
            }
        }

        private void WindowX_Closed(object sender, EventArgs e)
        {
            mw.Windows.Remove(this);
        }
    }
}
