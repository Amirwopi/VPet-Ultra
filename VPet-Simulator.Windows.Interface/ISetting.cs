using LinePutScript;
using System;
using System.Windows;

namespace VPet_Simulator.Windows.Interface
{
    /// <summary>
    /// </summary>
    public interface ISetting
    {
        /// <summary>
        /// </summary>
        double ZoomLevel { get; }

        /// <summary>
        /// </summary>
        /// <param name="Level">Zoom Level</param>
        void SetZoomLevel(double level);

        /// <summary>
        /// </summary>
        double VoiceVolume { get; }

        /// <summary>
        /// </summary>
        void SetVoiceVolume(double volume);

        /// <summary>
        /// </summary>
        int AutoSaveInterval { get; }

        /// <summary>
        /// </summary>
        void SetAutoSaveInterval(int interval);

        /// <summary>
        /// </summary>
        int BackupSaveMaxNum { get; set; }

        /// <summary>
        /// </summary>
        bool TopMost { get; }

        /// <summary>
        /// SettingsYesNoAlways On Top
        /// </summary>
        void SetTopMost(bool topMost);

        /// <summary>
        /// </summary>
        DateTime LastCacheDate { get; set; }

        /// <summary>
        /// </summary>
        string Language { get; }

        /// <summary>
        /// </summary>
        void SetLanguage(string language);

        /// <summary>
        /// </summary>
        int PressLength { get; set; }

        /// <summary>
        /// </summary>
        int InteractionCycle { get; set; }

        /// <summary>
        /// </summary>
        double LogicInterval { get; }

        /// <summary>
        /// SettingsComputation interval（Sec）
        /// </summary>
        /// <param name="interval">Computation interval</param>
        void SetLogicInterval(double interval);

        /// <summary>
        /// </summary>
        bool AllowMove { get; }

        /// <summary>
        /// </summary>
        void SetAllowMove(bool allowMove);

        /// <summary>
        /// </summary>
        bool SmartMove { get; }

        /// <summary>
        /// </summary>
        void SetSmartMove(bool smartMove);

        /// <summary>
        /// </summary>
        bool EnableFunction { get; }

        /// <summary>
        /// </summary>
        void SetEnableFunction(bool enableFunction);

        /// <summary>
        /// </summary>
        int SmartMoveInterval { get; }

        /// <summary>
        /// </summary>
        void SetSmartMoveInterval(int interval);

        /// <summary>
        /// </summary>
        bool MessageBarOutside { get; set; }

        /// <summary>
        /// </summary>
        bool StartRecordLast { get; set; }

        /// <summary>
        /// </summary>
        Point StartRecordLastPoint { get; }

        /// <summary>
        /// </summary>
        Point StartRecordPoint { get; set; }

        /// <summary>
        /// </summary>
        double MusicCatch { get; set; }

        /// <summary>
        /// </summary>
        double MusicMax { get; set; }

        /// <summary>
        /// </summary>
        int Resolution { get; set; }

        /// <summary>
        /// </summary>
        bool AutoBuy { get; set; }

        /// <summary>
        /// </summary>
        bool AutoGift { get; set; }

        /// <summary>
        /// </summary>
        bool HideFromTaskControl { get; set; }

        /// <summary>
        /// </summary>
        ILine this[string lineName] { get; set; }

        /// <summary>
        /// </summary>
        bool MPNOTouch { get; set; }
        /// <summary>
        /// </summary>
        string PetGraph { get; }
        /// <summary>
        /// </summary>
        bool DeBug { get; set; }
    }

}
