using LinePutScript;
using LinePutScript.Converter;
using LinePutScript.Dictionary;
using LinePutScript.Localization.WPF;
using NAudio.CoreAudioApi;
using Panuon.WPF.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media;
using VPet_Simulator.Core;
using VPet_Simulator.Windows.Interface;
using static VPet_Simulator.Core.GraphHelper;
using static VPet_Simulator.Core.GraphInfo;
using static VPet_Simulator.Windows.Interface.Food;
using static VPet_Simulator.Windows.Interface.Photo.UnlockCondition;
using Application = System.Windows.Application;
using Color = System.Windows.Media.Color;
using ContextMenu = System.Windows.Forms.ContextMenuStrip;
using Image = System.Windows.Controls.Image;
using Line = LinePutScript.Line;
using MenuItem = System.Windows.Forms.ToolStripMenuItem;
using MessageBox = System.Windows.MessageBox;
using Timer = System.Timers.Timer;
using ToolBar = VPet_Simulator.Core.ToolBar;

namespace VPet_Simulator.Windows
{
    public partial class MainWindow : IMainWindow
    {

        /// <summary>
        /// LoadTheme
        /// </summary>
        /// <param name="Themename">ThemeName</param>
        public void LoadTheme(string themename)
        {
            Theme ctheme = Themes.Find(x => x.xName == themename);
            if (ctheme == null)
            {
                return;
            }
            Theme = ctheme;

            ImageSources.AddSources(ctheme.Images);

            Application.Current.Resources["ShadowColor"] = Function.HEXToColor('#' + ctheme.ThemeColor[(gstr)"ShadowColor"]);

            foreach (ILine lin in ctheme.ThemeColor.Assemblage.FindAll(x => !x.Name.Contains("Color")))
                Application.Current.Resources[lin.Name] = new SolidColorBrush(Function.HEXToColor('#' + lin.info));

            Color c = Function.HEXToColor('#' + ctheme.ThemeColor["Primary"].info);
            c.A = 204;
            Application.Current.Resources["PrimaryTrans"] = new SolidColorBrush(c);
            c.A = 44;
            Application.Current.Resources["PrimaryTrans4"] = new SolidColorBrush(c);
            c.A = 170;
            Application.Current.Resources["PrimaryTransA"] = new SolidColorBrush(c);
            c.A = 238;
            Application.Current.Resources["PrimaryTransE"] = new SolidColorBrush(c);

            c = Function.HEXToColor('#' + ctheme.ThemeColor["Secondary"].info);
            c.A = 204;
            Application.Current.Resources["SecondaryTrans"] = new SolidColorBrush(c);
            c.A = 44;
            Application.Current.Resources["SecondaryTrans4"] = new SolidColorBrush(c);
            c.A = 170;
            Application.Current.Resources["SecondaryTransA"] = new SolidColorBrush(c);
            c.A = 238;
            Application.Current.Resources["SecondaryTransE"] = new SolidColorBrush(c);


            c = Function.HEXToColor('#' + ctheme.ThemeColor["DARKPrimary"].info);
            c.A = 204;
            Application.Current.Resources["DARKPrimaryTrans"] = new SolidColorBrush(c);
            c.A = 44;
            Application.Current.Resources["DARKPrimaryTrans4"] = new SolidColorBrush(c);
            c.A = 170;
            Application.Current.Resources["DARKPrimaryTransA"] = new SolidColorBrush(c);
            c.A = 238;
            Application.Current.Resources["DARKPrimaryTransE"] = new SolidColorBrush(c);
        }

        public void LoadFont(string fontname)
        {
            IFont cfont = Fonts.Find(x => x.Name == fontname);
            if (cfont == null)
            {
                return;
            }
            var font = cfont.Font;
            Application.Current.Resources["MainFont"] = font;
            Panuon.WPF.UI.GlobalSettings.Setting.FontFamily = font;
        }


        /// <summary>
        /// </summary>
        /// <returns>SpeakContent</returns>
        public ClickText GetClickText()
        {
            ClickText.DayTime dt;
            var now = DateTime.Now.Hour;
            if (now < 6)
                dt = ClickText.DayTime.Midnight;
            else if (now < 12)
                dt = ClickText.DayTime.Morning;
            else if (now < 18)
                dt = ClickText.DayTime.Afternoon;
            else
                dt = ClickText.DayTime.Night;

            ClickText.ModeType mt;
            switch (Core.Save.Mode)
            {
                case IGameSave.ModeType.PoorCondition:
                    mt = ClickText.ModeType.PoorCondition;
                    break;
                default:
                case IGameSave.ModeType.Nomal:
                    mt = ClickText.ModeType.Nomal;
                    break;
                case IGameSave.ModeType.Happy:
                    mt = ClickText.ModeType.Happy;
                    break;
                case IGameSave.ModeType.Ill:
                    mt = ClickText.ModeType.Ill;
                    break;
            }
            var list = ClickTexts.FindAll(x => x.DaiTime.HasFlag(dt) && x.Mode.HasFlag(mt) && x.CheckState(Main));
            if (list.Count == 0)
                return null;
            return list[Function.Rnd.Next(list.Count)];
        }
        private Image hashcheckimg;

        /// <summary>
        /// </summary>
        public void HashCheckOff()
        {
            HashCheck = false;
        }
        /// <summary>
        /// </summary>
        public bool HashCheck
        {
            get => GameSavesData.HashCheck;
            set
            {
                if (!value)
                {
                    GameSavesData.HashCheckOff();
                }
                Main?.Dispatcher.Invoke(() =>
                {
                    if (GameSavesData.HashCheck)
                    {
                        if (hashcheckimg == null)
                        {
                            hashcheckimg = new Image();
                            int hours = GameSavesData.Statistics[(gint)"stat_total_time"] / 3600;
                            //if (hours < 10)
                            //    hashcheckimg.Source = ImageResources.NewSafeBitmapImage("pack://application:,,,/Res/hash.png");
                            //else if (hours < 50)
                            //    hashcheckimg.Source = ImageResources.NewSafeBitmapImage("pack://application:,,,/Res/hash1.png");
                            //else if (hours < 100)
                            //    hashcheckimg.Source = ImageResources.NewSafeBitmapImage("pack://application:,,,/Res/hash2.png");
                            //else if (hours < 200)
                            //    hashcheckimg.Source = ImageResources.NewSafeBitmapImage("pack://application:,,,/Res/hash3.png");
                            //else if (hours < 500)
                            //    hashcheckimg.Source = ImageResources.NewSafeBitmapImage("pack://application:,,,/Res/hash4.png");
                            //else if (hours < 1000)
                            //    hashcheckimg.Source = ImageResources.NewSafeBitmapImage("pack://application:,,,/Res/hash5.png");
                            //else if (hours < 2000)
                            //    hashcheckimg.Source = ImageResources.NewSafeBitmapImage("pack://application:,,,/Res/hash6.png");
                            //else if (hours < 5000)
                            //    hashcheckimg.Source = ImageResources.NewSafeBitmapImage("pack://application:,,,/Res/hash7.png");
                            //else if (hours < 10000)
                            //    hashcheckimg.Source = ImageResources.NewSafeBitmapImage("pack://application:,,,/Res/hash8.png");
                            //else
                            //    hashcheckimg.Source = ImageResources.NewSafeBitmapImage("pack://application:,,,/Res/hash9.png");

                            hashcheckimg.Source = ImageResources.NewSafeBitmapImage("pack://application:,,,/Res/hash.png");
                            hashcheckimg.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
                            hashcheckimg.ToolTip = "Is there no modified archive/supermodel MOD player exclusive logo".Translate() + ' ' + ((int)(Math.Sqrt(hours))).ToString("X");
                            hashcheckimg.Width = 64;
                            hashcheckimg.Height = 64;
                            Grid.SetColumn(hashcheckimg, 4);
                            Grid.SetRowSpan(hashcheckimg, 2);
                            Main.ToolBar.gdPanel.Children.Add(hashcheckimg);
                        }
                    }
                    else
                    {
                        if (hashcheckimg != null)
                        {
                            Main.ToolBar.gdPanel.Children.Remove(hashcheckimg);
                            hashcheckimg = null;
                        }
                    }
                });

            }
        }
        public void SetZoomLevel(double zl)
        {
            Set.ZoomLevel = zl;
            //this.Height = 500 * zl;
            MGrid.Width = 500 * zl;
            if (petHelper != null)
            {
                petHelper.Width = 50 * zl;
                petHelper.Height = 50 * zl;
                petHelper.ReloadLocation();
            }
        }



        //private DateTime timecount = DateTime.Now;
        /// <summary>
        /// Save the Settings
        /// </summary>
        public void Save()
        {
            // SaveSchedule
            ScheduleTask?.Save();

            // SaveInventory
            foreach (var v in GameSavesData.Data.Assemblage.Keys.Where(x => x.StartsWith("item")))
                GameSavesData.Data.Remove(v);
            for (int i = 0; i < Items.Count; i++)
            {
                GameSavesData.Data.Add(LPSConvert.SerializeObjectToLine<Line>(Items[i], "item" + i.ToString()));
            }

            try
            {
                foreach (MainPlugin mp in Plugins)
                    mp.Save();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString(), "Save error caused by the plugin".Translate());
            }
            if (Set != null)
            {
                var st = Set.SaveTimesPP;
                if (Main != null)
                {
                    Set.VoiceVolume = Main.PlayVoiceVolume;
                    List<string> list = new List<string>();
                    Foods.FindAll(x => x.Star).ForEach(x => list.Add(x.Name));
                    Set["betterbuy"]["star"].info = string.Join(",", list);
                    //GameSavesData.Statistics[(gint)"stat_time"] = (int)(DateTime.Now - timecount).TotalMinutes;
                    //timecount = DateTime.Now;
                }
                Set.StartRecordLastPoint = new Point(Dispatcher.Invoke(() => Left), Dispatcher.Invoke(() => Top));
                if (PrefixSave == "" && File.Exists(ExtensionValue.BaseDirectory + @"\Setting.lps"))
                {
                    if (new FileInfo(ExtensionValue.BaseDirectory + @"\Setting.lps").Length < 10)
                    {
                        File.Delete(ExtensionValue.BaseDirectory + @"\Setting.lps");
                    }
                    else
                    {
                        if (File.Exists(ExtensionValue.BaseDirectory + @"\Setting.bkp"))
                            File.Delete(ExtensionValue.BaseDirectory + @"\Setting.bkp");
                        File.Move(ExtensionValue.BaseDirectory + @"\Setting.lps", ExtensionValue.BaseDirectory + @"\Setting.bkp");
                    }

                }
                File.WriteAllText(ExtensionValue.BaseDirectory + @$"\Setting{PrefixSave}.lps", Set.ToString());

                if (!Directory.Exists(ExtensionValue.BaseDirectory + @"\Saves"))
                    Directory.CreateDirectory(ExtensionValue.BaseDirectory + @"\Saves");
                if (!Directory.Exists(ExtensionValue.BaseDirectory + @"\Saves_BKP"))
                    Directory.CreateDirectory(ExtensionValue.BaseDirectory + @"\Saves_BKP");

                if (Core != null && Core.Save != null)
                {
                    var ds = new List<string>(Directory.GetFiles(ExtensionValue.BaseDirectory + @"\Saves", $"Save{PrefixSave}_*.lps")).OrderBy(x =>
                    {
                        if (int.TryParse(x.Split('_').Last().Split('.')[0], out int i))
                            return i;
                        return 0;
                    }).ToList();
                    while (ds.Count > Set.BackupSaveMaxNum)
                    {
                        File.Delete(ds[0]);
                        ds.RemoveAt(0);
                    }

                    if (File.Exists(ExtensionValue.BaseDirectory + $"\\Saves\\Save{PrefixSave}_{st}.lps"))
                        File.Delete(ExtensionValue.BaseDirectory + $"\\Saves\\Save{PrefixSave}_{st}.lps");

                    var saveslps = GameSavesData.ToLPS();
                    var savesdata = saveslps.ToString();

                    int hash = Math.Abs(saveslps.GetHashCode() % 255);
                    if (File.Exists(ExtensionValue.BaseDirectory + $"\\Saves_BKP\\Save{PrefixSave}_{hash:X}.lps"))
                        File.Delete(ExtensionValue.BaseDirectory + $"\\Saves_BKP\\Save{PrefixSave}_{hash:X}.lps");

                    File.WriteAllText(ExtensionValue.BaseDirectory + $"\\Saves\\Save{PrefixSave}_{st}.lps", savesdata);
                    File.WriteAllText(ExtensionValue.BaseDirectory + $"\\Saves_BKP\\Save{PrefixSave}_{hash:X}.lps", savesdata);

                    if (File.Exists(ExtensionValue.BaseDirectory + @"\Save.lps"))
                    {
                        if (File.Exists(ExtensionValue.BaseDirectory + @"\Save.bkp"))
                            File.Delete(ExtensionValue.BaseDirectory + @"\Save.bkp");
                        File.Move(ExtensionValue.BaseDirectory + @"\Save.lps", ExtensionValue.BaseDirectory + @"\Save.bkp");
                    }
                }
            }
        }
        /// <summary>
        /// </summary>
        public void LoadDIY()
        {
            Main.ToolBar.MenuDIY.Items.Clear();

            if (App.MutiSaves.Count > 1)
            {
                var list = App.MutiSaves.ToList();
                foreach (var win in App.MainWindows)
                {
                    list.Remove(win.PrefixSave);
                }
                list.Remove(PrefixSave);
                if (list.Count > 0)
                {
                    var menuItem = new System.Windows.Controls.MenuItem()
                    {
                        Header = "VPet Multiple ".Translate(),
                        HorizontalContentAlignment = System.Windows.HorizontalAlignment.Center,
                    };
                    foreach (var win in list)
                    {
                        var mo = new System.Windows.Controls.MenuItem()
                        {
                            Header = win.Translate(),
                            HorizontalContentAlignment = System.Windows.HorizontalAlignment.Center,
                        };
                        mo.Click += (s, e) =>
                        {
                            if (App.MainWindows.FirstOrDefault(x => x.PrefixSave.Trim('-') == win) == null)
                            {
                                new MainWindow(win, this).Show();
                            }
                            menuItem.Items.Remove(s);
                        };
                        menuItem.Items.Add(mo);
                    }
                    Main.ToolBar.MenuDIY.Items.Add(menuItem);
                }
            }

            foreach (ISub sub in Set["diy"])
                Main.ToolBar.AddMenuButton(ToolBar.MenuType.DIY, sub.Name, () =>
                {
                    Main.ToolBar.Visibility = Visibility.Collapsed;
                    RunDIY(sub.Info);
                });

            foreach (MainPlugin mp in Plugins)
                try
                {
                    mp.LoadDIY();
                }
                catch (Exception e)
                {
                    MessageBoxX.Show(e.ToString(), "Custom button loading error due to plug-in".Translate() + '-' + mp.PluginName);
                }
            Main.ToolBar.LoadDIY();
        }
        /// <summary>
        /// </summary>
        public void LoadPetHelper()
        {
            petHelper = new PetHelper(this);
            petHelper.Show();
        }

