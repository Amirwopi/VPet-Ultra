using System;
using System.Windows.Controls;
using System.Windows.Media;

namespace VPet_Simulator.Core
{
    /// <summary>
    /// </summary>
    public interface IGraph : IEquatable<object>, IDisposable
    {
        /// <summary>
        /// </summary>
        /// <param name="EndAction">Stop Action</param>
        void Run(Decorator parant, Action EndAction = null);

        /// <summary>
        /// </summary>
        bool IsLoop { get; set; }

        /// <summary>
        /// </summary>
        bool IsReady { get; }

        /// <summary>
        /// </summary>
        bool IsFail { get; }
        /// <summary>
        /// </summary>
        string FailMessage { get; }
        /// <summary>
        /// </summary>
        GraphInfo GraphInfo { get; }

        /// <summary>
        /// </summary>
        TaskControl Control { get; }

        /// <summary>
        /// </summary>
        void Stop(bool StopEndAction)
        {
            if (Control == null)
                return;
            if (StopEndAction)
                Control.EndAction = null;
            Control.Type = TaskControl.ControlType.Stop;
        }
        /// <summary>
        /// </summary>
        void SetContinue()
        {
            Control.Type = TaskControl.ControlType.Continue;
        }

        /// <summary>
        /// </summary>
        string Path { get; }
        /// <summary>
        /// </summary>
        long LastUseTimeTicks => 0;

        /// <summary>
        /// </summary>
        void Touch() { }
        /// <summary>
        /// </summary>
        void CleanupIdleCache(long nowTicks) { }

        /// <summary>
        /// </summary>
        public interface IRunImage : IGraph
        {
            /// <summary>
            /// </summary>
            void Run(Decorator parant, ImageSource image, Action EndAction = null);
        }

        /// <summary>
        /// </summary>
        public class TaskControl
        {
            /// <summary>
            /// </summary>
            public bool PlayState => Type != ControlType.Status_Stoped && Type != ControlType.Stop;
            /// <summary>
            /// </summary>
            public void SetContinue() { Type = ControlType.Continue; }
            /// <summary>
            /// </summary>
            public void Stop(Action endAction = null) { EndAction = endAction; Type = ControlType.Stop; }
            /// <summary>
            /// </summary>
            public enum ControlType
            {
                /// <summary>
                /// </summary>
                Status_Quo,
                /// <summary>
                /// </summary>
                Stop,
                /// <summary>
                /// </summary>
                Continue,
                /// <summary>
                /// </summary>
                Status_Stoped,
            }
            /// <summary>
            /// </summary>
            public Action EndAction;
            /// <summary>
            /// </summary>
            public ControlType Type = ControlType.Status_Quo;
            /// <summary>
            /// </summary>
            /// <param name="endAction"></param>
            public TaskControl(Action endAction = null)
            {
                EndAction = endAction;
            }
        }
    }
}
