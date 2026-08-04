using LinePutScript;
using LinePutScript.Converter;
using LinePutScript.Localization.WPF;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Compression;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.Drawing;
using VPet_Simulator.Core;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;


namespace VPet_Simulator.Windows.Interface;
public class Photo
{
    public Photo() { }

    public Photo(Line line)
    {
        Zip = line[(gstr)"zip"];
        Path = line[(gstr)"path"];
        if (Enum.TryParse<PhotoType>(line[(gstr)"type"], true, out var tp))
            Type = tp;
        Name = line[(gstr)"name"];
        Description = line[(gstr)"desc"];
        var tags = line.Find("tags");
        if (tags != null)
            Tags = tags.GetInfos().ToList();

        UnlockAble = new UnlockCondition(line);
    }
    /// <summary>
    /// </summary>
    public string Zip { get; set; }
    /// <summary>
    /// </summary>
    public string Path { get; set; }

    /// <summary>
    /// </summary>
    public enum PhotoType
    {
        /// <summary>
        /// DefaultType
        /// </summary>
        ALL,
        /// <summary>
        /// Illustration
        /// </summary>
        Illustration,
        /// <summary>
        /// </summary>
        Thumbnail
    }
    /// <summary>
    /// </summary>
    public PhotoType Type { get; set; } = PhotoType.ALL;
    /// <summary>
    /// </summary>
    public string Name { get; set; }
    private string transname = null;
    /// <summary>
    /// </summary>
    public string TranslateName
    {
        get
        {
            if (transname == null)
            {
                transname = LocalizeCore.Translate(Name);
            }
            return transname;
        }
    }
    /// <summary>
    /// Tags
    /// </summary>
    public List<string> Tags { get; set; } = new List<string>();
    private List<string> tagstrans = null;
    /// <summary>
    /// </summary>
    public List<string> TagsTrans
    {
        get
        {
            if (tagstrans == null)
            {
                tagstrans = Tags.Select(x => LocalizeCore.Translate(x)).ToList();
            }
            return tagstrans;
        }
    }
    /// <summary>
    /// descriptions
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Unlock conditions
    /// </summary>
    public class UnlockCondition
    {
        public UnlockCondition() { }

        public UnlockCondition(Line line)
        {
            var sub = line.Find("llockstring");
            if (sub != null)
                LockString = sub.Info;
            sub = line.Find("llock");
            if (sub != null)
                Lock = sub.GetBoolean();
            sub = line.Find("lnone");
            if (sub != null)
                None = sub.GetBoolean();
            sub = line.Find("lsellprice");
            if (sub != null)
                SellPrice = sub.GetInteger();
            sub = line.Find("lsellboth");
            if (sub != null)
                SellBoth = sub.GetBoolean();
            sub = line.Find("llevel");
            if (sub != null)
                Level = sub.GetInteger();
            sub = line.Find("llevelmax");
            if (sub != null)
                LevelMax = sub.GetInteger();
            sub = line.Find("lmoney");
            if (sub != null)
                Money = sub.GetInteger();
            sub = line.Find("llikability");
            if (sub != null)
                Likability = sub.GetInteger();
            sub = line.Find("lfeeling");
            if (sub != null)
                Feeling = sub.GetInteger();
            sub = line.Find("ldate");
            if (sub != null)
            {
                var dt = sub.Info.Split('/', '-');
                try
                {
                    Date = new DateOnly(DateTime.Now.Year, int.Parse(dt[0]), int.Parse(dt[1]));
                }
                catch
                {
                    Date = new DateOnly(DateTime.Now.Year, 1, 1);
                }
            }

            sub = line.Find("ltime");
            if (sub != null)
                Time = TimeOnly.Parse(sub.Info);
            sub = line.Find("ldateoffset");
            if (sub != null)
                DateOffset = sub.GetInteger();
            sub = line.Find("ltimeoffset");
            if (sub != null)
                TimeOffset = sub.GetInteger();
            sub = line.Find("lholiday");
            if (sub != null)
                Holiday = Enum.Parse<HolidayType>(sub.Info);

            foreach (var sub2 in line.Subs.FindAll(x => x.Name.StartsWith("ls_")))
            {
                StatCheck.Add((sub2.Name.Substring(3), sub2.GetInteger()));
            }
        }


