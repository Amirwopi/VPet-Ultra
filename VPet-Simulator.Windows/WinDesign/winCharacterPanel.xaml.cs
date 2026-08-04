using LinePutScript;
using LinePutScript.Localization.WPF;
using Microsoft.Win32;
using Panuon.WPF.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VPet_Simulator.Core;
using VPet_Simulator.Windows.Interface;
using Color = System.Windows.Media.Color;

namespace VPet_Simulator.Windows
{
    /// <summary>
    /// </summary>
    public partial class winCharacterPanel : WindowX
    {
        MainWindow mw;

        public winCharacterPanel(MainWindow mw)
        {
            this.mw = mw;
            InitializeComponent();
            Title = "Status".Translate() + ' ' + mw.PrefixSave;
            mw.Windows.Add(this);
            foreach (var v in mw.GameSavesData.Statistics.Data)
            {
                StatList.Add(new StatInfo(v.Key, v.Value.GetDouble()));
            }
            DataGridStatic.ItemsSource = StatList;
            mw.GameSavesData.Statistics.StatisticChanged += Statistics_StatisticChanged;

            if (mw.GameSavesData.HashCheck)
            {
                cb_NoCheat.IsEnabled = true;
            }
            Task.Run(Load_Log);
        }

        private void Statistics_StatisticChanged(Statistics sender, string name, SetObject value)
        {
            Dispatcher.Invoke(() =>
            {
                try
                {
                    var v = StatList.FirstOrDefault(x => x.StatId == name);
                    if (v != null)
                    {
                        v.StatCount = value.GetDouble();
                    }
                    else
                    {
                        StatList.Add(new StatInfo(name, value.GetDouble()));
                    }
                }
                catch { }
            });
        }

        private ObservableCollection<StatInfo> StatList { get; set; } = new();

        private class StatInfo : INotifyPropertyChanged
        {
            public StatInfo(string statId, double statCount)
            {
                StatId = statId;
                StatCount = statCount;
                if (statId.StartsWith("buy_"))
                {
                    StatName = "Purchase".Translate() + '_' + statId.Substring(4).Translate();
                }
                else if (statId.StartsWith("stat_"))
                {
                    StatName = "Statistics".Translate() + '_' + statId.Substring(5).Translate();
                }
                else
                {
                    StatName = statId.Translate();
                }
            }

            /// <summary>
            /// StatisticsID
            /// </summary>
            public string StatId { get; set; }

            /// <summary>
            /// StatisticsDisplay Name
            /// </summary>
            public string StatName { get; set; }

