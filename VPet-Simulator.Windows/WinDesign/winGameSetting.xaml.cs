using LinePutScript;
using LinePutScript.Localization.WPF;
using NAudio.SoundFont;
using Panuon.WPF.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using VPet_Simulator.Core;
using VPet_Simulator.Windows.Interface;
using static VPet_Simulator.Windows.Win32;

namespace VPet_Simulator.Windows
{
    /// <summary>
    /// </summary>
    public partial class winGameSetting : WindowX
    {
        MainWindow mw;
        private bool AllowChange = false;

        public winGameSetting(MainWindow mw)
        {
            this.mw = mw;
            //Console.WriteLine(DateTime.Now.ToString("mm:ss.fff"));
            InitializeComponent();
            //Console.WriteLine(DateTime.Now.ToString("mm:ss.fff"));
            //var bit = new BitmapImage(new Uri("pack://application:,,,/Res/TopLogo2019.png"));
            //Console.WriteLine(DateTime.Now.ToString("mm:ss.fff"));
            ////ImageWHY.Source = bit;
            //Console.WriteLine(DateTime.Now.ToString("mm:ss.fff"));

            Title = "Settings".Translate() + ' ' + mw.PrefixSave;
            SettingMenuWidth.Width = new GridLength(LocalizeCore.GetDouble("SettingMenuWidth", 150));
            TopMostBox.IsChecked = mw.Set.TopMost;
            if (mw.Set.IsBiggerScreen)
            {
                FullScreenBox.IsChecked = true;
                ZoomSlider.Maximum = 8;
            }
            else
            {
                FullScreenBox.IsChecked = false;
                ZoomSlider.Maximum = 3;
            }
            ZoomSlider.Value = mw.Set.ZoomLevel * 2;
            //this.Width = 400 * Math.Sqrt(ZoomSlider.Value);
            //this.Height = 450 * Math.Sqrt(ZoomSlider.Value);

            CalFunctionBox.IsChecked = mw.Set.EnableFunction;
            CalSlider.Value = mw.Set.LogicInterval;
            InteractionSlider.Value = mw.Set.InteractionCycle;
            MoveEventBox.IsChecked = mw.Set.AllowMove;
            SmartMoveEventBox.IsChecked = mw.Set.SmartMove;
            AutoChangeWindowEvent.IsChecked = mw.Set.AutoChangeWindow;
            PressLengthSlider.Value = mw.Set.PressLength / 1000.0;
            SwitchMsgOut.IsChecked = mw.Set.MessageBarOutside;
            SwitchHideFromTaskControl.IsChecked = mw.Set.HideFromTaskControl;
            ConsoleBox.IsChecked = mw.Set.DeBug;

            StartUpBox.IsChecked = mw.Set.StartUPBoot;
            TextBoxPetName.Text = mw.Core.Save.Name;
            foreach (PetLoader pl in mw.Pets)
            {
                PetBox.Items.Add(pl.Name.Translate());
            }
            int petboxid = mw.Pets.FindIndex(x => x.Name == mw.Set.PetGraph);
            if (petboxid == -1)
                petboxid = 0;
            PetBox.SelectedIndex = petboxid;
            petboxbef = petboxid;
            PetIntor.Text = mw.Pets[petboxid].Intor.Translate();

            TextBoxStartUpX.Text = mw.Set.StartRecordPoint.X.ToString();
            TextBoxStartUpY.Text = mw.Set.StartRecordPoint.Y.ToString();
            numBackupSaveMaxNum.Value = mw.Set.BackupSaveMaxNum;
            combCalFunState.SelectedIndex = (int)mw.Set.CalFunState;
            combCalFunState.IsEnabled = !mw.Set.EnableFunction;

            TextBoxHostBDay.MaxDateTime = DateTime.Now;
            TextBoxHostBDay.SelectedDateTime = mw.GameSavesData.GetDateTime("HostBDay", mw.GameSavesData[(gdat)"birthday"]);
            TextBoxHostName.Text = mw.GameSavesData.GameSave.HostName;

            CalTimeInteraction();

            swAutoCal.IsChecked = !mw.Set["gameconfig"].GetBool("noAutoCal");

            LoadMutiUI();

            LanguageBox.Items.Add("null");
            foreach (string v in LocalizeCore.AvailableCultures)
            {
                LanguageBox.Items.Add(v);
            }
            LanguageBox.SelectedItem = LocalizeCore.CurrentCulture;

            HitThroughBox.IsChecked = mw.Set.HitThrough;
            PetHelperBox.IsChecked = mw.Set.PetHelper;

            if (mw.Set.StartRecordLast == true)
            {
                StartPlace.IsChecked = true;
                TextBoxStartUpX.IsEnabled = false;
                TextBoxStartUpY.IsEnabled = false;
                BtnStartUpGet.IsEnabled = false;
            }
            else
            {
                StartPlace.IsChecked = false;
                TextBoxStartUpX.IsEnabled = true;
                TextBoxStartUpY.IsEnabled = true;
                BtnStartUpGet.IsEnabled = true;
            }

            if (mw.Set.Diagnosis)
                RBDiagnosisYES.IsChecked = true;
            else
                RBDiagnosisNO.IsChecked = true;

            List<int> cbDiagnosis = new List<int> { 200, 500, 1000, 2000, 5000, 10000, 20000 };
            int ds = cbDiagnosis.IndexOf(mw.Set.DiagnosisInterval);
            if (ds == -1)
                ds = 1;
            CBDiagnosis.SelectedIndex = ds;

            foreach (ComboBoxItem v in CBAutoSave.Items)
            {
                if ((int)v.Tag == mw.Set.AutoSaveInterval)
                {
                    CBAutoSave.SelectedItem = v;
                    break;
                }
            }
            foreach (ComboBoxItem v in CBSmartMove.Items)
            {
                if ((int)v.Tag == mw.Set.SmartMoveInterval)
                {
                    CBSmartMove.SelectedItem = v;
                    break;
                }
            }

            foreach (var v in mw.Fonts)
            {
                FontBox.Items.Add(v.TranslateName);
            }
            FontBox.SelectedIndex = mw.Fonts.FindIndex(x => x.Name == mw.Set.Font);

            foreach (var v in mw.Themes)
            {
                ThemeBox.Items.Add(v.TranslateName);
            }
            if (mw.Theme != null)
                ThemeBox.SelectedItem = mw.Theme.TranslateName;

            VoiceCatchSilder.Value = mw.Set.MusicCatch;
            VoiceMaxSilder.Value = mw.Set.MusicMax;

            foreach (Sub sub in mw.Set["diy"])
                StackDIY.Children.Add(new DIYViewer(sub));

            SliderResolution.Maximum = Math.Min(System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width,
                System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height);
            SliderResolution.Value = mw.Set.Resolution;

            SwitchOpacityHitThrough.IsChecked = mw.Set.OpacityHitThrough;
            SwitchOpacity.IsChecked = mw.Set.OpacityMain;
            OpacitySlider.Value = mw.Set.Opacity;

#if X64
            GameVerison.Content = "Game version ".Translate() + $"v{mw.Version} x64";
#else
            GameVerison.Content = "Game version ".Translate() + $"v{mw.Version} x86";
#endif
            // Aboutui
            runUserName.Text = Environment.UserName;
            runActivate.Text = "Standalone mode".Translate();
            btn_fixdata.Visibility = Visibility.Collapsed;
            //CGPT
            if (mw.TalkAPI.Count > 0)
            {
                foreach (var v in mw.TalkAPI)
                    cbChatAPISelect.Items.Add(v.APIName.Translate());
                if (mw.TalkAPIIndex != -1)
                    cbChatAPISelect.SelectedIndex = mw.TalkAPIIndex;
            }
            else
            {
                cbChatAPISelect.Items.Add("There is no chat API yet, you can add it by subscribing to the mod.".Translate());
                cbChatAPISelect.SelectedIndex = 0;
                cbChatAPISelect.IsEnabled = false;
            }
            switch (mw.Set["CGPT"][(gstr)"type"])
            {
                //case "API":
                //    RBCGPTUseAPI.IsChecked = true;
                // BtnCGPTReSet.Content = "Open ChatGPT API Settings".Translate();
                //    break;
                case "DIY":
                    RBCGPTDIY.IsChecked = true;
                    BtnCGPTReSet.Content = "Open {0} Settings".Translate(mw.TalkBoxCurr?.APIName ?? "Steam Workshop");
                    break;
                case "LB":
                    RBCGPTUseLB.IsChecked = true;
                    BtnCGPTReSet.Content = "Initialize VPET chat program".Translate();
                    //if (!mf.IsSteamUser)
                    //    BtnCGPTReSet.IsEnabled = false;
                    break;
                case "OFF":
                default:
                    RBCGPTClose.IsChecked = true;
                    BtnCGPTReSet.Content = "Chat Box Closed".Translate();
                    break;
            }
            runabVer.Text = $"v{mw.Version} ({mw.version})";

            ShowModList();
            ListMod.SelectedIndex = 0;
            ShowMod((string)((ListBoxItem)ListMod.SelectedItem).Content);

            voicetimer = new DispatcherTimer()
            {
                Interval = TimeSpan.FromSeconds(0.2)
            };
            voicetimer.Tick += Voicetimer_Tick;

            ListMenuItems.Add(listmenuswith("Always On Top", 0, TopMostBox));
            ListMenuItems.Add(listmenuswith("Auto-start", 0, StartUpBox));
            ListMenuItems.Add(listmenuswith("Pet animation", 0, PetBox));
            ListMenuItems.Add(listmenuswith("Hide Window", 0, SwitchHideFromTaskControl));

            ListMenuItems.Add(listmenuswith("Autosave Frequency", 1, CBAutoSave));
            ListMenuItems.Add(listmenuswith("Restore from backup", 1, numBackupSaveMaxNum));
            ListMenuItems.Add(listmenuswith("Chat Settings", 1, RBCGPTUseLB));
            ListMenuItems.Add(listmenuswith("Game operation", 1, btn_cleancache));
            ListMenuItems.Add(listmenuswith("VPet Multiple ", 1, btn_mutidel));

            ListMenuItems.Add(listmenuswith("Interaction Settings", 2, CalFunctionBox));
            ListMenuItems.Add(listmenuswith("Computation interval", 2, CalSlider));
            ListMenuItems.Add(listmenuswith("Pet Moving", 2, MoveEventBox));
            ListMenuItems.Add(listmenuswith("Operation Settings", 2, PressLengthSlider));
            ListMenuItems.Add(listmenuswith("Desktop Pet Name", 2, TextBoxPetName));
            ListMenuItems.Add(listmenuswith("Music Recognition Settings", 2, VoiceMaxSilder));

            ListMenuItems.Add(listmenuswith("Custom Links", 3, btn_DIY));

            ListMenuItems.Add(listmenuswith("Automatic supermodel MOD optimization", 4, swAutoCal));
            ListMenuItems.Add(listmenuswith("Diagnostic & Feedback", 4, RBDiagnosisYES));

            ListMenuItems.Add(listmenuswith("MOD Management", 5, ButtonOpenModFolder));

            ListMenuItems.Add(listmenuswith("About", 6, ImageWHY));

            foreach (var v in ListMenuItems)
                ListMenu.Items.Add(v);

            AllowChange = true;

            UpdateMoveAreaText();

            ToolTipService.SetInitialShowDelay(runMODGameVerInfo, 0);

        }
        public List<ListBoxItem> ListMenuItems = new List<ListBoxItem>();
        private void tb_seach_menu_textchange(object sender, TextChangedEventArgs e)
        {
            if (!AllowChange)
                return;
            ListMenu.Items.Clear();
            if (string.IsNullOrEmpty(tb_seach_menu.Text))
            {
                foreach (var v in ListMenuItems)
                    ListMenu.Items.Add(v);
                return;
            }
            foreach (var v in ListMenuItems)
            {
                if (((string)v.Content).Contains(tb_seach_menu.Text))
                    ListMenu.Items.Add(v);
            }
        }

