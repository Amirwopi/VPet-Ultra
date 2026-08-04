using System;
using System.IO.Packaging;
using System.Reflection;
using System.Security.Cryptography.Xml;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Navigation;
using VPet_Simulator.Core;
using static VPet_Simulator.Core.GraphInfo;
using static VPet_Simulator.Core.Main;

namespace VPet_Simulator.Windows.Interface
{

    /// <summary>
    /// </summary>
    public abstract partial class TalkBox : UserControl, ITalkAPI
    {
        /// <summary>
        /// </summary>
        protected MainPlugin MainPlugin;
        public TalkBox(MainPlugin mainPlugin)
        {
            var baseUri = "/VPet-Simulator.Windows.Interface;component/talkbox.xaml";
            var resourceLocater = new Uri(baseUri, UriKind.Relative);
            var exprCa = (PackagePart)typeof(Application).GetMethod("GetResourceOrContentPart", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { resourceLocater });
            var stream = exprCa.GetStream();
            var uri = new Uri((Uri)typeof(BaseUriHelper).GetProperty("PackAppBaseUri", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null, null), resourceLocater);
            var parserContext = new ParserContext
            {
                BaseUri = uri
            };
            typeof(XamlReader).GetMethod("LoadBaml", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { stream, parserContext, this, true });

            InitializeComponent();
            MainPlugin = mainPlugin;
        }
        /// <summary>
        /// </summary>
        /// <param name="Text Set">Content</param>
        public abstract void Responded(string text);
        /// <summary>
        /// </summary>
        public abstract string APIName { get; }

        private void Send_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(tbTalk.Text))
            {
                return;
            }
            var cont = tbTalk.Text;
            tbTalk.Text = "";
            MainPlugin.MW.Main.ToolBar.Visibility = Visibility.Collapsed;

            Task.Run(() => Responded(cont));
        }
        /// <summary>
        /// </summary>
        public void DisplayThink()
        {
            if (MainPlugin.MW.Main.DisplayType.Name == "think")
                return;

            var think = MainPlugin.MW.Core.Graph.FindGraphs("think", AnimatType.B_Loop, MainPlugin.MW.Core.Save.Mode);
            var think2 = MainPlugin.MW.Core.Graph.FindGraphs("think", AnimatType.A_Start, MainPlugin.MW.Core.Save.Mode);
            if (think.Count > 0 && think2.Count > 0)
            {
                MainPlugin.MW.Main.Display("think", AnimatType.A_Start, MainPlugin.MW.Main.DisplayBLoopingForce);
            }
        }
        /// <summary>
        /// </summary>
        public void DisplayThinkToSayRnd(string text, string desc = null)
        {
            var think = MainPlugin.MW.Core.Graph.FindGraphs("think", AnimatType.C_End, MainPlugin.MW.Core.Save.Mode);
            Action Next = () => { MainPlugin.MW.Main.SayRnd(text, true, desc); };
            if (think.Count > 0)
            {
                MainPlugin.MW.Main.Display(think[Function.Rnd.Next(think.Count)], Next);
            }
            else
            {
                Next();
            }
        }

        /// <summary>
        /// </summary>
        public void DisplayThinkToSayRnd(SayInfoWithStream sayInfostream)
        {
            var think = MainPlugin.MW.Core.Graph.FindGraphs("think", AnimatType.C_End, MainPlugin.MW.Core.Save.Mode);
            sayInfostream.Force = true;
            if (think.Count > 0)
            {
                Task.Run(() =>
                {
                    while (!sayInfostream.IsFinishGen && Function.ComCheck(sayInfostream.CurrentText.ToString()) < 4 && sayInfostream.CurrentText.Length < 80)
                    {
                        Thread.Sleep(50);
                    }
                    int a = Function.ComCheck(sayInfostream.CurrentText.ToString());
                    int b = sayInfostream.CurrentText.Length;
                    MainPlugin.MW.Main.Display(think[Function.Rnd.Next(think.Count)], () => MainPlugin.MW.Main.SayRnd(sayInfostream));
                });
            }
            else
            {
                MainPlugin.MW.Main.SayRnd(sayInfostream);
            }
        }

        /// <summary>
        /// Chat Settings
        /// </summary>
        public abstract void Setting();

        private void tbTalk_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Control))
            {
                Send_Click(sender, e);
                e.Handled = true;
                MainPlugin.MW.Main.ToolBar.Visibility = Visibility.Collapsed;
                return;
            }
            if (tbTalk.Text.Length > 0)
            {
                MainPlugin.MW.Main.ToolBar.CloseTimer.Stop();
            }
            else
            {
                MainPlugin.MW.Main.ToolBar.CloseTimer.Start();
            }
        }
        public UIElement This => this;
    }
    public interface ITalkAPI
    {
        /// <summary>
        /// </summary>
        UIElement This { get; }

        /// <summary>
        /// </summary>
        string APIName { get; }
        /// <summary>
        /// Chat Settings
        /// </summary>
        void Setting();
    }
}