        public void RunDIY(string content)
        {
            if (content.Contains(@":\"))
            {
                try
                {
                    if (!Set["v"][(gbol)"rundiy"])
                    {
                        MessageBoxX.Show("Due to the design of the operating system, programs launched by our software may be categorized as sub-processes of our software in the task manager, which may result in higher displayed CPU/memory usage.".Translate(),
                            "One-time Notice Regarding Higher Displayed CPU/Memory Usage".Translate());
                        Set["v"][(gbol)"rundiy"] = true;
                    }
                    ProcessStartInfo startInfo = new ProcessStartInfo();
                    startInfo.FileName = content;
                    startInfo.UseShellExecute = false;
                    Process.Start(startInfo);
                }
                catch
                {
                    try
                    {
                        try
                        {
                            Process.Start(content);
                        }
                        catch
                        {
                            var psi = new ProcessStartInfo
                            {
                                FileName = content,
                                UseShellExecute = true
                            };
                            Process.Start(psi);
                        }
                    }
                    catch (Exception e)
                    {
                        MessageBoxX.Show("Hotkey failed to run: The specified content could not be run".Translate() + '\n' + e.Message);
                    }
                }
            }
            else if (content.Contains("://"))
            {
                try
                {
                    ExtensionFunction.StartURL(content);
                }
                catch (Exception e)
                {
                    MessageBoxX.Show("Hotkey failed to run: The specified content could not be run".Translate() + '\n' + e.Message);
                }
            }
            else
            {
                try
                {
                    SendKeys.SendWait(content);
                }
                catch (Exception e)
                {
                    MessageBoxX.Show("Hotkey failed to run: The specified content could not be run".Translate() + '\n' + e.Message);
                }
            }
        }

        public void ShowSetting(int page = -1)
        {
            if (page >= 0 && page <= 6)
                winSetting.MainTab.SelectedIndex = page;
            winSetting.Show();
        }
        public void ShowWorkMenu(Work.WorkType type)
        {
            if (winWorkMenu == null)
            {
                winWorkMenu = new winWorkMenu(this, type);
                winWorkMenu.Show();
            }
            else
            {
                winWorkMenu.LsbCategory.SelectedIndex = (int)type;
                winWorkMenu.Focus();
                winWorkMenu.Topmost = true;
            }
        }
        public void ShowBetterBuy(Food.FoodType type)
        {
            winBetterBuy.Show(type);
        }
        public void ShowGallery()
        {
            if (winGallery != null)
            {
                winGallery.Show();
                winGallery.Focus();
            }
            else
            {
                winGallery = new winGallery(this);
                winGallery.Show();
            }
        }
        int lowstrengthAskCountFood = 20;
        int lowstrengthAskCountDrink = 20;
        private void lowStrength()
        {
            var sm = Core.Save.StrengthMax;
            var sm75 = sm * 0.70;
            if (Set.AutoBuy && Core.Save.Money >= 100)
            {
                var havemoney = Core.Save.Money * 0.8;
                List<Food> food = Foods.FindAll(x => x.Price >= 2 && x.Health >= -5 && x.Exp >= -10 && x.Likability >= 0 && x.Price < havemoney
                 && !x.IsOverLoad()
                );

                if ((Core.Save.StrengthFood + Core.Save.StoreStrengthFood) < sm75)
                {
                    food = food.FindAll(x => x.Type == Food.FoodType.Meal && x.StrengthFood > Math.Min(sm * 0.20, 100));
                    if (food.Count == 0)
                        return;
                    var item = food[Function.Rnd.Next(food.Count)];
                    Core.Save.Money -= item.Price * 1.2;
                    TakeItemHandle(item, 1, "autofood");
                    TakeItem(item);
                    GameSavesData.Statistics[(gint)"stat_autobuy"]++;
                    Main.Display(item.GetGraph(), item.ImageSource, Main.DisplayToNomal);
                }
                else if ((Core.Save.StrengthDrink + Core.Save.StoreStrengthDrink) < sm75)
                {
                    food = food.FindAll(x => x.Type == Food.FoodType.Drink && x.StrengthDrink > Math.Min(sm * 0.20, 50));
                    if (food.Count == 0)
                        return;
                    var item = food[Function.Rnd.Next(food.Count)];
                    Core.Save.Money -= item.Price * 1.2;
                    TakeItemHandle(item, 1, "autodrink");
                    TakeItem(item);
                    GameSavesData.Statistics[(gint)"stat_autobuy"]++;
                    Main.Display(item.GetGraph(), item.ImageSource, Main.DisplayToNomal);
                }
                else if (Core.Save.Feeling < Core.Save.FeelingMax * 0.50)
                {
                    if (Set.AutoGift)
                    {
                        food = food.FindAll(x => x.Type == Food.FoodType.Gift && x.Feeling > Math.Min(Core.Save.FeelingMax * 0.10, 50));
                        if (food.Count == 0)
                            return;
                    }
                    else
                    {
                        food = food.FindAll(x => x.Type == Food.FoodType.Snack && x.Feeling > Math.Min(Core.Save.FeelingMax * 0.10, 40));
                        if (food.Count == 0)
                            return;
                    }
                    var item = food[Function.Rnd.Next(food.Count)];
                    Core.Save.Money -= item.Price * 1.2;
                    TakeItemHandle(item, 1, "autofeel");
                    TakeItem(item);
                    GameSavesData.Statistics[(gint)"stat_autogift"]++;
                    Main.Display(item.GetGraph(), item.ImageSource, Main.DisplayToNomal);
                }
            }
            else if (Core.Save.Mode == IGameSave.ModeType.Happy || Core.Save.Mode == IGameSave.ModeType.Nomal)
            {
                if (Core.Save.StrengthFood < sm75 && Function.Rnd.Next(lowstrengthAskCountFood--) == 0)
                {
                    lowstrengthAskCountFood = Set.InteractionCycle;
                    var like = Core.Save.Likability < 40 ? 0 : (Core.Save.Likability < 70 ? 1 : (Core.Save.Likability < 100 ? 2 : 3));
                    var txt = LowFoodText.FindAll(x => x.Mode == LowText.ModeType.H && (int)x.Like <= like);
                    if (txt.Count != 0)
                        if (Core.Save.StrengthFood > sm * 0.60)
                        {
                            txt = txt.FindAll(x => x.Strength == LowText.StrengthType.L);
                            if (txt.Count != 0)
                                Main.Say(txt[Function.Rnd.Next(txt.Count)].TranslateTextConvert(Main));
                        }
                        else if (Core.Save.StrengthFood > sm * 0.40)
                        {
                            txt = txt.FindAll(x => x.Strength == LowText.StrengthType.M);
                            if (txt.Count != 0)
                                Main.Say(txt[Function.Rnd.Next(txt.Count)].TranslateTextConvert(Main));
                        }
                        else
                        {
                            txt = txt.FindAll(x => x.Strength == LowText.StrengthType.S);
                            if (txt.Count != 0)
                                Main.Say(txt[Function.Rnd.Next(txt.Count)].TranslateTextConvert(Main));
                        }
                    Main.DisplayStopForce(() => Main.Display(GraphType.Switch_Hunger, AnimatType.Single, Main.DisplayToNomal));
                    return;
                }
                if (Core.Save.StrengthDrink < sm75 && Function.Rnd.Next(lowstrengthAskCountDrink--) == 0)
                {
                    lowstrengthAskCountDrink = Set.InteractionCycle;
                    var like = Core.Save.Likability < 40 ? 0 : (Core.Save.Likability < 70 ? 1 : (Core.Save.Likability < 100 ? 2 : 3));
                    var txt = LowDrinkText.FindAll(x => x.Mode == LowText.ModeType.H && (int)x.Like <= like);
                    if (txt.Count != 0)
                        if (Core.Save.StrengthDrink > sm * 0.60)
                        {
                            txt = txt.FindAll(x => x.Strength == LowText.StrengthType.L);
                            if (txt.Count != 0)
                                Main.Say(txt[Function.Rnd.Next(txt.Count)].TranslateTextConvert(Main));
                        }
                        else if (Core.Save.StrengthDrink > sm * 0.40)
                        {
                            txt = txt.FindAll(x => x.Strength == LowText.StrengthType.M);
                            if (txt.Count != 0)
                                Main.Say(txt[Function.Rnd.Next(txt.Count)].TranslateTextConvert(Main));
                        }
                        else
                        {
                            txt = txt.FindAll(x => x.Strength == LowText.StrengthType.S);
                            if (txt.Count != 0)
                                Main.Say(txt[Function.Rnd.Next(txt.Count)].TranslateTextConvert(Main));
                        }
                    Main.DisplayStopForce(() => Main.Display(GraphType.Switch_Thirsty, AnimatType.Single, Main.DisplayToNomal));
                    return;
                }
            }
            else
            {
                var sm20 = sm * 0.20;
                if (Core.Save.StrengthFood < sm * 0.60 && Function.Rnd.Next(lowstrengthAskCountFood--) == 0)
                {
                    lowstrengthAskCountFood = Set.InteractionCycle;
                    var like = Core.Save.Likability < 40 ? 0 : (Core.Save.Likability < 70 ? 1 : (Core.Save.Likability < 100 ? 2 : 3));
                    var txt = LowFoodText.FindAll(x => x.Mode == LowText.ModeType.L && (int)x.Like < like);
                    if (Core.Save.StrengthFood > sm * 0.40)
                    {
                        txt = txt.FindAll(x => x.Strength == LowText.StrengthType.L);
                        if (txt.Count != 0)
                            Main.Say(txt[Function.Rnd.Next(txt.Count)].TranslateTextConvert(Main));
                    }
                    else if (Core.Save.StrengthFood > sm20)
                    {
                        txt = txt.FindAll(x => x.Strength == LowText.StrengthType.M);
                        if (txt.Count != 0)
                            Main.Say(txt[Function.Rnd.Next(txt.Count)].TranslateTextConvert(Main));
                    }
                    else
                    {
                        txt = txt.FindAll(x => x.Strength == LowText.StrengthType.S);
                        if (txt.Count != 0)
                            Main.Say(txt[Function.Rnd.Next(txt.Count)].TranslateTextConvert(Main));
                    }
                    Main.DisplayStopForce(() => Main.Display(GraphType.Switch_Hunger, AnimatType.Single, Main.DisplayToNomal));
                    return;
                }
                if (Core.Save.StrengthDrink < sm * 0.60 && Function.Rnd.Next(lowstrengthAskCountDrink--) == 0)
                {
                    lowstrengthAskCountDrink = Set.InteractionCycle;
                    var like = Core.Save.Likability < 40 ? 0 : (Core.Save.Likability < 70 ? 1 : (Core.Save.Likability < 100 ? 2 : 3));
                    var txt = LowDrinkText.FindAll(x => x.Mode == LowText.ModeType.L && (int)x.Like < like);
                    if (Core.Save.StrengthDrink > sm * 0.40)
                    {
                        txt = txt.FindAll(x => x.Strength == LowText.StrengthType.L);
                        if (txt.Count != 0)
                            Main.Say(txt[Function.Rnd.Next(txt.Count)].TranslateTextConvert(Main));
                    }
                    else if (Core.Save.StrengthDrink > sm20)
                    {
                        txt = txt.FindAll(x => x.Strength == LowText.StrengthType.M);
                        if (txt.Count != 0)
                            Main.Say(txt[Function.Rnd.Next(txt.Count)].TranslateTextConvert(Main));
                    }
                    else
                    {
                        txt = txt.FindAll(x => x.Strength == LowText.StrengthType.S);
                        if (txt.Count != 0)
                            Main.Say(txt[Function.Rnd.Next(txt.Count)].TranslateTextConvert(Main));
                    }
                    Main.DisplayStopForce(() => Main.Display(GraphType.Switch_Thirsty, AnimatType.Single, Main.DisplayToNomal));
                    return;
                }
            }


        }
        /// <summary>
        /// </summary>
        public event Action<Food> Event_TakeItem;
        /// <summary>
        /// </summary>
        public event Action<Food, int, string> Event_TakeItemHandle;
        /// <summary>
        /// </summary>
        /// <param name="from">Source</param>
        public void TakeItemHandle(Food item, int count, string from)
        {
            Event_TakeItemHandle?.Invoke(item, count, from);
        }
        /// <summary>
        /// </summary>
        public void TakeItem(Food item)
        {
            Main.LastInteractionTime = DateTime.Now;
            DateTime now = DateTime.Now;
            DateTime eattime = GameSavesData["buytime"].GetDateTime(item.Name, now);
            double eattimes = 0;
            if (eattime > now)
            {
                eattimes = (eattime - now).TotalHours;
            }
            double eatuseps;
            if (item.Type == FoodType.Gift)
                eatuseps = Math.Max(0.5, 1 - eattimes * eattimes * 0.01);
            else
                eatuseps = Math.Max(0.5, 1 - eattimes * eattimes * 0.02);
            Core.Save.EatFood(item, eatuseps);
            eattimes += Math.Max(0.5, Math.Min(4, 2 - (item.Likability + item.Feeling / 2) / 5));
            GameSavesData["buytime"].SetDateTime(item.Name, now.AddHours(eattimes));
            item.LoadEatTimeSource(this);
            item.NotifyOfPropertyChange("Description");

            //Core.Save.Mode = Core.Save.CalMode();
            // Statistics
            GameSavesData.Statistics[(gint)"stat_buytimes"]++;
            GameSavesData.Statistics[(gint)("buy_" + item.Name)]++;
            GameSavesData.Statistics[(gdbe)"stat_betterbuy"] += item.Price;
            switch (item.Type)
            {
                case Food.FoodType.Food:
                    GameSavesData.Statistics[(gdbe)"stat_bb_food"] += item.Price;
                    break;
                case Food.FoodType.Drink:
                    GameSavesData.Statistics[(gdbe)"stat_bb_drink"] += item.Price;
                    break;
                case Food.FoodType.Drug:
                    GameSavesData.Statistics[(gdbe)"stat_bb_drug"] += item.Price;
                    GameSavesData.Statistics[(gdbe)"stat_bb_drug_exp"] += item.Exp;
                    break;
                case Food.FoodType.Snack:
                    GameSavesData.Statistics[(gdbe)"stat_bb_snack"] += item.Price;
                    break;
                case Food.FoodType.Functional:
                    GameSavesData.Statistics[(gdbe)"stat_bb_functional"] += item.Price;
                    break;
                case Food.FoodType.Meal:
                    GameSavesData.Statistics[(gdbe)"stat_bb_meal"] += item.Price;
                    break;
                case Food.FoodType.Gift:
                    GameSavesData.Statistics[(gdbe)"stat_bb_gift"] += item.Price;
                    GameSavesData.Statistics[(gdbe)"stat_bb_gift_like"] += item.Likability;
                    break;
            }

            Event_TakeItem?.Invoke(item);
        }

        public void RunAction(string action)
        {
            switch (action)
            {
                case "DisplayNomal":
                    Main.DisplayNomal();
                    break;
                case "DisplayToNomal":
                    Main.DisplayToNomal();
                    break;
                case "DisplayTouchHead":
                    Main.DisplayTouchHead();
                    break;
                case "DisplayTouchBody":
                    Main.DisplayTouchBody();
                    break;
                case "DisplayIdel":
                    Main.DisplayIdel();
                    break;
                case "DisplayIdel_StateONE":
                    Main.DisplayIdel_StateONE();
                    break;
                case "DisplaySleep":
                    Main.DisplaySleep();
                    break;
                case "DisplayRaised":
                    Main.DisplayRaised();
                    break;
                case "DisplayMove":
                    Main.DisplayMove();
                    break;
            }
        }
        /// <summary>
        /// </summary>
        private void StatisticsCalHandle()
        {
            var stat = GameSavesData.Statistics;
            var save = Core.Save;
            stat["stat_money"] = (SetObject)save.Money;
            stat["stat_level"] = save.Level;
            stat["stat_likability"] = save.Likability;

            stat[(gi64)"stat_total_time"] += (int)Set.LogicInterval;
            switch (Main.State)
            {
                case Main.WorkingState.Work:
                    if (Main.NowWork.Type == Work.WorkType.Work)
                        stat[(gi64)"stat_work_time"] += (int)Set.LogicInterval;
                    else
                        stat[(gi64)"stat_study_time"] += (int)Set.LogicInterval;
                    break;
                case Main.WorkingState.Sleep:
                    stat[(gi64)"stat_sleep_time"] += (int)Set.LogicInterval;
                    break;
            }
            if (save.Mode == IGameSave.ModeType.Ill)
            {
                if (save.Money < 100)
                    stat["stat_ill_nomoney"] = 1;
            }
            if (save.Money < save.Level)
            {
                stat["stat_level_g_money"] = 1;
            }
            if (save.Feeling < 1)
            {
                stat["stat_0_feel"] = 1;
                if (save.StrengthDrink < 1)
                    stat["stat_0_f_sd"] = 1;
            }
            if (save.Strength < 1 && save.Feeling < 1 && save.StrengthFood < 1 && save.StrengthDrink < 1)
                stat["stat_0_all"] = 1;
            if (save.StrengthFood < 1)
                stat["stat_0_strengthfood"] = 1;
            if (save.StrengthDrink < 1)
            {
                stat["stat_0_strengthdrink"] = 1;
                if (save.StrengthFood < 1)
                    stat["stat_0_sd_sf"] = 1;
            }
            var smm = save.StrengthMax - 1;
            if (save.Strength > smm && save.Feeling > save.FeelingMax - 1 && save.StrengthFood > smm && save.StrengthDrink > smm)
                stat[(gint)"stat_100_all"]++;
        }
        /// <summary>
        /// </summary>
        public bool SavesLoad(ILPS lps)
        {
            if (lps == null)
                return false;
            if (string.IsNullOrWhiteSpace(lps.ToString()))
                return false;
            GameSave_v2 tmp;
            if (GameSavesData != null)
                tmp = new GameSave_v2(lps, GameSavesData);
            else
            {
                var data = new LPS_D();
                foreach (var item in Set.PetData_OLD)
                {
                    if (item.Name.Contains("_"))
                    {
                        var strs = Sub.Split(item.Name, "_", 1);
                        data[strs[0]][(gstr)strs[1]] = item.Info;
                    }
                    else
                        data.Add(new Line(item.Name, item.Info));
                }
                tmp = new GameSave_v2(lps, null, olddata: data);
            }
            if (tmp.GameSave == null)
                return false;
            if (tmp.GameSave.Money == 0 && tmp.GameSave.Likability == 0 && tmp.GameSave.Exp == 0
                && tmp.GameSave.StrengthDrink == 0 && tmp.GameSave.StrengthFood == 0)
                return false;
            if (tmp.GameSave.Exp < -1000000000)
            {
                tmp.GameSave.Exp = 1000000;
                tmp.Data[(gbol)"round"] = true;
                Dispatcher.Invoke(() => NoticeBox.Show("Detected experience value exceeding 9,223,372,036 causing arithmetic overflow\\nAutomatically corrected".Translate(), "Data Overflow Warning".Translate()));

            }
            if (tmp.GameSave.Money < -1000000000)
            {
                tmp.GameSave.Money = 100000;
                Dispatcher.Invoke(() => NoticeBox.Show("Detected money exceeding 9,223,372,036 causing arithmetic overflow\\nAutomatically corrected".Translate(), "Data Overflow Warning".Translate()));
            }

            if (tmp.Data[(gbol)"round"])
            {
                Dispatcher.Invoke(() => NoticeBox.Show("You have encountered data overflow before, and the current value has been automatically added according to the game duration".Translate(), "Data overflow recovery".Translate()));
                var totalhour = (int)(tmp.Statistics[(gint)"stat_total_time"] / 3600);
                if (totalhour < 500)
                {
                    tmp.GameSave.Exp += totalhour * 200;
                }
                else
                {
                    double lm = Math.Sqrt(totalhour / 500);
                    tmp.GameSave.LevelMax += (int)lm;
                    tmp.GameSave.Exp += (totalhour % 500 + (lm - (int)lm) * 500) * 200;

                }
                tmp.GameSave.LikabilityMax += totalhour / 10;
                tmp.Data[(gbol)"round"] = false;
            }
            GameSavesData = tmp;
            Core.Save = tmp.GameSave;
            Items.Clear();
            foreach (var line in GameSavesData.Data.Assemblage.Where(x => x.Key.StartsWith("item")))
            {
                var itm = Item.CreateItem(this, line.Value);
                Dispatcher.Invoke(() => itm.LoadSource(this));
                ItemsAdd(itm);
            }
            HashCheck = HashCheck;
            GameSavesData.GameSave.Event_LevelUp += LevelUP;
            return true;
        }



        private bool? AudioPlayingVolumeOK = null;
        /// <summary>
        /// </summary>
        public float AudioPlayingVolume()
        {
            if (AudioPlayingVolumeOK == null)
            {
                try
                {
                    using (var enumerator = new MMDeviceEnumerator())
                    {
                        if (enumerator.HasDefaultAudioEndpoint(DataFlow.Render, Role.Console))
                        {
                            var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);
                            AudioPlayingVolumeOK = true;
                            return device.AudioMeterInformation.MasterPeakValue;
                        }
                        else
                        {
                            AudioPlayingVolumeOK = false;
                            return -1;
                        }
                    }
                }
                catch
                {
                    AudioPlayingVolumeOK = false;
                    return -1;
                }
            }
            else if (AudioPlayingVolumeOK == false)
            {
                return -1;
            }
            try
            {
                using (var enumerator = new MMDeviceEnumerator())
                {
                    if (enumerator.HasDefaultAudioEndpoint(DataFlow.Render, Role.Console))
                    {
                        var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);
                        return device.AudioMeterInformation.MasterPeakValue;
                    }
                    else
                    {
                        return -1;
                    }
                }
            }
            catch
            {
                return -1;
            }
        }
        /// <summary>
        /// </summary>
        private void Handle_Music(Main obj)
        {
            if (MusicTimer.Enabled == false && Core.Graph.FindGraphs("music", AnimatType.B_Loop, Core.Save.Mode) != null &&
                Main.IsIdel && AudioPlayingVolume() > Set.MusicCatch)
            {
                catch_MusicVolSum = 0;
                catch_MusicVolCount = 0;
                CurrMusicType = null;
                MusicTimer.Start();
                Task.Run(() =>
                {
                    Thread.Sleep(3000);

                    if (CurrMusicType != null && Main.IsIdel)
                    {
                        GameSavesData.Statistics[(gint)"stat_music"]++;
                        Main.Display(Core.Graph.FindGraph("music", AnimatType.A_Start, Core.Save.Mode), Display_Music);
                    }
                    else
                    {
                        MusicTimer.Stop();
                    }
                });
            }
        }
        private void Display_Music()
        {
            if (CurrMusicType.HasValue)
            {
                if (CurrMusicType.Value)
                {
                    var mg = Core.Graph.FindGraph("music", AnimatType.Single, Core.Save.Mode);
                    mg ??= Core.Graph.FindGraph("music", AnimatType.B_Loop, Core.Save.Mode);
                    Main.Display(mg, Display_Music);
                }
                else
                {
                    Main.Display(Core.Graph.FindGraph("music", AnimatType.B_Loop, Core.Save.Mode), Display_Music);
                }
            }
            else
            {
                Main.Display("music", AnimatType.C_End, Main.DisplayToNomal);
            }
        }
        private void MusicTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (!(Main.IsIdel || Main.DisplayType.Name == "music"))
                return;
            catch_MusicVolSum += AudioPlayingVolume();
            catch_MusicVolCount++;
            if (catch_MusicVolCount >= 10)
            {
                double ans = catch_MusicVolSum / catch_MusicVolCount;
                catch_MusicVolSum /= 4;
                catch_MusicVolCount /= 4;
                if (ans > Set.MusicCatch)
                {
                    var bef = CurrMusicType;
                    CurrMusicType = ans > Set.MusicMax;
                    if (bef != null && bef != CurrMusicType)
                        Display_Music();
                    MusicTimer.Start();
                }
                else
                {
                    CurrMusicType = null;
                    if (Main.DisplayType.Name == "music")
                        Main.Display("music", AnimatType.C_End, Main.DisplayToNomal);
                }
            }
            else
            {
                MusicTimer.Start();
            }
        }

        public Timer MusicTimer;
        private double catch_MusicVolSum;
        private int catch_MusicVolCount;
        /// <summary>
        /// </summary>
        public bool? CurrMusicType { get; private set; }

        int LastDiagnosisTime = 0;

        /// <summary>
        /// </summary>
        public void DiagnosisUPLoad()
        {
            if (!Set.DiagnosisDayEnable)
                return;
            if (!Set.Diagnosis)
                return;
            if (!HashCheck)
                return;
            if (LastDiagnosisTime++ < Set.DiagnosisInterval)
                return;
            LastDiagnosisTime = 0;
            string _url = "https://report.exlb.net/VPET/Report";
            StringBuilder sb = new StringBuilder();
            sb.Append("action=data");
            sb.Append($"&ver={version}");
            sb.Append("&save=");
            sb.AppendLine(HttpUtility.UrlEncode(Core.Save.ToLine().ToString() + Set.ToString()));
#pragma warning disable SYSLIB0014
            var request = (HttpWebRequest)WebRequest.Create(_url);
#pragma warning restore SYSLIB0014
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";//ContentType
            byte[] byteData = Encoding.UTF8.GetBytes(sb.ToString());
            int length = byteData.Length;
            request.ContentLength = length;
            using (Stream writer = request.GetRequestStream())
            {
                writer.Write(byteData, 0, length);
                writer.Close();
                writer.Dispose();
            }
            string responseString;
            using (var response = (HttpWebResponse)request.GetResponse())
            {
                responseString = new StreamReader(response.GetResponseStream(), Encoding.UTF8).ReadToEnd();
                response.Dispose();
            }
            if (responseString == "IP times Max")
            {
                Set.DiagnosisDayEnable = false;
            }
#if DEBUG
            else
            {
                throw new Exception("Diagnostic upload failed");
            }
#endif

        }
        /// <summary>
        /// </summary>
        public bool CloseConfirm { get; private set; } = true;

        public List<ITalkAPI> TalkAPI { get; } = new List<ITalkAPI>();
        /// <summary>
        /// </summary>
        public int TalkAPIIndex = -1;
        /// <summary>
        /// </summary>
        public ITalkAPI TalkBoxCurr
        {
            get
            {
                if (TalkAPIIndex == -1)
                    return null;
                return TalkAPI[TalkAPIIndex];
            }
        }

        Grid IMainWindow.MGHost => MGHost;

        Grid IMainWindow.PetGrid => MGrid;
        internal MWController MWController { get; set; }
        /// <summary>
        /// </summary>
        public void RemoveTalkBox()
        {
            if (TalkBox != null)
            {
                Main.ToolBar.MainGrid.Children.Remove(TalkBox);
                TalkBox = null;
            }
            if (TalkAPIIndex == -1)
                return;
            Main.ToolBar.MainGrid.Children.Remove(TalkAPI[TalkAPIIndex].This);
        }
        /// <summary>
        /// </summary>
        public void LoadTalkDIY()
        {
            RemoveTalkBox();
            if (TalkAPIIndex == -1)
                return;
            Main.ToolBar.MainGrid.Children.Add(TalkAPI[TalkAPIIndex].This);
        }
        /// <summary>
        /// </summary>
        public bool WorkCheck(Work work)
        {
            if (HashCheck && work.IsOverLoad())
            {
                if (Set["gameconfig"].GetBool("noAutoCal"))
                {
                    if (MessageBoxX.Show("Current WorkData attributes are overpowered, continue working?\nOverpowered work may cause unpredictable errors\nOverpowered work does not affect most achievement unlocks\nYou can enable auto-calculation in Settings to set reasonable values"
                        .Translate(), "Overpowered work warning".Translate(), MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                    {
                        return false;
                    }
                    HashCheck = false;
                }
                else
                {
                    MessageBoxX.Show("The current work data attribute is supermodel, and it has been automatically cancelled".Translate(), "Overpowered work warning".Translate());
                    return false;
                }
            }
            return true;
        }
        /// <summary>
        /// </summary>
        public void GameInitialization()
        {
            Console.SetOut(new ConsoleRedirectWriter(ActivityLogs));

            Console.WriteLine($"VPet Simulator {version}");

            App.MainWindows.Add(this);
            try
            {
                if (new FileInfo(ExtensionValue.BaseDirectory + @$"\Setting{PrefixSave}.lps").Exists)
                {
                    Set = new Setting(this, File.ReadAllText(ExtensionValue.BaseDirectory + @$"\Setting{PrefixSave}.lps"));
                }
                if (PrefixSave == "" && (Set == null || (Set != null && !Set["SingleTips"].GetBool("helloworld"))) && File.Exists(ExtensionValue.BaseDirectory + @"\Setting.bkp"))
                {
                    Set = new Setting(this, File.ReadAllText(ExtensionValue.BaseDirectory + @"\Setting.bkp"));
                }

                Set ??= new Setting(this, "Setting#VPET:|\n");

                var visualTree = new FrameworkElementFactory(typeof(Border));
                visualTree.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(BackgroundProperty));
                var childVisualTree = new FrameworkElementFactory(typeof(ContentPresenter));
                childVisualTree.SetValue(ClipToBoundsProperty, true);
                visualTree.AppendChild(childVisualTree);

                Template = new ControlTemplate
                {
                    TargetType = typeof(Window),
                    VisualTree = visualTree,
                };

                InitializeComponent();

                //MGrid.Height = 500 * Set.ZoomLevel;
                MGrid.Width = 500 * Set.ZoomLevel;
                if (Set.OpacityMain)
                    this.Opacity = Set.Opacity;

                double L = 0, T = 0;
                if (Set.StartRecordLast)
                {
                    var point = Set.StartRecordLastPoint;
                    if (point.X != 0 || point.Y != 0)
                    {
                        L = point.X;
                        T = point.Y;
                    }
                }
                else
                {
                    var point = Set.StartRecordPoint;
                    L = point.X; T = point.Y;
                }

                Left = L;
                Top = T;

                // control position inside bounds
                MWController = new MWController(this);
                Core.Controller = MWController;
                Task.Run(() =>
                {
                    double dist;
                    if ((dist = Core.Controller.GetWindowsDistanceLeft()) < 0)
                    {
                        Thread.Sleep(100);
                        Dispatcher.Invoke(() => Left -= dist);
                    }
                    if ((dist = Core.Controller.GetWindowsDistanceRight()) < 0)
                    {
                        Thread.Sleep(100);
                        Dispatcher.Invoke(() => Left += dist);
                    }
                    if ((dist = Core.Controller.GetWindowsDistanceUp()) < 0)
                    {
                        Thread.Sleep(100);
                        Dispatcher.Invoke(() => Top -= dist);
                    }
                    if ((dist = Core.Controller.GetWindowsDistanceDown()) < 0)
                    {
                        Thread.Sleep(100);
                        Dispatcher.Invoke(() => Top += dist);
                    }
                });
                if (Set.TopMost)
                {
                    Topmost = true;
                }

                var modpath = new DirectoryInfo(ModPath + @"\0000_core\pet\vup");
                if (!modpath.Exists)
                {
                    MessageBoxX.Show("Lack Mod Core, Unable start desktop Pet animation\nMissing module Core, can't start up", "Startup error boot error", Panuon.WPF.UI.MessageBoxIcon.Error);
                    Close();
                    return;
                }

            }
            catch (Exception e)
            {
                string errstr = "An error occurred in the game, probably".Translate() + (string.IsNullOrWhiteSpace(CoreMOD.NowLoading) ?
              "Game or Mod".Translate() : $"MOD({CoreMOD.NowLoading})") +
              "If possible, Please send a screenshot of the error message and the action you took before triggering the error to developer: service@exlb.net\\nThanks for your support of the game development\\n".Translate()
              + e.ToString();
                MessageBoxX.Show(errstr, "Game Fatal Error".Translate() + ' ' + "Startup error".Translate(), Panuon.WPF.UI.MessageBoxIcon.Error);
                Close();
            }
        }

        /// <summary>
        /// </summary>
        public MainWindow(string prefixsave, MainWindow basemw = null)
        {
            PrefixSave = prefixsave;
            if (prefixsave != string.Empty && !PrefixSave.StartsWith("-"))
                PrefixSave = '-' + prefixsave;

            Args = new LPS_D();
            foreach (var str in App.Args)
            {
                Args.Add(new Line(str));
            }
            _dwmEnabled = Win32.Dwmapi.DwmIsCompositionEnabled();
            _hwnd = new WindowInteropHelper(this).EnsureHandle();

            GameInitialization();

            if (basemw != null)
            {
                Set["workshop"] = basemw.Set["workshop"];
                Set.Resolution = basemw.Set.Resolution;
            }


            List<DirectoryInfo> Path = new List<DirectoryInfo>();
            Path.AddRange(new DirectoryInfo(ModPath).EnumerateDirectories());

            var workshop = Set["workshop"];
            foreach (ISub ws in workshop)
            {
                Path.Add(new DirectoryInfo(ws.Name));
            }


            Task.Run(() => GameLoad(Path));
        }
        /// <summary>
        /// </summary>
        public List<DirectoryInfo> MODPath { get; private set; }

        public IEnumerable<IModInfo> ModInfo => CoreMODs;

        public IEnumerable<IModInfo> OnModInfo => CoreMODs.FindAll(x => x.IsOnMOD(this));

        /// <summary>
        /// </summary>
        public async Task GameLoad(List<DirectoryInfo> Path)
        {
            MODPath = Path.GroupBy(x => x.FullName).Select(group => group.First()).ToList();
            await Dispatcher.InvokeAsync(new Action(() => LoadingText.Content = "Loading MOD"));
            // Loadmod
            foreach (DirectoryInfo di in MODPath)
            {
                if (!File.Exists(di.FullName + @"\info.lps"))
                    continue;
                await Dispatcher.InvokeAsync(new Action(() => LoadingText.Content = $"Loading MOD: {di.Name}"));
                CoreMODs.Add(new CoreMOD(di, this));
            }

            CoreMOD.NowLoading = null;

            if (App.MainWindows.Count == 1 && Set.LastCacheDate < CoreMODs.Max(x => x.CacheDate))
            {
                Set.LastCacheDate = DateTime.Now;
                if (Directory.Exists(GraphCore.CachePath))
                {
                    Directory.Delete(GraphCore.CachePath, true);
                    Directory.CreateDirectory(GraphCore.CachePath);
                }
            }



            await Dispatcher.InvokeAsync(() =>
            {
                MessageBoxXSettings.Setting.OKButtonContent = "OK".Translate();
                MessageBoxXSettings.Setting.CancelButtonContent = "cancel".Translate();
                MessageBoxXSettings.Setting.YesButtonContent = "Yes".Translate();
                MessageBoxXSettings.Setting.NoButtonContent = "No".Translate();
                PendingBoxSettings.Setting.CancelButtonContent = "cancel".Translate();
                LoadingText.Content = "Try loading game MOD".Translate();
            });

            if (Set.PetGraph == "Default desktop pet")
                Set.PetGraph = "vup";

            var petloader = Pets.Find(x => x.Name == Set.PetGraph);
            petloader ??= Pets[0];
            var tag = petloader.Config.Data.GetString("tag", "all").Split(',');
            LowDrinkText.RemoveAll(x => !x.FindTag(tag));
            LowFoodText.RemoveAll(x => !x.FindTag(tag));
            ClickTexts.RemoveAll(x => !x.FindTag(tag));
            SelectTexts.RemoveAll(x => !x.FindTag(tag));

            await Dispatcher.InvokeAsync(new Action(() => LoadingText.Content = "Attempting to game save".Translate()));
            // Load Save
            if (File.Exists(ExtensionValue.BaseDirectory + @"\Save.lps"))
                try
                {
                    if (!SavesLoad(new LpsDocument(File.ReadAllText(ExtensionValue.BaseDirectory + @"\Save.lps"))))
                    {
                        LoadLatestSave(petloader.PetName);
                    }

                }
                catch (Exception ex)
                {
                    MessageBoxX.Show("Archive damage, unable to load the archive\\nIt may be caused by data overflow/overmodel" + '\n' + ex.Message, "Archive damage".Translate());
                    LoadLatestSave(petloader.PetName);
                }
            else
                LoadLatestSave(petloader.PetName);

            if (!Set["gameconfig"].GetBool("noAutoCal"))
            {
                foreach (Food f in Foods)
                {
                    if (f.IsOverLoad())
                    {
                        f.Price = Math.Max((int)f.RealPrice, 1);
                        f.isoverload = false;
                    }
                }
                //var food = new Food();
                foreach (var selet in SelectTexts)
                {
                    selet.Exp = Math.Max(Math.Min(selet.Exp, 1000), -1000);
                    selet.Feeling = Math.Max(Math.Min(selet.Feeling, 100), -100);
                    selet.Health = Math.Max(Math.Min(selet.Health, 100), -100);
                    selet.Likability = Math.Max(Math.Min(selet.Likability, 50), -50);
                    selet.Money = Math.Max(Math.Min(selet.Money, 1000), -1000);
                    selet.Strength = Math.Max(Math.Min(selet.Strength, 1000), -1000);
                    selet.StrengthDrink = Math.Max(Math.Min(selet.StrengthDrink, 1000), -1000);
                    selet.StrengthFood = Math.Max(Math.Min(selet.StrengthFood, 1000), -1000);
                }
                foreach (var selet in ClickTexts)
                {
                    selet.Exp = Math.Max(Math.Min(selet.Exp, 1000), -1000);
                    selet.Feeling = Math.Max(Math.Min(selet.Feeling, 1000), -1000);
                    selet.Health = Math.Max(Math.Min(selet.Health, 100), -100);
                    selet.Likability = Math.Max(Math.Min(selet.Likability, 50), -50);
                    selet.Money = Math.Max(Math.Min(selet.Money, 1000), -1000);
                    selet.Strength = Math.Max(Math.Min(selet.Strength, 1000), -1000);
                    selet.StrengthDrink = Math.Max(Math.Min(selet.StrengthDrink, 1000), -1000);
                    selet.StrengthFood = Math.Max(Math.Min(selet.StrengthFood, 1000), -1000);
                }
            }

            var food = new Food()
            {
                Name = "1st Birthday Cake",
                Likability = 5,
                Exp = 1000,
                Feeling = 100,
                StrengthDrink = Core.Save.StrengthMax,
                StrengthFood = Core.Save.StrengthMax,
                Type = FoodType.Food,
                isoverload = false,
                Desc = "Lolis's exclusive birthday cake is made of 3 buckets of milk, 2 portions of sugar, 1 egg, and 3 portions of wheat. It is nutritious and can restore all states. You can only eat it on Lolis's birthday."
            };
            food.LoadImageSource(this);
            food.Star = true;
            food.Price = (int)Math.Max(0, food.RealPrice * .5);
            Foods.Add(food);
            food = new Food()
            {
                Name = "2nd Surprise Birthday Cake",
                Likability = Core.Save.Level / 10,
                Exp = Core.Save.Level,
                Feeling = Core.Save.FeelingMax / 20,
                StrengthDrink = Core.Save.StrengthMax / 20,
                StrengthFood = Core.Save.StrengthMax / 20,
                Type = FoodType.Food,
                isoverload = false,
                Desc = "A surprise cake crafted by owners for Lolis! Each bite randomly restores one stat or grants bonus rewards, with mysterious special effects!\\nSecret Recipe:\\n1. Extract seeds from vanilla beans, then immediately grind the vanilla pods into hot milk.\\n2. Add vanilla seeds to the egg batter precisely when sugar is incorporated.\\n3. Infuse with lemon zest essence.\\n4. For chocolate ganache: Use cocoa beans harvested post-autumn, blend with fresh cream while warm.\\n5. Add clarified butter (natural whale oil alternative).\\n6. Extract flavor from whole vanilla pods (uncut) – avoid metal contact.\\n7. Maintain viscosity above satin consistency.\\n8. Whip Chantilly cream (prepared ≤4hrs in advance) in an ice bath while chilled."
            };
            food.LoadImageSource(this);
            food.Star = true;
            food.Price = food.RealPrice;
            Foods.Add(food);

            if (GameSavesData.Data.FindLine("birthday") == null)
            {
                var sf = new FileInfo(ExtensionValue.BaseDirectory + @$"\Setting{PrefixSave}.lps");
                if (sf.Exists)
                {
                    GameSavesData[(gdat)"birthday"] = sf.CreationTime.Date;
                }
                else
                    GameSavesData[(gdat)"birthday"] = DateTime.Now.Date;
            }

            if (string.IsNullOrEmpty(GameSavesData.GameSave.HostName))
            {
                GameSavesData.GameSave.HostName = Environment.UserName;
            }

            //if (GameSavesData.Data.FindLine("HostBDay") == null)
            //{
            //    GameSavesData[(gdat)"HostBDay"] = GameSavesData[(gdat)"birthday"];
            //}


            AutoSaveTimer.Elapsed += AutoSaveTimer_Elapsed;

            if (GameSavesData.Statistics[(gdbe)"stat_bb_food"] < 0 || GameSavesData.Statistics[(gdbe)"stat_bb_drink"] < 0 || GameSavesData.Statistics[(gdbe)"stat_bb_drug"] < 0
                || GameSavesData.Statistics[(gdbe)"stat_bb_snack"] < 0 || GameSavesData.Statistics[(gdbe)"stat_bb_functional"] < 0 || GameSavesData.Statistics[(gdbe)"stat_bb_meal"] < 0
                || GameSavesData.Statistics[(gdbe)"stat_bb_gift"] < 0)
            {
                HashCheck = false;
            }

            if (Set.AutoSaveInterval > 0)
            {
                AutoSaveTimer.Interval = Set.AutoSaveInterval * 60000;
                AutoSaveTimer.Start();
            }
            ClickTexts.Add(new ClickText("Did you know? Mouse right click to open the menu bar"));
            ClickTexts.Add(new ClickText("Did you know? You can modify the game's zoom ratio in the settings"));
            ClickTexts.Add(new ClickText("Want your pet to stay still? Smart moving can be enabled in the settings"));
            ClickTexts.Add(new ClickText("This game's development is so slow all due to the game artist's procrastination"));
            //ClickTexts.Add(new ClickText("Do you have any suggestions or game feedback? Let's go to the Menu - System - Feedback Hub for feedback"));
            ClickTexts.Add(new ClickText("Hold the head and drag the desktop pet wherever you like"));

            //ClickTexts.Add(new ClickText("Master, {name} is participating in the Sbeam Awards , vote for me meow"));
            //ClickTexts.Add(new ClickText("That... Master... I'm participating in the Sbeam Awards . Can you... vote for {name}~"));
            //ClickTexts.Add(new ClickText("There is a game called \"VPet Simulator\" in the computer that is participating in the 2023 Sbeam Awards , come and vote for the desktop pet"));
            //"If you feel that there are too few functions at the moment, just stay AFK more. Pets will move on their own".Translate(),
            //"Did you know? You can modify the game's zoom ratio in the settings".Translate(),
            //"Your randomly tapping to speaking is part of the speaking system, but not do yet. It doing yet.".Translate(),
            //"Have you added VUP-Simulator and VPet-Simulator to your wishlist? Go ahead and add them".Translate(),
            //"This game's development is so slow all due to the game artist's procrastination".Translate(),
            //"This open-source project is located at https://github.com/LorisYounger/VPet".Translate()

            await Dispatcher.InvokeAsync(new Action(() => LoadingText.Content = "Loading content".Translate()));
            ClickTexts.Add(new ClickText("Subscribe {0} thanks")
            {
                TranslateText = "Subscribe {0} thanks".Translate(Environment.UserName)
            });

            MusicTimer = new System.Timers.Timer(200)
            {
                AutoReset = false
            };
            MusicTimer.Elapsed += MusicTimer_Elapsed;


            //await Dispatcher.InvokeAsync(new Action(() => LoadingText.Content = "Attempting to load animations".Translate()));
            await Dispatcher.InvokeAsync(new Action(() => LoadingText.Content = "Attempting to load animations and generate cache\\nThis step may take a while\\nPlease be patient".Translate()));
            Core.Graph = petloader.Graph(Set.Resolution, Dispatcher);

            Main = await Dispatcher.InvokeAsync(() => new Main(Core));

            int totalGraphs = petloader.GraphCount;
            Main.LoadALL((c) =>
            {
                Dispatcher.Invoke(() =>
                {
                    int percent = totalGraphs > 0 ? (int)(c * 100.0 / totalGraphs) : 0;
                    if (percent > 100) percent = 100;
                    LoadingText.Content = "Attempting to load animations and generate cache\\nThis step may take a while\\nPlease be patient".Translate()
                        + $"\n  {c} / {totalGraphs} ({percent}%)";
                    // Update progress bar and percentage label (debug only)
                    if (LoadingProgressBar != null)
                        LoadingProgressBar.Value = percent;
                    if (LoadingPercent != null)
                        LoadingPercent.Content = $"{percent}%";
                });
            }
            //#if NewYear
            //            , Core.Graph.FindGraph("newyear", AnimatType.Single, Core.Save.Mode)
            //#endif
            );

            // Debug: show any loading errors in a popup window
            // Wrapped in #if DEBUG so it can be removed easily for release builds.
#if DEBUG
            if (Main.ErrorMessage.Count > 0)
            {
                Dispatcher.Invoke(() =>
                {
                    var errorWin = new System.Windows.Window
                    {
                        Title = "Debug: Animation Loading Errors",
                        Width = 600,
                        Height = 400,
                        WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen,
                        Content = new System.Windows.Controls.ScrollViewer
                        {
                            Content = new System.Windows.Controls.TextBlock
                            {
                                Text = string.Join("\n\n", Main.ErrorMessage),
                                TextWrapping = System.Windows.TextWrapping.Wrap,
                                FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                                FontSize = 11,
                                Margin = new System.Windows.Thickness(10)
                            }
                        }
                    };
                    errorWin.ShowDialog();
                });
            }
#endif
            Main.NoFunctionMOD = Set.CalFunState;
            await Dispatcher.InvokeAsync(() =>
              {
                  Main.Resources = Application.Current.Resources;
                  Main.MsgBar.This.Resources = Application.Current.Resources;
                  Main.ToolBar.Resources = Application.Current.Resources;
                  Main.ToolBar.LoadClean();
                  Main.WorkList(out List<Work> ws, out List<Work> ss, out List<Work> ps);

                  // ScheduleLoad
                  ScheduleTask = new ScheduleTask(this);

                  if (ws.Count == 0)
                  {
                      Main.ToolBar.MenuWork.Visibility = Visibility.Collapsed;
                  }
                  else
                  {
                      Main.ToolBar.MenuWork.MouseDoubleClick += (x, y) =>
                      {
                          Main.ToolBar.Visibility = Visibility.Collapsed;
                          ShowWorkMenu(Work.WorkType.Work);
                      };
                      Main.ToolBar.MenuWork.Click += (x, y) =>
                      {
                          Main.ToolBar.Visibility = Visibility.Collapsed;
                          if (Main.ToolBar.MenuWork.Items.Count == 0)
                              ShowWorkMenu(Work.WorkType.Work);
                      };
                  }
                  if (ss.Count == 0)
                  {
                      Main.ToolBar.MenuStudy.Visibility = Visibility.Collapsed;
                  }
                  else
                  {
                      Main.ToolBar.MenuStudy.MouseDoubleClick += (x, y) =>
                      {
                          Main.ToolBar.Visibility = Visibility.Collapsed;
                          ShowWorkMenu(Work.WorkType.Study);
                      };
                      Main.ToolBar.MenuStudy.Click += (x, y) =>
                      {
                          Main.ToolBar.Visibility = Visibility.Collapsed;
                          if (Main.ToolBar.MenuStudy.Items.Count == 0) ShowWorkMenu(Work.WorkType.Study);
                      };
                  }
                  if (ps.Count == 0)
                  {
                      Main.ToolBar.MenuPlay.Visibility = Visibility.Collapsed;
                  }
                  else
                  {
                      Main.ToolBar.MenuPlay.MouseDoubleClick += (x, y) =>
                      {
                          Main.ToolBar.Visibility = Visibility.Collapsed;
                          ShowWorkMenu(Work.WorkType.Play);
                      };
                      Main.ToolBar.MenuPlay.Click += (x, y) =>
                      {
                          Main.ToolBar.Visibility = Visibility.Collapsed;
                          if (Main.ToolBar.MenuPlay.Items.Count == 0) ShowWorkMenu(Work.WorkType.Play);
                      };
                  }
                  WorkStarMenu = new System.Windows.Controls.MenuItem()
                  {
                      Header = "Favorites".Translate(),
                      HorizontalContentAlignment = System.Windows.HorizontalAlignment.Center,
                  };
                  foreach (var w in WorkStar())
                  {
                      var mi = new System.Windows.Controls.MenuItem()
                      {
                          Header = w.NameTrans
                      };
                      mi.Click += (s, e) => Main.ToolBar.StartWork(w.Double(Set["workmenu"].GetInt("double_" + w.Name, 1)));
                      WorkStarMenu.Items.Add(mi);
                  }
                  Main.ToolBar.MenuInteract.Items.Add(WorkStarMenu);

                  // LoadTheme:
                  LoadTheme(Set.Theme);
                  // LoadFont
                  LoadFont(Set.Font);

                  LoadingText.Content = "Loading the game\\nThis step may take a long time\\nPlease be patient".Translate();


                  if (!Set["gameconfig"].GetBool("noAutoCal"))
                  {
                      foreach (var work in Core.Graph.GraphConfig.Works)
                      {
                          if (work.LevelLimit > 200)
                              work.LevelLimit = 200;
                          work.FixOverLoad();
                      }
                  }
                  foreach (var stp in SchedulePackage)
                      stp.FixOverLoad();


                  var m = new System.Windows.Controls.MenuItem()
                  {
                      Header = "MOD Management".Translate(),
                      HorizontalContentAlignment = System.Windows.HorizontalAlignment.Center,
                  };
                  m.Click += (x, y) =>
                  {
                      Main.ToolBar.Visibility = Visibility.Collapsed;
                      winSetting.MainTab.SelectedIndex = 5;
                      winSetting.Show();
                  };
                  Main.FunctionSpendHandle += lowStrength;
                  Main.WorkTimer.E_FinishWork += WorkTimer_E_FinishWork;
                  Main.ToolBar.MenuMODConfig.Items.Add(m);

                  foreach (MainPlugin mp in Plugins)
                      try
                      {
                          mp.LoadPlugin();
                      }
                      catch (Exception e)
                      {
                          NoticeBox.Show("Game launch error due to plugin".Translate() + "\n" + e.ToString(), "Game launch error due to plugin".Translate() + '-' + mp.PluginName);
                      }
                  Foods.ForEach(item => item.LoadImageSource(this));
                  Photos.ForEach(item => item.LoadUserInfo(this));

                  if (Set.PetGraph == "vup")
                  {
                      if (!Items.Any(x => x.Name == "L Badge"))
                      {
                          var itm = new Item()
                          {
                              Name = "L Badge",
                              Desc = "Objection! Evidence Archive - L Badge - Present!\\nPlease do not present this as evidence in court.".Translate(),
                              ItemType = "Item",
                              Price = 100,
                              IsSingle = true,
                              CanUse = false,
                          };
                          itm.LoadSource(this);
                          ItemsAdd(itm);
                      }
                      if (!Items.Any(x => x.Name == "Cat Teaser"))
                      {
                          var itm = new Item()
                          {
                              Name = "Cat Teaser",
                              Desc = "A fishing-rod-style cat teaser. Usually hangs mice, pom-poms, W’s, etc. This one hangs a spotted dove.\\nWho says a cat teaser can’t tease people too?".Translate(),
                              ItemType = "Toy",
                              Price = 100,
                              IsSingle = true,
                              Data = "meow",
                          };
                          itm.LoadSource(this);
                          ItemsAdd(itm);
                      }
                      if (!Items.Any(x => x.Name == "Bubble Gun"))
                      {
                          var itm = new Item()
                          {
                              Name = "Bubble Gun",
                              Desc = "A pink-and-white bubble gun with a ribbon decoration. Can’t find where to add soapy water—is it high-tech?\\nA bit childish for kids, but just right for Lolis.".Translate(),
                              ItemType = "Toy",
                              Price = 100,
                              IsSingle = true,
                              Data = "bubbles",
                          };
                          itm.LoadSource(this);
                          ItemsAdd(itm);
                      }
                      if (!Items.Any(x => x.Name == "Badminton Racket"))
                      {
                          var itm = new Item()
                          {
                              Name = "Badminton Racket",
                              Desc = "The latest carbon fiber racket from the Boss brand. Built-in assist power allows you to easily use \"Zero Serve\" \"Seamless\" and other techniques\\nDid you just say, tennis?".Translate(),
                              ItemType = "Toy",
                              Price = 100,
                              IsSingle = true,
                              Data = "tennis",
                          };
                          itm.LoadSource(this);
                          ItemsAdd(itm);
                      }
                      if (!Items.Any(x => x.Name == "Compass"))
                      {
                          var itm = new Item()
                          {
                              Name = "Compass",
                              Desc = "Pointer? The needles are all warped, I really can’t tell which way they're pointing, and they keep shaking randomly—it’s a bit creepy\\nMy dear comrade, why is this thing shaking so much?".Translate(),
                              ItemType = "Tool",
                              Price = 100,
                              IsSingle = true,
                          };
                          itm.LoadSource(this);
                          ItemsAdd(itm);
                      }
                  }
                  everydaygift();



                  Main.TimeHandle += Handle_Music;
                  Main.TimeHandle += (x) => DiagnosisUPLoad();


                  var tlv = Main.ToolBar.Tlv;
                  Main.ToolBar.gdPanel.Children.Remove(tlv);
                  var sp = new StackPanel();
                  Grid.SetColumnSpan(sp, 3);
                  sp.Orientation = System.Windows.Controls.Orientation.Horizontal;
                  sp.Children.Add(tlv);
                  tlvplus = new TextBlock();
                  tlvplus.Margin = new Thickness(1);
                  tlvplus.VerticalAlignment = VerticalAlignment.Bottom;
                  tlvplus.FontSize = 18;
                  tlvplus.Foreground = Function.ResourcesBrush(Function.BrushType.PrimaryText);
                  sp.Children.Add(tlvplus);
                  Main.ToolBar.gdPanel.Children.Add(sp);
                  Main.TimeUIHandle += MWUIHandle;
                  Main.ToolBar.EventMenuPanelShow += () => MWUIHandle(Main);


                  switch (Set["CGPT"][(gstr)"type"])
                  {
                      case "DIY":
                          TalkAPIIndex = TalkAPI.FindIndex(x => x.APIName == Set["CGPT"][(gstr)"DIY"]);
                          LoadTalkDIY();
                          break;
                      //case "API":
                      //    TalkBox = new TalkBoxAPI(this);
                      //    Main.ToolBar.MainGrid.Children.Add(TalkBox);
                      //    break;
                      case "LB":
                          //if (IsSteamUser)
                          //{
                          //    TalkBox = new TalkSelect(this);
                          //    Main.ToolBar.MainGrid.Children.Add(TalkBox);
                          //}
                          TalkBox = new TalkSelect(this);
                          Main.ToolBar.MainGrid.Children.Add(TalkBox);
                          break;
                  }

                  winSetting = new winGameSetting(this);
                  winBetterBuy = new winBetterBuy(this);

                  Main.DefaultClickAction = () =>
                  {
                      if (new TimeSpan(DateTime.Now.Ticks - lastclicktime).TotalSeconds > 20)
                      {
                          lastclicktime = DateTime.Now.Ticks;
                          var rt = GetClickText();
                          if (rt != null)
                          {
                              if (rt.Exp != 0)
                              {
                                  if (rt.Exp > 0)
                                  {
                                      GameSavesData.Statistics[(gint)"stat_say_exp_p"]++;
                                  }
                                  else
                                      GameSavesData.Statistics[(gint)"stat_say_exp_d"]++;
                              }
                              if (rt.Likability != 0)
                              {
                                  if (rt.Likability > 0)
                                      GameSavesData.Statistics[(gint)"stat_say_like_p"]++;
                                  else
                                      GameSavesData.Statistics[(gint)"stat_say_like_d"]++;
                              }
                              if (rt.Money != 0)
                              {
                                  if (rt.Money > 0)
                                      GameSavesData.Statistics[(gint)"stat_say_money_p"]++;
                                  else
                                      GameSavesData.Statistics[(gint)"stat_say_money_d"]++;
                              }
                              Main.Core.Save.EatFood(rt);
                              Main.Core.Save.Money += rt.Money;
                              Main.SayRnd(rt.TranslateTextConvert(Main), desc: rt.FoodToDescription());
                          }
                      }
                  };
                  Main.PlayVoiceVolume = Set.VoiceVolume;
                  Main.FunctionSpendHandle += StatisticsCalHandle;
                  DisplayGrid.Child = Main;
                  Task.Run(async () =>
                  {
                      while (!Main.IsWorking)
                      {
                          Thread.Sleep(100);
                      }
                      await Dispatcher.InvokeAsync(async () =>
                      {
                          while (LoadingText.Visibility != Visibility.Collapsed)
                          {
                              LoadingText.Visibility = Visibility.Collapsed;
                              await Task.Delay(1000);
                          }
                      });
                  });

                  Main.ToolBar.AddMenuButton(ToolBar.MenuType.Setting, "Exit".Translate(), () => { Main.ToolBar.Visibility = Visibility.Collapsed; Close(); });
                  if (Set.DeBug)
                      Main.ToolBar.AddMenuButton(ToolBar.MenuType.Setting, "Console".Translate(), () => { Main.ToolBar.Visibility = Visibility.Collapsed; new winConsole(this).Show(); });
                  Main.ToolBar.AddMenuButton(ToolBar.MenuType.Setting, "Photo Gallery ".Translate(), ShowGallery);
                  Main.ToolBar.AddMenuButton(ToolBar.MenuType.Setting, "How-to Tutorials".Translate(), () =>
                  {
                      if (LocalizeCore.CurrentCulture == "zh-Hans")
                          ExtensionFunction.StartURL("https://wiki.exlb.net/vpet/tutorial");
                      else if (LocalizeCore.CurrentCulture == "zh-Hant")
                          ExtensionFunction.StartURL("https://wiki.exlb.net/zh-hant/vpet/tutorial");
                      else
                          ExtensionFunction.StartURL("https://wiki.exlb.net/en/vpet/tutorial");
                  });
                  Main.ToolBar.AddMenuButton(ToolBar.MenuType.Setting, "Feedback".Translate(), () => { Main.ToolBar.Visibility = Visibility.Collapsed; new winReport(this).Show(); });
                  Main.ToolBar.AddMenuButton(ToolBar.MenuType.Setting, "Settings".Translate(), () =>
                  {
                      Main.ToolBar.Visibility = Visibility.Collapsed;
                      winSetting.Show();
                      winSetting.Activate();
                  });

                  //this.Background = new ImageBrush(new BitmapImage(new Uri("pack://application:,,,/Res/TopLogo2019.PNG")));

                  //    {
                  //        Main.ToolBar.Visibility = Visibility.Collapsed;
                  //        IRunImage eat = (IRunImage)Core.Graph.FindGraph(GraphType.Eat, GameSave.ModeType.Nomal);
                  //        var b = Main.FindDisplayBorder(eat);
                  // eat.Run(b, new BitmapImage(new Uri("pack://application:,,,/Res/Hamburger.png")), Main.DisplayToNomal);
                  //    }
                  //);
                  Main.ToolBar.AddMenuButton(ToolBar.MenuType.Feed, "Food".Translate(), () =>
                  {
                      winBetterBuy.Show(Food.FoodType.Meal);
                  });
                  Main.ToolBar.AddMenuButton(ToolBar.MenuType.Feed, "Drinks".Translate(), () =>
                  {
                      winBetterBuy.Show(Food.FoodType.Drink);
                  });
                  Main.ToolBar.AddMenuButton(ToolBar.MenuType.Feed, "Favorites".Translate(), () =>
                  {
                      winBetterBuy.Show(Food.FoodType.Star);
                  });
                  Main.ToolBar.AddMenuButton(ToolBar.MenuType.Feed, "Medicine".Translate(), () =>
                  {
                      winBetterBuy.Show(Food.FoodType.Drug);
                  });
                  Main.ToolBar.AddMenuButton(ToolBar.MenuType.Feed, "Gift".Translate(), () =>
                  {
                      winBetterBuy.Show(Food.FoodType.Gift);
                  });
                  Main.ToolBar.AddMenuButton(ToolBar.MenuType.Feed, "Backpack".Translate(), () =>
                  {
                      if (winInventory != null && !winInventory.IsClosed)
                          winInventory.Show();
                      else
                      {
                          winInventory = new winInventory(this);
                          winInventory.Show();
                      }
                  });
                  Main.SetMoveMode(Set.AllowMove, Set.SmartMove, Set.SmartMoveInterval * 1000);
                  Main.SetLogicInterval((int)(Set.LogicInterval * 1000));
                  if (Set.MessageBarOutside)
                      Main.MsgBar.SetPlaceOUT();

                  Main.WorkCheck = WorkCheck;

                  notifyIcon = new NotifyIcon();
                  notifyIcon.Text = "VPet-Simulator".Translate() + PrefixSave;
                  ContextMenu m_menu;

                  if (Set.PetHelper)
                      LoadPetHelper();



                  m_menu = new ContextMenu();
                  m_menu.Opening += (x, y) => { GameSavesData.Statistics[(gint)"stat_menu_pop"]++; };
                  var hitThrough = new MenuItem("Click Through Window".Translate(), null, (x, y) => { SetTransparentHitThrough(); })
                  {
                      Name = "NotifyIcon_HitThrough",
                      Checked = HitThrough
                  };
                  m_menu.Items.Add(hitThrough);
                  var topmost = new MenuItem("Always On Top".Translate(), null, (x, y) =>
                  {
                      Topmost = ((MenuItem)x).Checked;
                  })
                  {
                      Name = "NotifyIcon_TopMost",
                      CheckOnClick = true,
                      Checked = Topmost
                  };
                  m_menu.Items.Add(topmost);
                  m_menu.Items.Add(new MenuItem("How-to Tutorials".Translate(), null, (x, y) =>
                  {
                      if (LocalizeCore.CurrentCulture == "zh-Hans")
                          ExtensionFunction.StartURL(ExtensionValue.BaseDirectory + @"\Tutorial.html");
                      else if (LocalizeCore.CurrentCulture == "zh-Hant")
                          ExtensionFunction.StartURL(ExtensionValue.BaseDirectory + @"\Tutorial_zht.html");
                      else
                          ExtensionFunction.StartURL(ExtensionValue.BaseDirectory + @"\Tutorial_en.html");
                  }));
                  m_menu.Items.Add(new MenuItem("Reset Position && Status".Translate(), null, (x, y) =>
                  {
                      Main.CleanState();
                      Main.DisplayToNomal();
                      Left = (SystemParameters.PrimaryScreenWidth - Width) / 2;
                      Top = (SystemParameters.PrimaryScreenHeight - Height) / 2;
                  }));
                  m_menu.Items.Add(new MenuItem("Feedback".Translate(), null, (x, y) => { new winReport(this).Show(); }));
                  if (Set.DeBug)
                      m_menu.Items.Add(new MenuItem("Console".Translate(), null, (x, y) => { new winConsole(this).Show(); }));

                  m_menu.Items.Add(new MenuItem("Settings".Translate(), null, (x, y) =>
                  {
                      winSetting.Show();
                  }));
                  m_menu.Items.Add(new MenuItem("Restart VPet".Translate(), null, (x, y) => Restart()));
                  m_menu.Items.Add(new MenuItem("Exit".Translate(), null, (x, y) => Close()));

                  LoadDIY();

                  notifyIcon.ContextMenuStrip = m_menu;

                  notifyIcon.Icon = new System.Drawing.Icon(Application.GetResourceStream(new Uri("pack://application:,,,/vpeticon.ico")).Stream);

                  notifyIcon.Visible = true;
                  notifyIcon.BalloonTipClicked += (a, b) =>
                  {
                      winSetting.Show();
                  };
                  if (Set.StartUPBoot == true && !Set["v"][(gbol)"newverstartup"])
                  {
                      try
                      {
                          winSetting.GenStartUP();
                          Set["v"][(gbol)"newverstartup"] = true;
                      }
                      catch
                      {

                      }
                  }


                  GameSavesData.Statistics[(gint)"stat_open_times"]++;
                  Main.MoveTimer.Elapsed += MoveTimer_Elapsed;
                  Main.SayProcess.Add(Main_OnSay);
                  Main.Event_TouchHead += Main_Event_TouchHead;
                  Main.Event_TouchBody += Main_Event_TouchBody;

                  HashCheck = HashCheck;

                  if (Core.Graph.GraphConfig.Data.ContainsLine("pinch"))
                  {
                      var pin = Core.Graph.GraphConfig.Data["pinch"];
                      Main.Core.TouchEvent.Insert(0, new TouchArea(
                          new Point(pin[(gdbe)"px"], pin[(gdbe)"py"]), new Size(pin[(gdbe)"sw"], pin[(gdbe)"sh"])
                          , DisplayPinch, true));
                  }


                  if (Set.HitThrough)
                  {
                      if (!Set["v"][(gbol)"HitThrough"])
                      {
                          Set["v"][(gbol)"HitThrough"] = true;
                          Set.HitThrough = false;
                      }
                      else
                          SetTransparentHitThrough();
                  }

                  if (File.Exists(ExtensionValue.BaseDirectory + @"\Tutorial.html") && Set["SingleTips"].GetDateTime("tutorial") <= new DateTime(2023, 10, 20) && App.MainWindows.Count == 1)
                  {
                      Set["SingleTips"].SetDateTime("tutorial", DateTime.Now);
                      if (LocalizeCore.CurrentCulture == "zh-Hans")
                          ExtensionFunction.StartURL(ExtensionValue.BaseDirectory + @"\Tutorial.html");
                      else if (LocalizeCore.CurrentCulture == "zh-Hant")
                          ExtensionFunction.StartURL(ExtensionValue.BaseDirectory + @"\Tutorial_zht.html");
                      else
                          ExtensionFunction.StartURL(ExtensionValue.BaseDirectory + @"\Tutorial_en.html");
                  }
                  if (!Set["SingleTips"].GetBool("helloworld"))
                  {
                      Task.Run(() =>
                      {
                          Thread.Sleep(2000);
                          Set["SingleTips"].SetBool("helloworld", true);
                          NoticeBox.Show("Welcome to use VPet-Simulator!\\nIf you can't find desktop pet, you can set the center or exit the desktop pet in here.".Translate(),
                             "Hi".Translate() + Environment.UserName, Panuon.WPF.UI.MessageBoxIcon.Info, true, 5000);
                          //Thread.Sleep(2000);
                          //Main.SayRnd("Welcome to use VPet-Simulator\\nThis is a beta version. Please forgive me for any bugs\\n You're welcome to check https://github.com/LorisYounger/VPet, or leave feedback at Menu - System - Feedback".Translate());
                      });
                  }
                  if (Set["v"][(gint)"rank"] != DateTime.Now.Year && GameSavesData.Statistics[(gint)"stat_total_time"] > 3600)
                  {
                      Task.Run(() =>
                      {
                          Thread.Sleep(Function.Rnd.Next(200000, 400000));
                          Set["v"][(gint)"rank"] = DateTime.Now.Year;
                          var btn = Dispatcher.Invoke(() =>
                          {
                              var button = new System.Windows.Controls.Button()
                              {
                                  Content = "Click to view".Translate(),
                                  FontSize = 20,
                                  HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                                  Background = Function.ResourcesBrush(Function.BrushType.PrimaryDark),
                                  Foreground = Function.ResourcesBrush(Function.BrushType.PrimaryText),
                              };
                              button.Click += (x, y) =>
                              {
                                  var panelWindow = new winCharacterPanel(this);
                                  panelWindow.MainTab.SelectedIndex = 1;
                                  panelWindow.Show();
                                  Main.MsgBar.ForceClose();
                              };
                              return button;
                          });
                          Main.Say("Humph~Master, my exam results are out, come and see my report card with me".Translate(), btn, "shining");
                      });
                  }
                  if (GameSavesData.Data.FindLine("HostBDay") == null)
                  {
                      Task.Run(() =>
                      {
                          Thread.Sleep(Function.Rnd.Next(100000, 200000));
                          var btn = Dispatcher.Invoke(() =>
                          {
                              var button = new System.Windows.Controls.Button()
                              {
                                  Content = "Settings".Translate(),
                                  FontSize = 20,
                                  HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                                  Background = Function.ResourcesBrush(Function.BrushType.PrimaryDark),
                                  Foreground = Function.ResourcesBrush(Function.BrushType.PrimaryText),
                              };
                              button.Click += (x, y) =>
                              {
                                  ShowSetting(2);
                              };
                              return button;
                          });
                          Main.Say("Don't forget to set the time for your birthday {0}, I'll secretly prepare a gift for you.".Translate(GameSavesData.GameSave.HostName), btn, "shining");
                      });
                  }
                  else
                  {
                      var bdt = GameSavesData.GetDateTime("HostBDay");
                      if (DateTime.Now.Month == bdt.Month && DateTime.Now.Day == bdt.Day)
                      {
                          Task.Run(() =>
                          {
                              Thread.Sleep(Function.Rnd.Next(100000, 200000));
                              HostBDay();
                          });
                      }
                  }

#if BDAY
                  if (DateTime.Now < new DateTime(2025, 8, 22) && DateTime.Now >= new DateTime(2025, 8, 14))
                  {
                      food.Star = true;
                      Task.Run(() =>
                      {
                          Thread.Sleep(10000);
                          var btn = Dispatcher.Invoke(() =>
                          {
                              var button = new System.Windows.Controls.Button()
                              {
                                  Content = "View Birthday Announcement/Video".Translate(),
                                  FontSize = 20,
                                  HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                                  Background = Function.ResourcesBrush(Function.BrushType.PrimaryDark),
                                  Foreground = Function.ResourcesBrush(Function.BrushType.PrimaryText),
                              };
                              button.Click += (x, y) =>
                              {
                                  if (LocalizeCore.CurrentCulture.StartsWith("zh"))
                                      ExtensionFunction.StartURL("https://www.bilibili.com/opus/1100685352151023623");
                                  else
                                      ExtensionFunction.StartURL("https://store.steampowered.com/news/app/1920960/view/528729123117336267");
                              };
                              return button;
                          });
                          string bdt;
                          switch (DateTime.Now.Day)
                          {
                              case 14:
                                  bdt = "What's wrong master, do you need me?\n~Oh it's my birthday!? I forgot! Thank you master! I'm always happy being with you meow~ Another year passed, let's stay together forever meow!";
                                  break;
                              case 15:
                                  bdt = "How was yesterday master? Did you have fun meow? If not enough, no worries! Loris will play with you again meow!";
                                  break;
                              case 16:
                                  bdt = "What? Birthday passed already? Time to start the schedule meow?? Help meow! Loris doesn't want to grow up!";
                                  break;
                              case 17:
                                  bdt = "Master~ I'm having birthday withdrawal symptoms meow~ I need master to feed me cake to get better~";
                                  break;
                              case 18:
                                  bdt = "I've grown up meow, I don't celebrate birthdays... this kind of childish thing that only kids like meow woo woo woo.";
                                  break;
                              case 19:
                                  bdt = "Don't worry master, I understand everything meow! With master, every day is birthday meow!";
                                  break;
                              default:
                                  bdt = "It's been so long, should be almost the third anniversary meow! What?! Only one week passed meow! Woo woo woo, master~ can I get an advance on my next birthday meow~";
                                  break;

                          }
                          Main.Say(bdt.Translate(), btn, "self");
                          //Main.Say(bdt.Translate(), "self");
                      });
                  }
#endif
                  newday = DateTime.Now.Day;
                  Main.TimeHandle += NewDayHandle;
                  Event_NewDay += () =>
                  {
                      var bdt = GameSavesData.GetDateTime("HostBDay");
                      if (DateTime.Now.Month == bdt.Month && DateTime.Now.Day == bdt.Day)
                      {
                          HostBDay();
                      }
                  };
                  Event_NewDay += everydaygift;

                  Event_TakeItem += MainWindow_Event_TakeItem;
                  Event_TakeItemHandle += (item, count, from) => ActivityLogs.Add(new ActivityLog("take_" + from, item.TranslateName, count.ToString()));
                  Main.Event_WorkStart += (work) => ActivityLogs.Add(new ActivityLog("work_start", work.NameTrans));
                  Main.Event_WorkEnd += (workinfo) => ActivityLogs.Add(new ActivityLog("work_end", workinfo.work.NameTrans, workinfo.Reason.ToString(), workinfo.spendtime.ToString("f0"), workinfo.count.ToString("f0")));
                  Main.SayProcess.Add((sayinfo) =>
                  {
                      Task.Run(async () =>
                      {
                          ActivityLogs.Add(new ActivityLog("petsay", await sayinfo.GetSayText()));
                      });
                  });
                  //if (DateTime.Now.DayOfYear == 1)
                  //{
                  //    Task.Run(() =>
                  //    {
                  //        Thread.Sleep(5000);
                  //        Main.SayRnd("If 2025 has already passed, what else can't we get through? {0}, you've worked hard this year! Let's continue to get along well in the New Year!".Translate(GameSavesData.GameSave.HostName));
                  //    });
                  //}
                  //var photo25 = Photos.Find(x => x.Name == "2026 New Year’s Eve");
                  //if (photo25?.IsUnlock == true && GameSavesData.HashCheck == false && GameSavesData.Data["debug"][(gbol)"fix26"] == false)
                  //{
                  //    GameSave_v2 ogs = GameSavesData;
                  //    GameSavesData = new GameSave_v2(ogs.GameSave.Name);
                  //    GameSavesData.Data = ogs.Data;
                  //    GameSavesData.GameSave = ogs.GameSave;
                  //    GameSavesData.Statistics = ogs.Statistics;
                  //    HashCheck = true;
                  //}
                  //GameSavesData.Data["debug"][(gbol)"fix26"] = true;
                  if (GameSavesData.HashCheck == false && GameSavesData["debug"].Find("losthash") == null)
                  {
                      GameSavesData["debug"][(gdat)"losthash"] = DateTime.Now;
                  }
#if NewYear
                  if (DateTime.Now < new DateTime(2026, 2, 25))
                  {
                      Event_NewDay += NewYearSay;
                      Task.Run(() =>
                      {
                          Thread.Sleep(5000);
                          NewYearSay();
                      });
                  }
#endif
                  foreach (CoreMOD cm in CoreMODs)
                      if (!cm.SuccessLoad)
                          if (cm.Tag.Contains("The module is corrupted."))
                              MessageBoxX.Show("Module {0} plug-in damage\\nVirtual desktop pet simulator failed to load the plug-in\\nPlease contact the mod author to fix the problem.".Translate(cm.Name) + '\n' + cm.ErrorMessage, "The module is corrupted.".Translate());
                          else if (Set.IsPassMOD(cm.Name) || !string.IsNullOrEmpty(cm.ErrorMessage))
                              MessageBoxX.Show("Mod {0} Code plugins broken\\nVPet-Simulator failed to load the plugin\\nPlease contact MOD author to fix this problem".Translate(cm.Name) + '\n' + cm.ErrorMessage, "Code plugin {0} not loaded".Translate(cm.Name));
                          else if (Set.IsMSGMOD(cm.Name))
                              MessageBoxX.Show("Since {0} contains code plugin\\nVPet-Simulator auto stopped loading the plugin\\nplease go to the settings to enabled the mod code plugin".Translate(cm.Name), "Code plugin {0} not loaded".Translate(cm.Name));
                  if (Main.ErrorMessage.Count != 0)
                  {
                      var errstr = string.Join("\n------\n", Main.ErrorMessage);
                      if (errstr.Contains("0000_core"))
                      {
                          MessageBoxX.Show("Animation loading error, try the following fixes:\n\t1. Delete the `Cache` folder in the game root directory\n\t2. Delete the `mod\\0000_core\\pet` folder and verify game integrity".Translate(), "Animation loading error".Translate());
                          var winrep = new winReport(this, errstr);
                          winrep.tDescription.Text = "Animation loading error".Translate();
                          winrep.Show();
                      }
                      else
                          MessageBoxX.Show("Animation loading error\\nVPet Simulator failed to load the animation successfully\\nPlease contact the MOD author to fix the problem".Translate() + '\n' + errstr, "Animation loading error".Translate());

                      Main.ErrorMessage.Clear();
                  }
                  foreach (MainPlugin mp in Plugins)
                      try
                      {
                          mp.GameLoaded();
                      }
                      catch (Exception e)
                      {
                          NoticeBox.Show("Game launch error due to plugin".Translate() + "\n" + e.ToString(), "Game launch error due to plugin".Translate() + '-' + mp.PluginName);
                      }


                  if (GameSavesData.GameSave.Likability < 520)
                      Core.Graph.GraphsName[GraphType.Idel].Remove("like520");
                  else if (Core.Graph.FindGraph("like520", AnimatType.Single, IGameSave.ModeType.Happy) != null)
                  {
                      Event_NewDay += like520;
                      like520();
                  }
                  if (Set.DeBug)
                      ActivityLogs.CollectionChanged += ActivityLogs_WriteFile;
              });


            //if (Set["SingleTips"][(gint)"open"] == 0 && Set.StartUPBoot == true && Set.StartUPBootSteam == true)
            //{
            //    await Dispatcher.InvokeAsync(new Action(() =>
            //    {
            //        MessageBoxX.Show("We have detected that you have booted up the game, the following is a message about booting up the game: (shown only once)".Translate() + "\n------\n" +
            //          , "The problem of uninstallation is due to the startup being turned on.".Translate(), Panuon.WPF.UI.MessageBoxIcon.Info);
            //        Set["SingleTips"][(gint)"open"] = 1;
            //    }));
            //}

        }
        public static object LogsLock = new object();
        private void ActivityLogs_WriteFile(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                List<string> sb = new List<string>();
                foreach (ActivityLog log in e.NewItems)
                {
                    sb.Add(log.ToString(Main));
                }
                string logPath = ExtensionValue.BaseDirectory + $"\\Logs{PrefixSave}.txt";
                lock (LogsLock)
                {
                    if (File.Exists(logPath) && new FileInfo(logPath).Length > 1024 * 1024)
                    {
                        var allLines = File.ReadAllLines(logPath);
                        if (allLines.Length > 2000)
                        {
                            File.WriteAllLines(logPath, allLines.Skip(allLines.Length - 2000));
                        }
                    }
                    File.AppendAllLines(logPath, sb);
                }
            }
        }

        private void everydaygift()
        {
            if (Set["dailydata"][(gint)"everydaygift"] == DateTime.Now.DayOfYear)
            {
                return;
            }
            Set["dailydata"][(gint)"everydaygift"] = DateTime.Now.DayOfYear;
            var itm = new Item
            {
                Name = "Daily Gift Pack",
                Desc = "A daily gift pack included with the item system. Opening it grants 3 random items. The tutorial even gives gifts—Lolis is truly generous.".Translate(),
                ItemType = "Mail",
                Price = 15,
            };
            Dispatcher.Invoke(() => itm.LoadSource(this));
            ItemsAdd(itm);
        }


        private void like520()
        {
            var date = DateTime.Now.Day + DateTime.Now.Month * 100;
            var thisy77 = GetLunarDate(7, 7);
            if (date == 520 || date == 521 || date == 214 || date == (thisy77.Day + thisy77.Month * 100))
            {
                Task.Run(() =>
                {
                    Thread.Sleep(52000);
                    Main.Display("like520", AnimatType.Single, Main.DisplayNomal);
                });
            }
        }

        private void MainWindow_Event_TakeItem(Food obj)
        {
            switch (obj.Name)
            {
                case "2nd Surprise Birthday Cake":
                    obj.Exp = Core.Save.Level;
                    obj.Likability = Core.Save.LikabilityMax / 20;
                    obj.StrengthDrink = Core.Save.StrengthMax / 20;
                    obj.StrengthFood = Core.Save.StrengthMax / 20;
                    obj.isoverload = false;
                    obj.Price = (int)Math.Max(0, obj.RealPrice * .5);
                    switch (Function.Rnd.Next(15))
                    {
                        case 1:
                        case 2:
                        case 3:
                            Core.Save.Strength = Core.Save.StrengthMax;
                            Main.LabelDisplayShow("{0} POWER MAX!".Translate(Core.Save.Name), 3000);
                            break;
                        case 4:
                        case 5:
                            Core.Save.Feeling = Core.Save.FeelingMax;
                            Main.LabelDisplayShow("{0} Today's mood: ★★★★★!".Translate(Core.Save.Name), 3000);
                            break;
                        case 6:
                        case 7:
                            Core.Save.StrengthFood = Core.Save.StrengthMax;
                            Main.LabelDisplayShow("{0} Hunger satisfied!".Translate(Core.Save.Name), 3000);
                            break;
                        case 8:
                        case 9:
                            Core.Save.StrengthDrink = Core.Save.StrengthMax;
                            Main.LabelDisplayShow("{0} Hydration full!".Translate(Core.Save.Name), 3000);
                            break;
                        case 10:
                            int get = (Function.Rnd.Next(Core.Save.LevelUpNeed() * (GameSavesData.GameSave.LevelMax + 1)) / 200 + 1) * 100;
                            Core.Save.Exp += get;
                            Main.LabelDisplayShow("{0} EXP +{1} – Mission complete!".Translate(Core.Save.Name, get.ToString("N0")), 4000);
                            break;
                        case 11:
                            get = (Function.Rnd.Next(Core.Save.LevelUpNeed() * (GameSavesData.GameSave.LevelMax + 1)) / 500 + 1) * 10;
                            Core.Save.Exp += get;
                            Main.LabelDisplayShow("{0} Found {1} money on the roadside!".Translate(Core.Save.Name, get.ToString("N0")), 4000);
                            break;
                        case 12:
                            if (Function.Rnd.Next(3) != 0)
                            {
                                get = Function.Rnd.Next((int)Core.Save.LikabilityMax / 25) + 1;
                                Core.Save.Likability += get;
                                Main.LabelDisplayShow("{0} Affection for {1} increased!".Translate(Core.Save.Name, Core.Save.HostName), 4000);
                                break;
                            }
                            var photos = Photos.FindAll(x => x.IsUnlock == false && x.UnlockAble.Lock == false);
                            if (photos.Count > 0)
                            {
                                var tempphoto = photos.FindAll(x => x.UnlockAble.Time != null || x.UnlockAble.Date != null || x.UnlockAble.Holiday != HolidayType.None);
                                if (tempphoto.Count > 0)
                                    photos = tempphoto;
                                else
                                {
                                    tempphoto = photos.FindAll(x => x.UnlockAble.SellBoth == false && (x.UnlockAble.Feeling > 10 || x.UnlockAble.Likability >= 10 || x.UnlockAble.Money >= 10));
                                    if (tempphoto.Count > 0)
                                        photos = tempphoto;
                                }

                                var photo = photos[Function.Rnd.Next(photos.Count)];
                                photo.Unlock(this);
                                Main.LabelDisplayShow("{0} New photo unlocked!".Translate(Core.Save.Name) + '\n' + photo.Name, 4000);
                            }
                            else
                                goto case 11;
                            break;
                        default:
                            Main.LabelDisplayShow("{0} Consolation prize: Better luck next time!".Translate(Core.Save.Name), 4000);
                            break;
                    }
                    break;
            }
        }

        TextBlock tlvplus;

        private void MWUIHandle(Main main)
        {
            if (Main.ToolBar.BdrPanel.Visibility == Visibility.Visible)
            {
                if (GameSavesData.GameSave.LevelMax != 0)
                    tlvplus.Text = $" / {1000 + GameSavesData.GameSave.LevelMax * 100} x{GameSavesData.GameSave.LevelMax}";
            }
        }

        /// <summary>
        /// </summary>
        bool showeatanm = true;
        /// <summary>
        /// </summary>
        public void DisplayFoodAnimation(string graphName, ImageSource imageSource)
        {
            if (showeatanm)
            {
                showeatanm = false;
                Main.Display(graphName, imageSource, () =>
                {
                    showeatanm = true;
                    if (Core.Controller.EnableFunction)
                    {
                        var newmod = Core.Save.CalMode();
                        if (Core.Save.Mode != newmod)
                        {
                            Main.DisplayType.Type = GraphType.Default;
                            Main.PlaySwitchAnimat(Core.Save.Mode, newmod);
                            Core.Save.Mode = newmod;
                        }
                        else
                            Main.DisplayToNomal();
                    }
                    else
                        Main.DisplayToNomal();
                });
            }
            else
            {
                if (Main.DisplayType.Animat != AnimatType.Single && Main.DisplayType.Name != graphName)
                {
                    showeatanm = true;
                }
            }
        }

        public void HostBDay()
        {
            var petloader = Pets.Find(x => x.Name == Set.PetGraph);
            petloader ??= Pets[0];

            string sbv = "Special_Birthday_Voice_" + petloader.Name;
            string sbv_trans = sbv.Translate(GameSavesData.GameSave.HostName);
            if (sbv == sbv_trans)
            {
                Main.Say("Today is {0}'s birthday! Happy birthday to {0}!".Translate(GameSavesData.GameSave.HostName), "bday", true);
            }
            else
            {
                Main.Say(sbv_trans, "bday");
                Dispatcher.Invoke(() =>
                {
                    var panelWindow = new winCharacterPanel(this);
                    panelWindow.MainTab.SelectedIndex = 2;
                    panelWindow.Show();
                });
            }
        }



        int newday = 0;
        private void NewDayHandle(Main main)
        {
            if (DateTime.Now.Hour == 0 && newday != DateTime.Now.Day)
            {
                newday = DateTime.Now.Day;
                Event_NewDay?.Invoke();
            }
        }
        /// <summary>
        /// </summary>
        public event Action Event_NewDay;
