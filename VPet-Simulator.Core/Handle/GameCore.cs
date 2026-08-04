using System;
using System.Collections.Generic;
using System.Windows;

namespace VPet_Simulator.Core
{
    /// <summary>
    /// </summary>
    public class GameCore
    {
        /// <summary>
        /// </summary>
        public IController Controller;
        /// <summary>
        /// </summary>
        public List<TouchArea> TouchEvent = new List<TouchArea>();
        /// <summary>
        /// </summary>
        public GraphCore Graph;
        /// <summary>
        /// </summary>
        public IGameSave Save;
    }
    /// <summary>
    /// </summary>
    public class TouchArea
    {
        /// <summary>
        /// </summary>
        public Point Locate;
        /// <summary>
        /// </summary>
        public Size Size;
        /// <summary>
        /// </summary>
        public Func<bool> DoAction;
        /// <summary>
        /// </summary>
        public bool IsPress;
        /// <summary>
        /// </summary>
        public TouchArea(Point locate, Size size, Func<bool> doAction, bool isPress = false)
        {
            Locate = locate;
            Size = size;
            DoAction = doAction;
            IsPress = isPress;
        }
        /// <summary>
        /// </summary>
        public bool Touch(Point point)
        {
            double inx = point.X - Locate.X;
            double iny = point.Y - Locate.Y;
            return inx >= 0 && inx <= Size.Width && iny >= 0 && iny <= Size.Height;
        }
    }
}
