using LinePutScript;
using LinePutScript.Converter;
using LinePutScript.Localization.WPF;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using static VPet_Simulator.Core.GraphInfo;
using static VPet_Simulator.Core.IGraph;
using static VPet_Simulator.Core.Picture;

namespace VPet_Simulator.Core
{
    public static class GraphHelper
    {
        internal static string[][] graphtypevalue = null;
        /// <summary>
        /// </summary>
        public static string[][] GraphTypeValue
        {
            get
            {
                if (graphtypevalue == null)
                {
                    List<string[]> gtv = new List<string[]>();
                    foreach (string v in Enum.GetNames(typeof(GraphType)))
                    {
                        gtv.Add(v.ToLowerInvariant().Split('_'));
                    }
                    graphtypevalue = gtv.ToArray();
                }
                return graphtypevalue;
            }
        }
        /// <summary>
        /// </summary>
        public static void Run(this IGraph graph, Decorator parant, ImageSource image, Action EndAction = null)
        {
            if (graph is IRunImage iri)
            {
                iri.Run(parant, image, EndAction);
            }
            else
            {
                graph.Run(parant, EndAction);
            }
        }
        /// <summary>
        /// </summary>
        public static Task Run(this IGraph graph, Image img, Action EndAction = null)
        {
            if (graph is IImageRun iri)
            {
                return iri.Run(img, EndAction);
            }
            else
            {
                return null;
            }
        }


        /// <summary>
        /// Work/Learning
        /// </summary>
        public class Work : ICloneable
        {
            /// <summary>
            /// Type
            /// </summary>
            public enum WorkType { Work, Study, Play }
            /// <summary>
            /// Work/Learning
            /// </summary>
            [Line(ignoreCase: true)]
            public WorkType Type { get; set; }
            /// <summary>
            /// WorkName
            /// </summary>
            [Line(ignoreCase: true)]
            public string Name { get; set; }
            public string nametrans = null;
            /// <summary>
            /// </summary>
            public string NameTrans
            {
                get
                {
                    if (nametrans == null)
                        nametrans = Name.Translate();
                    return nametrans;
                }
            }
            /// <summary>
            /// </summary>
            [Line(ignoreCase: true, converter: typeof(Function.LPSConvertToLower))]
            public string Graph { get; set; }
            /// <summary>
            /// </summary>
            [Line(ignoreCase: true)]
            public double MoneyBase { get; set; }
            /// <summary>
            /// </summary>
            [Line(ignoreCase: true)]
            public double StrengthFood { get; set; }
            /// <summary>
            /// </summary>
            [Line(ignoreCase: true)]
            public double StrengthDrink { get; set; }
            /// <summary>
            /// </summary>
            [Line(ignoreCase: true)]
            public double Feeling { get; set; }
            /// <summary>
            /// Level limit
            /// </summary>
            [Line(ignoreCase: true)]
            public int LevelLimit { get; set; }
            /// <summary>
            /// Time spent(Min)
            /// </summary>
            [Line(ignoreCase: true)]
            public int Time { get; set; }
            /// <summary>
            /// </summary>
            [Line(ignoreCase: true)]
            public double FinishBonus { get; set; }


            [Line(ignoreCase: true)]
            public string BorderBrush = "0290D5";
            [Line(ignoreCase: true)]
            public string Background = "81d4fa";
            [Line(ignoreCase: true)]
            public string ButtonBackground = "0286C6";
            [Line(ignoreCase: true)]
            public string ButtonForeground = "ffffff";
            [Line(ignoreCase: true)]
            public string Foreground = "0286C6";
            [Line(ignoreCase: true)]
            public double Left = 100;
            [Line(ignoreCase: true)]
            public double Top = 160;
            [Line(ignoreCase: true)]
            public double Width = 300;