#if NewYear
        /// <summary>
        /// </summary>
        private void NewYearSay()
        {
            string sayny;
            switch (newday)
            {

                case 16:
                    sayny = "Chipi chipi chapa chapa~ Look at that horse go! \\nRacing Lolis wishes you: Health & Vitality! May you have the stamina of a champion racehorse and energy that never runs out! Keep running!".Translate();
                    break;
                case 17:
                    sayny = "New year, new brain! ...Wait, where did I put my notes? Did I study for this? \\nScholar Lolis wishes you: Big Brain Energy! May you ace every exam, finish every assignment!".Translate();
                    break;
                case 18:
                    sayny = "\"Praying to the RNG Gods... One tap, please!\"\\nGacha Lolis wishes you: S-Tier Luck! May your pulls always be Legendary, your pity timer never trigger, and your loot drops be absolutely God-Tier!".Translate();
                    break;
                case 19:
                    sayny = "\"I didn't miss! It was lag! My ping is 999!\" \\nGamer Lolis wishes you: Low Ping and High Frames! May your aim be cracked, your reflexes sharp, and may you carry your team to victory every single match. GG EZ".Translate();
                    break;
                case 20:
                    sayny = "I want to make so much money this year!\\nFortune Lolis wishes you: Massive Gains! May your bank account grow like a high-score leaderboard, your investments only go \"Stonks\", and your wallet stay heavy all year long!".Translate();
                    break;
                case 21:
                    sayny = "I just want to be happy every day!\\nCheerful Lolis wishes you: Every single day is going to be full of smiles! 100% Happiness!".Translate();
                    break;
                case 22:
                    sayny = "I want everything to go my way this year!\\nLucky Lolis wishes you: May every path you take be a shortcut to success, every green light be waiting for you, and every small wish of yours come true instantly!".Translate();
                    break;
                default:
                case 23:
                    sayny = "I’m going to have a super successful year at work! \\nWorker Lolis wishes you: Massive Success! May your career go smoothly, every project be a winner, and you make tons of money this year! Let's get that bread!".Translate();
                    break;
            }
            Main.SayRnd(sayny);
        }
