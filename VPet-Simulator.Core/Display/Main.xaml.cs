using LinePutScript.Localization.WPF;
using Panuon.WPF.UI;
using System;
using System.Collections.Generic;
using System.Media;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using static VPet_Simulator.Core.GraphInfo;

namespace VPet_Simulator.Core
{
    /// <summary>
    /// </summary>
    public partial class Main : ContentControlX, IDisposable
    {
        /// <summary>
        /// </summary>
        public GameCore Core;
        /// <summary>
        /// </summary>
        public ToolBar ToolBar;
        /// <summary>
        /// </summary>
        public IMassageBar MsgBar;
        /// <summary>
        /// </summary>
        public WorkTimer WorkTimer;
        /// <summary>
        /// </summary>
        public event Action<Main> TimeHandle;
        /// <summary>
        /// </summary>
        public event Action<Main> TimeUIHandle;
        /// <summary>
        /// </summary>
        public IGameSave.ModeType NoFunctionMOD = IGameSave.ModeType.Happy;
        /// <summary>
        /// </summary>
        public bool IsWorking { get; private set; } = false;
        public SoundPlayer soundPlayer = new SoundPlayer();
        public bool windowMediaPlayerAvailable = true;

        public void Load_0_BaseConsole()
        {
            Dispatcher.Invoke(() =>
            {
                WorkTimer = new WorkTimer(this);
                WorkTimer.Visibility = Visibility.Collapsed;
                UIGrid.Children.Add(WorkTimer);
                ToolBar = new ToolBar(this);
                ToolBar.Visibility = Visibility.Collapsed;
                UIGrid.Children.Add(ToolBar);
                MsgBar = new MessageBar(this);
                MsgBar.Visibility = Visibility.Collapsed;
                UIGrid.Children.Add(MsgBar.This);
            });
        }
        public void Load_3_BindingTimer()
        {
            EventTimer.Elapsed += (s, e) => EventTimer_Elapsed();
            MoveTimer.Elapsed += MoveTimer_Elapsed;
            SmartMoveTimer.Elapsed += SmartMoveTimer_Elapsed;
        }
        public List<string> ErrorMessage = new List<string>();
        /// <summary>
        /// </summary>
        public async Task Load_2_WaitGraph()
        {
            foreach (var igs in Core.Graph.GraphsList.Values)
            {
                foreach (var ig2 in igs.Values)
                {
                    for (int i = 0; i < ig2.Count; i++)
                    {
                        IGraph ig3 = ig2[i];
                        var timeoutCts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
                        while (!ig3.IsReady)
                        {
                            if (ig3.IsFail)
                            {
                                ErrorMessage.Add(ig3.FailMessage);
                                ig2.Remove(ig3);
                                break;
                            }
                            if (timeoutCts.IsCancellationRequested)
                            {
                                ErrorMessage.Add($"Timeout waiting for graph: {ig3}");
                                ig2.Remove(ig3);
                                break;
                            }
                            await Task.Delay(100);
                        }
                    }
                }
            }
        }
        /// <summary>
        /// </summary>
        public async Task Load_2_WaitGraph(Action<int> WaitCountAction)
        {
            if (WaitCountAction == null)
            {
                await Load_2_WaitGraph();
                return;
            }
            int count = 0;
            DateTime start = DateTime.Now.AddSeconds(1);
            var tasks = new List<Task>();

            // Periodic progress reporter: fires every 500ms even if no graph
            // just became ready, so the UI doesn't appear frozen at 0%.
            var progressTimer = new System.Timers.Timer(500);
            progressTimer.AutoReset = true;
            progressTimer.Elapsed += (s, e) =>
            {
                if (start < DateTime.Now)
                {
                    start = DateTime.Now.AddSeconds(1);
                    WaitCountAction?.Invoke(count);
                }
            };
            progressTimer.Start();

            foreach (var igs in Core.Graph.GraphsList.Values)
            {
                foreach (var ig2 in igs.Values)
                {
                    for (int i = 0; i < ig2.Count; i++)
                    {
                        IGraph ig3 = ig2[i];
                        tasks.Add(Task.Run(async () =>
                        {
                            var timeoutCts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
                            while (!ig3.IsReady)
                            {
                                if (ig3.IsFail)
                                {
                                    lock (ErrorMessage)
                                    {
                                        ErrorMessage.Add(ig3.FailMessage);
                                        ig2.Remove(ig3);
                                    }
                                    break;
                                }
                                if (timeoutCts.IsCancellationRequested)
                                {
                                    lock (ErrorMessage)
                                    {
                                        ErrorMessage.Add($"Timeout waiting for graph: {ig3}");
                                        ig2.Remove(ig3);
                                    }
                                    break;
                                }
                                else
                                {
                                    await Task.Delay(100);
                                }
                            }
                            Interlocked.Increment(ref count);
                            if (start < DateTime.Now)
                            {
                                start = DateTime.Now.AddSeconds(1);
                                WaitCountAction.Invoke(count);
                            }
                        }));
                    }

                }
            }

            await Task.WhenAll(tasks);
            progressTimer.Stop();
            progressTimer.Dispose();
            // Final progress report
            WaitCountAction?.Invoke(count);
        }
        /// <summary>
        /// </summary>
        public void Load_4_Start(IGraph startUPGraph = null)
        {
            IGraph ig = startUPGraph ?? Core.Graph.FindGraph(Core.Graph.FindName(GraphType.StartUP), AnimatType.Single, Core.Save.Mode);
            ig ??= Core.Graph.FindGraph(Core.Graph.FindName(GraphType.Default), AnimatType.Single, Core.Save.Mode);
            Task.Run(() =>
            {
                ig.Run(PetGrid, () =>
                {
                    DisplayNomal();
                });
            });

            IsWorking = true;
        }