        /// <summary>
        /// </summary>
        public string LockString { get; set; } = "Locked by program";
        /// <summary>
        /// </summary>
        public bool Lock { get; set; } = false;
        /// <summary>
        /// </summary>
        public bool None { get; set; } = false;
        /// <summary>
        /// </summary>
        public int SellPrice { get; set; } = -1;
        /// <summary>
        /// </summary>
        public bool SellBoth { get; set; } = false;
        /// <summary>
        /// </summary>
        public List<(string, int)> StatCheck { get; set; } = new List<(string, int)>();
        /// <summary>
        /// </summary>
        public bool Check(GameSave_v2 save)
        {
            if (None)
            {
                return true;
            }
            if (Lock)
            {
                return false;
            }

            if (LevelMax > save.GameSave.LevelMax)
                return false;
            if (save.GameSave.LevelMax != 0 && Level > save.GameSave.Level)
                return false;
            if (Money > save.GameSave.Money)
                return false;
            if (Likability > save.GameSave.Likability)
                return false;
            if (Feeling > save.GameSave.Feeling)
                return false;
            DateTime now = DateTime.Now;
            if (Date != null)
            {
                var date = new DateTime(now.Year, Date.Value.Month, Date.Value.Day);
                if (!CheckDate(date)) return false;
            }
            if (Time != null)
            {
                var time = new DateTime(now.Year, now.Month, now.Day, Time.Value.Hour, Time.Value.Minute, Time.Value.Second);
                if (time > now || time.AddMinutes(TimeOffset) < now)
                {
                    return false;
                }
            }
            if (Holiday != HolidayType.None)
            {
                switch (Holiday)
                {
                    case HolidayType.Mid_Autumn_Festival:
                        if (!CheckDate(GetLunarDate(8, 15)))
                            return false;
                        break;
                    case HolidayType.Dragon_Boat_Festival:
                        if (!CheckDate(GetLunarDate(5, 5)))
                            return false;
                        break;
                    case HolidayType.New_Years_Day:
                        if (!CheckDate(new DateTime(now.Year, 1, 1)))
                            return false;
                        break;
                    case HolidayType.Spring_Festival:
                        if (!CheckDate(GetLunarDate(1, 1)))
                            return false;
                        break;
                    case HolidayType.Christmas:
                        if (!CheckDate(new DateTime(now.Year, 12, 25)))
                            return false;
                        break;
                        //    if (now.Month != save.GameSave.Birthday.Month || now.Day != save.GameSave.Birthday.Day)
                        //        return false;
                        //    break;
                }
            }
            foreach (var (stat, value) in StatCheck)
            {
                var statvalue = save.Statistics.GetInt(stat, -1);
                if (statvalue < value)
                    return false;
            }
            return true;
        }
        public string CheckReason(GameSave_v2 gamesave)
        {
            if (None) return string.Empty;
            if (Lock)
            {
                return LocalizeCore.Translate(LockString);
            }
            StringBuilder sb = new StringBuilder();

            //if (SellPrice > 0)
            //    if (SellBoth)
            // sb.AppendLine("Spend ${0} and meet the following conditions".Translate(SellPrice));
            //    else
            //        sb.AppendLine("Spend ${0} or meet the following conditions".Translate(SellPrice));


            if (LevelMax > 0)
                if (gamesave.GameSave.LevelMax == 0)
                    sb.AppendLine("Level: {0}".Translate(1000 + (LevelMax - 1) * 100));
                else
                {
                    sb.AppendLine("Level Breakthrough: {0}".Translate(LevelMax));
                    if (Level > 0)
                        sb.AppendLine("Level: {0}".Translate(Level));
                }

            else if (Level > 0)
                sb.AppendLine("Level: {0}".Translate(Level));
            if (Money > 0)
                sb.AppendLine("Monetary: ${0}".Translate(Money));
            if (Likability > 0)
                sb.AppendLine("Favorability: {0}".Translate(Likability));
            if (Feeling > 0)
                sb.AppendLine("Mood: {0}".Translate(Feeling));
            if (Date != null)
                sb.AppendLine("date: {0}".Translate(Date.Value.ToString("MM-dd")));
            if (Time != null)
                sb.AppendLine("time: {0}".Translate(Time.Value.ToString("HH:mm")));
            if (Holiday != HolidayType.None)
                sb.AppendLine("festival: {0}".Translate(Holiday.ToString().Translate()));

            // StatisticsData
            foreach (var (stat, value) in StatCheck)
            {
                sb.AppendLine("{0}: {1}".Translate(
                   (stat.StartsWith("stat_") ? stat.Substring(5) : stat)
                    .Translate(), value));
            }

            return sb.ToString().Trim('\n', '\r');
        }
        /// <summary>
        /// </summary>
        public int Level { get; set; } = 0;
        /// <summary>
        /// </summary>
        public int LevelMax { get; set; } = 0;
        /// <summary>
        /// </summary>
        public int Money { get; set; } = int.MinValue;
        /// <summary>
        /// </summary>
        public int Likability { get; set; } = 0;
        /// <summary>
        /// </summary>
        public int Feeling { get; set; } = 0;
        /// <summary>
        /// </summary>
        public DateOnly? Date { get; set; } = null;
        /// <summary>
        /// </summary>
        public TimeOnly? Time { get; set; } = null;
        /// <summary>
        /// </summary>
        public int DateOffset { get; set; } = 2;
        /// <summary>
        /// </summary>
        public int TimeOffset { get; set; } = 60;
        /// <summary>
        /// </summary>
        public enum HolidayType
        {
            /// <summary>
            /// </summary>
            None,
            /// <summary>
            /// </summary>
            Mid_Autumn_Festival,
            /// <summary>
            /// </summary>
            Dragon_Boat_Festival,
            /// <summary>
            /// </summary>
            New_Years_Day,
            /// <summary>
            /// </summary>
            Spring_Festival,
            /// <summary>
            /// </summary>
            Christmas,
            /// <summary>
            /// </summary>
            Player_Birthday,
        }
        /// <summary>
        /// </summary>
        public HolidayType Holiday { get; set; } = HolidayType.None;