#endif
        /// <summary>
        /// </summary>
        public bool DisplayPinch()
        {
            if (Core.Graph.FindGraphs("pinch", AnimatType.A_Start, Core.Save.Mode) == null)
            {
                return false;
            }
            Main.CountNomal = 0;

            if (Core.Controller.EnableFunction && Core.Save.Strength >= 10 && Core.Save.Feeling < Core.Save.FeelingMax)
            {
                Core.Save.StrengthChange(-2);
                Core.Save.FeelingChange(1);
                Core.Save.Mode = Core.Save.CalMode();
                Main.LabelDisplayShowChangeNumber(LocalizeCore.Translate("Stamina: -{0:f0}, Mood: +{1:f0}"), 2, 1);
            }
            if (Main.DisplayType.Name == "pinch")
            {
                if (Main.DisplayType.Animat == AnimatType.A_Start)
                    return false;
                else if (Main.DisplayType.Animat == AnimatType.B_Loop)
                    if (Dispatcher.Invoke(() => Main.PetGrid.Tag) is IGraph ig && ig.GraphInfo.Name == "pinch" && ig.GraphInfo.Animat == AnimatType.B_Loop)
                    {
                        ig.SetContinue();
                        return true;
                    }
                    else if (Dispatcher.Invoke(() => Main.PetGrid2.Tag) is IGraph ig2 && ig2.GraphInfo.Name == "pinch" && ig2.GraphInfo.Animat == AnimatType.B_Loop)
                    {
                        ig2.SetContinue();
                        return true;
                    }
            }
            Main_Event_TouchHead();
            Main_Event_TouchBody();
            Main.Display("pinch", AnimatType.A_Start, () =>
               Main.Display("pinch", AnimatType.B_Loop, DisplayPinch_loop));
            return true;
        }
        private void DisplayPinch_loop()
        {
            if (Main.isPress && Main.DisplayType.Name == "pinch" && Main.DisplayType.Animat == AnimatType.B_Loop)
            {
                if (Core.Controller.EnableFunction && Core.Save.Strength >= 10 && Core.Save.Feeling < Core.Save.FeelingMax)
                {
                    Core.Save.StrengthChange(-2);
                    Core.Save.FeelingChange(1);
                    Core.Save.Mode = Core.Save.CalMode();
                    Main.LabelDisplayShowChangeNumber(LocalizeCore.Translate("Stamina: -{0:f0}, Mood: +{1:f0}"), 2, 1);
                }
                Main.Display("pinch", AnimatType.B_Loop, DisplayPinch_loop);
            }
            else
            {
                Main.DisplayCEndtoNomal("pinch");
            }
        }
        /// <summary>
        /// </summary>
        public List<Work> WorkStar()
        {
            List<Work> works = new List<Work>();
            foreach (var work in Core.Graph.GraphConfig.Works)
            {
                if (Set["work_star"].GetBool(work.Name))
                    works.Add(work);
            }
            return works;
        }
        public System.Windows.Controls.MenuItem WorkStarMenu;

        public void LevelUP(GameSave_VPet.LevelUpEventArgs args)
        {
            var gf = Core.Graph.FindGraph("levelup", GraphInfo.AnimatType.Single, GameSavesData.GameSave.Mode);
            if (gf != null)
            {
                Main.Say("Bang bang kabang, level {0} has been advanced through!".Translate(Name), "levelup", true);
            }
            if (args.IsLevelMaxUp)
            {
                Task.Run(() =>
                {
                    Thread.Sleep(5000);
                    Dispatcher.Invoke(() =>
                    {
                        MessageBoxX.Show("System Notice\\nYour VPet level has advanced\\nLv{0}→LV{1} x{2}\\nHas advanced to the prestigious x{3} rank".Translate(
                            1000 + args.BeforeLevelMax * 100, 100 * GameSavesData.GameSave.LevelMax, GameSavesData.GameSave.LevelMax),
                            "VPet Level Advanced through".Translate());
                    });
                });
            }
        }

        public void CheckGalleryUnlock()
        {
            var ps = Photos.FindAll(x => !x.IsUnlock && !x.UnlockAble.SellBoth && x.UnlockAble.Check(GameSavesData));
            if (ps.Count == 0) return;
            StringBuilder sb = new StringBuilder();
            foreach (Photo p in ps)
            {
                sb.Append(", ");
                p.Unlock(this);
                sb.Append(p.TranslateName);
            }
            ActivityLogs.Add(new ActivityLog("photo_unlock", sb.ToString().AsSpan(2).ToString()));
            Dispatcher.Invoke(() =>
            NoticeBox.Show(string.Concat(sb.ToString().AsSpan(2), "\n", "The above photos have been unlocked".Translate()), "New photos have been unlocked".Translate()
            , Panuon.WPF.UI.MessageBoxIcon.Info, true, 5000));
        }
        /// <summary>
        /// </summary>
        public void ItemsAdd(Item item)
        {
            var sameitem = Items.Find(x => x.Name == item.Name);
            if (sameitem != null)
            {
                sameitem.Count += item.Count;
            }
            else
            {
                Items.Add(item);
            }
        }
    }
}
