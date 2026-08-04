using LinePutScript;
using LinePutScript.Dictionary;
using LinePutScript.Localization.WPF;
using Panuon.WPF.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography.Xml;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using VPet_Simulator.Core;
using VPet_Simulator.Windows.Interface;
using static VPet_Simulator.Core.GraphInfo;
using static VPet_Simulator.Core.Main;
using static VPet_Simulator.Windows.PerformanceDesktopTransparentWindow;
using Line = LinePutScript.Line;

namespace VPet_Simulator.Windows
{
    /// <summary>
    /// </summary>
    public partial class MainWindow : WindowX
    {
        internal System.Windows.Forms.NotifyIcon notifyIcon;
        public PetHelper petHelper;
        public System.Timers.Timer AutoSaveTimer = new System.Timers.Timer();

        public MainWindow()
        {
            Args = new LPS_D();
            foreach (var str in App.Args)
            {
                Args.Add(new Line(str));
            }

            if (Args.ContainsLine("prefix"))
            {
                PrefixSave = '-' + Args["prefix"].Info;
            }
            if (Args.ContainsLine("linux"))
            {
                AllowsTransparency = true;
                WindowStyle = WindowStyle.None;
            }

            PNGAnimation.MaxLoadMemory = (int)Function.MemoryAvailable() / 2;
#if !X64
            if(PNGAnimation.MaxLoadMemory > 3000)
                PNGAnimation.MaxLoadMemory = 3000;
#endif
            if (PNGAnimation.MaxLoadMemory < 512)
                PNGAnimation.MaxLoadMemory = 512;

            PNGAnimation.MaxLoadMemory += (int)Function.MemoryUsage();

            ExtensionValue.BaseDirectory = new FileInfo(System.Reflection.Assembly.GetExecutingAssembly().Location).DirectoryName;


            LocalizeCore.StoreTranslation = true;
            LocalizeCore.TranslateFunc = (str) =>
            {
                var destr = Sub.TextDeReplace(str);
                if (destr != str && LocalizeCore.CurrentLPS != null && LocalizeCore.CurrentLPS.Assemblage.TryGetValue(destr, out ILine line))
                {
                    return line.GetString();
                }
                if (str.Contains('_') && double.TryParse(str.Split('_').Last(), out double d))
                    return d.ToString();
                return null;
            };

            CultureInfo.CurrentCulture = new CultureInfo(CultureInfo.CurrentCulture.Name);
            CultureInfo.CurrentCulture.NumberFormat = new CultureInfo("en-US").NumberFormat;

            if (Directory.Exists(ExtensionValue.BaseDirectory + @"\BackUP"))
            {
                if (!Directory.Exists(ExtensionValue.BaseDirectory + @"\Saves"))
                    Directory.Move(ExtensionValue.BaseDirectory + @"\BackUP", ExtensionValue.BaseDirectory + @"\Saves");
                else
                {
                    foreach (var file in new DirectoryInfo(ExtensionValue.BaseDirectory + @"\BackUP").GetFiles())
                        if (!File.Exists(ExtensionValue.BaseDirectory + @"\Saves\" + file.Name))
                            file.MoveTo(ExtensionValue.BaseDirectory + @"\Saves\" + file.Name);
                        else
                            file.Delete();
                    Directory.Delete(ExtensionValue.BaseDirectory + @"\BackUP", true);
                }
            }

            _dwmEnabled = Win32.Dwmapi.DwmIsCompositionEnabled();
            _hwnd = new WindowInteropHelper(this).EnsureHandle();

            GameInitialization();

            Task.Run(async () =>
            {
                List<DirectoryInfo> Path = new List<DirectoryInfo>();
                Path.AddRange(new DirectoryInfo(ModPath).EnumerateDirectories());

                bool NOCancel = true;
                CancellationTokenSource source = new CancellationTokenSource();
                var tsk = Task.Run(async () =>
                {
                    var workshop = Set["workshop"];
                    foreach (Sub ws in workshop)
                    {
                        Path.Add(new DirectoryInfo(ws.Name));
                    }
                }, source.Token);

                while (NOCancel && !tsk.IsCompleted)
                {
                    Thread.Sleep(500);
                }
                if (!NOCancel)
                {
                    source.Cancel();
                    var workshop = Set["workshop"];
                    foreach (Sub ws in workshop)
                    {
                        Path.Add(new DirectoryInfo(ws.Name));
                    }
                }


                Dispatcher.InvokeAsync(new Action(() => LoadingText.Content = "Loading Translate")).Wait();
                LocalizeCore.StoreTranslation = true;
                if (Set.Language == "null")
                {
                    LocalizeCore.LoadDefaultCulture();
                    if (LocalizeCore.CurrentCulture == "null")
                        LocalizeCore.CurrentCulture = "en";
                    Set.Language = LocalizeCore.CurrentCulture;
                }
                else
                    LocalizeCore.LoadCulture(Set.Language);

                var cgpte = Set.FindLine("CGPT");
                if (cgpte != null)
                {
                    var cgpteb = cgpte.Find("enable");
                    if (cgpteb != null)
                    {
                        if (Set["CGPT"][(gbol)"enable"])
                        {
                            Set["CGPT"][(gstr)"type"] = "API";
                        }
                        else
                        {
                            Set["CGPT"][(gstr)"type"] = "LB";
                        }
                        Set["CGPT"].Remove(cgpteb);
                    }
                }
                else if (Set["CGPT"][(gstr)"type"] == "OFF")
                {
                    Set["CGPT"][(gstr)"type"] = "LB";
                }
                else
                    Set["CGPT"][(gstr)"type"] = "LB";

                await GameLoad(Path);



                Item.UseAction.Add("Food", [(imw,Item) =>
                  {
                      if(Item is Food food)
                      {
                          imw.TakeItem(food);
                          imw.TakeItemHandle(food, 1, "item");
                          imw.DisplayFoodAnimation(food.GetGraph(), food.ImageSource);
                          Item.Consume(imw);
                          return true;
                      }
                      return false;
                  }]);
                Item.UseAction.Add("Toy", [(imw,Item) =>
                  {
                       var graph = imw.Core.Graph.FindGraph(Item.Data, AnimatType.A_Start, imw.GameSavesData.GameSave.Mode);
                       imw.ActivityLogs.Add(new ActivityLog("al_take_item", Item.TranslateName));
                      if (graph == null)
                          {
                             graph = imw.Core.Graph.FindGraph(Item.Data, AnimatType.Single, imw.GameSavesData.GameSave.Mode);
                              if(graph != null)
                                {
                                    imw.Main.Display(graph, Main.DisplayToNomal);
                                }
                                else
                                {
                                    imw.Main.SayRnd("This toy doesn't seem to be playable".Translate());
                                }
                          return true;
                          }

                        imw.Main.Display(Item.Data, AnimatType.A_Start, imw.Main.DisplayBLoopingToNomal(imw.Core.Graph.GraphConfig.GetDuration(graph.GraphInfo.Name)));
                        return true;
                  }]);
                Item.UseAction.Add("Mail", [
                (imw,Item) => {
                      switch (Item.Name)
                      {
                          case "Daily Gift Pack":
                              var moneylimit = Math.Min(20000, (50 * (imw.GameSavesData.GameSave.LevelMax + 1) + imw.GameSavesData.GameSave.Level +1) * 50);
                              var chosenfood = imw.Foods.FindAll(x=>x.Price > 10 && x.Price < moneylimit);
                              if(chosenfood.Count == 0)
                                    return false;
                              imw.ItemsAdd(chosenfood[Function.Rnd.Next(chosenfood.Count)].Clone());
                              imw.ItemsAdd(chosenfood[Function.Rnd.Next(chosenfood.Count)].Clone());
                              imw.ItemsAdd(chosenfood[Function.Rnd.Next(chosenfood.Count)].Clone());
                              Item.Consume(imw);
                              return true;
                      }
                      return false;
                  },
                   (imw,Item) =>
                  {
                     var lps = new LpsDocument(Item.Data);
                      List<string> itemnames = new List<string>();
                      foreach(var line in lps)
                      {
                          var itm = Item.CreateItem(imw,line);
                          itm.LoadSource(this);
                          imw.ItemsAdd(itm);
                          itemnames.Add(itm.TranslateName);
                      }
                      if(itemnames.Count != 0)
                      {
                          Main.SayRnd("You opened {0} and obtained an item".Translate(Item.Name) +"\n" + string.Join(',',itemnames));
                      }
                      Item.Consume(this);
                     return true;
                  }]);
                Item.UseAction.Add("Tool", [(imw,Item) =>
                  {
                     switch (Item.Name)
                      {
                          case "Compass":
                               imw.Main.DisplayMove();
                              return true;
                      }
                      return false;
                   }]);
            });
        }


        public new void Close()
        {
            if (Main == null)
            {
                base.Close();
            }
            else
            {
                Main.Display(GraphType.Shutdown, AnimatType.Single, () => Dispatcher.Invoke(base.Close));
            }
        }
        public void Restart()
        {
            this.Closed -= Window_Closed;
            this.Closed += Restart_Closed;
            base.Close();
        }

        private void Restart_Closed(object sender, EventArgs e)
        {
            CloseConfirm = false;
            try
            {
                foreach (MainPlugin mp in Plugins)
                    mp.EndGame();
            }
            catch { }
            Save();
            if (App.MainWindows.Count == 1)
            {
                var psi = new ProcessStartInfo
                {
                    FileName = System.IO.Path.ChangeExtension(System.Reflection.Assembly.GetExecutingAssembly().Location, "exe"),
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
            else
            {
                new MainWindow(PrefixSave, this).Show();
            }
            Exit();
        }
        private void Exit()
        {
            if (App.MainWindows.Count <= 1)
            {
                Task.Run(() =>
                {
                    Thread.Sleep(10000);
                    Environment.Exit(0);
                });
                try
                {
                    if (Core != null && Core.Graph != null)
                    {
                        foreach (var igs in Core.Graph.GraphsList.Values)
                        {
                            foreach (var ig2 in igs.Values)
                            {
                                foreach (var ig3 in ig2)
                                {
                                    ig3.Stop(true);
                                }
                            }
                        }
                    }
                    while (Windows.Count != 0)
                    {
                        var w = Windows[0];
                        w.Close();
                        Windows.Remove(w);
                    }
                    Main?.Dispose();
                    AutoSaveTimer?.Stop();
                    MusicTimer?.Stop();
                    petHelper?.Close();
                    winSetting?.Close();
                    winBetterBuy?.Close();
                    winWorkMenu?.Close();
                    winGallery?.Close();

                    if (notifyIcon != null)
                    {
                        notifyIcon.Visible = false;
                        notifyIcon.Dispose();
                    }
                    notifyIcon?.Dispose();
                }
                finally
                {
                    Environment.Exit(0);
                }
                while (true)
                    Environment.Exit(0);
            }
            else
            {
                if (Core != null && Core.Graph != null)
                {
                    foreach (var igs in Core.Graph.GraphsList.Values)
                    {
                        foreach (var ig2 in igs.Values)
                        {
                            foreach (var ig3 in ig2)
                            {
                                ig3.Stop(true);
                            }
                        }
                    }
                }
                while (Windows.Count != 0)
                {
                    Windows[0].Close();
                }
                Main?.Dispose();
                AutoSaveTimer?.Stop();
                MusicTimer?.Stop();
                petHelper?.Close();
                winSetting?.Close();
                winBetterBuy?.Close();
                winWorkMenu?.Close();
                winGallery?.Close();
                App.MainWindows.Remove(this);
                if (notifyIcon != null)
                {
                    notifyIcon.Visible = false;
                    notifyIcon.Dispose();
                }
                notifyIcon?.Dispose();
            }
        }


        public long lastclicktime { get; set; }

        public void LoadLatestSave(string petname)
        {
            if (Directory.Exists(ExtensionValue.BaseDirectory + @"\Saves"))
            {
                var ds = new List<string>(Directory.GetFiles(ExtensionValue.BaseDirectory + @"\Saves", $@"Save{PrefixSave}_*.lps"))
                    .OrderBy(x =>
                 {
                     if (int.TryParse(x.Split('_').Last().Split('.')[0], out int i))
                         return i;
                     return 0;
                 }).ToList();

                if (ds.Count != 0)
                {
                    int.TryParse(ds.Last().Split('_').Last().Split('.')[0], out int lastid);
                    if (Set.SaveTimes < lastid)
                    {
                        Set.SaveTimes = lastid;
                    }
                }
                for (int i = ds.Count - 1; i >= 0; i--)
                {
                    var latestsave = ds[i];
                    if (latestsave != null)
                    {
                        if (TryLoadSaveFile(latestsave))
                            return;
                    }
                }

            }
            GameSavesData = new GameSave_v2(petname.Translate());
            CheckBackupConsistency(GameSavesData, "New Game");
            Core.Save = GameSavesData.GameSave;
            HashCheck = HashCheck;
            GameSavesData.GameSave.Event_LevelUp += LevelUP;
        }

        /// <summary>
        /// </summary>
        /// <param name="saveFilePath"></param>
        /// <returns></returns>
        private bool TryLoadSaveFile(string saveFilePath)
        {
            if (string.IsNullOrEmpty(saveFilePath))
                return false;
#if !DEBUG
            try
            {
#endif
            var content = File.ReadAllText(saveFilePath);
            GameSave_v2 gs = new GameSave_v2(new LPS(content));
            CheckBackupConsistency(gs, new FileInfo(saveFilePath).Name);

            if (SavesLoad(new LPS(content)))
                return true;
#if !DEBUG
            }
            catch (Exception ex)
            {
                MessageBoxX.Show("Archive damage, unable to load the archive\\nIt may be caused by data overflow/overmodel" + '\n' + ex.Message, "Archive damage".Translate());
            }
#endif
            return false;
        }

        /// <summary>
        /// </summary>
        private void CheckBackupConsistency(GameSave_v2 gs, string currentName)
        {
            if (!Directory.Exists(ExtensionValue.BaseDirectory + @"\Saves_BKP"))
                return;
            try
            {
                var bks = new DirectoryInfo(ExtensionValue.BaseDirectory + @"\Saves_BKP")
                    .GetFiles($"Save{PrefixSave}_*.lps").OrderByDescending(x => x.LastWriteTime).FirstOrDefault();
                if (bks != null)
                {
                    try
                    {
                        var gs2 = new GameSave_v2(new LPS(File.ReadAllText(bks.FullName)));
                        if (!(gs2.GameSave.Level == gs.GameSave.Level &&
                            gs2.GameSave.Exp == gs.GameSave.Exp &&
                            gs2.GameSave.Money == gs.GameSave.Money))
                        {
                            MessageBox.Show("Detected discrepancy between save and backup.\\nCurrent Save: {0} Lv{1} ${4:f0}\\nBackup Save: {2} Lv{3} ${5:f0}\\nTo restore, please load the backup from settings."
                                .Translate(currentName, gs.GameSave.Level, bks.Name, gs2.GameSave.Level, gs.GameSave.Money, gs2.GameSave.Money)
                                , "".Translate());

                        }
                    }
                    catch
                    {
                    }
                }
            }
            catch
            {

            }
        }

        private void WorkTimer_E_FinishWork(WorkTimer.FinishWorkInfo obj)
        {
            if (obj.work.Type == GraphHelper.Work.WorkType.Work)
            {
                GameSavesData.Statistics[(gint)"stat_single_profit_money"] = (int)obj.count;
            }
            else
            {
                GameSavesData.Statistics[(gint)"stat_single_profit_exp"] = (int)obj.count;
            }
        }

        private void Main_Event_TouchBody()
        {
            GameSavesData.Statistics[(gint)"stat_touch_body"]++;
        }

        private void Main_Event_TouchHead()
        {
            GameSavesData.Statistics[(gint)"stat_touch_head"]++;
        }

        private void Main_OnSay(SayInfo obj)
        {
            GameSavesData.Statistics[(gint)"stat_say_times"]++;
        }

        private void MoveTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            GameSavesData.Statistics[(gint)"stat_move_length"] += (int)(Math.Abs(Main.MoveTimerPoint.X) + Math.Abs(Main.MoveTimerPoint.Y));
        }

        private void AutoSaveTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            CheckGalleryUnlock();
            Save();
        }


        private void Window_Closed(object sender, EventArgs e)
        {
            CloseConfirm = false;
            try
            {
                foreach (MainPlugin mp in Plugins)
                    mp.EndGame();
            }
            catch { }
            Save();
            Exit();
        }

        [DllImport("user32", EntryPoint = "SetWindowLong")]
        private static extern uint SetWindowLong(IntPtr hwnd, int nIndex, uint dwNewLong);

        [DllImport("user32", EntryPoint = "GetWindowLong")]
        private static extern uint GetWindowLong(IntPtr hwnd, int nIndex);
        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            //const int WS_EX_TRANSPARENT = 0x20;
            //const int GWL_EXSTYLE = -20;
            //IntPtr hwnd = new WindowInteropHelper(this).Handle;
            //uint extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            //SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT);
            ((HwndSource)PresentationSource.FromVisual(this)).AddHook((IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) =>
            {
                if (msg == (int)Win32.WM.STYLECHANGING && (long)wParam == (long)Win32.GetWindowLongFields.GWL_EXSTYLE)
                {
                    var styleStruct = (STYLESTRUCT)Marshal.PtrToStructure(lParam, typeof(STYLESTRUCT));
                    styleStruct.styleNew |= (int)Win32.ExtendedWindowStyles.WS_EX_LAYERED;

                    // Hide windows from alt+tab: https://stackoverflow.com/questions/357076/best-way-to-hide-a-window-from-the-alt-tab-program-switcher
                    if (Set.HideFromTaskControl)
                    {
                        styleStruct.styleNew |= (int)Win32.ExtendedWindowStyles.WS_EX_TOOLWINDOW;
                    }

                    Marshal.StructureToPtr(styleStruct, lParam, false);
                    handled = true;
                }
                return IntPtr.Zero;
            });
        }
        private readonly bool _dwmEnabled;
        private readonly IntPtr _hwnd;
        public bool HitThrough { get; private set; } = false;
        public bool MouseHitThrough
        {
            get => HitThrough;
            set
            {
                if (value != HitThrough)
                    SetTransparentHitThrough();
            }
        }
        /// <summary>
        /// </summary>
        public void SetTransparentHitThrough()
        {
            if (_dwmEnabled)
            {
                //const int WS_EX_TRANSPARENT = 0x20;
                //const int GWL_EXSTYLE = -20;
                //IntPtr hwnd = new WindowInteropHelper(this).Handle;
                //uint extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
                //SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT);
                HitThrough = !HitThrough;
                (notifyIcon.ContextMenuStrip.Items.Find("NotifyIcon_HitThrough", false).First() as System.Windows.Forms.ToolStripMenuItem).Checked = HitThrough;
                if (HitThrough)
                {
                    Win32.User32.SetWindowLongPtr(_hwnd, Win32.GetWindowLongFields.GWL_EXSTYLE,
                        (IntPtr)(int)((long)Win32.User32.GetWindowLongPtr(_hwnd, Win32.GetWindowLongFields.GWL_EXSTYLE) | (long)Win32.ExtendedWindowStyles.WS_EX_TRANSPARENT));
                    petHelper?.SetOpacity(false);
                    if (Set.OpacityHitThrough)
                        Opacity = Set.Opacity;
                }
                else
                {
                    Win32.User32.SetWindowLongPtr(_hwnd, Win32.GetWindowLongFields.GWL_EXSTYLE,
                  (IntPtr)(int)((long)Win32.User32.GetWindowLongPtr(_hwnd, Win32.GetWindowLongFields.GWL_EXSTYLE) & ~(long)Win32.ExtendedWindowStyles.WS_EX_TRANSPARENT));
                    petHelper?.SetOpacity(true);
                    if (Set.OpacityMain)
                        Opacity = Set.Opacity;
                    else
                        Opacity = 1;
                }
            }
        }
        private void WindowX_LocationChanged(object sender, EventArgs e)
        {
            petHelper?.SetLocation();
        }
        /// <summary>
        /// </summary>
        public void ShowInputBox(string title, string text, string defaulttext, Action<string> ENDAction, bool AllowMutiLine = false, bool TextCenter = true, bool CanHide = false)
        {
            winInputBox.Show(this, title, text, defaulttext, ENDAction, AllowMutiLine, TextCenter, CanHide);
        }
    }
}
