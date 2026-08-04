using LinePutScript;
using System;
using System.IO;
using System.Linq;
using static VPet_Simulator.Core.GraphHelper;

namespace VPet_Simulator.Core
{
    /// <summary>
    /// </summary>
    public class GraphInfo
    {
        /// <summary>
        /// </summary>
        public GraphInfo()
        {

        }
        /// <summary>
        /// </summary>
        public GraphInfo(string name, GraphType type = GraphType.Common, AnimatType animat = AnimatType.Single, IGameSave.ModeType modeType = IGameSave.ModeType.Nomal)
        {
            Name = name;
            Animat = animat;
            Type = type;
            ModeType = modeType;
        }
        /// <summary>
        /// </summary>
        public GraphInfo(FileSystemInfo path, ILine info)
        {
            string pn;
            if (path is DirectoryInfo)
                pn = Sub.Split(path.FullName.ToLowerInvariant(), info[(gstr)"startuppath"].ToLowerInvariant()).Last();
            else
                pn = Sub.Split(path.FullName.Substring(0, path.FullName.Length - path.Extension.Length).ToLowerInvariant(), info[(gstr)"startuppath"].ToLowerInvariant()).Last();

            var path_name = pn.Replace('\\', '_').Split('_').ToList();
            path_name.RemoveAll(string.IsNullOrWhiteSpace);
            if (!Enum.TryParse(info[(gstr)"mode"], true, out IGameSave.ModeType modetype))
            {
                if (path_name.Remove("happy"))
                {
                    modetype = IGameSave.ModeType.Happy;
                }
                else if (path_name.Remove("nomal"))
                {
                    modetype = IGameSave.ModeType.Nomal;
                }
                else if (path_name.Remove("poorcondition"))
                {
                    modetype = IGameSave.ModeType.PoorCondition;
                }
                else if (path_name.Remove("ill"))
                {
                    modetype = IGameSave.ModeType.Ill;
                }
                else
                {
                    modetype = IGameSave.ModeType.Nomal;
                }
            }

            if (!Enum.TryParse(info[(gstr)"graph"], true, out GraphType graphtype))
            {
                graphtype = GraphInfo.GraphType.Common;
                for (int i = 0; i < GraphTypeValue.Length; i++)
                {
                    if (path_name.Contains(GraphTypeValue[i][0]))
                    {
                        int index = path_name.IndexOf(GraphTypeValue[i][0]);
                        bool ismatch = true;
                        for (int b = 1; b < GraphTypeValue[i].Length && b + index < path_name.Count; b++)
                        {
                            if (path_name[index + b] != GraphTypeValue[i][b])
                            {
                                ismatch = false;
                                break;
                            }
                        }
                        if (ismatch)
                        {
                            graphtype = (GraphType)i;
                            path_name.RemoveRange(index, GraphTypeValue[i].Length);
                            break;
                        }
                    }
                }
            }

            if (!Enum.TryParse(info[(gstr)"animat"], true, out AnimatType animatType))
            {
                if (path_name.Remove("a") || path_name.Remove("start"))
                {
                    animatType = AnimatType.A_Start;
                }
                else if (path_name.Remove("b") || path_name.Remove("loop"))
                {
                    animatType = AnimatType.B_Loop;
                }
                else if (path_name.Remove("c") || path_name.Remove("end"))
                {
                    animatType = AnimatType.C_End;
                }
                else if (path_name.Remove("single"))
                {
                    animatType = AnimatType.Single;
                }
                else
                {
                    animatType = AnimatType.Single;
                }
            }
            Name = info.Info;
            if (string.IsNullOrWhiteSpace(Name))
            {
                while (path_name.Count > 0 && (double.TryParse(path_name.Last(), out _) || path_name.Last().StartsWith("~")))
                {
                    path_name.RemoveAt(path_name.Count - 1);
                }
                if (path_name.Count > 0)
                    Name = path_name.Last();
            }
            if (string.IsNullOrWhiteSpace(Name))
            {
                Name = graphtype.ToString().ToLowerInvariant();
            }
            Type = graphtype;
            Animat = animatType;
            ModeType = modetype;
        }
        /// <summary>
        /// </summary>
        public enum GraphType
        {
            /// <summary>
            /// </summary>
            Common,
            /// <summary>
            /// </summary>
            Raised_Dynamic,
            /// <summary>
            /// </summary>
            Raised_Static,
            /// <summary>
            /// </summary>
            Move,
            /// <summary>
            /// </summary>
            Default,
            /// <summary>
            /// </summary>
            Touch_Head,
            /// <summary>
            /// </summary>
            Touch_Body,
            /// <summary>
            /// </summary>
            Idel,
            /// <summary>
            /// </summary>
            Sleep,
            /// <summary>
            /// </summary>
            Say,
            /// <summary>
            /// </summary>
            StateONE,
            /// <summary>
            /// </summary>
            StateTWO,
            /// <summary>
            /// </summary>
            StartUP,
            /// <summary>
            /// </summary>
            Shutdown,
            /// <summary>
            /// </summary>
            Work,
            /// <summary>
            /// </summary>
            Switch_Up,
            /// <summary>
            /// </summary>
            Switch_Down,
            /// <summary>
            /// </summary>
            Switch_Thirsty,
            /// <summary>
            /// </summary>
            Switch_Hunger,
            /// <summary>
            /// </summary>
            SideHide_Left_Main,
            /// <summary>
            /// </summary>
            SideHide_Left_Rise,
            /// <summary>
            /// </summary>
            SideHide_Right_Main,
            /// <summary>
            /// </summary>
            SideHide_Right_Rise,
        }
        /// <summary>
        /// </summary>
        public enum AnimatType
        {
            /// <summary>
            /// </summary>
            Single,
            /// <summary>
            /// </summary>
            A_Start,
            /// <summary>
            /// </summary>
            B_Loop,
            /// <summary>
            /// </summary>
            C_End,
        }
        /// <summary>
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// </summary>
        public AnimatType Animat { get; set; }
        /// <summary>
        /// </summary>
        public GraphType Type { get; set; }
        /// <summary>
        /// </summary>
        public IGameSave.ModeType ModeType { get; set; }
        ///// <summary>
        ///// </summary>
        //public ILine Info { get; set; }
        public override string ToString()
        {
            return $"[{Name}]{Type}_{ModeType.ToString()[0]}{Animat.ToString()[0]}]";
        }
    }
}
