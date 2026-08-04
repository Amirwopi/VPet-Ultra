using LinePutScript.Dictionary;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using VPet_Simulator.Core;
using static VPet_Simulator.Windows.Interface.ScheduleTask;

namespace VPet_Simulator.Windows.Interface
{
    /// <summary>
    /// </summary>
    public interface IMainWindow
    {
        /// <summary>
        /// </summary>
        string PrefixSave { get; }
        /// <summary>
        /// </summary>
        LPS_D Args { get; }
        /// <summary>
        /// </summary>
        ISetting Set { get; }
        /// <summary>
        /// </summary>
        List<PetLoader> Pets { get; }
        /// <summary>
        /// </summary>
        List<ITalkAPI> TalkAPI { get; }
        /// <summary>
        /// </summary>
        ITalkAPI TalkBoxCurr { get; }
        /// <summary>
        /// </summary>
        GameCore Core { get; }
        /// <summary>
        /// </summary>
        Main Main { get; }
        /// <summary>
        /// </summary>
        int version { get; }
        /// <summary>
        /// </summary>
        string Version { get; }
        /// <summary>
        /// </summary>
        long lastclicktime { get; set; }
        /// <summary>
        /// </summary>
        List<MainPlugin> Plugins { get; }
        /// <summary>
        /// </summary>
        List<Food> Foods { get; }
        /// <summary>
        /// </summary>
        List<LowText> LowFoodText { get; }
        /// <summary>
        /// </summary>
        List<LowText> LowDrinkText { get; }
        /// <summary>
        /// </summary>
        List<ClickText> ClickTexts { get; }
        /// <summary>
        /// </summary>
        List<SelectText> SelectTexts { get; }
        /// <summary>
        /// </summary>
        /// <returns>SpeakContent</returns>
        ClickText GetClickText();
        /// <summary>
        /// </summary>
        List<Photo> Photos { get; }
        /// <summary>
        /// </summary>
        ImageResources ImageSources { get; }
        /// <summary>
        /// </summary>
        Resources FileSources { get; }
        /// <summary>
        /// </summary>
        void SetZoomLevel(double zl);
        /// <summary>
        /// Save the Settings
        /// </summary>
        void Save();
        /// <summary>
        /// LoadDIYContent
        /// </summary>
        void LoadDIY();
        /// <summary>
        /// </summary>
        /// <param name="page">SettingsPage</param>
        void ShowSetting(int page = -1);
        /// <summary>
        /// </summary>
        /// <param name="type">FoodType</param>
        void ShowBetterBuy(Food.FoodType type);
        /// <summary>
        /// </summary>
        void ShowGallery();
        /// <summary>
        /// </summary>
        void Close();
        /// <summary>
        /// Restart VPet
        /// </summary>
        void Restart();
        /// <summary>
        /// Click Through Window
        /// </summary>
        bool MouseHitThrough { get; set; }

        /// <summary>
        /// </summary>
        bool HashCheck { get; }

        /// <summary>
        /// </summary>
        float AudioPlayingVolume();
        /// <summary>
        /// </summary>
        bool CloseConfirm { get; }
        /// <summary>
        /// </summary>
        void HashCheckOff();
        /// <summary>
        /// </summary>
        List<Window> Windows { get; set; }
        /// <summary>
        /// </summary>
        GameSave_v2 GameSavesData { get; }
        /// <summary>
        /// </summary>
        Grid MGHost { get; }
        /// <summary>
        /// </summary>
        Grid PetGrid { get; }

        /// <summary>
        /// </summary>
        void DisplayFoodAnimation(string graphName, ImageSource imageSource);
        /// <summary>
        /// </summary>
        void TakeItem(Food item);

        /// <summary>
        /// </summary>
        void ShowInputBox(string title, string text, string defaulttext, Action<string> ENDAction, bool AllowMutiLine = false, bool TextCenter = true, bool CanHide = false);
        /// <summary>
        /// </summary>
        Dispatcher Dispatcher { get; }
        /// <summary>
        /// </summary>
        IEnumerable<IModInfo> ModInfo { get; }
        /// <summary>
        /// </summary>
        IEnumerable<IModInfo> OnModInfo { get; }

        /// <summary>
        /// </summary>
        List<DirectoryInfo> MODPath { get; }
        /// <summary>
        /// Schedule
        /// </summary>
        ScheduleTask ScheduleTask { get; }
        /// <summary>
        /// </summary>
        List<PackageFull> SchedulePackage { get; }
        /// <summary>
        /// </summary>
        event Action<Food> Event_TakeItem;

        /// <summary>
        /// </summary>
        event Action Event_NewDay;

        /// <summary>
        /// </summary>
        Dictionary<string, object> DynamicResources { get; }

        /// <summary>
        /// </summary>
        /// <param name="from">Source</param>
        public void TakeItemHandle(Food item, int count, string from);

        /// <summary>
        /// </summary>
        public ObservableCollection<ActivityLog> ActivityLogs { get; }

        /// <summary>
        /// </summary>
        public List<Item> Items { get; }

        /// <summary>
        /// </summary>
        public void ItemsAdd(Item item);
    }
}
