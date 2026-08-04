using LinePutScript.Localization.WPF;
using System;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using static VPet_Simulator.Core.GraphHelper;
using static VPet_Simulator.Core.GraphInfo;
using static VPet_Simulator.Core.WorkTimer.FinishWorkInfo;

namespace VPet_Simulator.Core
{
    /// <summary>
    /// </summary>
    public partial class WorkTimer : Viewbox
    {
        Main m;
        public WorkTimer(Main m)
        {
            InitializeComponent();
            this.m = m;
            m.TimeUIHandle += M_TimeUIHandle;
        }
        /// <summary>
        /// 0 = Default
        /// 1 = Remaining time
        /// </summary>
        public int DisplayType = 0;
        /// <summary>
        /// </summary>
        public double GetCount;
        /// <summary>
        /// </summary>
        public DateTime StartTime;
        /// <summary>
        /// </summary>
        public struct FinishWorkInfo
        {
            /// <summary>
            /// </summary>
            public Work work;
            /// <summary>
            /// </summary>
            public double count;
            /// <summary>
            /// </summary>
            public double spendtime;
            /// <summary>
            /// </summary>
            public enum StopReason
            {
                /// <summary>
                /// </summary>
                TimeFinish,
                /// <summary>
                /// </summary>
                MenualStop,
                /// <summary>
                /// </summary>
                StateFail,
                /// <summary>
                /// </summary>
                Other,
            }
            /// <summary>
            /// </summary>
            public StopReason Reason;
            /// <summary>
            /// </summary>
            public FinishWorkInfo(Work work, double count, StopReason reason)
            {
                this.work = work;
                this.count = count * (1 + work.FinishBonus);
                this.spendtime = work.Time;
                this.Reason = reason;
            }
            /// <summary>
            /// </summary>
            public FinishWorkInfo(Work work, double count, DateTime starttime, StopReason reason)
            {
                this.work = work;
                this.count = count * (1 + work.FinishBonus);
                this.spendtime = DateTime.Now.Subtract(starttime).TotalMinutes;
                this.Reason = reason;
            }
        }
        /// <summary>
        /// </summary>
        /// <param name="m"></param>
        private void M_TimeUIHandle(Main m)
        {
            if (Visibility == Visibility.Collapsed) return;
            TimeSpan ts = DateTime.Now - StartTime;
            TimeSpan tleft;
            if (ts.TotalMinutes > m.NowWork.Time)
            {
                //ts = TimeSpan.FromMinutes(MaxTime);
                //tleft = TimeSpan.Zero;
                //PBLeft.Value = MaxTime;
                FinishWorkInfo fwi = new FinishWorkInfo(m.NowWork, GetCount, FinishWorkInfo.StopReason.TimeFinish);
                if (m.NowWork.Type == Work.WorkType.Work)
                {
                    m.Core.Save.Money += GetCount * m.NowWork.FinishBonus;
                    Stop(() => m.SayRnd(LocalizeCore.Translate("{2} completed: earned {0:f2} Vpet-bucks", fwi.count,
                        fwi.spendtime, fwi.work.NameTrans), true), StopReason.TimeFinish);
                }
                else
                {
                    m.Core.Save.Exp += GetCount * m.NowWork.FinishBonus;
                    Stop(() => m.SayRnd(LocalizeCore.Translate("{2} completed: earned {0:f2} experience\\n Total time spent: {1} minutes", fwi.count,
                        fwi.spendtime, fwi.work.NameTrans), true), StopReason.TimeFinish);
                }
                return;
            }
            else
            {
                tleft = TimeSpan.FromMinutes(m.NowWork.Time) - ts;
                PBLeft.Value = ts.TotalMinutes;
            }
            switch (DisplayType)
            {
                default:
                case 0:
                    ShowTimeSpan(ts); break;
                case 1:
                    ShowTimeSpan(tleft); break;
                case 2:
                    tNumber.Text = GetCount.ToString("f0");
                    if (m.NowWork.Type == Work.WorkType.Work)
                        tNumberUnit.Text = LocalizeCore.Translate("$");
                    else
                        tNumberUnit.Text = "EXP";
                    break;
                case 3:
                    break;
            }
        }
        public void ShowTimeSpan(TimeSpan ts)
        {
            if (ts.TotalSeconds < 90)
            {
                tNumber.Text = ts.TotalSeconds.ToString("f1");
                tNumberUnit.Text = LocalizeCore.Translate("Sec");
            }
            else if (ts.TotalMinutes < 90)
            {
                tNumber.Text = ts.TotalMinutes.ToString("f1");
                tNumberUnit.Text = LocalizeCore.Translate("Min");
            }
            else
            {
                tNumber.Text = ts.TotalHours.ToString("f1");
                tNumberUnit.Text = LocalizeCore.Translate("Hour");
            }
        }
        public void DisplayUI()
        {
            if (DisplayType == 3)
            {
                btnSwitch.Opacity = 0.5;
                DisplayBorder.Visibility = Visibility.Collapsed;
            }
            else
            {
                btnSwitch.Opacity = 1;
                DisplayBorder.Visibility = Visibility.Visible;
                btnStop.Content = LocalizeCore.Translate("Stop ") + m.NowWork.NameTrans;
                switch (DisplayType)
                {
                    default:
                    case 0:
                        tNow.Text = LocalizeCore.Translate("{0} for", m.NowWork.NameTrans);
                        break;
                    case 1:
                        tNow.Text = LocalizeCore.Translate("Remaining {0} time", m.NowWork.NameTrans);
                        break;
                    case 2:
                        if (m.NowWork.Type == Work.WorkType.Work)
                            tNow.Text = LocalizeCore.Translate("Earning money");
                        else
                            tNow.Text = LocalizeCore.Translate("Gain EXP");
                        break;
                }
            }
            M_TimeUIHandle(m);
        }
        private void SwitchState_Click(object sender, RoutedEventArgs e)
        {
            DisplayType++;
            if (DisplayType >= 4)
                DisplayType = 0;
            DisplayUI();
        }
        public void Start(Work work)
        {
            //if (state == Main.WorkingState.Nomal)
            //    return;
            Visibility = Visibility.Visible;
            m.State = Main.WorkingState.Work;
            m.NowWork = work;
            StartTime = DateTime.Now;
            GetCount = 0;

            work.SetStyle(this);
            work.Display(m);
            m.Event_WorkStartInvoke(work);

            PBLeft.Maximum = work.Time;
            DisplayUI();
        }
        /// <summary>
        /// Stop Working
        /// </summary>
        /// <param name="then"></param>
        public void Stop(Action @then = null, StopReason reason = StopReason.MenualStop)
        {
            if (m.State == Main.WorkingState.Work && m.NowWork != null)
            {
                FinishWorkInfo fwi = new FinishWorkInfo(m.NowWork, GetCount, StartTime, reason);
                E_FinishWork?.Invoke(fwi);
            }
            Visibility = Visibility.Collapsed;
            m.State = Main.WorkingState.Nomal;
            m.Display(m.NowWork.Graph, AnimatType.C_End, then ?? m.DisplayNomal);
        }
        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            Stop(reason: StopReason.MenualStop);
        }
        /// <summary>
        /// </summary>
        public event Action<FinishWorkInfo> E_FinishWork;
    }
}
