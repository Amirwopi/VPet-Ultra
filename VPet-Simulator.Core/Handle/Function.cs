using LinePutScript.Converter;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace VPet_Simulator.Core
{
    public static partial class Function
    {
        /// <summary>
        /// </summary>
        /// <param name="HEX">HEXValue</param>
        public static Color HEXToColor(string HEX) => (Color)ColorConverter.ConvertFromString(HEX);
        /// <summary>
        /// </summary>
        /// <returns>HEXValue</returns>
        public static string ColorToHEX(Color color) => "#" + color.R.ToString("X2") + color.G.ToString("X2") + color.B.ToString("X2");
        public static Random Rnd = new Random();
        /// <summary>
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static Brush ResourcesBrush(BrushType name)
        {
            return (Brush)Application.Current.Resources[name.ToString()];
        }
        public enum BrushType
        {
            Primary,
            PrimaryTrans,
            PrimaryTrans4,
            PrimaryTransA,
            PrimaryTransE,
            PrimaryLight,
            PrimaryLighter,
            PrimaryDark,
            PrimaryDarker,
            PrimaryText,

            Secondary,
            SecondaryTrans,
            SecondaryTrans4,
            SecondaryTransA,
            SecondaryTransE,
            SecondaryLight,
            SecondaryLighter,
            SecondaryDark,
            SecondaryDarker,
            SecondaryText,

            DARKPrimary,
            DARKPrimaryTrans,
            DARKPrimaryTrans4,
            DARKPrimaryTransA,
            DARKPrimaryTransE,
            DARKPrimaryLight,
            DARKPrimaryLighter,
            DARKPrimaryDark,
            DARKPrimaryDarker,
            DARKPrimaryText,
        }
        ///// <summary>
        ///// </summary>
        //public static string Translate(this LPS_D TranFile, string Name) => TranFile.GetString(Name, Name);

        public class LPSConvertToLower : LPSConvert.ConvertFunction
        {
            public override string Convert(dynamic value) => value;

            public override dynamic ConvertBack(string info) => info.ToLowerInvariant();
        }

        /// <summary>
        /// </summary>
        public static double MemoryUsage()
        {
            return Process.GetCurrentProcess().WorkingSet64 / 1024.0 / 1024.0;
        }
        public static double MemoryAvailable()
        {
            try
            {
                var d = DateTime.Now;
                var info = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / 1024.0 / 1024.0 / 3 * 2;
                return info;

                //using (PerformanceCounter pc = new PerformanceCounter("Memory", "Available Bytes"))
                //{
                //    var v = pc.NextValue() / 1024.0 / 1024.0;
                //    //MessageBox.Show((DateTime.Now - d).TotalSeconds.ToString());
                //    return v;
                //}
            }
            catch
            {
                return 4000;
            }
        }
        /// <summary>
        /// </summary>
        public static List<char> com { get; } = new List<char> { '，', '。', '！', '？', '；', '：', '\n', '.', ',', '!', '?', ';', ':' };
        /// <summary>
        /// </summary>
        public static int ComCheck(string text)
        {
            return text.Replace("\r","").Replace("\n\n", "\n").Count(com.Contains);
        }
    }
}
