using LinePutScript.Converter;
using LinePutScript.Localization.WPF;
using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using static VPet_Simulator.Core.GraphInfo;

namespace VPet_Simulator.Core
{
    public partial class Main
    {
        /// <summary>
        /// </summary>
        public GraphInfo DisplayType = new GraphInfo("");
        /// <summary>
        /// </summary>
        public int CountNomal = 0;
        /// <summary>
        /// </summary>
        public void DisplayToNomal()
        {
            switch (State)
            {
                default:
                case WorkingState.Nomal:
                    DisplayNomal();
                    return;
                case WorkingState.Sleep:
                    DisplaySleep(true);
                    return;
                case WorkingState.Work:
                    NowWork.Display(this);
                    return;
                case WorkingState.Travel:
                    //TODO
                    return;
            }
        }
        /// <summary>
        /// </summary>
        public Action DisplayNomal { get; set; }
        /// <summary>
        /// </summary>
        public Func<bool> DisplayMove { get; set; }
        /// <summary>
        /// </summary>
        public Func<bool> DisplayIdel { get; set; }
        /// <summary>
        /// </summary>
        public Action DisplayIdel_StateONE { get; set; }
        /// <summary>
        /// </summary>
        public Action DisplayTouchHead { get; set; }
        /// <summary>
        /// </summary>
        public Action DisplayTouchBody { get; set; }
        /// <summary>
        /// </summary>
        public void DisplayDefault()
        {
            CountNomal++;
            Display(GraphType.Default, AnimatType.Single, DisplayNomal);
        }
        /// <summary>
        /// </summary>
        public bool DisplayStop(Action EndAction)
        {
            var graph = Core.Graph.FindGraph(DisplayType.Name, AnimatType.C_End, Core.Save.Mode);
            if (graph != null)
            {
                if (State == WorkingState.Sleep)
                    State = WorkingState.Nomal;
                Display(graph, EndAction);
                return true;
            }
            return false;
        }
        /// <summary>
        /// </summary>
        public void DisplayStopForce(Action EndAction)
        {
            if (!DisplayStop(EndAction))
                EndAction?.Invoke();
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public bool DisplayToMove()
        {
            var list = Core.Graph.GraphConfig.Moves.ToList();
            for (int i = Function.Rnd.Next(list.Count); 0 != list.Count; i = Function.Rnd.Next(list.Count))
            {
                var move = list[i];
                if (move.Triggered(this))
                {
                    move.Display(this);
                    return true;
                }
                else
                {
                    list.RemoveAt(i);
                }
            }
            return false;
        }
        /// <summary>
        /// </summary>
        public event Action Event_TouchHead;
        /// <summary>
        /// </summary>
        public void DisplayToTouchHead()
        {
            CountNomal = 0;
            if (Core.Controller.EnableFunction && Core.Save.Strength >= 10 && Core.Save.Feeling < Core.Save.FeelingMax)
            {
                Core.Save.StrengthChange(-2);
                Core.Save.FeelingChange(1);
                Core.Save.Mode = Core.Save.CalMode();
                LabelDisplayShowChangeNumber(LocalizeCore.Translate("Stamina: -{0:f0}, Mood: +{1:f0}"), 2, 1);
            }
            if (DisplayType.Type == GraphType.Touch_Head)
            {
                if (DisplayType.Animat == AnimatType.A_Start)
                    return;
                else if (DisplayType.Animat == AnimatType.B_Loop)
                    if (Dispatcher.Invoke(() => PetGrid.Tag) is IGraph ig && ig.GraphInfo.Type == GraphType.Touch_Head && ig.GraphInfo.Animat == AnimatType.B_Loop)
                    {
                        ig.SetContinue();
                        return;
                    }
                    else if (Dispatcher.Invoke(() => PetGrid2.Tag) is IGraph ig2 && ig2.GraphInfo.Type == GraphType.Touch_Head && ig2.GraphInfo.Animat == AnimatType.B_Loop)
                    {
                        ig2.SetContinue();
                        return;
                    }
            }
            Event_TouchHead?.Invoke();
            Display(GraphType.Touch_Head, AnimatType.A_Start, (graphname) =>
               Display(graphname, AnimatType.B_Loop, (graphname) =>
               DisplayCEndtoNomal(graphname)));
        }
        /// <summary>
        /// </summary>
        public event Action Event_TouchBody;
        /// <summary>
        /// </summary>
        public void DisplayToTouchBody()
        {
            CountNomal = 0;
            if (Core.Controller.EnableFunction && Core.Save.Strength >= 10 && Core.Save.Feeling < Core.Save.FeelingMax)
            {
                Core.Save.StrengthChange(-2);
                Core.Save.FeelingChange(1);
                Core.Save.Mode = Core.Save.CalMode();
                LabelDisplayShowChangeNumber(LocalizeCore.Translate("Stamina: -{0:f0}, Mood: +{1:f0}"), 2, 1);
            }
            if (DisplayType.Type == GraphType.Touch_Body)
            {
                if (DisplayType.Animat == AnimatType.A_Start)
                    return;
                else if (DisplayType.Animat == AnimatType.B_Loop)
                    if (Dispatcher.Invoke(() => PetGrid.Tag) is IGraph ig && ig.GraphInfo.Type == GraphType.Touch_Body && ig.GraphInfo.Animat == AnimatType.B_Loop)
                    {
                        ig.SetContinue();
                        return;
                    }
                    else if (Dispatcher.Invoke(() => PetGrid2.Tag) is IGraph ig2 && ig2.GraphInfo.Type == GraphType.Touch_Body && ig2.GraphInfo.Animat == AnimatType.B_Loop)
                    {
                        ig2.SetContinue();
                        return;
                    }
            }
            Event_TouchBody?.Invoke();
            Display(GraphType.Touch_Body, AnimatType.A_Start, (graphname) =>
             Display(graphname, AnimatType.B_Loop, (graphname) =>
             DisplayCEndtoNomal(graphname)));
        }
        /// <summary>
        /// </summary>
        public void DisplayToIdel_StateONE()
        {
            looptimes = 0;
            CountNomal = 0;
            var name = Core.Graph.FindName(GraphType.StateONE);
            var list = Core.Graph.FindGraphs(name, AnimatType.A_Start, Core.Save.Mode)?.FindAll(x => x.GraphInfo.Type == GraphType.StateONE);
            if (list != null && list.Count > 0)
                Display(list[Function.Rnd.Next(list.Count)], () => DisplayIdel_StateONEing(name));
            else
                DisplayIdel();
        }
        /// <summary>
        /// </summary>
        private void DisplayIdel_StateONEing(string graphname)
        {
            if (Function.Rnd.Next(++looptimes) > Core.Graph.GraphConfig.GetDuration(graphname))
                switch (Function.Rnd.Next(2 + CountNomal))
                {
                    case 0:
                        DisplayIdel_StateTWO(graphname);
                        break;
                    default:
                        Display(graphname, AnimatType.C_End, GraphType.StateONE, DisplayNomal);
                        break;
                }
            else
            {
                Display(graphname, AnimatType.B_Loop, GraphType.StateONE, DisplayIdel_StateONEing);
            }
        }
        /// <summary>
        /// </summary>
        public void DisplayIdel_StateTWO(string graphname)
        {
            looptimes = 0;
            CountNomal++;
            Display(graphname, AnimatType.A_Start, GraphType.StateTWO, DisplayIdel_StateTWOing);
        }
        /// <summary>
        /// </summary>
        private void DisplayIdel_StateTWOing(string graphname)
        {
            if (Function.Rnd.Next(++looptimes) > Core.Graph.GraphConfig.GetDuration(graphname))
            {
                looptimes = 0;
                Display(graphname, AnimatType.C_End, GraphType.StateTWO, DisplayIdel_StateONEing);
            }
            else
            {
                Display(graphname, AnimatType.B_Loop, GraphType.StateTWO, DisplayIdel_StateTWOing);
            }
        }

        int looptimes;
        /// <summary>
        /// </summary>
        public bool DisplayToIdel()
        {
            if (Core.Graph.GraphsName.TryGetValue(GraphType.Idel, out var gl))
            {
                var list = gl.ToList();
                for (int i = Function.Rnd.Next(list.Count); 0 != list.Count; i = Function.Rnd.Next(list.Count))
                {
                    var idelname = list[i];
                    var ig = Core.Graph.FindGraphs(idelname, AnimatType.A_Start, Core.Save.Mode);
                    if (ig != null && ig.Count != 0)
                    {
                        looptimes = 0;
                        CountNomal = 0;
                        Display(ig[Function.Rnd.Next(ig.Count)], () =>
                        DisplayBLoopingToNomal(idelname, Core.Graph.GraphConfig.GetDuration(idelname)));
                        return true;
                    }
                    else
                    {
                        ig = Core.Graph.FindGraphs(idelname, AnimatType.Single, Core.Save.Mode);
                        if (ig != null && ig.Count != 0)
                        {
                            looptimes = 0;
                            CountNomal = 0;
                            Display(ig[Function.Rnd.Next(ig.Count)], DisplayToNomal);
                            return true;
                        }
                        list.RemoveAt(i);
                    }
                }
                return false;
            }
            else
                return false;
        }
        /// <summary>
        /// </summary>
        public Action<string> DisplayBLoopingToNomal(int looplength) => (gn) => DisplayBLoopingToNomal(gn, looplength);
        /// <summary>
        /// </summary>
        public void DisplayBLoopingToNomal(string graphname, int loopLength)
        {
            if (Function.Rnd.Next(++looptimes) > loopLength)
                DisplayCEndtoNomal(graphname);
            else
                Display(graphname, AnimatType.B_Loop, DisplayBLoopingToNomal(loopLength));
        }


        /// <summary>
        /// </summary>
        public void DisplaySleep(bool force = false)
        {
            looptimes = 0;
            CountNomal = 0;
            if (force)
            {
                State = WorkingState.Sleep;
                Display(GraphType.Sleep, AnimatType.A_Start, DisplayBLoopingForce);
            }
            else
                Display(GraphType.Sleep, AnimatType.A_Start, (x) => DisplayBLoopingToNomal(x, Core.Graph.GraphConfig.GetDuration(x)));
        }
        /// <summary>
        /// </summary>
        public void DisplayBLoopingForce(string graphname)
        {
            Display(graphname, AnimatType.B_Loop, DisplayBLoopingForce);
        }


        /// <summary>
        /// </summary>
        public void DisplayRaised()
        {
            MainGrid.MouseMove -= MainGrid_MouseWave;
            MainGrid.MouseMove -= MainGrid_MouseMove;
            MainGrid.MouseMove += MainGrid_MouseMove;

            var mp = Dispatcher.Invoke(() => Mouse.GetPosition(MainGrid));
            var x = mp.X - Core.Graph.GraphConfig.RaisePoint[(int)Core.Save.Mode].X;
            var y = mp.Y - Core.Graph.GraphConfig.RaisePoint[(int)Core.Save.Mode].Y;
            if (Math.Abs(x) < 1)
                x = 0;
            if (Math.Abs(y) < 1)
                y = 0;
            Core.Controller.MoveWindows(x, y);
            rasetype = 0;
            DisplayRaising();
        }
        int rasetype = int.MinValue;
        /// <summary>
        /// </summary>
        private void DisplayRaising(string name = null)
        {
            Console.WriteLine(rasetype);
            switch (rasetype)
            {
                case int.MinValue:
                    break;
                case -1:
                    rasetype = int.MinValue;
                    Core.Controller.RePositionActive = !Core.Controller.CheckPosition();
                    if(!MoveSideHideCheck())
                    {
                        if (string.IsNullOrEmpty(name))
                            Display(GraphType.Raised_Static, AnimatType.C_End, DisplayToNomal);
                        else
                            Display(name, AnimatType.C_End, GraphType.Raised_Static, DisplayToNomal);
                    }
                   
                    return;
                case 0:
                case 1:
                case 2:
                    rasetype++;
                    if (string.IsNullOrEmpty(name))
                        Display(GraphType.Raised_Dynamic, AnimatType.Single, DisplayRaising);
                    else
                        Display(name, AnimatType.Single, GraphType.Raised_Dynamic, DisplayRaising);
                    return;
                case 3:
                    rasetype++;
                    if (string.IsNullOrEmpty(name))
                        Display(name, AnimatType.A_Start, DisplayRaising);
                    else
                        Display(name, AnimatType.A_Start, GraphType.Raised_Static, DisplayRaising);
                    return;
                default:
                    rasetype = 4;
                    if (string.IsNullOrEmpty(name))
                        Display(name, AnimatType.B_Loop, DisplayRaising);
                    else
                        Display(name, AnimatType.B_Loop, GraphType.Raised_Static, DisplayRaising);
                    return;
            }
        }

        /// <summary>
        /// </summary>
        public void DisplayCEndtoNomal(string graphname)
        {
            Display(graphname, AnimatType.C_End, DisplayToNomal);
        }




        /// <summary>
        /// </summary>
        public void Display(GraphType Type, AnimatType animat, Action<string> EndAction = null)
        {
            var name = Core.Graph.FindName(Type);
            Display(name, animat, EndAction);
        }
        /// <summary>
        /// </summary>
        public void Display(string name, AnimatType animat, Action<string> EndAction)
        {
            Display(Core.Graph.FindGraph(name, animat, Core.Save.Mode), new Action(() => EndAction.Invoke(name)));
        }
        /// <summary>
        /// </summary>
        public void Display(string name, AnimatType animat, GraphType Type, Action<string> EndAction = null)
        {
            var list = Core.Graph.FindGraphs(name, animat, Core.Save.Mode)?.FindAll(x => x.GraphInfo.Type == Type);
            if ((list?.Count ?? -1) > 0)
                Display(list[Function.Rnd.Next(list.Count)], () => EndAction(name));
            else
                Display(Type, animat, EndAction);
        }
        /// <summary>
        /// </summary>
        public void Display(string name, AnimatType animat, GraphType Type, Action EndAction = null)
        {
            var list = Core.Graph.FindGraphs(name, animat, Core.Save.Mode)?.FindAll(x => x.GraphInfo.Type == Type);
            if ((list?.Count ?? -1) > 0)
                Display(list[Function.Rnd.Next(list.Count)], EndAction);
            else
                Display(Type, animat, EndAction);
        }

        /// <summary>
        /// </summary>
        public void Display(GraphType Type, AnimatType animat, Action EndAction = null)
        {
            var name = Core.Graph.FindName(Type);
            Display(name, animat, EndAction);
        }
        /// <summary>
        /// </summary>
        public void Display(string name, AnimatType animat, Action EndAction = null)
        {
            Display(Core.Graph.FindGraph(name, animat, Core.Save.Mode), EndAction);
        }
        bool petgridcrlf = true;
        int nodisplayLoop = 0;
        /// <summary>
        /// </summary>
        public event Action<GraphInfo> GraphDisplayHandler;
        /// <summary>
        /// </summary>
        public void Display(IGraph graph, Action EndAction = null)
        {
            if (graph == null)
            {
                if (nodisplayLoop++ > 20)
                {
                    if (nodisplayLoop < 100)
                        Display(GraphType.Default, AnimatType.Single, EndAction);
                    else
                    {
                        Dispatcher.Invoke(() =>
                        {
                            LabelDisplayText.Text = "No playable animation found, Stopped running VPET Module".Translate();
                            LabelDisplay.Visibility = Visibility.Visible;
                            IsEnabled = false;
                        });
                    }
                }
                else
                    EndAction?.Invoke();
                return;
            }
            else
            {
                nodisplayLoop = 0;
            }
#if DEBUG
            Debug.WriteLine(LPSConvert.SerializeObject(graph.GraphInfo, "DISPLAY" + DateTime.Now.Minute, convertNoneLineAttribute: true).ToString());
#endif
            //if(graph.GraphType == GraphType.Climb_Up_Left)
            //{
            //    Dispatcher.Invoke(() => Say(graph.GraphType.ToString()));
            //}
            DisplayType = graph.GraphInfo;
            GraphDisplayHandler?.Invoke(graph.GraphInfo);
            var PetGridTag = Dispatcher.Invoke(() => PetGrid.Tag);
            var PetGrid2Tag = Dispatcher.Invoke(() => PetGrid2.Tag);
            if (graph.Equals(PetGridTag))
            {
                petgridcrlf = true;
                if (PetGrid2Tag is IGraph ig)
                    ig.Stop(true);
                Dispatcher.Invoke(() =>
                {
                    PetGrid.Visibility = Visibility.Visible;
                    PetGrid2.Visibility = Visibility.Hidden;
                });
                graph.Run(PetGrid, EndAction);//(x) => PetGrid.Child = x
                return;
            }
            else if (graph.Equals(PetGrid2Tag))
            {
                petgridcrlf = false;
                if (PetGridTag is IGraph ig)
                    ig.Stop(true);
                Dispatcher.Invoke(() =>
                {
                    PetGrid2.Visibility = Visibility.Visible;
                    PetGrid.Visibility = Visibility.Hidden;
                });
                graph.Run(PetGrid2, EndAction);
                return;
            }

            if (petgridcrlf)
            {
                graph.Run(PetGrid2, EndAction);
                ((IGraph)(PetGridTag)).Stop(true);
                Dispatcher.Invoke(() =>
                {
                    PetGrid.Visibility = Visibility.Hidden;
                    PetGrid2.Visibility = Visibility.Visible;
                    //PetGrid2.Tag = graph;
                });
            }
            else
            {
                graph.Run(PetGrid, EndAction);
                ((IGraph)(PetGrid2Tag)).Stop(true);
                Dispatcher.Invoke(() =>
                {
                    PetGrid2.Visibility = Visibility.Hidden;
                    PetGrid.Visibility = Visibility.Visible;
                    //PetGrid.Tag = graph;
                });
            }
            petgridcrlf = !petgridcrlf;
            GC.Collect();
        }
        /// <summary>
        /// </summary>
        public Decorator FindDisplayBorder(IGraph graph)
        {
            DisplayType = graph.GraphInfo;
            var PetGridTag = Dispatcher.Invoke(() => PetGrid.Tag);
            var PetGrid2Tag = Dispatcher.Invoke(() => PetGrid2.Tag);
            if (PetGridTag == graph)
            {
                petgridcrlf = true;
                ((IGraph)(PetGrid2Tag)).Stop(true);
                Dispatcher.Invoke(() =>
                {
                    PetGrid.Visibility = Visibility.Visible;
                    PetGrid2.Visibility = Visibility.Hidden;
                });
                return PetGrid;
            }
            else if (PetGrid2Tag == graph)
            {
                petgridcrlf = false;
                ((IGraph)(PetGridTag)).Stop(true);
                Dispatcher.Invoke(() =>
                {
                    PetGrid2.Visibility = Visibility.Visible;
                    PetGrid.Visibility = Visibility.Hidden;
                });
                return PetGrid2;
            }

            if (petgridcrlf)
            {
                ((IGraph)(PetGridTag)).Stop(true);
                Dispatcher.Invoke(() =>
                {
                    PetGrid.Visibility = Visibility.Hidden;
                    PetGrid2.Visibility = Visibility.Visible;
                    //PetGrid2.Tag = graph;
                });
                petgridcrlf = !petgridcrlf;
                GC.Collect();
                return PetGrid2;
            }
            else
            {
                ((IGraph)(PetGrid2Tag)).Stop(true);
                Dispatcher.Invoke(() =>
                {
                    PetGrid2.Visibility = Visibility.Hidden;
                    PetGrid.Visibility = Visibility.Visible;
                    //PetGrid.Tag = graph;
                });
                petgridcrlf = !petgridcrlf;
                GC.Collect();
                return PetGrid;
            }

        }



        /// <summary>
        /// </summary>
        public void Display(GraphType Type, ImageSource img, Action EndAction)
        {
            var name = Core.Graph.FindName(Type);
            var ig = Core.Graph.FindGraph(name, AnimatType.Single, Core.Save.Mode);
            if (ig != null)
            {
                var b = FindDisplayBorder(ig);
                ig.Run(b, img, EndAction);
            }
        }
        /// <summary>
        /// </summary>
        public void Display(string name, ImageSource img, Action EndAction)
        {
            var ig = Core.Graph.FindGraph(name, AnimatType.Single, Core.Save.Mode);
            if (ig != null)
            {
                var b = FindDisplayBorder(ig);
                ig.Run(b, img, EndAction);
            }
        }
    }
}