        /// <summary>
        /// </summary>
        public bool CheckDate(DateTime date)
        {
            var now = DateTime.Now;
            return date < now && date.AddDays(DateOffset) > now;
        }
        /// <summary>
        /// </summary>
        public static DateTime GetLunarDate(int month, int day)
        {
            ChineseLunisolarCalendar lunarCalendar = new ChineseLunisolarCalendar();
            DateTime lunarDate = lunarCalendar.ToDateTime(DateTime.Now.Year, month, day, 0, 0, 0, 0);
            return lunarDate;
        }
    }
    /// <summary>
    /// Unlock conditions
    /// </summary>
    public UnlockCondition UnlockAble { get; set; }

    /// <summary>
    /// </summary>
    public class Info
    {
        private ISub sub;
        public Info(ISub sub) { this.sub = sub; }
        public DateTime UnlockTime
        {
            get => sub.Infos[(gdat)"time"];
            set => sub.Infos[(gdat)"time"] = value;
        }
        public bool Star
        {
            get => sub.Infos[(gbol)"star"];
            set => sub.Infos[(gbol)"star"] = value;
        }
    }
    /// <summary>
    /// </summary>
    public Info PlayerInfo { get; set; } = null;
    /// <summary>
    /// YesNoFavorites
    /// </summary>
    public bool IsStar
    {
        get => PlayerInfo?.Star ?? false;
        set { if (PlayerInfo != null) PlayerInfo.Star = value; }
    }
    /// <summary>
    /// Unlock or not
    /// </summary>
    public bool IsUnlock => PlayerInfo != null;
    /// <summary>
    /// </summary>
    public void Unlock(IMainWindow imw)
    {
        ISub sub = imw.GameSavesData["photo"][Name];
        PlayerInfo = new Info(sub);
        PlayerInfo.UnlockTime = DateTime.Now;
    }
    public void LoadUserInfo(IMainWindow imw)
    {
        if (imw.GameSavesData["photo"].Contains(Name))
        {
            PlayerInfo = new Info(imw.GameSavesData["photo"][Name]);
        }
        else
            PlayerInfo = null;
    }

