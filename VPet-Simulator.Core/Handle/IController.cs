namespace VPet_Simulator.Core
{
    /// <summary>
    /// </summary>
    public interface IController
    {
        /// <summary>
        /// </summary>
        /// <param name="X">X-axis</param>
        /// <param name="Y">Y-axis</param>
        void MoveWindows(double X, double Y);

        /// <summary>
        /// </summary>
        double GetWindowsDistanceLeft();
        /// <summary>
        /// </summary>
        double GetWindowsDistanceRight();
        /// <summary>
        /// </summary>
        double GetWindowsDistanceUp();
        /// <summary>
        /// </summary>
        double GetWindowsDistanceDown();
        /// <summary>
        /// </summary>
        /// <returns></returns>
        bool IfInActivateScreen() => true;
        /// <summary>
        /// </summary>
        void SetNowScreenActivate() { }
        ///// <summary>
        ///// </summary>
        //double WindowsWidth { get; set; }
        ///// <summary>
        ///// </summary>
        //double WindowsHight { get; set; }
        /// <summary>
        /// </summary>
        double ZoomRatio { get; }
        /// <summary>
        /// </summary>
        int PressLength { get; }

        /// <summary>
        /// </summary>
        void ShowPanel();

        /// <summary>
        /// </summary>
        void ResetPosition();
        /// <summary>
        /// </summary>
        bool CheckPosition();

        /// <summary>
        /// </summary>
        bool EnableFunction { get; }
        /// <summary>
        /// Interaction Cycles
        /// </summary>
        int InteractionCycle { get; }

        /// <summary>
        /// </summary>
        bool RePositionActive { get; set; }

        /// <summary>
        /// </summary>
        bool AutoChangeWindow => false;
    }
}