        /// <summary>
        /// </summary>
        public void Load_24_WaitAndStart()
        {
            Load_2_WaitGraph().Wait();
            Load_4_Start();
        }
        /// <summary>
        /// </summary>
        public void Load_24_WaitAndStart(Action<int> WaitCountAction, IGraph startUPGraph = null)
        {
            Load_2_WaitGraph(WaitCountAction).Wait();
            Load_4_Start(startUPGraph);
        }

        public Main(GameCore core)
        {
            InitializeComponent();
            Core = core;

            labeldisplaytimer.Elapsed += Labledisplaytimer_Elapsed;
            Event_MoveEnd += (_) => MoveSideHideCheck();

            DisplayNomal = DisplayDefault;
            DisplayMove = DisplayToMove;
            DisplayIdel = DisplayToIdel;
            DisplayIdel_StateONE = DisplayToIdel_StateONE;
            DisplayTouchBody = DisplayToTouchBody;
            DisplayTouchHead = DisplayToTouchHead;

            SayRndFunction = new Func<string, string>((x) => Core.Graph.FindName(GraphType.Say));

            if (!core.Controller.EnableFunction)
                Core.Save.Mode = NoFunctionMOD;
        }



        /// <summary>
        /// </summary>
        public void LoadALL(Action<int> WaitCountAction = null, IGraph startUPGraph = null)
        {
            Load_0_BaseConsole();
            Load_2_TouchEvent();
            Load_2_WaitGraph(WaitCountAction).Wait();
            Load_3_BindingTimer();
            Load_4_Start(startUPGraph);
        }

        private void Labledisplaytimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (--labeldisplaycount <= 0)
            {
                labeldisplaytimer.Enabled = false;
                labeldisplaychangenum1 = 0;
                labeldisplaychangenum2 = 0;
                Dispatcher.Invoke(() =>
                {
                    LabelDisplay.Visibility = Visibility.Collapsed;
                });
            }
            else if (labeldisplaycount < 50)
            {
                Dispatcher.Invoke(() =>
                {
                    LabelDisplay.Opacity = labeldisplaycount / 50;
                });
            }
        }

