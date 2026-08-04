using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Timer = System.Timers.Timer;
using static VPet_Simulator.Core.Main;
using System.Security.Cryptography.Xml;
using static System.Net.Mime.MediaTypeNames;

namespace VPet_Simulator.Core
{
    public interface IMassageBar : IDisposable
    {
        /// <summary>
        /// </summary>
        /// <param name="Text Set">Content</param>
        /// <param name="msgContent">Message BoxContent</param>
        void Show(string name, string text, string graphName = null, UIElement msgContent = null);


        /// <summary>
        /// </summary>
        /// <param name="sayInfoWithStream">Content</param>
        void Show(string name, SayInfoWithStream sayInfoWithStream);
        /// <summary>
        /// </summary>
        void ForceClose();
        /// <summary>
        /// </summary>
        void SetPlaceIN();
        /// <summary>
        /// </summary>
        void SetPlaceOUT();
        /// <summary>
        /// Display Status
        /// </summary>
        Visibility Visibility { get; set; }
        /// <summary>
        /// </summary>
        Control This { get; }
        /// <summary>
        /// </summary>
        event Action EndAction;
    }
    /// <summary>
    /// </summary>
    public partial class MessageBar : UserControl, IDisposable, IMassageBar
    {
        public Control This => this;
        Main m;
        public MessageBar(Main m)
        {
            InitializeComponent();
            EndTimer.Elapsed += EndTimer_Elapsed;
            ShowTimer.Elapsed += ShowTimer_Elapsed;
            CloseTimer.Elapsed += CloseTimer_Elapsed;
            this.m = m;
        }

        private void CloseTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (Dispatcher.Invoke(() => Opacity) <= 0.05)
            {
                CloseTimer.Stop();
                Dispatcher.Invoke(() =>
                {
                    Opacity = 1;
                    this.Visibility = Visibility.Collapsed;
                    MessageBoxContent.Children.Clear();
                });
                EndAction?.Invoke();
            }
            else
            {
                Dispatcher.Invoke(() => Opacity -= 0.02);
            }
        }