    /// <summary>
    /// </summary>   
    /// <returns></returns>
    public static BitmapSource ConvertToThumbnail(BitmapImage originalImage, int width, int height)
    {
        if (originalImage.Width < width && originalImage.Height < height
            || width == 0
            || height == 0)
        {
            return originalImage;
        }
        double scaleX = (double)width / originalImage.PixelWidth;
        double scaleY = (double)height / originalImage.PixelHeight;
        double scale = Math.Min(scaleX, scaleY);

        int scaledWidth = (int)(originalImage.PixelWidth * scale);
        int scaledHeight = (int)(originalImage.PixelHeight * scale);

        RenderTargetBitmap renderBitmap = new RenderTargetBitmap(scaledWidth, scaledHeight, 96d, 96d, PixelFormats.Pbgra32);
        DrawingVisual visual = new DrawingVisual();

        using (DrawingContext drawingContext = visual.RenderOpen())
        {
            drawingContext.DrawImage(originalImage, new Rect(0, 0, scaledWidth, scaledHeight));
        }

        renderBitmap.Render(visual);
        return renderBitmap;
    }
    /// <summary>
    /// </summary>
    /// <returns></returns>
    public static BitmapSource ConvertToGrayScale(BitmapSource originalImage)
    {
        // Create WriteableBitmap
        WriteableBitmap writeableBitmap = new WriteableBitmap(originalImage);
        int width = writeableBitmap.PixelWidth;
        int height = writeableBitmap.PixelHeight;

        int[] pixels = new int[width * height];
        writeableBitmap.CopyPixels(pixels, width * 4, 0);

        for (int i = 0; i < pixels.Length; i++)
        {
            byte a = (byte)((pixels[i] >> 24) & 0xff); // Alpha
            byte r = (byte)((pixels[i] >> 16) & 0xff); // Red
            byte g = (byte)((pixels[i] >> 8) & 0xff);  // Green
            byte b = (byte)(pixels[i] & 0xff);         // Blue

            byte gray = (byte)((r + g + b) / 3);

            pixels[i] = (a << 24) | (gray << 16) | (gray << 8) | gray; // ARGB
        }

        WriteableBitmap grayBitmap = new WriteableBitmap(width, height, writeableBitmap.DpiX, writeableBitmap.DpiY, PixelFormats.Pbgra32, null);
        grayBitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, width * 4, 0);