            private double _statCount;
            /// <summary>
            /// StatisticsContent
            /// </summary>
            public double StatCount
            {
                get { return Math.Round(_statCount, 2); }
                set
                {
                    if (_statCount != value)
                    {
                        _statCount = value;
                        OnPropertyChanged(nameof(StatCount));
                    }
                }
            }
            protected virtual void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
            public event PropertyChangedEventHandler PropertyChanged;
        }
        private void TextBox_Search_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is not TextBox textBox)
                return;
            if (string.IsNullOrWhiteSpace(textBox.Text))
            {
                DataGridStatic.ItemsSource = StatList;
            }
            else
            {
                DataGridStatic.ItemsSource = StatList.Where(
                    i =>
                        i.StatName.IndexOf(
                            textBox.Text,
                            StringComparison.InvariantCultureIgnoreCase
                        ) >= 0 || i.StatId.IndexOf(
                            textBox.Text,
                            StringComparison.InvariantCultureIgnoreCase
                        ) >= 0
                );
            }
        }

        private void WindowX_Closed(object sender, EventArgs e)
        {
            mw.GameSavesData.Statistics.StatisticChanged -= Statistics_StatisticChanged;
            mw.Windows.Remove(this);
        }

        private void btn_r_save_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog()
            {
                FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "VPet_Rank.png"),
                Filter = "PNG Image File|*.png"
            };
            if (saveFileDialog.ShowDialog() != true)
                return;
            r_viewbox.ScrollToTop();
            FrameworkElement outputbox;
            if (r_output.ActualWidth > r_output_base.ActualWidth)
                outputbox = r_output;
            else
                outputbox = r_output_base;
            RenderTargetBitmap image = new RenderTargetBitmap((int)outputbox.ActualWidth, (int)outputbox.ActualHeight, 96, 96, PixelFormats.Pbgra32);
            image.Render(outputbox);
            var path = saveFileDialog.FileName;
            using (MemoryStream ms = new MemoryStream())
            {
                BitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(image));
                encoder.Save(ms);
                File.WriteAllBytes(path, ms.ToArray());

                var psi = new ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
        }

        private void cb_NoCheat_Unchecked(object sender, RoutedEventArgs e)
        {
        }

        private void btn_r_genRank_Click(object sender, RoutedEventArgs e)
        {
            btn_r_genRank.IsEnabled = false;
            pb_r_genRank.Value = 0;
            pb_r_genRank.Visibility = Visibility.Visible;
            Task.Run(GenRank);
        }
        private async void GenRank()
        {
            mw.Set["v"][(gint)"rank"] = DateTime.Now.Year;

            string petname = mw.GameSavesData.GameSave.Name;
            string username = Environment.UserName;

            int timelength = mw.GameSavesData.Statistics[(gint)"stat_total_time"];
            double timelength_h = (timelength / 3600.0);
            double startdatelength = (DateTime.Now - mw.GameSavesData[(gdat)"birthday"]).TotalDays;
            double startlengthrank = 0;
            string startlengthranktext;
            if (startlengthrank < 0.5)
                startlengthranktext = '"' + "Master, stay with me!".Translate() + '"';
            else
                startlengthranktext = '"' + "Master, thank you for your company.".Translate() + '"';

            double timelengthph = timelength_h / startdatelength;
            string timelengthphtext;
            string timelengthtext;
            int timelength_i;
            if (timelengthph < 2)
            {
                timelengthphtext = "Classmates".Translate();
                timelengthtext = '"' + "Senior".Translate() + '"';
                timelength_i = 1;
            }
            else if (timelengthph < 4)
            {
                timelengthphtext = "Friend!".Translate();
                timelengthtext = '"' + "Brother!".Translate() + '"';
                timelength_i = 2;
            }
            else if (timelengthph < 7)
            {
                timelengthphtext = "Best friend!".Translate();
                timelengthtext = '"' + "We don't want to be born in the same year, month and day, but we want to open VPet Simulator in the same year, month and day.".Translate() + '"';
                timelength_i = 3;
            }
            else if (timelengthph < 10)
            {
                timelengthphtext = "Family".Translate();
                timelengthtext = '"' + "We are Family.".Translate() + '"';
                timelength_i = 4;
            }
            else
            {
                timelengthphtext = "Daughter".Translate();
                timelengthtext = '"' + "Mom and Dad!, I don't think it's a good idea to call them that.".Translate() + '"';
                timelength_i = 5;
            }

            await Dispatcher.InvokeAsync(() => pb_r_genRank.Value = 10);
            string studytext;
            int study_i;
            if (mw.GameSavesData.GameSave.Level < 20)
            {
                studytext = "\n\"Purge! {0}'s Requiem\"".Translate(petname);
                study_i = 1;
            }
            else if (mw.GameSavesData.GameSave.Level < 40)
            {
                studytext = "It's the equivalent of VPet's high school education.\\n<College Entrance Exam for VPet>".Translate();
                study_i = 2;
            }
            else if (mw.GameSavesData.GameSave.Level < 60)
            {
                studytext = "\n\"College students eat sleep study, {0} eats sleeps studies, {0} = college student\"".Translate(petname);
                study_i = 3;
            }
            else if (mw.GameSavesData.GameSave.Level < 80)
            {
                studytext = "\n\"College students eat sleep study, I have nothing to do with that hooded guy\"".Translate();
                study_i = 4;
            }
            else
            {
                studytext = "<VPet-SimulatorExpert>\n\"{0} definitely did it!\"".Translate(username);
                study_i = 5;
            }

            int studyexpmax, studymoneymax;
            double studyexpmaxrank = 0, studymoneymaxrank = 0;
            studyexpmax = mw.GameSavesData.Statistics[(gint)"stat_single_profit_exp"];
            studymoneymax = mw.GameSavesData.Statistics[(gint)"stat_single_profit_money"];
            await Dispatcher.InvokeAsync(() => pb_r_genRank.Value = 20);
            string studyexptext, workmoneytext;
            int studyexp_i, workmoney_i;
            if (studyexpmaxrank < 0.25)
            {
                studyexptext = '"' + "How do you sleep at your age?".Translate() + '"';
                studyexp_i = 5;
            }
            else if (studyexpmaxrank < 0.4)
            {
                studyexptext = '"' + "The child's learning is not good, mostly waste, come and try the Nintendo switch learning machine".Translate() + '"';
                studyexp_i = 4;
            }
            else if (studyexpmaxrank < 0.55)
            {
                studyexptext = '"' + "The child's learning is not good, mostly waste, come and try the Nintendo switch learning machine".Translate() + '"';
                studyexp_i = 3;
            }
            else if (studyexpmaxrank < 0.75)
            {
                studyexptext = '"' + "Learning without thought means labour lost; thought without learning is perilous.".Translate() + '"';
                studyexp_i = 2;
            }
            else
            {
                studyexptext = '"' + "Look at my quantum speed reading method!".Translate() + '"';
                studyexp_i = 1;
            }

            if (studymoneymaxrank < 0.25)
            {
                workmoneytext = '"' + "Money is nothing.".Translate() + '"';
                workmoney_i = 4;
            }
            else if (studymoneymaxrank < 0.5)
            {
                workmoneytext = '"' + "The sound of the wind and rain, the sound of reading, the sound of the day, the sound of the month, the sound of the day, the sound of the month, the sound of the money.".Translate() + '"';
                workmoney_i = 3;
            }
            else if (studymoneymaxrank < 0.75)
            {
                workmoneytext = '"' + "Money can make things go away.".Translate() + '"';
                workmoney_i = 2;
            }
            else
            {
                workmoneytext = '"' + "But I really need that money!".Translate() + '"';
                workmoney_i = 1;
            }

            await Dispatcher.InvokeAsync(() => pb_r_genRank.Value = 40);

            int worktime = mw.GameSavesData.Statistics[(gint)"stat_work_time"];
            double worktimeph = (double)worktime / timelength;
            double worktimephrank = 0;
            string worktimephtext;
            int worktime_i;
            if (worktimephrank < 0.25)
            {
                worktimephtext = '"' + "One day at a time, one day off, one day at a time.".Translate() + '"';
                worktime_i = 1;
            }
            else if (worktimephrank < 0.35)
            {
                worktimephtext = '"' + "8 a.m. to 5 p.m., go home happy".Translate() + '"';
                worktime_i = 2;
            }
            else if (worktimephrank < 0.45)
            {
                worktimephtext = '"' + "8 a.m. to 5 p.m., go home happy".Translate() + '"';
                worktime_i = 3;
            }
            else if (worktimephrank < 0.55)
            {
                worktimephtext = '"' + "8 a.m. to 5 p.m., go home happy".Translate() + '"';
                worktime_i = 4;
            }
            else if (worktimephrank < 0.75)
            {
                worktimephtext = '"' + "Isn't it basic knowledge that overtime work is not paid?".Translate() + '"';
                worktime_i = 5;
            }
            else
            {
                worktimephtext = '"' + "Boss! The streetlights are ready!".Translate() + '"';
                worktime_i = 6;
            }

            int betterbuytimes = mw.GameSavesData.Statistics[(gint)"stat_buytimes"];
            int betterbuycount = (int)mw.GameSavesData.Statistics[(gdbe)"stat_betterbuy"];

            Food mostfood = new Food()
            {
                Name = "None",
            };

            foreach (var pair in mw.GameSavesData.Statistics.Data.Where(x => x.Key.StartsWith("buy_")).OrderByDescending(x => ((int)x.Value)))
            {
                var fn = pair.Key.Substring(4);
                var f = mw.Foods.FirstOrDefault(x => x.Name == fn);
                if (f != null)
                {
                    mostfood = f;
                    break;
                }
            }

            string foodtext = "haven't eaten anything. {0} is starving.".Translate(petname);
            switch (mostfood.Type)
            {
                case Food.FoodType.Meal:
                    foodtext = '"' + "Iron is steel, four dishes and one soup.".Translate() + '"';
                    break;
                case Food.FoodType.Drug:
                    foodtext = '"' + "Did you forget to turn on the auto-purchase?".Translate() + '"';
                    break;
                case Food.FoodType.Drink:
                    foodtext = '"' + "I need to drink plenty of hot beverages otherwise i get cold master.".Translate() + '"';
                    break;
                case Food.FoodType.Functional:
                    foodtext = '"' + "It's not that you can't afford to eat, it's that functionality is more cost-effective.".Translate() + '"';
                    break;
                case Food.FoodType.Snack:
                    foodtext = '"' + "Snacking is good for your mental health.".Translate() + '"';
                    break;
                case Food.FoodType.Gift:
                    foodtext = '"' + "If you don't give me up, May {0} be worshipped as adoptive father".Translate(petname) + '"';
                    break;
            }

            await Dispatcher.InvokeAsync(() => pb_r_genRank.Value = 60);

            int autobuytimes = mw.GameSavesData.Statistics[(gint)"stat_autobuy"];
            double autobuytimesph = (double)autobuytimes / betterbuytimes;
            double autobuytimesphrank = 0;
            string autobuytext;
            int autobuy_i;
            if (autobuytimesph < 0.25)
            {
                autobuytext = '"' + "Master, Are you worried that I will buy things randomly?".Translate() + '"';
                autobuy_i = 4;
            }
            else if (autobuytimesph < 0.5)
            {
                autobuytext = '"' + "I spend what I earn.".Translate() + '"';
                autobuy_i = 3;
            }
            else if (autobuytimesph < 0.75)
            {
                autobuytext = '"' + "Don't underestimate my intelligence network! You're not buying gifts automatically, are you?".Translate() + '"';
                autobuy_i = 2;
            }
            else
            {
                autobuytext = '"' + "We're looking for a nanny. Salary negotiable.".Translate() + '"';
                autobuy_i = 1;
            }

            await Dispatcher.InvokeAsync(() => pb_r_genRank.Value = 70);

            var modworkshoplist = mw.CoreMODs.FindAll(x => x.Path.FullName.Contains("workshop"));
            int modworkshop = modworkshoplist.Count;
            int modon = modworkshoplist.FindAll(x => x.IsOnMOD(mw)).Count;
            double modworkshoprank = 0;
            string modworkshoptext;
            int modworkshop_i;
            if (modworkshop == 0)
            {
                modworkshoptext = '"' + "VPet's steam workshop is full of mods, so go try them out.".Translate() + '"';
                modworkshop_i = 3;
            }
            else if (modworkshoprank < 0.3)
            {
                modworkshoptext = '"' + "Master can also go to the workshop to experience more MOD".Translate() + '"';
                modworkshop_i = 3;
            }
            else if (modworkshoprank < 0.7)
            { modworkshoptext = '"' + "There's a lot of interesting mods in the steam workshop. Would you like to take a look?".Translate() + '"'; modworkshop_i = 2; }
            else
            { modworkshoptext = '"' + "Master is already a master modder, try the mod maker and make mods for me!".Translate() + '"'; modworkshop_i = 1; }

            await Dispatcher.InvokeAsync(() => pb_r_genRank.Value = 80);

            int like = (int)mw.GameSavesData.GameSave.Likability;
            string liketext = "";
            while (like > 100)
            {
                like -= 100;
                liketext += '\uEE0E';
            }
            while (like > 50)
            {
                like -= 50;
                liketext += '\uEE0F';
            }
            if (liketext.Length == 0)
            {
                liketext = "\uEECA";
            }
            double likerank = 0;
            await Dispatcher.InvokeAsync(() => pb_r_genRank.Value = 88);

            await Dispatcher.InvokeAsync(() =>
            {
                r_r_startday.Text = mw.GameSavesData[(gdat)"birthday"].ToLongDateString();
                r_r_startlength.Text = startdatelength.ToString("f1");
                r_r_length_h.Text = timelength_h.ToString("f1");
                r_r_length_p.Text = startlengthrank.ToString("p1");
                r_r_lenghranktext.Text = startlengthranktext;

                r_r_lengthph.Text = timelengthph.ToString("f1");
                r_r_lengthphtext.Text = timelengthphtext;
                r_r_lenghtext.Text = timelengthtext;
                r_i_timelength.Source = new BitmapImage(new Uri($"pack://application:,,,/Res/img/r_timelength_{timelength_i}.png"));

                r_r_level.Text = mw.GameSavesData.GameSave.Level.ToString();
                r_r_exp.Text = mw.GameSavesData.GameSave.TotalExpGained().ToString("f0");
                r_r_studytime.Text = (mw.GameSavesData.Statistics[(gint)"stat_study_time"] / 60).ToString();
                r_r_studytext.Text = studytext;
                r_i_exp.Source = new BitmapImage(new Uri($"pack://application:,,,/Res/img/r_level_{study_i}.png"));

                r_r_studyexpmax.Text = studyexpmax.ToString();
                r_r_studyexpmaxrank.Text = studyexpmaxrank.ToString("p1");
                r_r_studyexptext.Text = studyexptext;
                r_i_singleexp.Source = new BitmapImage(new Uri($"pack://application:,,,/Res/img/r_singleexp_{studyexp_i}.png"));

                r_r_worktime.Text = (worktime / 60).ToString();
                r_r_worktimeps.Text = worktimeph.ToString("p1");
                r_r_worktimepsrank.Text = worktimephrank.ToString("p1");
                r_r_worktext.Text = worktimephtext;
                r_i_money.Source = new BitmapImage(new Uri($"pack://application:,,,/Res/img/r_worktime_{worktime_i}.png"));

                r_r_workmoneymax.Text = studymoneymax.ToString();
                r_r_workmoneyrank.Text = studymoneymaxrank.ToString("p1");
                r_r_workmoneytext.Text = workmoneytext;
                r_i_singlemoney.Source = new BitmapImage(new Uri($"pack://application:,,,/Res/img/r_singlemoney_{workmoney_i}.png"));

                r_r_username.Text = username;
                r_r_petname.Text = r_r_petname_2.Text = r_r_petname_3.Text = r_r_petname_4.Text = petname;
                r_r_now.Text = DateTime.Now.ToShortDateString();

                r_r_betterbuytimes.Text = betterbuytimes.ToString();
                r_r_betterbuycount.Text = betterbuycount.ToString();
                r_r_betterbuymosttype.Text = mostfood.Type.ToString().Translate();
                r_r_betterbuymostitem.Text = mostfood.TranslateName;
                r_r_betterbuymosttext.Text = foodtext;
                r_i_mostfood.Source = new BitmapImage(new Uri($"pack://application:,,,/Res/img/r_mostfood_{mostfood.Type}.png"));

                r_r_autobuy.Text = autobuytimes.ToString();
                r_r_autobuypres.Text = autobuytimesph.ToString("p1");
                r_r_autobuyrank.Text = autobuytimesphrank.ToString("p1");
                r_r_autobuytext.Text = autobuytext;
                r_i_autobuy.Source = new BitmapImage(new Uri($"pack://application:,,,/Res/img/r_autobuy_{autobuy_i}.png"));

                r_r_modcount.Text = modworkshop.ToString();
                r_r_modenablecount.Text = modon.ToString();
                r_r_modcountrank.Text = modworkshoprank.ToString("p1");
                r_r_modcounttext.Text = modworkshoptext;
                r_i_mod.Source = new BitmapImage(new Uri($"pack://application:,,,/Res/img/r_mod_{modworkshop_i}.png"));

                r_r_sleeplength.Text = (mw.GameSavesData.Statistics[(gint)"stat_sleep_time"] / 3600.0).ToString("f1");
                r_r_movelength.Text = px_tocm(mw.GameSavesData.Statistics[(gi64)"stat_move_length"], out string cm);
                r_r_movelengthcm.Text = cm;
                r_r_saycount.Text = mw.GameSavesData.Statistics[(gint)"stat_say_times"].ToString();
                r_r_musiccount.Text = mw.GameSavesData.Statistics[(gint)"stat_music"].ToString();
                r_r_touchtotal.Text = (mw.GameSavesData.Statistics[(gint)"stat_touch_body"] + mw.GameSavesData.Statistics[(gint)"stat_touch_head"]).ToString();

                if (mw.GameSavesData.GameSave.Likability > 100)
                    r_i_like.Visibility = Visibility.Visible;
                else
                    r_i_like.Visibility = Visibility.Collapsed;

                r_r_opencount.Text = mw.GameSavesData.Statistics[(gint)"stat_open_times"].ToString();
                r_r_bettercount.Text = mw.GameSavesData.Statistics[(gint)"stat_100_all"].ToString();
                r_r_likecount.Text = liketext;
                r_r_likecountrank.Text = likerank.ToString("p1");

                r_viewbox.Visibility = Visibility.Visible;
                btn_r_genRank.IsEnabled = true;
                btn_r_save.IsEnabled = true;
                pb_r_genRank.Visibility = Visibility.Collapsed;
                Width = 800;
                Height = 800;
            });
        }
        private string px_tocm(long px, out string cm)
        {
            if (px < 37795)
            {
                cm = "px";
                return px.ToString();
            }
            else if (px < 3779527)
            {
                cm = "cm";
                return (px * 2.54 / 96).ToString("f1");
            }
            else if (px < 377952755)
            {
                cm = "m";
                return (px * 2.54 / 9600).ToString("f1");
            }
            else
            {
                cm = "km";
                return (px * 2.54 / 9600000).ToString("f1");
            }
        }

        bool load2 = false;
        List<(string name, Core.PetLoader loader)> bdpetlist = null;
        private async void MainTab_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MainTab.SelectedIndex == 2 && load2 == false)
            {
                var petloader = mw.Pets.Find(x => x.Name == mw.Set.PetGraph);
                petloader ??= mw.Pets[0];
                var ordname = petloader.PetName.Translate();

                bdpetlist = mw.Pets.FindAll(x => x.Config.Data.FindLine("bday") != null).Select(x => (x.PetName.Translate(), x)).ToList();

                if (bdpetlist.Count == 0)
                {
                    tab_bday.IsEnabled = false;
                    return;
                }

                int sidx = 0;
                for (int i = 0; i < bdpetlist.Count; i++)
                {
                    if (bdpetlist[i].name == ordname)
                    {
                        if (petloader == bdpetlist[i].loader)
                        {
                            sidx = i;
                            bdpetlist[i] = (mw.GameSavesData.GameSave.Name, bdpetlist[i].loader);
                        }
                        else
                            bdpetlist[i] = (mw.GameSavesData.GameSave.Name + $"({bdpetlist[i].loader.Name.Translate()})", bdpetlist[i].loader);
                    }
                }
                cb_birthday.ItemsSource = bdpetlist.Select(x => x.name);
                lb_b_datetime.Content = "Shot on VPet - " + DateTime.Now.ToShortDateString();
                cb_birthday.SelectedIndex = sidx;
                BDay_Load();
                Width = 800;
                Height = 675;
                load2 = true;
            }

        }
        public void BDay_Load()
        {
            var pl = bdpetlist[cb_birthday.SelectedIndex];
            img_b_background.Source = mw.ImageSources.FindImage("bday_" + pl.loader.Name);
            tb_bdiy.Text = "{0} Happy Birthday to {1}!".Translate(pl.name, mw.GameSavesData.GameSave.HostName);
            ILine bdinfo = pl.loader.Config.Data.FindLine("bday");
            img_b_head.Width = bdinfo[(gdbe)"w"];
            img_b_head.Margin = new Thickness(bdinfo[(gdbe)"x"], bdinfo[(gdbe)"y"], 0, 0);
            lb_b_datetime.VerticalAlignment = Enum.Parse<VerticalAlignment>(bdinfo[(gstr)"va"], true);
            lb_b_datetime.HorizontalAlignment = Enum.Parse<HorizontalAlignment>(bdinfo[(gstr)"ha"], true);
            b_b_text.Background = new SolidColorBrush(Function.HEXToColor('#' + bdinfo[(gstr)"tb"]));
            tb_b_text.Foreground = new SolidColorBrush(Function.HEXToColor('#' + bdinfo[(gstr)"tf"]));
            vb_b_text.Margin = new Thickness(bdinfo[(gdbe)"tleft"], bdinfo[(gdbe)"ttop"], bdinfo[(gdbe)"tright"], bdinfo[(gdbe)"tbottom"]);
        }

        private void cb_birthday_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (load2 == false)
            {
                return;
            }
            BDay_Load();
        }

        private void btn_b_headimage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog()
            {
                Filter = "Image File|*.png;*.jpg;*.jpeg;",
                Title = "Select avatar image".Translate()
            };
            if (openFileDialog.ShowDialog() == true)
            {
                img_b_head.Source = new BitmapImage(new Uri(openFileDialog.FileName));
            }
        }

        private void btn_b_save_Click(object sender, RoutedEventArgs e)
        {
            if (load2 == false)
            {
                return;
            }
            SaveFileDialog saveFileDialog = new SaveFileDialog()
            {
                FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "VPet_BDay.png"),
                Filter = "PNG Image File|*.png"
            };
            if (saveFileDialog.ShowDialog() != true)
                return;
            RenderTargetBitmap image = new RenderTargetBitmap((int)b_Output.ActualWidth, (int)b_Output.ActualHeight, 96, 96, PixelFormats.Pbgra32);
            image.Render(b_Output);
            var path = saveFileDialog.FileName;
            using (MemoryStream ms = new MemoryStream())
            {
                BitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(image));
                encoder.Save(ms);
                File.WriteAllBytes(path, ms.ToArray());

                var psi = new ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
        }

        private void dtp_bdiy_SelectedDateTimeChanged(object sender, Panuon.WPF.SelectedValueChangedRoutedEventArgs<DateTime?> e)
        {
            if (load2 == false)
            {
                return;
            }
            lb_b_datetime.Content = "Shot on VPet - " + dtp_bdiy.SelectedDateTime?.ToShortDateString();
        }

        private void Load_Log()
        {
            string text = "";
            if (mw.Set.DeBug)
                text = string.Join('\n', mw.ActivityLogs.Select(x => x.ToString(mw.Main)).ToList());
            else
                text = string.Join('\n', mw.ActivityLogs.Where(x => x.IsDebug == false).Select(x => x.ToString(mw.Main)).ToList());
            Dispatcher.Invoke(() =>
            {
                tb_log.Text = text;
            });
            mw.ActivityLogs.CollectionChanged += ActivityLogs_CollectionChanged;
        }

        private void ActivityLogs_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (e.NewItems != null)
                {
                    foreach (ActivityLog log in e.NewItems)
                    {
                        if (mw.Set.DeBug || log.IsDebug == false)
                        {
                            tb_log.AppendText("\n" + log.ToString(mw.Main));
                        }
                    }
                }
            });
        }
    }
}