        List<char> outputtext;
        StringBuilder outputtextsample = new StringBuilder();
        private void ShowTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (outputtext.Count > 0)
            {
                int batchSize = Math.Min(2, outputtext.Count);
                string textToAdd = string.Empty;

                for (int i = 0; i < batchSize; i++)
                {
                    textToAdd += outputtext[0];
                    outputtext.RemoveAt(0);
                }
                outputtextsample.Append(textToAdd);
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    TText.Text = outputtextsample.ToString();
                }), System.Windows.Threading.DispatcherPriority.Background);
            }
            else
            {
                if (m.PlayingVoice)
                {
                    if (m.windowMediaPlayerAvailable)
                    {
                        TimeSpan ts = Dispatcher.Invoke(() => m.VoicePlayer?.Clock?.NaturalDuration.HasTimeSpan == true ? (m.VoicePlayer.Clock.NaturalDuration.TimeSpan - m.VoicePlayer.Clock.CurrentTime.Value) : TimeSpan.Zero);
                        if (ts.TotalSeconds > 2)
                        {
                            return;
                        }
                        else
                        {
                            Console.WriteLine(1);
                        }
                    }
                    else
                    {
                        Dispatcher.Invoke(() =>
                        {
                            if (m.soundPlayer.IsLoadCompleted)
                            {
                                m.PlayingVoice = false;
                                m.soundPlayer.PlaySync();
                            }
                        });
                    }
                }
                ShowTimer.Stop();
                EndTimer.Start();
                if ((m.DisplayType.Name == graphName || m.DisplayType.Type == GraphInfo.GraphType.Say) && m.DisplayType.Animat != GraphInfo.AnimatType.C_End)
                    m.DisplayCEndtoNomal(m.DisplayType.Name);
            }
        }
        /// <summary>
        /// </summary>
        public event Action EndAction;
        private void EndTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (--timeleft <= 0)
            {
                EndTimer.Stop();
                CloseTimer.Start();
            }
        }

        public Timer EndTimer = new Timer() { Interval = 200 };
        public Timer ShowTimer = new Timer() { Interval = 150 };
        public Timer CloseTimer = new Timer() { Interval = 50 };
        int timeleft;
        string graphName;
        /// <summary>
        /// </summary>
        /// <param name="Text Set">Content</param>
        public void Show(string name, string text, string graphName = null, UIElement msgContent = null)
        {
            if (m.UIGrid.Children.IndexOf(this) != m.UIGrid.Children.Count - 1)
            {
                Panel.SetZIndex(this, m.UIGrid.Children.Count - 1);
            }
            MessageBoxContent.Children.Clear();
            TText.Text = "";
            outputtext = text.ToList();
            outputtextsample.Clear();
            LName.Content = name;
            timeleft = Function.ComCheck(text) * 10 + 20;
            ShowTimer.Start(); EndTimer.Stop(); CloseTimer.Stop();
            this.Visibility = Visibility.Visible;
            Opacity = .8;
            this.graphName = graphName;
            if (msgContent != null)
            {
                MessageBoxContent.Children.Add(msgContent);
            }
        }
        private SayInfoWithStream oldsaystream;
        /// <summary>
        /// </summary>
        public void Show(string name, SayInfoWithStream sayInfoWithStream)
        {
            if (m.UIGrid.Children.IndexOf(this) != m.UIGrid.Children.Count - 1)
            {
                Panel.SetZIndex(this, m.UIGrid.Children.Count - 1);
            }

            if (oldsaystream != null)
            {
                oldsaystream.Event_Update -= DealWithUpdate;
                oldsaystream.Event_Finish -= DealWithStreamFinish;
            }
            oldsaystream = sayInfoWithStream;

            MessageBoxContent.Children.Clear();
            TText.Text = "";
            LName.Content = name;
            ShowTimer.Stop();
            EndTimer.Stop();
            CloseTimer.Stop();
            this.Visibility = Visibility.Visible;
            Opacity = .8;
            graphName = sayInfoWithStream.GraphName;

            var msgcontent = sayInfoWithStream.MsgContent ?? (string.IsNullOrWhiteSpace(sayInfoWithStream.Desc)
                ? null
                : new TextBlock()
                {
                    Text = sayInfoWithStream.Desc,
                    FontSize = 20,
                    ToolTip = sayInfoWithStream.Desc,
                    HorizontalAlignment = HorizontalAlignment.Right
                });
            if (msgcontent != null)
            {
                MessageBoxContent.Children.Add(msgcontent);
            }



            Dispatcher.Invoke(() => { TText.Text = sayInfoWithStream.CurrentText.ToString(); });

            sayInfoWithStream.Event_Update += DealWithUpdate;
            if (sayInfoWithStream.IsFinishGen)
                DealWithStreamFinish(sayInfoWithStream.CurrentText.ToString());
            else
                sayInfoWithStream.Event_Finish += DealWithStreamFinish;
        }
        /// <summary>
        /// </summary>
        DateTime nextshow = DateTime.Now;
        /// <summary>
        /// </summary>
        public void DealWithUpdate((string fullText, string changedText) data)
        {
            timeleft = data.fullText.Length;
            Task.Run(() =>
            {
                int sleeptime = 0;
                lock (oldsaystream)
                    if (DateTime.Now < nextshow)
                    {
                        sleeptime = (int)(nextshow - DateTime.Now).TotalMilliseconds;
                        nextshow = nextshow.AddMilliseconds(150);
                    }
                    else
                        nextshow = DateTime.Now.AddMilliseconds(150);
                if (sleeptime > 0)
                    Thread.Sleep(sleeptime);
                Dispatcher.Invoke(() => { TText.Text = data.fullText; });
            });

        }
        /// <summary>
        /// </summary>
        public void DealWithStreamFinish(string fullText)
        {
            Task.Run(() =>
            {
                if (m.PlayingVoice)
                {
                    if (m.windowMediaPlayerAvailable)
                    {
                        TimeSpan ts = Dispatcher.Invoke(() => m.VoicePlayer?.Clock?.NaturalDuration.HasTimeSpan == true ? (m.VoicePlayer.Clock.NaturalDuration.TimeSpan - m.VoicePlayer.Clock.CurrentTime.Value) : TimeSpan.Zero);
                        while (ts.TotalSeconds > 2)
                        {
                            ts = Dispatcher.Invoke(() => m.VoicePlayer?.Clock?.NaturalDuration.HasTimeSpan == true ? (m.VoicePlayer.Clock.NaturalDuration.TimeSpan - m.VoicePlayer.Clock.CurrentTime.Value) : TimeSpan.Zero);
                            Thread.Sleep(100);
                        }
                    }
                    else
                    {
                        Dispatcher.Invoke(() =>
                        {
                            if (m.soundPlayer.IsLoadCompleted)
                            {
                                m.PlayingVoice = false;
                                m.soundPlayer.PlaySync();
                            }
                        });
                    }
                }
                if (oldsaystream?.CurrentText.ToString() == fullText)
                {
                    oldsaystream.Event_Update -= DealWithUpdate;
                    oldsaystream.Event_Finish -= DealWithStreamFinish;
                    oldsaystream = null;
                }

                timeleft = Function.ComCheck(fullText) * 5 + 10;
                EndTimer.Start();
                if ((m.DisplayType.Name == graphName || m.DisplayType.Type == GraphInfo.GraphType.Say) && m.DisplayType.Animat != GraphInfo.AnimatType.C_End)
                    m.DisplayCEndtoNomal(m.DisplayType.Name);

            });
        }

        public void Border_MouseEnter(object sender, MouseEventArgs e)
        {
            EndTimer.Stop();
            CloseTimer.Stop();
            this.Opacity = .8;
        }

        public void Border_MouseLeave(object sender, MouseEventArgs e)
        {
            if (!ShowTimer.Enabled)
                EndTimer.Start();
        }

        private void UserControl_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ForceClose();
        }
        /// <summary>
        /// </summary>
        public void ForceClose()
        {
            EndTimer.Stop(); ShowTimer.Stop(); CloseTimer.Close();
            this.Visibility = Visibility.Collapsed;
            MessageBoxContent.Children.Clear();
            if ((m.DisplayType.Name == graphName || m.DisplayType.Type == GraphInfo.GraphType.Say) && m.DisplayType.Animat != GraphInfo.AnimatType.C_End)
                m.DisplayCEndtoNomal(m.DisplayType.Name);
            EndAction?.Invoke();
        }
        public void Dispose()
        {
            EndTimer.Dispose();
            ShowTimer.Dispose();
            CloseTimer.Dispose();
        }
        public void SetPlaceIN()
        {
            this.Height = 500;
            BorderMain.VerticalAlignment = VerticalAlignment.Bottom;
            Margin = new Thickness(0);
        }
        public void SetPlaceOUT()
        {
            this.Height = double.NaN;
            BorderMain.VerticalAlignment = VerticalAlignment.Top;
            Margin = new Thickness(0, 500, 0, 0);
        }

        private void MenuItemCopy_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(TText.Text);
        }

        private void MenuItemClose_Click(object sender, RoutedEventArgs e)
        {
            ForceClose();
        }

        private void ContextMenu_Opened(object sender, RoutedEventArgs e) => Border_MouseEnter(null, null);

        private void ContextMenu_Closed(object sender, RoutedEventArgs e) => Border_MouseLeave(null, null);

        private void TText_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            sv.ScrollToEnd();
        }
    }
}