        return grayBitmap;
    }
    /// <summary>
    /// </summary>
    public BitmapImage GetImage(IMainWindow imw)
    {
        string zippath = imw.FileSources.FindSource(Zip + ".zlps");
        if (zippath == null)
        {
            zippath = imw.FileSources.FindSource(Zip + ".zip");
        }
        if (zippath == null)
        {
            return ImageResources.NewSafeBitmapImage("pack://application:,,,/Res/img/error.png");
        }
        using (ZipArchive archive = ZipFile.OpenRead(zippath))
        {
            ZipArchiveEntry entry = archive.GetEntry(Path);
            if (entry != null)
            {
                using (Stream stream = entry.Open())
                {
                    using (MemoryStream memoryStream = new MemoryStream())
                    {
                        stream.CopyTo(memoryStream);
                        memoryStream.Position = 0;

                        // Create BitmapImage
                        BitmapImage bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.StreamSource = memoryStream;
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();
                        bitmap.Freeze();
                        return bitmap;
                    }
                }
            }
            else
            {
                return ImageResources.NewSafeBitmapImage("pack://application:,,,/Res/img/error.png");
            }
        }
    }
    /// <summary>
    /// </summary>
    public BitmapImage GetGifImage(IMainWindow imw)
    {

        string zippath = imw.FileSources.FindSource(Zip + ".zlps");
        if (zippath == null)
        {
            zippath = imw.FileSources.FindSource(Zip + ".zip");
        }
        if (zippath == null)
        {
            return ImageResources.NewSafeBitmapImage("pack://application:,,,/Res/img/error.png");
        }

        using (ZipArchive archive = ZipFile.OpenRead(zippath))
        {
            ZipArchiveEntry entry = archive.GetEntry(Path);
            if (entry != null)
            {
                using (Stream stream = entry.Open())
                {
                    var memstr = new MemoryStream();
                    stream.CopyTo(memstr);

                    // Create BitmapImage
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.StreamSource = memstr;
                    bitmap.EndInit();
                    bitmap.Freeze();

                    return bitmap;
                }
            }
            else
            {
                return ImageResources.NewSafeBitmapImage("pack://application:,,,/Res/img/error.png");
            }
        }
    }



    /// <summary>
    /// </summary>
    public string FilePath(string savedir) => savedir + '\\' + FilePath();
    /// <summary>
    /// </summary>
    public string FilePath()
    {
        var filepath = TranslateName;
        char[] illegalChars = { '\\', '/', ':', '*', '?', '"', '<', '>', '|' };
        foreach (char c in illegalChars)
        {
            filepath = filepath.Replace(c.ToString(), "");
        }
        return filepath.Replace(' ', '_') + '.' + Path.Split('.').Last();
    }
    /// <summary>
    /// </summary>
    public void SaveAs(IMainWindow imw, string filepath)
    {
        string zippath = imw.FileSources.FindSource(Zip + ".zlps");
        if (zippath == null)
        {
            zippath = imw.FileSources.FindSource(Zip + ".zip");
        }
        if (zippath == null)
        {
            return;
        }
        using (ZipArchive archive = ZipFile.OpenRead(zippath))
        {
            ZipArchiveEntry entry = archive.GetEntry(Path);
            if (entry != null)
            {
                using (Stream sourceStream = entry.Open())
                {
                    using (FileStream destinationStream = new FileStream(filepath, FileMode.Create, FileAccess.Write))
                    {
                        sourceStream.CopyTo(destinationStream);
                    }
                }
            }
            else
            {
                return;
            }
        }
    }
    /// <summary>
    /// </summary>
    public bool CopyImageToClipboard(IMainWindow imw)
    {
        string zippath = imw.FileSources.FindSource(Zip + ".zlps");
        if (zippath == null)
        {
            zippath = imw.FileSources.FindSource(Zip + ".zip");
        }
        if (zippath == null)
        {
            return false;
        }
        if (!Directory.Exists(System.IO.Path.Combine(GraphCore.CachePath, "photo")))
        {
            Directory.CreateDirectory(System.IO.Path.Combine(GraphCore.CachePath, "photo"));
        }
        string filepath = System.IO.Path.Combine(GraphCore.CachePath, "photo", $"pic_{Zip}_{Sub.GetHashCode(Path):x}.png");
        if (!File.Exists(filepath))
        {
            using (ZipArchive archive = ZipFile.OpenRead(zippath))
            {
                ZipArchiveEntry entry = archive.GetEntry(Path);
                if (entry != null)
                {
                    using (Stream sourceStream = entry.Open())
                    {
                        using (FileStream destinationStream = new FileStream(filepath, FileMode.Create, FileAccess.Write))
                        {
                            sourceStream.CopyTo(destinationStream);
                        }
                    }
                }
                else
                {
                    return false;
                }
            }
        }

        DataObject dataObject = new DataObject();
        dataObject.SetFileDropList(new System.Collections.Specialized.StringCollection { filepath });

        Clipboard.SetDataObject(dataObject);

        return true;
    }
}

