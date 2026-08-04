using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace VPet_Simulator.Core
{
    /// <summary>
    /// </summary>
    public abstract class SayInfo
    {
        public SayInfo()
        {

        }
        /* --------- Message info -----------*/
        /// <summary>
        /// </summary>
        public string GraphName;
        /// <summary>
        /// </summary>
        public string Desc;
        /// <summary>
        /// </summary>
        public UIElement MsgContent;
        /// <summary>
        /// </summary>
        public bool Force = false;
        /// <summary>
        /// </summary>
        public bool IsGenVoice = false;
        /// <summary>
        /// </summary>
        public abstract Task<string> GetSayText();
    }
    /// <summary>
    /// </summary>
    public class SayInfoWithOutStream : SayInfo
    {
        /// <summary>
        /// </summary>
        /// <param name="Text Set">SpeakContent</param>
        /// <param name="desc">descriptions</param>
        public SayInfoWithOutStream(string text, string graphname = null, bool force = false, string desc = null)
        {
            Text = text;
            GraphName = graphname;
            Force = force;
            Desc = desc;
        }

        /// <summary>
        /// </summary>
        /// <param name="Text Set">SpeakContent</param>
        public SayInfoWithOutStream(string text, UIElement msgcontent, string graphname = null, bool force = false)
        {
            Text = text;
            GraphName = graphname;
            MsgContent = msgcontent;
            Force = force;
        }
        /// <summary>
        /// </summary>
        public SayInfoWithOutStream() { }
        /// <summary>
        /// SpeakContent
        /// </summary>
        public string Text;
        /// <summary>
        /// </summary>
        public override Task<string> GetSayText()
        {
            return Task.FromResult(Text);
        }
    }
    /// <summary>
    /// </summary>
    public class SayInfoWithStream : SayInfo
    {
        /// <summary>
        /// </summary>
        public SayInfoWithStream()
        {
        }
        /// <summary>
        /// </summary>
        /// <param name="desc">descriptions</param>
        public SayInfoWithStream(string graphname, bool force = false, string desc = null)
        {
            GraphName = graphname;
            Force = force;
            Desc = desc;
        }

        /// <summary>
        /// </summary>
        public SayInfoWithStream(UIElement msgcontent, string graphname = null, bool force = false)
        {
            GraphName = graphname;
            MsgContent = msgcontent;
            Force = force;
        }

        /// <summary>
        /// </summary>
        public event Action<(string fullText, string changedText)> Event_Update;
        /// <summary>
        /// </summary>
        public event Action<string> Event_Finish;
        /// <summary>
        /// </summary>
        public StringBuilder CurrentText = new StringBuilder();
        /// <summary>
        /// </summary>
        public bool IsFinishGen = false;

        /// <summary>
        /// </summary>
        public void UpdateAllText(string fullText)
        {
            CurrentText = new StringBuilder(fullText);
            Event_Update?.Invoke((fullText, fullText));
        }

        /// <summary>
        /// </summary>
        public void UpdateText(string text)
        {
            CurrentText.Append(text);
            Event_Update?.Invoke((CurrentText.ToString(), text));
        }

        /// <summary>
        /// </summary>
        public void FinishGenerate()
        {
            if (IsFinishGen)
                return;
            IsFinishGen = true;
            Event_Finish?.Invoke(CurrentText.ToString());
        }

        /// <summary>
        /// </summary>
        public async Task<SayInfoWithOutStream> ToNoneStream()
        {
            while (!IsFinishGen)
            {
                await Task.Delay(10);
            }
            return new SayInfoWithOutStream()
            {
                GraphName = GraphName,
                Force = Force,
                Desc = Desc,
                MsgContent = MsgContent,
                Text = CurrentText.ToString()
            };
        }
        /// <summary>
        /// </summary>
        public override async Task<string> GetSayText()
        {
            while (!IsFinishGen)
            {
                await Task.Delay(10);
            }
            return CurrentText.ToString();
        }
    }
}
