using LinePutScript;
using LinePutScript.Localization.WPF;
using Panuon.WPF.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using VPet_Simulator.Core;
using VPet_Simulator.Windows.Interface;

namespace VPet_Simulator.Windows
{
    /// <summary>
    /// </summary>
    public partial class winSaveManager : WindowX
    {
        private readonly MainWindow mw;
        private List<SaveEntry> allEntries = new List<SaveEntry>();

        public winSaveManager(MainWindow mw)
        {
            InitializeComponent();
            this.mw = mw;
            Owner = mw;
            Title = "Saves Management".Translate() + ' ' + mw.PrefixSave;
            DPStart.SelectedDateTime = DateTime.Now.AddYears(-1);
            RefreshSaveList();

        }

        private sealed class SaveEntry
        {
            public enum SourceType
            {
                Local,
                Backup,
                Steam
            }
            public SourceType Source { get; init; }
            public string SourceText => Source switch
            {
                SourceType.Local => "Local".Translate(),
                SourceType.Backup => "".Translate(),
                SourceType.Steam => "Steam".Translate(),
                _ => string.Empty
            };
            public string SourceToolTip => Source switch
            {
                SourceType.Local => "Local saves, will be automatically synced by Steam".Translate(),
                SourceType.Backup => "Local backup files, will not be synced by Steam Cloud".Translate(),
                SourceType.Steam => "Steam cloud backup saves, saved with Steam account".Translate(),
                _ => string.Empty
            };
            public string SaveId { get; init; }
            public string PetName { get; init; }
            public DateTime SaveTime { get; init; }
            public string SaveTimeText { get; init; }
            public string LevelText { get; init; }
            public string MoneyText { get; init; }
            public string FullPath { get; init; }
            public string SteamPath { get; init; }
            public bool HashCheck { get; init; }
            public string HashCheckText => HashCheck ? "Pass" : "Fail";
        }

        private void RefreshSaveList()
        {
            var list = new List<SaveEntry>();
            foreach (var item in GetLocalSaveEntries())
            {
                list.Add(item);
            }

            allEntries = list.OrderByDescending(x => x.SaveTime).ToList();
            ApplyFilters();
        }

        private void ApplyFilters()
        {            
            IEnumerable<SaveEntry> filtered = allEntries;

            if (CBTypeFilter.SelectedItem is ComboBoxItem cbi)
            {
                var tag = cbi.Tag?.ToString();
                switch (tag)
                {
                    case "local":
                        filtered = filtered.Where(x => x.Source == SaveEntry.SourceType.Local);
                        break;
                    case "localbackup":
                        filtered = filtered.Where(x => x.Source == SaveEntry.SourceType.Backup);
                        break;
                    case "steam":
                        filtered = filtered.Where(x => x.Source == SaveEntry.SourceType.Steam);
                        break;
                }
            }

            if (DPStart.SelectedDateTime.HasValue)
            {
                var start = DPStart.SelectedDateTime.Value.Date;
                filtered = filtered.Where(x => x.SaveTime >= start);
            }

            if (DPEnd.SelectedDateTime.HasValue)
            {
                var endExclusive = DPEnd.SelectedDateTime.Value.Date.AddDays(1);
                filtered = filtered.Where(x => x.SaveTime < endExclusive);
            }

            var result = filtered.OrderByDescending(x => x.SaveTime).ToList();
            DataGridSaves.ItemsSource = result;

            var view = CollectionViewSource.GetDefaultView(DataGridSaves.ItemsSource);
            if (view != null)
            {
                view.SortDescriptions.Clear();
                view.SortDescriptions.Add(new SortDescription(nameof(SaveEntry.SaveTime), ListSortDirection.Descending));
                view.Refresh();
            }

            foreach (var c in DataGridSaves.Columns)
                c.SortDirection = null;
            var saveTimeColumn = DataGridSaves.Columns.FirstOrDefault(c => c.SortMemberPath == nameof(SaveEntry.SaveTime));
            if (saveTimeColumn != null)
                saveTimeColumn.SortDirection = ListSortDirection.Descending;

            if (DataGridSaves.Items.Count > 0)
                DataGridSaves.SelectedIndex = 0;
        }