        private ListBoxItem listmenuswith(string content, int page, FrameworkElement element)
        {
            var lbi = new ListBoxItem() { Content = content.Translate() };
            lbi.PreviewMouseLeftButtonDown += (_, _) =>
            {
                if (page >= 0 && page <= 6)
                    MainTab.SelectedIndex = page;
                if (page == 2)
                {
                    voicetimer.Start();
                }
                Task.Run(() =>
                {
                    Thread.Sleep(100);
                    Dispatcher.Invoke(element.BringIntoView);
                });

            };
            return lbi;
        }
        private void Voicetimer_Tick(object sender, EventArgs e)
        {
            var v = mw.AudioPlayingVolume();
            RVoice.Text = v.ToString("p2");
            if (v > mw.Set.MusicCatch)
            {
                RVoice.Foreground = new SolidColorBrush(Colors.Green);
                if (v > mw.Set.MusicMax)
                {
                    RVoice.FontWeight = FontWeights.Bold;
                }
                else
                    RVoice.FontWeight = FontWeights.Normal;
            }
            else
                RVoice.Foreground = Function.ResourcesBrush(Function.BrushType.PrimaryText);
        }

        public void ShowModList()
        {
            ListMod.Items.Clear();
            foreach (CoreMOD mod in mw.CoreMODs)
            {
                ListBoxItem moditem = (ListBoxItem)ListMod.Items[ListMod.Items.Add(new ListBoxItem())];
                moditem.Padding = new Thickness(5, 0, 5, 0);
                moditem.Content = mod.Name;
                if (!mod.IsOnMOD(mw))
                {
                    moditem.Foreground = new SolidColorBrush(Color.FromRgb(100, 100, 100));
                }
                else
                {
                    if (mod.GameVer / 1000 == mw.version / 1000)
                    {
                        moditem.Foreground = Function.ResourcesBrush(Function.BrushType.PrimaryText);
                    }
                    else
                    {
                        if (mod.Tag.Contains("plugin"))
                            moditem.Foreground = new SolidColorBrush(Color.FromRgb(190, 0, 0));
                    }
                }
            }
        }
        CoreMOD mod;
        private void ShowMod(string modname)
        {
            mod = mw.CoreMODs.Find(x => x.Name == modname);
            LabelModName.Content = mod.Name.Translate();
            runMODAuthor.Text = mod.Author;
            runMODGameVer.Text = CoreMOD.INTtoVER(mod.GameVer);
            runMODGameVer.Foreground = Function.ResourcesBrush(Function.BrushType.PrimaryText);
            if (ImageMOD.Source is BitmapImage bitmapImage)
            {
                bitmapImage.StreamSource?.Dispose();
            }
            if (File.Exists(mod.Path.FullName + @"\icon.png"))
            {
                bitmapImage = new();
                bitmapImage.BeginInit();
                try
                {
                    var bytes = File.ReadAllBytes(mod.Path.FullName + @"\icon.png");
                    bitmapImage.StreamSource = new MemoryStream(bytes);
                    bitmapImage.DecodePixelWidth = 250;
                }
                finally
                {
                    bitmapImage.EndInit();
                }
                ImageMOD.Source = bitmapImage;
            }
            else
                ImageMOD.Source = ImageResources.NewSafeBitmapImage(@"pack://application:,,,/Res/TopLogo2019.PNG");
            runMODGameVerInfo.Visibility = Visibility.Collapsed;
            if (mod.GameVer / 100 != mw.version / 100)
                if (mod.GameVer < mw.version)
                {
                    if (mod.GameVer / 1000 == mw.version / 1000)
                    {
                        runMODGameVer.Text += "  (Compatible)".Translate();
                    }
                    else
                    {
                        runMODGameVer.Text += "  (Low Version)".Translate();
                        runMODGameVerInfo.Visibility = Visibility.Visible;
                        if (mod.Tag.Contains("plugin"))
                        {
                            runMODGameVer.Foreground = new SolidColorBrush(Color.FromRgb(190, 0, 0));
                            runMODGameVerInfo.ToolTip = "The MOD corresponds to a game version lower than the current game version, and may have serious compatibility issues due to the inclusion of code plugins.\\nPlease contact the MOD author to update the MOD".Translate()
                                + $"\nv{CoreMOD.INTtoVER(mod.GameVer)} < v{mw.Version}";
                        }
                        else
                            runMODGameVerInfo.ToolTip = "The MOD corresponds to a game version lower than the current game version, but the compatibility function of the game may take effect, and the MOD may work normally.\\nTo ensure the best experience, please contact the MOD author to update the MOD".Translate()
                               + $"\nv{CoreMOD.INTtoVER(mod.GameVer)} < v{mw.Version}";
                    }
                }
                else if (mod.GameVer > mw.version)
                {
                    if (mod.GameVer / 1000 == mw.version / 1000)
                    {
                        runMODGameVer.Text += "  (Compatible)".Translate();
                        runMODGameVer.Foreground = Function.ResourcesBrush(Function.BrushType.PrimaryText);
                    }
                    else
                    {
                        runMODGameVer.Text += "  (Version High)".Translate();
                        runMODGameVerInfo.Visibility = Visibility.Visible;
                        runMODGameVer.Foreground = new SolidColorBrush(Color.FromRgb(190, 0, 0));
                        runMODGameVerInfo.ToolTip = "The MOD corresponds to a game version higher than the current game version, and may have compatibility issues.\\nPlease update the game".Translate()
                            + $"\nv{CoreMOD.INTtoVER(mod.GameVer)} > v{mw.Version}";
                    }
                }
            if (!mod.IsOnMOD(mw))
            {
                LabelModName.Foreground = new SolidColorBrush(Color.FromRgb(100, 100, 100));
                ButtonEnable.Foreground = Function.ResourcesBrush(Function.BrushType.DARKPrimaryDarker);
                ButtonDisEnable.Foreground = new SolidColorBrush(Color.FromRgb(100, 100, 100));
                ButtonEnable.IsEnabled = true;
                ButtonDisEnable.IsEnabled = false;
            }
            else
            {
                LabelModName.Foreground = runMODGameVer.Foreground;
                ButtonDisEnable.Foreground = Function.ResourcesBrush(Function.BrushType.DARKPrimaryDarker);
                ButtonEnable.Foreground = new SolidColorBrush(Color.FromRgb(100, 100, 100));
                ButtonEnable.IsEnabled = false;
                ButtonDisEnable.IsEnabled = true;
            }
            runMODVer.Text = CoreMOD.INTtoVER(mod.Ver);
            GameInfo.Text = mod.Intro.Translate();
            string content = "";
            foreach (string tag in mod.Tag)
            {
                content += tag.Translate() + "\n";
            }
            GameHave.Text = content;
            ButtonAllow.Visibility = mod.SuccessLoad || mw.Set.IsPassMOD(mod.Name) ? Visibility.Collapsed : Visibility.Visible;

            foreach (var mainplug in mw.Plugins)
            {
                try
                {
                    if (mainplug.PluginName == mod.Name &&
                        mainplug.GetType().GetMethod("Setting").DeclaringType != typeof(MainPlugin)
                    && mainplug.GetType().Assembly.Location.Contains(mod.Path.FullName))
                    {
                        ButtonSetting.Visibility = Visibility.Visible;
                        return;
                    }
                }
                finally { }
            }
            ButtonSetting.Visibility = Visibility.Collapsed;
        }
        private void FullScreenBox_Check(object sender, RoutedEventArgs e)
        {
            if (!AllowChange)
                return;
            if (FullScreenBox.IsChecked == true)
            {
                mw.Set.IsBiggerScreen = true;
                ZoomSlider.Maximum = 8;
            }
            else
            {
                mw.Set.IsBiggerScreen = false;
                ZoomSlider.Maximum = 3;
            }
        }

