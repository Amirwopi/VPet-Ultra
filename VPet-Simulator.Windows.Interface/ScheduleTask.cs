using LinePutScript;
using LinePutScript.Converter;
using LinePutScript.Localization.WPF;
using Panuon.WPF;
using Panuon.WPF.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Media;
using static VPet_Simulator.Core.GraphHelper;
using static VPet_Simulator.Core.GraphHelper.Work;

namespace VPet_Simulator.Windows.Interface;

/// <summary>
/// </summary>
public class ScheduleTask
{
    public ObservableCollection<ScheduleItemBase> ScheduleItems { get; set; } = [];
    private IMainWindow mw;
    public int NowIndex { get; set; } = 0;
    public bool IsOn { get; set; } = false;
    /// <summary>
    /// </summary>
    public ScheduleTask(IMainWindow imw)
    {
        this.mw = imw;
        if (mw.GameSavesData.Data.ContainsLine("schedule"))
        {
            int i = 0;
            var schedule = mw.GameSavesData.Data["schedule"];
            while (schedule.Contains(i.ToString()))
            {
                var sub = schedule[(gstr)i.ToString()].Split(',');
                if (sub[0] == "rest")
                {
                    ScheduleItems.Add(new RestScheduleItem(this, int.Parse(sub[1])));
                }
                else
                {
                    Work work = mw.Core.Graph.GraphConfig.Works.Find(w => w.Name == sub[0]);
                    if (work != null)
                    {
                        int dbl = int.Parse(sub[1]);
                        switch (work.Type)
                        {
                            case WorkType.Work:
                                ScheduleItems.Add(new WorkScheduleItem(this, work, dbl));
                                break;
                            case WorkType.Study:
                                ScheduleItems.Add(new StudyScheduleItem(this, work, dbl));
                                break;
                            case WorkType.Play:
                                ScheduleItems.Add(new PlayScheduleItem(this, work, dbl));
                                break;
                        }
                    }
                }
                i++;
            }
            if (mw.GameSavesData.Data.ContainsLine("schedule_work"))
            {
                PackageWork = LPSConvert.DeserializeObject<Package>(mw.GameSavesData.Data["schedule_work"]);
            }
            if (mw.GameSavesData.Data.ContainsLine("schedule_study"))
            {
                PackageStudy = LPSConvert.DeserializeObject<Package>(mw.GameSavesData.Data["schedule_study"]);
            }
            NowIndex = schedule[(gint)"now"];
            IsOn = schedule[(gbol)"ison"];
        }

        imw.Main.WorkTimer.E_FinishWork += WorkTimer_E_FinishWork;
        RestTimer.Elapsed += RestTimer_Elapsed;
        if (IsOn)
            RestTimer.Start();
    }
    public void Save()
    {
        mw.GameSavesData.Data["schedule"].Clear();
        mw.GameSavesData.Data["schedule"][(gint)"now"] = NowIndex;
        mw.GameSavesData.Data["schedule"][(gbol)"ison"] = IsOn;
        for (int i = 0; i < ScheduleItems.Count; i++)
        {
            if (ScheduleItems[i] is RestScheduleItem rsi)
            {
                mw.GameSavesData.Data["schedule"][(gstr)i.ToString()] = $"rest,{rsi.RestTime}";
            }
            else if (ScheduleItems[i] is WorkScheduleItem wsi)
            {
                mw.GameSavesData.Data["schedule"][(gstr)i.ToString()] = $"{wsi.Work.Name},{wsi.DBL}";
            }
        }
        if (PackageWork == null)
        {
            mw.GameSavesData.Data.Remove("schedule_work");
        }
        else
        {
            mw.GameSavesData.Data["schedule_work"] = LPSConvert.SerializeObject(PackageWork, "schedule_work");
        }
        if (PackageStudy == null)
        {
            mw.GameSavesData.Data.Remove("schedule_study");
        }
        else
        {
            mw.GameSavesData.Data["schedule_study"] = LPSConvert.SerializeObject(PackageStudy, "schedule_study");
        }
    }