        /// <summary>
        /// </summary>
        public void Load_2_TouchEvent()
        {
            Core.TouchEvent.Add(new TouchArea(new Point(0, 0), new Size(500, 500), () =>
            {
                if (DisplayType.Type == GraphType.SideHide_Left_Main || DisplayType.Type == GraphType.SideHide_Left_Rise)
                {
                    Core.Controller.MoveWindows(-Core.Controller.GetWindowsDistanceLeft() / Core.Controller.ZoomRatio, 0);
                    DisplayCEndtoNomal(Core.Graph.FindName(GraphType.SideHide_Left_Main));
                    return true;
                }
                if (DisplayType.Type == GraphType.SideHide_Right_Main || DisplayType.Type == GraphType.SideHide_Right_Rise)
                {
                    Core.Controller.MoveWindows(Core.Controller.GetWindowsDistanceRight() / Core.Controller.ZoomRatio, 0);
                    DisplayCEndtoNomal(Core.Graph.FindName(GraphType.SideHide_Right_Main));
                    return true;
                }
                return false;
            }));
            Core.TouchEvent.Add(new TouchArea(Core.Graph.GraphConfig.TouchHeadLocate, Core.Graph.GraphConfig.TouchHeadSize, () => { DisplayTouchHead(); return true; }));
            Core.TouchEvent.Add(new TouchArea(Core.Graph.GraphConfig.TouchBodyLocate, Core.Graph.GraphConfig.TouchBodySize, () => { DisplayTouchBody(); return true; }));
            for (int i = 0; i < 4; i++)
            {
                IGameSave.ModeType m = (IGameSave.ModeType)i;
                Core.TouchEvent.Add(new TouchArea(Core.Graph.GraphConfig.TouchRaisedLocate[i], Core.Graph.GraphConfig.TouchRaisedSize[i],
                    () =>
                    {
                        if (Core.Save.Mode == m)
                        {
                            DisplayRaised();
                            return true;
                        }
                        else
                            return false;

                    }, true));
            }
        }
        /// <summary>
        /// </summary>
        public void PlayVoice(Uri VoicePath)//, TimeSpan timediff = TimeSpan.Zero) TODO
        {
            PlayingVoice = true;
            if (windowMediaPlayerAvailable)
            {
                Dispatcher.Invoke(() =>
                {
                    VoicePlayer.Clock = new MediaTimeline(VoicePath).CreateClock();
                    VoicePlayer.Clock.Completed += Clock_Completed;
                    VoicePlayer.MediaFailed += MediaPlayer_MediaFailed;
                    VoicePlayer.Play();
                    //Task.Run(() =>
                    //{
                    //    Thread.Sleep(1000);
                    //    Dispatcher.Invoke(() =>
                    //    {
                    //        if (VoicePlayer?.Clock?.NaturalDuration.HasTimeSpan == true)
                    //            PlayEndTime += VoicePlayer.Clock.NaturalDuration.TimeSpan - TimeSpan.FromSeconds(0.8);
                    //    });
                    //});
                });
            }
            else
            {
                soundPlayer.SoundLocation = VoicePath.LocalPath;
                soundPlayer.LoadAsync();
            }
        }
        /// <summary>
        /// </summary>
        public double PlayVoiceVolume
        {
            get => Dispatcher.Invoke(() => VoicePlayer.Volume);
            set => Dispatcher.Invoke(() => VoicePlayer.Volume = value);
        }
        /// <summary>
        /// </summary>
        public bool PlayingVoice = false;
        private void MediaPlayer_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            windowMediaPlayerAvailable = false;
            PlayingVoice = false;
            MessageBoxX.Show("Audio playback failed, attempted automatic switch to backup player. If the issue persists, please check if Windows Media Player is installed.".Translate(), "audio error".Translate(), MessageBoxIcon.Warning);
        }
        private void Clock_Completed(object sender, EventArgs e)
        {
            PlayingVoice = false;
            VoicePlayer.Clock = null;
        }
        public bool MoveTimerSmartMove = false;
        private void SmartMoveTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            MoveTimerSmartMove = false;
        }

        private void MoveTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (DisplayType.Type != GraphType.Move || !MoveTimerSmartMove)
            {
                MoveTimer.Stop();
                return;
            }
            Core.Controller.MoveWindows(MoveTimerPoint.X, MoveTimerPoint.Y);

            var z500 = -500 * Core.Controller.ZoomRatio;
            if (Core.Controller.GetWindowsDistanceLeft() < z500)
            {
                Console.WriteLine("Current animation movement design error: boundary reached left\\nAnimation name: {0}\\nDistance: {1}".Translate(DisplayType.Name, Core.Controller.GetWindowsDistanceLeft()));
            }
            else if (Core.Controller.GetWindowsDistanceRight() < z500)
            {
                Console.WriteLine("Current animation movement design error: boundary reached on the right side\\nAnimation name: {0}\\nDistance: {1}".Translate(DisplayType.Name, Core.Controller.GetWindowsDistanceRight()));
            }
            else if (Core.Controller.GetWindowsDistanceUp() < z500)
            {
                Console.WriteLine("Current animation movement design error: reached boundary upper side\\nAnimation name: {0}\\nDistance: {1}".Translate(DisplayType.Name, Core.Controller.GetWindowsDistanceUp()));
            }
            else if (Core.Controller.GetWindowsDistanceDown() < z500)
            {
                Console.WriteLine("Current animation movement design error: boundary reached lower side\\nAnimation name: {0}\\nDistance: {1}".Translate(DisplayType.Name, Core.Controller.GetWindowsDistanceDown()));
            }
            MoveTimer.Start();
        }
        /// <summary>
        /// </summary>
        public Action DefaultClickAction;
        /// <summary>
        /// </summary>
        public Action DefaultPressAction;
        public bool isPress = false;
        long presstime;
        private void MainGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            isPress = true;
            CountNomal = 0;
            Task.Run(() =>
            {
                var pth = DateTime.Now.Ticks;
                presstime = pth;
                Thread.Sleep(Core.Controller.PressLength);
                Point mp = default;
                Dispatcher.BeginInvoke(new Action(() => mp = Mouse.GetPosition(MainGrid))).Wait();
                //mp = new Point(mp.X * Core.Controller.ZoomRatio, mp.Y * Core.Controller.ZoomRatio);
                if (isPress && presstime == pth)
                {
                    LastInteractionTime = DateTime.Now;
                    foreach (var x in Core.TouchEvent)
                    {
                        if (x.IsPress == true && x.Touch(mp) && x.DoAction())
                            return;
                    }
                    DefaultPressAction?.Invoke();
                }
                else
                {
                    LastInteractionTime = DateTime.Now;
                    foreach (var x in Core.TouchEvent)
                    {
                        if (x.IsPress == false && x.Touch(mp) && x.DoAction())
                            return;
                    }
                    if (DisplayType.Type != GraphType.Default)
                    {
                        CleanState();
                        if (!IsIdel && State != WorkingState.Sleep && DisplayStop(DisplayToNomal))
                            return;
                    }
                    DefaultClickAction?.Invoke();
                }
            });
        }

        private void MainGrid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            isPress = false;
            if (DisplayType.Type.ToString().StartsWith("Raised"))
            {
                MainGrid.MouseMove -= MainGrid_MouseWave;
                MainGrid.MouseMove -= MainGrid_MouseMove;
                MainGrid.MouseMove += MainGrid_MouseWave;
                rasetype = -1;
                DisplayRaising();
            }
            else
            {
                //if (MsgBar.Visibility == Visibility.Visible)
                //{
                //    MsgBar.ForceClose();
                //}
                if (SmartMove)
                {
                    MoveTimerSmartMove = true;
                    SmartMoveTimer.Enabled = false;
                    SmartMoveTimer.Start();
                }
            }
            ((UIElement)e.Source).ReleaseMouseCapture();
        }

        private void MainGrid_MouseMove(object sender, MouseEventArgs e)
        {
            if (!((UIElement)e.Source).CaptureMouse() || !isPress)
            {
                MainGrid.MouseMove -= MainGrid_MouseWave;
                MainGrid.MouseMove -= MainGrid_MouseMove;
                MainGrid.MouseMove += MainGrid_MouseWave;
                rasetype = -1;
                DisplayRaising();
                return;
            }
            var mp = e.GetPosition(MainGrid);
            var x = mp.X - Core.Graph.GraphConfig.RaisePoint[(int)Core.Save.Mode].X;
            var y = mp.Y - Core.Graph.GraphConfig.RaisePoint[(int)Core.Save.Mode].Y;
            if (Math.Abs(x) < 1)
                x = 0;
            if (Math.Abs(y) < 1)
                y = 0;
            Core.Controller.MoveWindows(x, y);
            if (Math.Abs(x) + Math.Abs(y) > 20 && rasetype >= 1)
                rasetype = 0;
        }

        private void MainGrid_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (ToolBar.Visibility == Visibility.Visible)
            {
                ToolBar.CloseTimer.Enabled = false;
                ToolBar.Visibility = Visibility.Collapsed;
            }
            else
                ToolBar.Show();
        }

        public void Dispose()
        {
            EventTimer.Dispose();
            MoveTimer.Dispose();
            MsgBar.Dispose();
            ToolBar.Dispose();
            if (PetGrid.Child is IGraph g)
                g.Stop(true);
            if (PetGrid2.Child is IGraph g2)
                g2.Stop(true);
        }
        /// <summary>
        /// </summary>
        public void CleanState()
        {
            MoveTimer.Enabled = false;
            MainGrid.MouseMove -= MainGrid_MouseWave;
            MainGrid.MouseMove -= MainGrid_MouseMove;
            MainGrid.MouseMove += MainGrid_MouseWave;
        }
        private int wavetimes = 0;
        private int switchcount = 0;
        private bool? waveleft = null;
        private bool? wavetop = null;
        private DateTime wavespan;
        private void MainGrid_MouseWave(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                return;
            isPress = false;
            if (rasetype >= 0 || State != WorkingState.Nomal)
                return;

            if ((DateTime.Now - wavespan).TotalSeconds > 2)
            {
                wavetimes = 0;
                switchcount = 0;
                waveleft = null;
                wavetop = null;
            }
            wavespan = DateTime.Now;
            bool active = false;
            var p = e.GetPosition(MainGrid);

            if (p.Y < 200)
            {
                if (wavetop != false)
                    wavetop = true;
                else
                {
                    if (switchcount++ > 150)
                        wavespan = DateTime.MinValue;
                    return;
                }
            }
            else
            {
                if (wavetop != true)
                    wavetop = false;
                else
                {
                    if (switchcount++ > 150)
                        wavespan = DateTime.MinValue;
                    return;
                }
            }

            if (p.X < 200 && waveleft != true)
            {
                waveleft = true;
                active = true;
            }
            if (p.X > 300 && waveleft != false)
            {
                active = true;
                waveleft = false;
            }

            if (active)
            {
                if (wavetimes++ > 4)
                    if (wavetop == true)
                    {
                        if (wavetimes >= 10 || IsIdel || DisplayType.Type == GraphType.Touch_Head)
                            DisplayTouchHead();
                        //Console.WriteLine(wavetimes);
                        LastInteractionTime = DateTime.Now;
                    }
                    else
                    {
                        if (wavetimes >= 10 || IsIdel || DisplayType.Type == GraphType.Touch_Body)
                            DisplayTouchBody();
                        LastInteractionTime = DateTime.Now;
                    }
            }
        }

        private void MainGrid_MouseEnter(object sender, MouseEventArgs e)
        {
            string gfname;
            if (DisplayType.Type == GraphType.SideHide_Left_Main && (gfname = Core.Graph.FindName(GraphType.SideHide_Left_Rise)) != null)
            {
                Display(gfname, AnimatType.A_Start, DisplayBLoopingForce);
            }
            else if (DisplayType.Type == GraphType.SideHide_Right_Main && (gfname = Core.Graph.FindName(GraphType.SideHide_Right_Rise)) != null)
            {
                Display(gfname, AnimatType.A_Start, DisplayBLoopingForce);
            }
        }

        private void MainGrid_MouseLeave(object sender, MouseEventArgs e)
        {
            if (DisplayType.Type == GraphType.SideHide_Left_Rise)
            {
                Display(GraphType.SideHide_Left_Rise, AnimatType.C_End, () => Display(GraphType.SideHide_Left_Main, AnimatType.B_Loop, DisplayBLoopingForce));
            }
            else if (DisplayType.Type == GraphType.SideHide_Right_Rise)
            {
                Display(GraphType.SideHide_Right_Rise, AnimatType.C_End, () => Display(GraphType.SideHide_Right_Main, AnimatType.B_Loop, DisplayBLoopingForce));
            }
        }
    }
}