            public void SetStyle(WorkTimer wt)
            {
                wt.Margin = new Thickness(Left, Top, 0, 0);
                wt.Width = Width;
                wt.Height = Width / 300 * 180;
                wt.Resources.Clear();
                wt.Resources.Add("BorderBrush", new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF" + BorderBrush)));
                wt.Resources.Add("Background", new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF" + Background)));
                wt.Resources.Add("ButtonBackground", new SolidColorBrush((Color)ColorConverter.ConvertFromString("#AA" + ButtonBackground)));
                wt.Resources.Add("ButtonBackgroundHover", new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF" + ButtonBackground)));
                wt.Resources.Add("ButtonForeground", new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF" + ButtonForeground)));
                wt.Resources.Add("Foreground", new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF" + Foreground)));
            }
            /// <summary>
            /// </summary>
            /// <param name="m"></param>
            public void Display(Main m)
            {
                m.Display(Graph, AnimatType.A_Start, () => m.DisplayBLoopingForce(Graph));
            }
            /// <summary>
            /// </summary>
            public object Clone()
            {
                return new Work
                {
                    Type = this.Type,
                    Name = this.Name,
                    Graph = this.Graph,
                    MoneyBase = this.MoneyBase,
                    StrengthFood = this.StrengthFood,
                    StrengthDrink = this.StrengthDrink,
                    Feeling = this.Feeling,
                    LevelLimit = this.LevelLimit,
                    Time = this.Time,
                    FinishBonus = this.FinishBonus,
                    BorderBrush = this.BorderBrush,
                    Background = this.Background,
                    ButtonBackground = this.ButtonBackground,
                    ButtonForeground = this.ButtonForeground,
                    Foreground = this.Foreground,
                    Left = this.Left,
                    Top = this.Top,
                    Width = this.Width
                };
            }
        }

        /// <summary>
        /// </summary>
        public class Move
        {
            /// <summary>
            /// </summary>
            [Line(ignoreCase: true, converter: typeof(Function.LPSConvertToLower))]
            public string Graph { get; set; }
            /// <summary>
            /// </summary>
            [Flags]
            public enum DirectionType
            {
                None,
                Left,
                Right = 2,
                Top = 4,
                Bottom = 8,
                LeftGreater = 16,
                RightGreater = 32,
                TopGreater = 64,
                BottomGreater = 128,
            }
            /// <summary>
            /// </summary>
            [Line(ignoreCase: true)]
            public DirectionType LocateType { get; set; } = DirectionType.None;
            /// <summary>
            /// </summary>
            [Line(ignoreCase: true)]
            public int Interval { get; set; } = 125;

            [Line(ignoreCase: true)]
            private int checkType { get; set; }
            /// <summary>
            /// </summary>
            public DirectionType CheckType
            {
                get => (DirectionType)checkType;
                set => checkType = (int)value;
            }
            [Line(ignoreCase: true)]
            private int modeType { get; set; } = 30;

            /// <summary>
            /// </summary>
            public ModeType Mode
            {
                get => (ModeType)modeType;
                set => checkType = (int)value;
            }

            /// <summary>
            /// </summary>
            [Flags]
            public enum ModeType
            {
                /// <summary>
                /// </summary>
                Happy = 2,
                /// <summary>
                /// </summary>
                Nomal = 4,
                /// <summary>
                /// </summary>
                PoorCondition = 8,
                /// <summary>
                /// </summary>
                Ill = 16,
            }
            public static ModeType GetModeType(IGameSave.ModeType type)
            {
                switch (type)
                {
                    case IGameSave.ModeType.Happy:
                        return ModeType.Happy;
                    case IGameSave.ModeType.Nomal:
                        return ModeType.Nomal;
                    case IGameSave.ModeType.PoorCondition:
                        return ModeType.PoorCondition;
                    case IGameSave.ModeType.Ill:
                        return ModeType.Ill;
                    default:
                        return ModeType.Nomal;
                }
            }
            /// <summary>
            /// </summary>
            [Line(ignoreCase: true)] public int CheckLeft { get; set; } = 100;
            /// <summary>
            /// </summary>
            [Line(ignoreCase: true)] public int CheckRight { get; set; } = 100;
            /// <summary>
            /// </summary>
            [Line(ignoreCase: true)] public int CheckTop { get; set; } = 100;
            /// <summary>
            /// </summary>
            [Line(ignoreCase: true)] public int CheckBottom { get; set; } = 100;
            /// <summary>
            /// </summary>
            [Line(ignoreCase: true)] public int SpeedX { get; set; }
            /// <summary>
            /// </summary>
            [Line(ignoreCase: true)] public int SpeedY { get; set; }
            /// <summary>
            /// </summary>
            [Line(ignoreCase: true)]
            public int LocateLength { get; set; }
            /// <summary>
            /// </summary>
            [Line(ignoreCase: true)] public int Distance { get; set; } = 5;

            [Line(ignoreCase: true)]
            private int triggerType { get; set; }
            /// <summary>
            /// </summary>
            public DirectionType TriggerType
            {
                get => (DirectionType)triggerType;
                set => triggerType = (int)value;
            }
            /// <summary>
            /// </summary>
            [Line(ignoreCase: true)] public int TriggerLeft { get; set; } = 100;
            /// <summary>
            /// </summary>
            [Line(ignoreCase: true)] public int TriggerRight { get; set; } = 100;
            /// <summary>
            /// </summary>
            [Line(ignoreCase: true)] public int TriggerTop { get; set; } = 100;
            /// <summary>
            /// </summary>
            [Line(ignoreCase: true)] public int TriggerBottom { get; set; } = 100;
            /// <summary>
            /// </summary>
            public bool Triggered(Main m)
            {
                var c = m.Core.Controller;
                if (!Mode.HasFlag(GetModeType(m.Core.Save.Mode))) return false;
                if (TriggerType == DirectionType.None) return true;
                if (TriggerType.HasFlag(DirectionType.Left) && c.GetWindowsDistanceLeft() > TriggerLeft * c.ZoomRatio)
                    return false;
                if (TriggerType.HasFlag(DirectionType.Right) && c.GetWindowsDistanceRight() > TriggerRight * c.ZoomRatio)
                    return false;
                if (TriggerType.HasFlag(DirectionType.Top) && c.GetWindowsDistanceUp() > TriggerTop * c.ZoomRatio)
                    return false;
                if (TriggerType.HasFlag(DirectionType.Bottom) && c.GetWindowsDistanceDown() > TriggerBottom * c.ZoomRatio)
                    return false;
                if (TriggerType.HasFlag(DirectionType.LeftGreater) && c.GetWindowsDistanceLeft() < TriggerLeft * c.ZoomRatio)
                    return false;
                if (TriggerType.HasFlag(DirectionType.RightGreater) && c.GetWindowsDistanceRight() < TriggerRight * c.ZoomRatio)
                    return false;
                if (TriggerType.HasFlag(DirectionType.TopGreater) && c.GetWindowsDistanceUp() < TriggerTop * c.ZoomRatio)
                    return false;
                if (TriggerType.HasFlag(DirectionType.BottomGreater) && c.GetWindowsDistanceDown() < TriggerBottom * c.ZoomRatio)
                    return false;
                return true;
            }

            /// <summary>
            /// </summary>
            public bool Checked(IController c)
            {
                if (CheckType == DirectionType.None) return true;
                if (CheckType.HasFlag(DirectionType.Left) && c.GetWindowsDistanceLeft() > CheckLeft * c.ZoomRatio)
                    return false;
                if (CheckType.HasFlag(DirectionType.Right) && c.GetWindowsDistanceRight() > CheckRight * c.ZoomRatio)
                    return false;
                if (CheckType.HasFlag(DirectionType.Top) && c.GetWindowsDistanceUp() > CheckTop * c.ZoomRatio)
                    return false;
                if (CheckType.HasFlag(DirectionType.Bottom) && c.GetWindowsDistanceDown() > CheckBottom * c.ZoomRatio)
                    return false;
                if (CheckType.HasFlag(DirectionType.LeftGreater) && c.GetWindowsDistanceLeft() < CheckLeft * c.ZoomRatio)
                    return false;
                if (CheckType.HasFlag(DirectionType.RightGreater) && c.GetWindowsDistanceRight() < CheckRight * c.ZoomRatio)
                    return false;
                if (CheckType.HasFlag(DirectionType.TopGreater) && c.GetWindowsDistanceUp() < CheckTop * c.ZoomRatio)
                    return false;
                if (CheckType.HasFlag(DirectionType.BottomGreater) && c.GetWindowsDistanceDown() < CheckBottom * c.ZoomRatio)
                    return false;
                return true;
            }

            int walklength = 0;
            /// <summary>
            /// </summary>
            public Move GetCompatibilityMove(Main main)
            {
                List<Move> ms = new List<Move>();
                bool x = SpeedX > 0;
                bool y = SpeedY > 0;
                foreach (Move m in main.Core.Graph.GraphConfig.Moves)
                {
                    //if (m == this) continue;
                    int bns = 0;
                    if (SpeedX != 0 && m.SpeedX != 0)
                    {
                        if ((m.SpeedX > 0) != x)
                            bns--;
                        else
                            bns++;
                    }
                    if (SpeedY != 0 && m.SpeedY != 0)
                    {
                        if ((m.SpeedY > 0) != y)
                            bns--;
                        else
                            bns++;
                    }
                    if (bns >= 0 && m.Triggered(main))
                    {
                        ms.Add(m);
                    }
                }
                if (ms.Count == 0) return null;
                return ms[Function.Rnd.Next(ms.Count)];
            }

            /// <summary>
            /// </summary>
            public void Display(Main m)
            {
                m.Event_MoveStartInvoke(this);
                walklength = 0;
                m.CountNomal = 0;
                m.Display(Graph, AnimatType.A_Start, () =>
                {
                    if (m.MoveTimerSmartMove)
                    {
                        switch (LocateType)
                        {
                            case DirectionType.Top:
                                m.Core.Controller.MoveWindows(0, -m.Core.Controller.GetWindowsDistanceUp() / m.Core.Controller.ZoomRatio - LocateLength);
                                break;
                            case DirectionType.Bottom:
                                m.Core.Controller.MoveWindows(0, m.Core.Controller.GetWindowsDistanceDown() / m.Core.Controller.ZoomRatio + LocateLength);
                                break;
                            case DirectionType.Left:
                                m.Core.Controller.MoveWindows(-m.Core.Controller.GetWindowsDistanceLeft() / m.Core.Controller.ZoomRatio - LocateLength, 0);
                                break;
                            case DirectionType.Right:
                                m.Core.Controller.MoveWindows(m.Core.Controller.GetWindowsDistanceRight() / m.Core.Controller.ZoomRatio + LocateLength, 0);
                                break;
                        }
                        m.MoveTimerPoint = new Point(SpeedX, SpeedY);
                        m.MoveTimer.Interval = Interval;
                        m.MoveTimer.Start();
                    }
                    Displaying(m);
                });
            }
            /// <summary>
            /// </summary>
            /// <param name="m"></param>
            public void Displaying(Main m)
            {
                if (!Checked(m.Core.Controller))
                {
                    if (Function.Rnd.Next(Main.TreeRND) <= 1)
                    {
                        var newmove = GetCompatibilityMove(m);
                        if (newmove != null)
                        {
                            newmove.Display(m);
                            return;
                        }
                    }
                    StopMoving(m);
                    return;
                }
                if (Function.Rnd.Next(walklength++) < Distance)
                {
                    m.Display(Graph, AnimatType.B_Loop, () => Displaying(m));
                    return;
                }
                else if (Function.Rnd.Next(Main.TreeRND) <= 1)
                {
                    var newmove = GetCompatibilityMove(m);
                    if (newmove != null)
                    {
                        newmove.Display(m);
                        return;
                    }
                }
                StopMoving(m);
            }

            private void StopMoving(Main m)
            {
                if (m.Core.Controller.RePositionActive)
                    m.Core.Controller.ResetPosition();
                m.Core.Controller.RePositionActive = !m.Core.Controller.CheckPosition();
                m.MoveTimer.Enabled = false;

                m.Display(Graph, AnimatType.C_End, () => { m.Event_MoveEndInvoke(this); m.DisplayToNomal(); });
            }
        }

    }

}