    public bool AutoRenew()
    {
        bool isrenew = false;
        if (PackageWork != null && PackageWork.AutoRenew && !PackageWork.IsActive())
        {
            var pw = mw.SchedulePackage.Find(x => x.WorkType == Work.WorkType.Work && x.Name == PackageWork.Name);
            if (pw != null)
            {
                var np = new Package(pw, PackageWork.Level);
                if (np.Price < mw.GameSavesData.GameSave.Money)
                {
                    PackageWork = np;
                    mw.GameSavesData.GameSave.Money -= np.Price;
                    isrenew = true;
                }
            }
        }
        if (PackageStudy != null && PackageStudy.AutoRenew && !PackageStudy.IsActive())
        {
            var ps = mw.SchedulePackage.Find(x => x.WorkType == Work.WorkType.Study && x.Name == PackageStudy.Name);
            if (ps != null)
            {
                var np = new Package(ps, PackageStudy.Level);
                if (np.Price < mw.GameSavesData.GameSave.Money)
                {
                    PackageStudy = np;
                    mw.GameSavesData.GameSave.Money -= np.Price;
                    isrenew = true;
                }
            }
        }
        return isrenew;
    }

    private void RestTimer_Elapsed(object sender, ElapsedEventArgs e)
    {
        if (RestTime-- < 0)
            return;
        if (RestTime == 0)
        {
            StartWork();
        }
        else
        {
            RestTimer.Start();
        }
    }
    /// <summary>
    /// Start Working
    /// </summary>
    public void StartWork()
    {
        RestTime = -100;
        if (!IsOn)
            return;
        if (ScheduleItems.Count > 0)
        {
            if (NowIndex >= ScheduleItems.Count)
            {
                NowIndex = 0;
            }

            // Auto-RenewalWork
            AutoRenew();

            if (ScheduleItems[NowIndex] is WorkScheduleItem wsi)
            {
                if (wsi.Work.Type == WorkType.Work)
                {
                    if (PackageWork?.IsActive() != true)
                    {
                        mw.Dispatcher.Invoke(() => MessageBoxX.Show("The work package is not activated. Please go to the Schedule to sign up for the work intermediary package".Translate(), "The package is not activated".Translate()));
                        IsOn = false;
                        return;
                    }
                    else if (PackageWork.Level < wsi.Work.LevelLimit)
                    {
                        mw.Dispatcher.Invoke(() => MessageBoxX.Show("The work package level is insufficient ({0}/{1}),\\nPlease select a lower level requirement/ratio work or go to the Schedule to sign a new work intermediary package".Translate(PackageWork.Level,
                        wsi.Work.LevelLimit), "Insufficient package level".Translate()));
                        IsOn = false;
                        return;
                    }
                }
                else if (wsi.Work.Type == WorkType.Study)
                {
                    if (PackageStudy?.IsActive() != true)
                    {
                        mw.Dispatcher.Invoke(() => MessageBoxX.Show("The learning package has not been activated. Please go to the Schedule to sign up for the training institution package".Translate(), "The package is not activated".Translate()));
                        IsOn = false;
                        return;
                    }
                    else if (PackageStudy.Level < wsi.Work.LevelLimit)
                    {
                        mw.Dispatcher.Invoke(() => MessageBoxX.Show("The learning package level is insufficient ({0}/{1}),\\nPlease choose a lower level requirement/rate of learning or go to the Schedule to sign up for a new training institution package".Translate(PackageStudy.Level,
                        wsi.Work.LevelLimit), "Insufficient package level".Translate()));
                        IsOn = false;
                        return;
                    }
                }
                var newwork = wsi.Work.Double(wsi.DBL);
                while (mw.GameSavesData.GameSave.LevelMax < newwork.LevelLimit && wsi.DBL > 1)
                {
                    wsi.DBL = wsi.DBL - 1;
                    newwork = wsi.Work.Double(wsi.DBL);
                }
                mw.Dispatcher.Invoke(() => mw.Main.StartWork(wsi.Work.Double(wsi.DBL)));
                NowIndex++;
            }
            else if (ScheduleItems[NowIndex] is RestScheduleItem rsi)
            {
                NowIndex++;
                RestTime = rsi.RestTime * 2;
                RestTimer.Start();
            }
        }
    }
    /// <summary>
    /// </summary>
    public void Start()
    {
        IsOn = true;
        NowIndex = 0;
        StartWork();
    }
    /// <summary>
    /// </summary>
    public void Stop()
    {
        IsOn = false;
        RestTime = -100;
    }
    private int RestTime = 2;
    private Timer RestTimer = new Timer()
    {
        Interval = 30000,
        AutoReset = false
    };