        private IEnumerable<SaveEntry> GetLocalSaveEntries()
        {
            var entries = new List<SaveEntry>();
            var saves = new List<FileInfo>();
            var pattern = $"Save{mw.PrefixSave}_*.lps";
            var saveDir = Path.Combine(ExtensionValue.BaseDirectory, "Saves");
            var backupDir = Path.Combine(ExtensionValue.BaseDirectory, "Saves_BKP");

            if (Directory.Exists(saveDir))
                saves.AddRange(new DirectoryInfo(saveDir).GetFiles(pattern));
            if (Directory.Exists(backupDir))
                saves.AddRange(new DirectoryInfo(backupDir).GetFiles(pattern));

            foreach (var file in saves.OrderByDescending(x => x.LastWriteTime))
            {
                try
                {
                    var lpsText = File.ReadAllText(file.FullName);
                    var gs = new GameSave_v2(new LPS(lpsText));
                    entries.Add(new SaveEntry()
                    {
                        Source = file.Directory?.Name == "Saves_BKP" ? SaveEntry.SourceType.Backup : SaveEntry.SourceType.Local,
                        SaveId = Path.GetFileNameWithoutExtension(file.Name),
                        PetName = gs.GameSave.Name,
                        SaveTime = file.LastWriteTime,
                        SaveTimeText = file.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss"),
                        LevelText = $"{gs.GameSave.Level} (x{gs.GameSave.LevelMax})",
                        MoneyText = gs.GameSave.Money.ToString("f2"),
                        FullPath = file.FullName,
                        HashCheck = gs.HashCheck,
                    });
                }
                catch
                {
                }
            }
            return entries;
        }

        private void LoadSelectedSave()
        {
            if (DataGridSaves.SelectedItem is not SaveEntry selected)
                return;

            string lpsText;
            if (!File.Exists(selected.FullPath))
            {
                MessageBoxX.Show("Save file does not exist, please refresh and retry".Translate(), "LoadFail".Translate(), MessageBoxIcon.Warning);
                return;
            }
            lpsText = File.ReadAllText(selected.FullPath);

            var message = "Save name: {0}\nSave time: {1}\nSave level: {2}\nSave money: {3}\nHashCheck: {4}\nLoad the backup save? Current game data will be lost"
                .Translate(selected.PetName, selected.SaveTimeText, selected.LevelText, selected.MoneyText, selected.HashCheck);
            if (MessageBoxX.Show(message, "Load the backup save? Current game data will be lost".Translate(), MessageBoxButton.YesNo, MessageBoxIcon.Info) != MessageBoxResult.Yes)
                return;

            try
            {
                if (mw.Main.State != Main.WorkingState.Nomal)
                {
                    mw.Main.WorkTimer.Visibility = Visibility.Collapsed;
                    mw.Main.State = Main.WorkingState.Nomal;
                }

                if (!mw.SavesLoad(new LPS(lpsText)))
                    MessageBoxX.Show("Archive damage, unable to load the archive\\nIt may be caused by an error in the last storage or Steam cloud synchronization\\nPlease load the backup in the settings to restore the archive", "Archive damage".Translate());
                else
                    MessageBoxX.Show("Load successful".Translate());
            }
            catch (Exception ex)
            {
                MessageBoxX.Show("Archive damage, unable to load the archive\\nIt may be caused by data overflow/overmodel" + '\n' + ex.Message, "Archive damage".Translate());
            }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            RefreshSaveList();
        }

        private void BtnLoad_Click(object sender, RoutedEventArgs e)
        {
            LoadSelectedSave();
        }

        private void Filter_Changed(object sender, Panuon.WPF.SelectedValueChangedRoutedEventArgs<DateTime?> e)
        {
            if (!IsLoaded) return; ApplyFilters();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Filter_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return; ApplyFilters();
        }
    }
}