        private void ThemeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!AllowChange)
                return;
            mw.LoadTheme(mw.Themes[ThemeBox.SelectedIndex].xName);
            mw.Set.Theme = mw.Theme.xName;
        }

        private void FontBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!AllowChange)
                return;
            string str = mw.Fonts[FontBox.SelectedIndex].Name;
            mw.LoadFont(str);
            mw.Set.Font = str;
        }

        private void CBAutoSave_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!AllowChange)
                return;
            mw.Set.SetAutoSaveInterval((int)((ComboBoxItem)CBAutoSave.SelectedItem).Tag);
        }


        private void RBDiagnosisYES_Checked(object sender, RoutedEventArgs e)
        {
            if (!AllowChange)
                return;
            mw.Set.Diagnosis = true;
            CBDiagnosis.IsEnabled = true;
        }

        private void RBDiagnosisNO_Checked(object sender, RoutedEventArgs e)
        {
            if (!AllowChange)
                return;
            mw.Set.Diagnosis = false;
            CBDiagnosis.IsEnabled = false;
        }

        private void CBDiagnosis_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!AllowChange)
                return;
            List<int> cbDiagnosis = new List<int> { 200, 500, 1000, 2000, 5000, 10000, 20000 };
            mw.Set.DiagnosisInterval = cbDiagnosis[CBDiagnosis.SelectedIndex];
        }

        private void ListMod_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!AllowChange || ListMod.SelectedItem == null)
                return;

            ShowMod((string)((ListBoxItem)ListMod.SelectedItem).Content);
        }

        private void ButtonOpenModFolder_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var psi = new ProcessStartInfo
            {
                FileName = mod.Path.FullName,
                UseShellExecute = true
            };
            Process.Start(psi);
        }

        private void ButtonEnable_MouseDown(object sender, MouseButtonEventArgs e)
        {
            mw.Set.OnMod(mod.Name);
            ShowMod(mod.Name);
            ButtonRestart.Visibility = Visibility.Visible;
            //int seleid = ListMod.SelectedIndex();
            ShowModList();
        }

        private void ButtonDisEnable_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (mod.Name == "Core")
            {
                MessageBoxX.Show("Core is <VPet-Simulator> core file, cannot be disenable".Translate(), "DisEnable failed".Translate());
                return;
            }
            else if (CoreMOD.OnModDefList.Contains(mod.Name))
                return;
            mw.Set.OnModRemove(mod.Name);
            ShowMod(mod.Name);
            ButtonRestart.Visibility = Visibility.Visible;
            ShowModList();
        }

        private void ButtonAllow_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBoxX.Show("enable code plugin for {0}? \\nOnce enabled, plug-in will allow access to all data of the system (including external systems)\\nIf you are not sure, please use Antivirus software to check".Translate(mod.Name),
                "Enable code plugins for {0}?".Translate(mod.Name), MessageBoxButton.YesNo, MessageBoxIcon.Warning) == MessageBoxResult.Yes)
            {
                mw.Set.PassMod(mod.Name);
                ShowMod(mod.Name);
                ButtonRestart.Visibility = Visibility.Visible;
            }
        }

        private void ButtonRestart_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBoxX.Show("Exit <VPet-Simulator>?\\nPlease pay attention to saving the game".Translate(), "Restart the game".Translate(), MessageBoxButton.YesNo, MessageBoxIcon.Warning) == MessageBoxResult.Yes)
            {
                mw.Restart();
            }
        }

        public void UpdateMoveAreaText()
        {
            var mwCtrl = mw.Core.Controller as MWController;
            if (mwCtrl.IsPrimaryScreen && !mwCtrl.AutoChangeWindow)
            {
                textMoveArea.Text = "Home Screen".Translate();
                return;
            }
            else if (mwCtrl.AutoChangeWindow)
            {
                textMoveArea.Text = "".Translate();
                return;
            }
            var rect = mwCtrl.ScreenBorder;
            textMoveArea.Text = $"X:{rect.X};Y:{rect.Y};W:{rect.Width};H:{rect.Height}";
        }

        private void BtnSetMoveArea_Default_Click(object sender, RoutedEventArgs e)
        {
            var mwCtrl = mw.Core.Controller as MWController;
            mwCtrl.ResetScreenBorder();
            UpdateMoveAreaText();
        }

        private void BtnSetMoveArea_DetectScreen_Click(object sender, RoutedEventArgs e)
        {
            var mwCtrl = mw.Core.Controller as MWController;
            mwCtrl.SetNowScreenActivate();
            UpdateMoveAreaText();
        }

        internal static System.Reflection.FieldInfo leftGetter, topGetter;
        private void BtnSetMoveArea_Window_Click(object sender, RoutedEventArgs e)
        {
            var wma = new winMoveArea(mw);
            var mwCtrl = mw.Core.Controller as MWController;
            if (!mwCtrl.IsPrimaryScreen)
            {
                var rect = mwCtrl.ScreenBorder;
                wma.Width = rect.Width;
                wma.Height = rect.Height;
                wma.Left = rect.X;
                wma.Top = rect.Y;
            }
            wma.ShowDialog();
        }

        private void hyper_moreInfo(object sender, RoutedEventArgs e)
        {
            ExtensionFunction.StartURL("https://www.exlb.net/Diagnosis");
        }

        public new void Show()
        {
            if (MainTab.SelectedIndex == 2)
            {
                voicetimer.Start();
            }
            base.Show();
        }

        private void WindowX_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            mw.Topmost = mw.Set.TopMost;
            e.Cancel = mw.CloseConfirm;
            voicetimer.Stop();
            Hide();
        }

        private void TopMostBox_Checked(object sender, RoutedEventArgs e)
        {
            if (!AllowChange)
                return;
            mw.Set.TopMost = true;
            (mw.notifyIcon.ContextMenuStrip.Items.Find("NotifyIcon_TopMost", false).First() as System.Windows.Forms.ToolStripMenuItem).Checked = true;
        }

        private void TopMostBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!AllowChange)
                return;
            mw.Set.TopMost = false;
            (mw.notifyIcon.ContextMenuStrip.Items.Find("NotifyIcon_TopMost", false).First() as System.Windows.Forms.ToolStripMenuItem).Checked = false;
        }

        private void ZoomSlider_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!AllowChange)
                return;
            mw.SetZoomLevel(ZoomSlider.Value / 2);
        }

        private void PressLengthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!AllowChange)
                return;
            mw.Set.PressLength = (int)(PressLengthSlider.Value * 1000);
        }

        private void InteractionSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!AllowChange)
                return;
            mw.Set.InteractionCycle = (int)(InteractionSlider.Value);
            CalTimeInteraction();
        }
        private void CalTimeInteraction()
        {
            var interact = (60 / mw.Set.LogicInterval);
            rTimeMinute.Text = interact.ToString("f2");
            RInter.Text = ((0.08 * mw.Set.InteractionCycle - 0.9) / interact * 2).ToString("f2");
        }
        #region Link
        private void Git_Click(object sender, RoutedEventArgs e)
        {
            ExtensionFunction.StartURL("https://github.com/LorisYounger/VPet/graphs/contributors");
        }

        private void Github_Click(object sender, RoutedEventArgs e)
        {
            ExtensionFunction.StartURL("https://github.com/LorisYounger/VPet");
        }

        private void LB_Click(object sender, RoutedEventArgs e)
        {
            ExtensionFunction.StartURL("https://space.bilibili.com/609610777");
        }

        private void VPET_Click(object sender, RoutedEventArgs e)
        {
            ExtensionFunction.StartURL("https://www.exlb.net/");
        }
        private void VUP_Click(object sender, RoutedEventArgs e)
        {
            ExtensionFunction.StartURL("https://store.steampowered.com/app/1352140/_/");
        }

        private void Group_Click(object sender, RoutedEventArgs e)
        {
            if (LocalizeCore.CurrentCulture.StartsWith("zh"))
                ExtensionFunction.StartURL("https://space.bilibili.com/690425399");
            else
                ExtensionFunction.StartURL("https://github.com/LorisYounger/VPet");
        }
        private void sendkey_click(object sender, RoutedEventArgs e)
        {
            if (LocalizeCore.CurrentCulture.StartsWith("zh"))
                ExtensionFunction.StartURL("https://www.exlb.net/SendKeys");
            else if (LocalizeCore.CurrentCulture == "null")
                ExtensionFunction.StartURL("https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.sendkeys?view=windowsdesktop-7.0#remarks");
            else
                ExtensionFunction.StartURL($"https://learn.microsoft.com/{LocalizeCore.CurrentCulture}/dotnet/api/system.windows.forms.sendkeys?view=windowsdesktop-7.0#remarks");
        }
        #endregion        

        private void CalSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!AllowChange)
                return;
            mw.Set.SetLogicInterval(CalSlider.Value);
            CalTimeInteraction();
        }

        private void MoveEventBox_Checked(object sender, RoutedEventArgs e)
        {
            if (!AllowChange)
                return;
            mw.Set.SetAllowMove(MoveEventBox.IsChecked == true);
        }

        private void SmartMoveEventBox_Checked(object sender, RoutedEventArgs e)
        {
            if (!AllowChange)
                return;
            mw.Set.SetSmartMove(SmartMoveEventBox.IsChecked == true);
        }

        private void CBSmartMove_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!AllowChange)
                return;
            mw.Set.SetSmartMoveInterval((int)((ComboBoxItem)CBSmartMove.SelectedItem).Tag);
        }


        public void GenStartUP()
        {
            mw.Set["v"][(gbol)"newverstartup"] = true;
            var path = Environment.GetFolderPath(Environment.SpecialFolder.Startup) + @"\VPET_Simulator.lnk";
            if (mw.Set.StartUPBoot)
            {
                if (File.Exists(path))
                    File.Delete(path);
                var link = (IShellLink)new ShellLink();
                link.SetPath(System.Reflection.Assembly.GetExecutingAssembly().Location.Replace(".dll", ".exe"));

                link.SetDescription("VPet Simulator");
                link.SetIconLocation(ExtensionValue.BaseDirectory + @"vpeticon.ico", 0);
                try
                {
                    var file = (IPersistFile)link;
                    file.Save(path, false);
                }
                catch
                {
                    MessageBox.Show("Failed to create shortcut, insufficient permissions\\nPlease run as administrator and try again".Translate(), "Insufficient permissions".Translate());
                }
            }
            else
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }
        private void StartUpBox_Checked(object sender, RoutedEventArgs e)
        {
            if (!AllowChange)
                return;
            if (StartUpBox.IsChecked == true)
                if (MessageBoxX.Show("Game auto-start on login\\rDisable this option before uninstalling".Translate() + "\n------\n" + "I have confirmed and will disable it before uninstalling the game.".Translate(), "Important news about startup.".Translate(),
                    MessageBoxButton.YesNo, MessageBoxIcon.Warning) != MessageBoxResult.Yes)
                {
                    StartUpBox.IsChecked = false;
                    return;
                }
            //else
            //{
            //    mf.Set["SingleTips"][(gint)"open"] = 1;
            //        , "The problem of uninstallation is due to the startup being turned on.".Translate(), MessageBoxIcon.Info);
            //}

            mw.Set.StartUPBoot = StartUpBox.IsChecked == true;
            GenStartUP();
        }
        private int petboxbef;
        private void petbox_back()
        {
            AllowChange = false;
            PetBox.SelectedIndex = petboxbef;
            AllowChange = true;
        }
        private void LoadMutiUI()
        {
            LBHave.Items.Clear();
            foreach (var str in App.MutiSaves)
            {
                var rn = str.Translate();
                if (str == "")
                    rn = "Default archive".Translate();
                if (str == mw.PrefixSave.Trim('-'))
                    rn += " (" + "Current archive".Translate() + ')';
                LBHave.Items.Add(rn);
            }
        }
        private void PetBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!AllowChange)
                return;

            var petloader = mw.Pets.Find(x => x.Name == mw.Set.PetGraph);
            petloader ??= mw.Pets[0];

            if (mw.PrefixSave == "" && petloader.PetName != mw.Pets[PetBox.SelectedIndex].PetName)
            {
                switch (MessageBoxX.Show("Whether to open a new VPet to use {0} skin\\nEach archive is saved independently and does not affect each other\\nSupports displaying multiple pets at the same time".Translate(mw.Pets[PetBox.SelectedIndex].Name.Translate()),
                    "Whether to open multiple".Translate(), MessageBoxButton.YesNoCancel))
                {
                    case MessageBoxResult.Yes:
                        var savename = mw.Pets[PetBox.SelectedIndex].Name;
                        petbox_back();
                        if (App.MutiSaves.Contains(savename))
                        {
                            if (App.MainWindows.FirstOrDefault(x => x.PrefixSave.Trim('-') == savename) != null)
                            {
                                MessageBoxX.Show("Currently loaded".Translate());
                            }
                            else
                                new MainWindow(savename, mw).Show();
                            return;
                        }
                        foreach (var c in @"()#:|/\?*<>-")
                            if (savename.Contains(c))
                            {
                                MessageBoxX.Show("Archive name cannot include special symbols".Translate());
                                return;
                            }
                        var lps = new LPS(mw.Set.ToString());
                        lps.SetInt("savetimes", 0);
                        lps["gameconfig"].SetString("petgraph", savename);
                        File.WriteAllText(ExtensionValue.BaseDirectory + @$"\Setting-{savename}.lps", lps.ToString());
                        App.MutiSaves.Add(savename);
                        new MainWindow(savename, mw).Show();
                        LoadMutiUI();
                        return;
                    case MessageBoxResult.No:
                        break;
                    default:
                        petbox_back();
                        return;
                }
            }


            bool ischangename = mw.Core.Save.Name == petloader.PetName.Translate();
            petboxbef = PetBox.SelectedIndex;
            mw.Set.PetGraph = mw.Pets[petboxbef].Name;
            PetIntor.Text = mw.Pets[petboxbef].Intor.Translate();
            ButtonRestartGraph.Visibility = Visibility.Visible;

            if (ischangename)
            {
                mw.Core.Save.Name = mw.Pets[petboxbef].PetName.Translate();
                TextBoxPetName.Text = mw.Core.Save.Name;
            }
        }

        private void TextBoxPetName_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!AllowChange)
                return;
            mw.Core.Save.Name = TextBoxPetName.Text;
        }

        private void DIY_ADD_Click(object sender, RoutedEventArgs e)
        {
            StackDIY.Children.Add(new DIYViewer());
        }

        private void DIY_Save_Click(object sender, RoutedEventArgs e)
        {
            mw.Set["diy"].Clear();
            foreach (DIYViewer dv in StackDIY.Children)
            {
                mw.Set["diy"].Add(dv.ToSub());
            }
            mw.LoadDIY();
        }


        private void ChatGPT_Reset_Click(object sender, RoutedEventArgs e)
        {
            switch (mw.Set["CGPT"][(gstr)"type"])
            {
                //case "API":
                //    new winCGPTSetting(mf).ShowDialog();
                //    break;
                case "DIY":
                    if (mw.TalkBoxCurr != null)
                        mw.TalkBoxCurr.Setting();
                    else
                        ExtensionFunction.StartURL("https://steamcommunity.com/app/1920960/workshop/");
                    break;
                case "LB":
                    //Task.Run(() =>
                    //{
                    //    if (((TalkBox)mf.TalkBox).ChatGPT_Reset())
                    //    {
                    //        ((TalkBox)mf.TalkBox).btn_startup.Visibility = Visibility.Visible;
                    //        MessageBoxX.Show("Desktop pet reset successful".Translate());
                    //    }
                    //});
                    //((TalkSelect)mf.TalkBox).RelsTime
                    break;
                case "OFF":
                default:
                    break;
            }
        }

        private void CGPType_Checked(object sender, RoutedEventArgs e)
        {
            if (!AllowChange)
                return;
            if (RBCGPTUseLB.IsChecked == true)
            {
                mw.Set["CGPT"][(gstr)"type"] = "LB";
            }
            else if (RBCGPTDIY.IsChecked == true)
            {
                mw.Set["CGPT"][(gstr)"type"] = "DIY";
            }
            //else if (RBCGPTUseAPI.IsChecked == true)
            //{
            //    mf.Set["CGPT"][(gstr)"type"] = "API";
            //}
            else
            {
                mw.Set["CGPT"][(gstr)"type"] = "OFF";
            }


            switch (mw.Set["CGPT"][(gstr)"type"])
            {
                //case "API":
                //    BtnCGPTReSet.IsEnabled = true;
                // BtnCGPTReSet.Content = "Open ChatGPT API Settings".Translate();
                //    if (mf.TalkBox != null)
                //        mf.Main.ToolBar.MainGrid.Children.Remove(mf.TalkBox);
                //    mf.TalkBox = new TalkBoxAPI(mf);
                //    mf.Main.ToolBar.MainGrid.Children.Add(mf.TalkBox);
                //    break;
                case "DIY":
                    BtnCGPTReSet.IsEnabled = true;
                    mw.RemoveTalkBox();
                    BtnCGPTReSet.Content = "Open {0} Settings".Translate(mw.TalkBoxCurr?.APIName ?? "Steam Workshop");
                    mw.LoadTalkDIY();
                    break;
                case "LB":
                    mw.RemoveTalkBox();
                    BtnCGPTReSet.IsEnabled = true;
                    BtnCGPTReSet.Content = "Initialize VPET chat program".Translate();
                    mw.TalkBox = new TalkSelect(mw);
                    mw.Main.ToolBar.MainGrid.Children.Add(mw.TalkBox);
                    break;
                case "OFF":
                default:
                    mw.RemoveTalkBox();
                    BtnCGPTReSet.IsEnabled = false;
                    BtnCGPTReSet.Content = "Chat Box Closed".Translate();
                    break;
            }
        }

        private void ButtonSetting_MouseDown(object sender, MouseButtonEventArgs e)
        {
            foreach (var mainplug in mw.Plugins)
            {
                try
                {
                    if (mainplug.PluginName == mod.Name && mainplug.GetType().GetMethod("Setting").DeclaringType != typeof(MainPlugin)
                    && mainplug.GetType().Assembly.Location.Contains(mod.Path.FullName))
                    {
                        mainplug.Setting();
                        return;
                    }
                }
                finally { }
            }
        }

        private void StartPlace_Checked(object sender, RoutedEventArgs e)
        {
            if (!AllowChange)
                return;
            if (StartPlace.IsChecked == true)
            {
                mw.Set.StartRecordLast = true;
                TextBoxStartUpX.IsEnabled = false;
                TextBoxStartUpY.IsEnabled = false;
                BtnStartUpGet.IsEnabled = false;
            }
            else
            {
                mw.Set.StartRecordLast = false;
                TextBoxStartUpX.IsEnabled = true;
                TextBoxStartUpY.IsEnabled = true;
                BtnStartUpGet.IsEnabled = true;
            }
        }

        private void TextBoxStartUp_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!AllowChange)
                return;
            if (double.TryParse(TextBoxStartUpX.Text, out double x) && double.TryParse(TextBoxStartUpY.Text, out double y))
                mw.Set.StartRecordPoint = new Point(x, y);
        }

        private void BtnStartUpGet_Click(object sender, RoutedEventArgs e)
        {
            AllowChange = false;
            TextBoxStartUpX.Text = mw.Left.ToString();
            TextBoxStartUpY.Text = mw.Top.ToString();
            mw.Set.StartRecordPoint = new Point(mw.Left, mw.Top);
            AllowChange = true;
        }

        private void CalFunctionBox_Checked(object sender, RoutedEventArgs e)
        {
            if (!AllowChange)
                return;
            if (CalFunctionBox.IsChecked == true)
            {
                mw.Set.EnableFunction = true;
                combCalFunState.IsEnabled = false;
            }
            else
            {
                mw.Set.EnableFunction = false;
                combCalFunState.IsEnabled = true;
                if (mw.Main.State != Main.WorkingState.Nomal)
                {
                    mw.Main.WorkTimer.Visibility = Visibility.Collapsed;
                    mw.Main.State = Main.WorkingState.Nomal;
                }
            }
        }

        private void SwitchMsgOut_Checked(object sender, RoutedEventArgs e)
        {
            if (!AllowChange)
                return;
            mw.Set.MessageBarOutside = SwitchMsgOut.IsChecked.Value;
            if (SwitchMsgOut.IsChecked.Value)
                mw.Main.MsgBar.SetPlaceOUT();
            else
                mw.Main.MsgBar.SetPlaceIN();
        }

        private void numBackupSaveMaxNum_ValueChanged(object sender, Panuon.WPF.SelectedValueChangedRoutedEventArgs<double?> e)
        {
            if (!AllowChange)
                return;
            mw.Set.BackupSaveMaxNum = (int)numBackupSaveMaxNum.Value;
        }
        private void BtnOpenSaveManager_Click(object sender, RoutedEventArgs e)
        {
            new winSaveManager(mw).ShowDialog();
        }

        private void Mod_Click(object sender, RoutedEventArgs e)
        {
            List<string> list = new List<string>();
            foreach (CoreMOD mod in mw.CoreMODs)
            {
                foreach (string str in mod.Author.Split(','))
                    list.Add(str.Trim());
            }
            list = list.Distinct().ToList();
            MessageBoxX.Show(string.Join("\n", list), "Thanks to the following mod developers".Translate());
        }

        private void Using_Click(object sender, RoutedEventArgs e)
        {
            string NormalizeDllName(string dll)
            {
                var fileName = Path.GetFileName(dll);
                if (string.IsNullOrWhiteSpace(fileName))
                    fileName = dll;
                return Path.GetFileNameWithoutExtension(fileName) ?? fileName;
            }

            var rows = CoreMOD.LoadedDLL
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(NormalizeDllName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(name => ExtensionValue.DllReferenceDescriptions.TryGetValue(name, out var description)
                    ? $"> {name}\n{description}"
                    : $"> {name}.dll")
                .ToList();

            MessageBoxX.Show(string.Join("\n", rows), "DLL Reference List".Translate());
        }

        private void combCalFunState_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!AllowChange)
                return;
            mw.Set.CalFunState = (IGameSave.ModeType)combCalFunState.SelectedIndex;
            mw.Main.NoFunctionMOD = (IGameSave.ModeType)combCalFunState.SelectedIndex;
            mw.Main.EventTimer_Elapsed();
        }

        private void HitThroughBox_Checked(object sender, RoutedEventArgs e)
        {
            if (!AllowChange)
                return;
            mw.Set["v"][(gbol)"HitThrough"] = true;
            mw.Set.HitThrough = HitThroughBox.IsChecked.Value;
            if (HitThroughBox.IsChecked.Value != mw.HitThrough)
                mw.SetTransparentHitThrough();
            if (HitThroughBox.IsChecked.Value)
                PetHelperBox.IsChecked = true;
        }

        private void PetHelperBox_Checked(object sender, RoutedEventArgs e)
        {
            if (!AllowChange)
                return;
            if (PetHelperBox.IsChecked == true)
            {
                mw.Set.PetHelper = true;
                mw.LoadPetHelper();
            }
            else
            {
                mw.Set.PetHelper = false;
                mw.petHelper?.Close();
                mw.petHelper = null;
            }
        }

        private void LanguageBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!AllowChange)
                return;
            mw.Set.SetLanguage((string)LanguageBox.SelectedItem);
            TextBoxPetName.Text = mw.Core.Save.Name;
            ButtonRestartGraph.Visibility = Visibility.Visible;

        }

        private void MainTab_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!AllowChange)
                return;
            voicetimer.Stop();
            switch (MainTab.SelectedIndex)
            {
                case 2:
                    voicetimer.Start();
                    break;
                case 4:
                    if (mw.HashCheck)
                    {
                        RHashCheck.Text = "Pass".Translate();
                    }
                    else
                    {
                        RHashCheck.Text = "Fail".Translate();
                    }
                    break;
            }
        }
        DispatcherTimer voicetimer;

        private void VoiceCatchSilder_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!AllowChange)
                return;
            mw.Set.MusicCatch = VoiceCatchSilder.Value;
            mw.Set.MusicMax = VoiceMaxSilder.Value;
        }

        private void cleancache_click(object sender, RoutedEventArgs e)
        {
            mw.Set.LastCacheDate = DateTime.MinValue;
            MessageBoxX.Show("The cleanup command has been issued and will take effect the next time the table pet is started".Translate());
        }

        private void SliderResolution_MouseUp(object sender, MouseButtonEventArgs e)
        {
            mw.Set.Resolution = (int)SliderResolution.Value;
            ButtonRestartGraph.Visibility = Visibility.Visible;
        }

        private void save_click(object sender, RoutedEventArgs e)
        {
            mw.Save();
            MessageBoxX.Show("Save successfully".Translate());
        }

        private void swAutoCal_Checked(object sender, RoutedEventArgs e)
        {
            if (!AllowChange)
                return;
            mw.Set["gameconfig"].SetBool("noAutoCal", !swAutoCal.IsChecked.Value);
        }

        private void restart_click(object sender, RoutedEventArgs e)
        {
            if (MessageBoxX.Show("Reset the game data to restart?".Translate(), "Restart New Game".Translate(), MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                var oldsave = mw.GameSavesData;
                mw.GameSavesData = new GameSave_v2(mw.Core.Save.Name);
                mw.Core.Save = mw.GameSavesData.GameSave;
                mw.GameSavesData.GameSave.Event_LevelUp += mw.LevelUP;

                if (oldsave.HashCheck)
                {
                    mw.GameSavesData.Statistics = oldsave.Statistics;
                    if (oldsave.GameSave.Money > 10000000 || oldsave.GameSave.Money < -1000000000 || oldsave.GameSave.Exp > 100000000 || oldsave.GameSave.Exp < -10000000000)
                    {
                        mw.Core.Save.Money = 10000;
                        mw.Core.Save.Exp = 10000;
                    }
                }
                mw.HashCheck = true;
                MessageBoxX.Show("Reset successfully".Translate());
            }
        }

        private void cbChatAPISelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!AllowChange)
                return;
            mw.TalkAPIIndex = cbChatAPISelect.SelectedIndex;
            mw.Set["CGPT"][(gstr)"DIY"] = mw.TalkBoxCurr?.APIName ?? "";
            if (RBCGPTDIY.IsChecked == true)
                mw.LoadTalkDIY();
            BtnCGPTReSet.Content = "Open {0} Settings".Translate(mw.TalkBoxCurr?.APIName ?? "Steam Workshop");

        }

        private void btn_muti_open_click(object sender, RoutedEventArgs e)
        {
            if (LBHave.SelectedIndex == -1)
                return;
            var str = App.MutiSaves[LBHave.SelectedIndex];
            if (str.EndsWith(")") || App.MainWindows.FirstOrDefault(x => x.PrefixSave.Trim('-') == str) != null)
            {
                MessageBoxX.Show("Currently loaded".Translate());
                return;
            }
            new MainWindow(str, mw).Show();
        }

        private void btn_mutinew_click(object sender, RoutedEventArgs e)
        {
            var savename = TBNew.Text;
            foreach (var c in @"()#:|/\?*<>-")
                if (savename.Contains(c))
                {
                    MessageBoxX.Show("Archive name cannot include special symbols".Translate());
                    return;
                }
            if (App.MutiSaves.FirstOrDefault(x => x.ToLowerInvariant() == savename.ToLowerInvariant()) != null)
            {
                MessageBoxX.Show("Duplicate archive name".Translate());
                return;
            }

            var lps = new LPS(mw.Set);
            lps.SetInt("savetimes", 0);
            File.WriteAllText(ExtensionValue.BaseDirectory + @$"\Setting-{savename}.lps", lps.ToString());
            App.MutiSaves.Add(savename);
            new MainWindow(savename, mw).Show();
        }

        private void btn_mutidel_Click(object sender, RoutedEventArgs e)
        {
            if (LBHave.SelectedIndex == -1)
                return;
            var str = App.MutiSaves[LBHave.SelectedIndex];
            if (str == "Default archive".Translate())
            {
                MessageBoxX.Show("Default archive cannot be deleted, please restart the game using the restart function.".Translate());
                return;
            }
            if (str.EndsWith(")") || App.MainWindows.FirstOrDefault(x => x.PrefixSave.Trim('-') == str) != null)
            {
                MessageBoxX.Show("Please close the multicomputer and try again.".Translate());
                return;
            }
            if (!App.MutiSaves.Contains(str))
            {
                LoadMutiUI();
                return;
            }
            if (MessageBoxX.Show("Do you want to delete the currently selected ({0}) multi save?".Translate(str), "Confirm before deleting".Translate(), MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                File.Delete(ExtensionValue.BaseDirectory + @$"\Setting-{str}.lps");
                App.MutiSaves.Remove(str);
                LoadMutiUI();
            }
        }

        private void btn_muti_master_click(object sender, RoutedEventArgs e)
        {
            if (LBHave.SelectedIndex == -1)
                return;
            var str = App.MutiSaves[LBHave.SelectedIndex];
            foreach (var sp in new DirectoryInfo(ExtensionValue.BaseDirectory).GetFiles("startup_*"))
            {
                sp.Delete();
            }
            if (str != "")
                File.Create(ExtensionValue.BaseDirectory + @"\startup_" + str).Close();
            MessageBoxX.Show("The current selection {0} has been set as the default startup archive".Translate(str.Translate()));
        }

        private void btn_fixdata_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxX.Show("Standalone mode does not support data repair (requires Steam server recorded game time)".Translate(), "Feature unavailable".Translate(), MessageBoxIcon.Info);
        }

        private void ConsoleBox_Checked(object sender, RoutedEventArgs e)
        {
            if (!AllowChange)
                return;
            mw.Set.DeBug = ConsoleBox.IsChecked.Value;
        }

        private void TextBoxHostName_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!AllowChange)
                return;
            mw.GameSavesData.GameSave.HostName = TextBoxHostName.Text;
        }

        private void TextBoxHostBDay_SelectedDateTimeChanged(object sender, Panuon.WPF.SelectedValueChangedRoutedEventArgs<DateTime?> e)
        {
            if (!AllowChange || TextBoxHostBDay.SelectedDateTime == null)
                return;
            mw.GameSavesData.SetDateTime("HostBDay", TextBoxHostBDay.SelectedDateTime.Value);
        }

        private void SwitchOpacityHitThrough_Checked(object sender, RoutedEventArgs e)
        {
            if (!AllowChange)
                return;
            mw.Set.OpacityHitThrough = SwitchOpacityHitThrough.IsChecked == true;
        }

        private void SwitchOpacity_Checked(object sender, RoutedEventArgs e)
        {
            if (!AllowChange)
                return;
            if (SwitchOpacity.IsChecked == true)
            {
                mw.Set.OpacityHitThrough = true;
                mw.Set.OpacityMain = true;
                SwitchOpacityHitThrough.IsChecked = true;
                mw.Opacity = mw.Set.Opacity;
            }
            else
            {
                mw.Set.OpacityMain = false;
                mw.Opacity = 1;
            }
        }

        private void OpacitySlider_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            mw.Set.Opacity = OpacitySlider.Value;
            if (mw.Set.OpacityMain)
                mw.Opacity = mw.Set.Opacity;
        }

        private void AutoChangeWindowEvent_Checked(object sender, RoutedEventArgs e)
        {
            if (!AllowChange)
                return;
            if (AutoChangeWindowEvent.IsChecked == true)
            {
                mw.Set.AutoChangeWindow = true;
            }
            else
            {
                mw.Set.AutoChangeWindow = false;
            }
            UpdateMoveAreaText();
        }

        private void BC_Click(object sender, RoutedEventArgs e)
        {
            ExtensionFunction.StartURL($"https://bettercontribution.exlb.net/shop?lang={LocalizeCore.CurrentCulture}");
        }

        private void SwitchHideFromTaskControl_OnChecked(object sender, RoutedEventArgs e)
        {
            if (!AllowChange)
                return;
            mw.Set.HideFromTaskControl = SwitchHideFromTaskControl.IsChecked == true;
            ButtonRestartGraph.Visibility = Visibility.Visible;
        }

        // Steam stubs (Steamworks support removed)
        private void Steam_Click(object sender, RoutedEventArgs e) { }
        private void ButtonSteam_MouseDown(object sender, MouseButtonEventArgs e) { }
        private void ButtonPublish_MouseDown(object sender, MouseButtonEventArgs e) { }
        private void StartUpSteamBox_Checked(object sender, RoutedEventArgs e) { }


    }
}