    private void WorkTimer_E_FinishWork(Core.WorkTimer.FinishWorkInfo obj)
    {
        if (obj.spendtime < obj.work.Time / 2)
            Stop();
        else
        {
            RestTime = 1;
            RestTimer.Start();
        }
    }

    public Package PackageWork { get; set; }
    public Package PackageStudy { get; set; }
    /// <summary>
    /// </summary>
    /// <param name="Work">Work</param>
    public void AddWork(Work work, int dbl)
    {
        ScheduleItems.Add(new WorkScheduleItem(this, work, dbl));
    }
    /// <summary>
    /// </summary>
    /// <param name="Work">Work</param>
    public void AddStudy(Work work, int dbl)
    {
        ScheduleItems.Add(new StudyScheduleItem(this, work, dbl));
    }
    /// <summary>
    /// </summary>
    /// <param name="Work">Work</param>
    public void AddPlay(Work work, int dbl)
    {
        ScheduleItems.Add(new PlayScheduleItem(this, work, dbl));
    }
    /// <summary>
    /// </summary>
    /// <param name="restTime">Rest time</param>
    public void AddRest(int restTime)
    {
        ScheduleItems.Add(new RestScheduleItem(this, restTime));
    }
    /// <summary>
    /// </summary>
    public class ScheduleItemBase : NotifyPropertyChangedBase
    {
        public ScheduleItemBase(ScheduleTask task)
        {
            Task = task;
        }
        public ScheduleTask Task;
        /// <summary>
        /// Rest time
        /// </summary>
        public virtual int RestTime { get; set; } = 0;
        /// <summary>
        /// Working time
        /// </summary>
        public virtual int WorkTime { get; set; } = 0;
        /// <summary>
        /// </summary>
        public bool IsNow
        {
            get
            {
                if (Task.ScheduleItems.Count < Task.NowIndex)
                {
                    return false;
                }
                return Task.ScheduleItems[Task.NowIndex] == this;
            }
        }
        public Visibility IsNowVisibility
        {
            get => IsNow ? Visibility.Visible : Visibility.Collapsed;
            set { }
        }
    }
    /// <summary>
    /// </summary>
    public class WorkScheduleItem
        : ScheduleItemBase
    {
        /// <summary>
        /// </summary>
        public int DBL { get; set; }
        /// <summary>
        /// </summary>
        public Work Work { get; set; }
        public WorkScheduleItem(ScheduleTask task, Work work, int dbl) : base(task)
        {
            this.DBL = dbl;
            this.Work = work;
            string source = task.mw.ImageSources.FindSource("work_" + task.mw.Set.PetGraph + "_" + work.Graph) ?? task.mw.ImageSources.FindSource("work_" + task.mw.Set.PetGraph + "_" + work.Name);

            if (source == null)
            {
                Image = task.mw.ImageSources.FindImage("work_" + task.mw.Set.PetGraph + "_t_" + work.Type.ToString(), "work_" + work.Type.ToString());
            }
            else
            {
                Image = ImageResources.NewSafeBitmapImage(source);
            }

        }

        public ImageSource Image { get; set; }

        public string WorkName
        {
            get => Work.NameTrans;
            set { }
        }

        public string WorkLevel
        {
            get => $"Lv {(DBL == 0 ? Work.LevelLimit : (Work.LevelLimit + 10) * DBL)}";
            set { }
        }

        public override int WorkTime
        {
            get => Work.Time;
            set { }
        }

        public Visibility IsOKVisibility
        {
            get => (Task.PackageWork?.IsActive() != true || Task.PackageWork.Level < Work.LevelLimit) ? Visibility.Visible : Visibility.Collapsed;
            set { }
        }

        public bool IsPreviousIsRest { get => _isPreviousIsRest; set => Set(ref _isPreviousIsRest, value); }
        private bool _isPreviousIsRest;
    }
    /// <summary>
    /// </summary>
    public class StudyScheduleItem : WorkScheduleItem
    {
        public StudyScheduleItem(ScheduleTask task, Work work, int dbl) : base(task, work, dbl)
        {
        }
        public new Visibility IsOKVisibility => (Task.PackageStudy?.IsActive() != true || Task.PackageStudy.Level < Work.LevelLimit) ? Visibility.Visible : Visibility.Collapsed;

    }
    /// <summary>
    /// </summary>
    public class PlayScheduleItem : WorkScheduleItem
    {
        public PlayScheduleItem(ScheduleTask task, Work work, int dbl) : base(task, work, dbl)
        {
        }
        public override int WorkTime
        {
            get => Work.Time / 2;
            set { }
        }
        public override int RestTime
        {
            get => Work.Time / 2;
            set { }
        }

