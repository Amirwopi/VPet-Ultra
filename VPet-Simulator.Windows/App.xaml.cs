using LinePutScript.Localization.WPF;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Interop;
using VPet_Simulator.Windows.Interface;

namespace VPet_Simulator.Windows
{
    /// <summary>
    /// </summary>
    public partial class App : Application
    {
        public App() : base()
        {
            Environment.CurrentDirectory =
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
#if !DEBUG
            base.DispatcherUnhandledException += (s, e) => { e.Handled = true; UnhandledException(e.Exception, false); };
            AppDomain.CurrentDomain.UnhandledException += (s, e) => { UnhandledException((e.ExceptionObject as Exception), true); };
#endif
            //AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }
        public static string[] Args { get; set; }
        /// <summary>
        /// </summary>
        public static List<string> MutiSaves { get; set; } = new List<string>();

        public static List<MainWindow> MainWindows { get; set; } = new List<MainWindow>();

        public static HashSet<string> MODType { get; set; } = new HashSet<string>();

        protected override void OnStartup(StartupEventArgs e)
        {
            Args = e.Args;

            if (File.Exists(ExtensionValue.BaseDirectory + @"\Setting-.lps"))
                File.Delete(ExtensionValue.BaseDirectory + @"\Setting-.lps");

            foreach (var mss in new DirectoryInfo(ExtensionValue.BaseDirectory).GetFiles("Setting*.lps"))
            {
                var n = mss.Name.Substring(7).Trim('-');
                MutiSaves.Add(n.Substring(0, n.Length - 4));
            }

            if (MutiSaves.Count == 0)
            {
                MutiSaves.Add("");
            }
            if (!Args.Any(x => x.Contains("prefix")))
            {
                var file = new DirectoryInfo(ExtensionValue.BaseDirectory).GetFiles("startup_*").FirstOrDefault();
                if (file != null)
                {
                    var su = file.Name.Substring(8);
                    var al = Args.ToList();
                    al.Add($"prefix#{su}:|");
                    Args = al.ToArray();
                }
            }

        }
        HashSet<string> ErrorReport = new HashSet<string>();
        private void UnhandledException(Exception e, bool isFatality)
        {
            var expt = e.ToString();
            if (ErrorReport.Contains(expt))
                return;
            ErrorReport.Add(expt);
            if (expt.Contains("MainWindow.Close") || expt.Contains("System.Windows.Window.DragMove") ||
                expt.Contains("winConsole"))
                return;
            else if ((!isFatality && MainWindow != null && ((MainWindow)MainWindow).GameSavesData?.GameSave != null &&
                (((MainWindow)MainWindow).GameSavesData.GameSave.Money > int.MaxValue || ((MainWindow)MainWindow).GameSavesData.GameSave.Exp > int.MaxValue)
                ) && ((expt.ToLowerInvariant().Contains("value") && expt.ToLowerInvariant().Contains("nan")) ||
                expt.Contains("System.OverflowException") || expt.Contains("System.DivideByZeroException")))
            {
                MessageBox.Show("Due to the modification of the game data caused by data overflow, the archive may be wrong\\nThe developer reminds you not to use too much supermodel MOD.".Translate());
                return;
            }
            else if (expt.Contains("System.IO.FileNotFoundException") && expt.Contains("cache"))
            {
                MessageBox.Show("Cache is deleted by other software, the game can not continue to run\\nPlease restart the game to regenerate the cache.".Translate());
                return;
            }
            else if (expt.Contains("0x80070008"))
            {
                MessageBox.Show("Game memory is insufficient, please modify the rendering resolution in the settings to reduce memory usage.".Translate());
                return;
            }
            else if (expt.Contains("UnauthorizedAccessException"))
            {
                MessageBox.Show("Insufficient game permissions, unable to write game archives and settings, please check if the settings file is occupied by other software".Translate());
                return;
            }
            else if (expt.Contains("VPet.Plugin"))
            {
                var exptin = expt.Split('\n').First(x => x.Contains("VPet.Plugin"));
                exptin = exptin.Substring(exptin.IndexOf("VPet.Plugin") + 12).Split('.')[0];
                MessageBox.Show("An error occurred in the game, probably".Translate() + $"MOD({exptin.Translate()})" +
                    "Caused. \\nIf possible, please send the error message screenshot and the operation before the error to the corresponding MOD author\\nThank you for your support for MOD development\\n".Translate()
                     + expt, "An error occurred in the game, probably".Translate() + exptin);
                return;
            }

            foreach (var modname in MODType)
            {
                if (expt.Contains(modname))
                {
                    var exptin = modname.Split('.').Last();
                    MessageBox.Show("An error occurred in the game, probably".Translate() + $"MOD({modname})" +
                        "Caused. \\nIf possible, please send the error message screenshot and the operation before the error to the corresponding MOD author\\nThank you for your support for MOD development\\n".Translate()
                         + expt, "An error occurred in the game, probably".Translate() + exptin);
                    return;
                }
            }


            string errstr = "An error occurred in the game, probably".Translate() + (string.IsNullOrWhiteSpace(CoreMOD.NowLoading) ?
                "Game or Mod".Translate() : $"MOD({CoreMOD.NowLoading})") +
                "If possible, Please send a screenshot of the error message and the action you took before triggering the error to developer: service@exlb.net\\nThanks for your support of the game development\\n".Translate()
                + expt;
            if (isFatality || MainWindow == null)
            {
                MessageBox.Show(errstr, "Game Fatal Error".Translate());
                return;
            }
            else
            {
                new winReport(((MainWindow)MainWindow), errstr).Show();
                return;
            }
        }
    }
}
