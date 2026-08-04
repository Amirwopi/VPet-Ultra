using LinePutScript;
using LinePutScript.Localization.WPF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using VPet_Simulator.Core;
using VPet_Simulator.Windows.Interface;

namespace VPet_Simulator.Windows
{
    /// <summary>
    /// </summary>
    public partial class TalkSelect : UserControl
    {

        /// <summary>
        /// </summary>
        List<SelectText> textList = new List<SelectText>();
        /// <summary>
        /// </summary>
        HashSet<string> textSaid = new HashSet<string>();
        /// <summary>
        /// </summary>
        public DateTime RelsTime;
        private DateTime lastAddTime;

        MainWindow mw;
        public TalkSelect(MainWindow mw)
        {
            InitializeComponent();
            this.mw = mw;
            mw.Main.ToolBar.EventShow += RelsSelect;
            RelsSelect();
        }


        /// <summary>
        /// </summary>
        public void RelsSelect()
        {
            if (RelsTime < DateTime.Now)
            {
                RelsTime = DateTime.Now.AddMinutes(10);
                lastAddTime = DateTime.Now;
                textList.Clear();
                textSaid.Clear();
                var list = mw.SelectTexts.ToList();
                while (list.Count > 0 && textList.Count < 5)
                {
                    int sid = Function.Rnd.Next(list.Count);
                    var select = list[sid];
                    list.RemoveAt(sid);
                    if (textList.Find(x => x.Choose == select.Choose) == null && select.CheckState(mw.Main))
                    {
                        textList.Add(select);
                    }
                }
            }
            if (textList.Count > 0)
            {
                tbTalk.Items.Clear();
                foreach (var item in textList)
                {
                    if (!textSaid.Contains(item.Choose))
                    {
                        tbTalk.Items.Add(item.TranslateChoose);
                    }
                }
                btn_Send.IsEnabled = true;
            }
            else
            {
                tbTalk.Items.Clear();
                tbTalk.Items.Add("There's nothing to say".Translate());
                btn_Send.IsEnabled = false;
            }
            double min = (RelsTime - DateTime.Now).TotalMinutes;
            double interval = (RelsTime - lastAddTime).TotalMinutes;
            double progress = 1 - min / interval;
            progress = Math.Min(1, Math.Max(0, progress));
            PrograssUsed.Value = progress;
            PrograssUsed.ToolTip = "Next refresh remaining time: {0:f1} minutes".Translate(min);
        }

        private void btn_Send_Click(object sender, RoutedEventArgs e)
        {
            if (tbTalk.SelectedIndex == -1 || tbTalk.Text == "There's nothing to say".Translate() || textList.Count == 0)
            {
                return;
            }
            mw.Main.ToolBar.Visibility = Visibility.Collapsed;
            var say = textList[tbTalk.SelectedIndex];
            textList.RemoveAt(tbTalk.SelectedIndex);

            mw.ActivityLogs.Add(new ActivityLog("hostsay",say.TranslateChoose));

            if (say.Exp != 0)
            {
                if (say.Exp > 0)
                {
                    mw.GameSavesData.Statistics[(gint)"stat_say_exp_p"]++;
                }
                else
                    mw.GameSavesData.Statistics[(gint)"stat_say_exp_d"]++;
            }
            if (say.Likability != 0)
            {
                if (say.Likability > 0)
                    mw.GameSavesData.Statistics[(gint)"stat_say_like_p"]++;
                else
                    mw.GameSavesData.Statistics[(gint)"stat_say_like_d"]++;
            }
            if (say.Money != 0)
            {
                if (say.Money > 0)
                    mw.GameSavesData.Statistics[(gint)"stat_say_money_p"]++;
                else
                    mw.GameSavesData.Statistics[(gint)"stat_say_money_d"]++;
            }
            mw.Main.Core.Save.EatFood(say);
            mw.Main.Core.Save.Money += say.Money;

            
            textSaid.Add(say.Choose);
            RelsTime = RelsTime.AddMinutes(5);
            lastAddTime = DateTime.Now;

            mw.Main.SayRnd(say.TranslateTextConvert(mw.Main), desc: say.FoodToDescription());
            if (say.ToTags.Count > 0)
            {
                var list = mw.SelectTexts.FindAll(x => x.ContainsTag(say.ToTags)).ToList();
                while (list.Count > 0)
                {
                    int sid = Function.Rnd.Next(list.Count);
                    var select = list[sid];
                    list.RemoveAt(sid);
                    if (textList.Find(x => x.Choose == select.Choose) == null && !textSaid.Contains(select.Choose) && select.CheckState(mw.Main))
                    {
                        textList.Add(select);
                        break;
                    }
                }
            }
            RelsSelect();
        }
    }
}