        public new Visibility IsOKVisibility => Visibility.Collapsed;
    }
    /// <summary>
    /// </summary>
    public class RestScheduleItem
        : ScheduleItemBase
    {
        public RestScheduleItem(ScheduleTask task, int restTime) : base(task)
        {
            RestTime = restTime;
        }
        /// <summary>
        /// Rest time
        /// </summary>
        public override int RestTime { get => _restTime; set => Set(ref _restTime, value); }
        private int _restTime;
    }
    /// <summary>
    /// </summary>
    public class Package
    {
        public Package()
        {
        }
        /// <summary>
        /// PackageName
        /// </summary>
        [Line] public string Name { get; set; }
        /// <summary>
        /// </summary>
        public string NameTrans
        {
            get
            {
                if (string.IsNullOrEmpty(nametrans))
                {
                    nametrans = string.IsNullOrEmpty(Name) ? "" : Name.Translate();
                }
                return nametrans;
            }
            set => nametrans = value;
        }
        private string nametrans;
        /// <summary>
        /// descriptions
        /// </summary>
        [Line] public string Describe { get; set; }
        /// <summary>
        /// </summary>
        public string DescribeTrans
        {
            get
            {
                if (string.IsNullOrEmpty(describetrans))
                {
                    describetrans = string.IsNullOrEmpty(Describe) ? "" : Describe.Translate();
                }
                return describetrans;
            }
            set => describetrans = value;
        }
        private string describetrans;
        /// <summary>
        /// Percentage
        /// </summary>
        [Line] public double Commissions { get; set; }
        /// <summary>
        /// Handling fees
        /// </summary>
        [Line] public double Price { get; set; }
        /// <summary>
        /// </summary>
        [Line] public DateTime EndTime { get; set; } = DateTime.MinValue;
        /// <summary>
        /// YesNoAuto-Renewal
        /// </summary>
        [Line] public bool AutoRenew { get; set; } = false;
        /// <summary>
        /// Available levels
        /// </summary>
        [Line] public int Level { get; set; }
        /// <summary>
        /// </summary>
        public bool IsActive() => DateTime.Now < EndTime;

        public Package(PackageFull packageFull, int level)
        {
            Name = packageFull.Name;
            Describe = packageFull.Describe;
            Commissions = packageFull.Commissions;
            Price = packageFull.Price * (200 * level - 100);
            EndTime = DateTime.Now.AddDays(packageFull.Duration);
            Level = (int)(level / packageFull.LevelInNeed);
        }
    }
    /// <summary>
    /// PackageDetailed
    /// </summary>
    public class PackageFull : Package
    {
        public PackageFull()
        {
        }
        /// <summary>
        /// Duration (day)
        /// </summary>
        [Line] public int Duration { get; set; }
        /// <summary>
        /// </summary>
        [Line] public double LevelInNeed { get; set; }

        /// <summary>
        /// WorkType
        /// </summary>
        [Line] public WorkType WorkType { get; set; }

        public void FixOverLoad()
        {
            if (Duration < 1)
            {
                Duration = 1;
            }
            if (Price < 0)
            {
                Price = 1;
            }
            if (LevelInNeed < 1)
            {
                LevelInNeed = 1.25;
            }
            if (Commissions < 0)
            {
                Commissions = 0.2;
            }
            var use = Math.Sign(Commissions - 0.15) * Math.Pow(Math.Abs(Commissions * 100 - 15), 1.5) +
                Math.Sign(LevelInNeed - 1.2) * Math.Pow(Math.Abs(LevelInNeed * 100 - 120), 1.5) +
                (Price * LevelInNeed * 100 - 100) / 4;
            var get = Math.Sqrt(Duration);
            var realvalue = use / get;
            if (realvalue < 10)
            {
                Commissions = 0.2;
                LevelInNeed = 1.25;
                Price = 1;
                Duration = 7;
            }
        }

        public override string ToString() => NameTrans;
    }
}

