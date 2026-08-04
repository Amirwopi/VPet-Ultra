using LinePutScript;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using static VPet_Simulator.Core.IGraph;
using static VPet_Simulator.Core.Picture;

namespace VPet_Simulator.Core
{

    /// <summary>
    /// </summary>
    public partial class PNGAnimation : IImageRun
    {
        /// <summary>
        /// </summary>
        public List<Animation> Animations;
        /// <summary>
        /// </summary>
        public bool IsLoop { get; set; }

        /// <summary>
        /// </summary>
        public GraphInfo GraphInfo { get; private set; }

        /// <summary>
        /// </summary>
        public bool IsReady { get; private set; } = false;

        public TaskControl Control { get; set; }

        int nowid;
        /// <summary>
        /// </summary>
        public string Path { get; set; }
        private GraphCore GraphCore;
        private BitmapSource SpriteSheetSource;
        private Int32Rect[] FrameRects;
        private readonly object SpriteSheetLock = new object();
        private readonly object FrameCacheLock = new object();
        private readonly Dictionary<int, BitmapSource> FrameCache = new Dictionary<int, BitmapSource>();
        private int FrameWidth;
        private int FrameHeight;
        public long LastUseTimeTicks = DateTime.UtcNow.Ticks;

        private const int FrameCacheAheadCount = 2;

        public bool IsFail { get; set; } = false;

        public string FailMessage { get; set; } = "";

        /// <summary>
        /// </summary>
        public PNGAnimation(GraphCore graphCore, string path, FileInfo[] paths, GraphInfo graphinfo, bool isLoop = false)
        {
            Animations = new List<Animation>();
            IsLoop = isLoop;
            GraphInfo = graphinfo;
            GraphCore = graphCore;
            if (!GraphCore.CommConfig.ContainsKey("PA_Setup"))
            {
                GraphCore.CommConfig["PA_Setup"] = true;
                GraphCore.Dispatcher.Invoke(() =>
                {
                    GraphCore.CommUIElements["Image1.PNGAnimation"] = new System.Windows.Controls.Image() { Height = 500 };
                    GraphCore.CommUIElements["Image2.PNGAnimation"] = new System.Windows.Controls.Image() { Height = 500 };
                    GraphCore.CommUIElements["Image3.PNGAnimation"] = new System.Windows.Controls.Image() { Height = 500 };
                });
            }
            Task.Run(() => startup(path, paths));
        }

        public static void LoadGraph(GraphCore graph, FileSystemInfo path, ILine info)
        {
            if (!(path is DirectoryInfo p))
            {
                Picture.LoadGraph(graph, path, info);
                return;
            }
            var paths = p.GetFiles("*.png");
            if (paths.Length == 0)
            {
                return;
            }
            else if (paths.Length == 1)
            {
                Picture.LoadGraph(graph, paths[0], info);
                return;
            }

            bool isLoop = info[(gbol)"loop"];
            PNGAnimation pa = new PNGAnimation(graph, path.FullName, paths, new GraphInfo(path, info), isLoop);
            graph.AddGraph(pa);
        }

        /// <summary>
        /// </summary>
        public static int MaxLoadMemory = 2000;

        private async void startup(string path, FileInfo[] paths)
        {
            // Timeout: if memory is too high for too long, give up waiting
            var memTimeoutCts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
            while (Function.MemoryUsage() > MaxLoadMemory)
            {
                if (memTimeoutCts.IsCancellationRequested)
                {
                    IsFail = true;
                    FailMessage = $"--PNGAnimation--{GraphInfo}--\nPath: {path}\nTimeout waiting for memory to drop below {MaxLoadMemory}MB";
                    return;
                }
                await Task.Delay(100);
            }
            try
            {
                Path = System.IO.Path.Combine(GraphCore.CachePath, $"{GraphCore.Resolution}_{Math.Abs(Sub.GetHashCode(path))}_{paths.Length}.png");
                var sem = GraphCore.SpriteSheetBuildLocks.GetOrAdd(Path, _ => new SemaphoreSlim(1, 1));
                await sem.WaitAsync();
                try
                {
                    if (!File.Exists(Path) && !((List<string>)GraphCore.CommConfig["Cache"]).Contains(path))
                    {
                        ((List<string>)GraphCore.CommConfig["Cache"]).Add(path);
                        int w = 0;
                        int h = 0;
                        // Load the first image
                        using (var firstImage = SKBitmap.Decode(paths[0].FullName))
                        {
                            w = firstImage.Width;
                            h = firstImage.Height;

                            // Adjust width and height based on resolution
                            if (w > GraphCore.Resolution)
                            {
                                w = GraphCore.Resolution;
                                h = (int)(h * (GraphCore.Resolution / (double)firstImage.Width));
                            }

                            if (paths.Length * w >= 60000)
                            {
                                w = 60000 / paths.Length;
                                h = (int)(firstImage.Height * (w / (double)firstImage.Width));
                            }
                        }

                        FrameWidth = w;
                        FrameHeight = h;

                        // Create a new bitmap to draw on
                        using (var combinedBitmap = new SKBitmap(w * paths.Length, h))
                        using (var canvas = new SKCanvas(combinedBitmap))
                        {
                            // Draw the first image
                            using (var firstImage = SKBitmap.Decode(paths[0].FullName))
                            {
                                canvas.DrawBitmap(firstImage, new SKRect(0, 0, w, h));
                            }

                            // Create an array to hold bitmaps for the remaining images
                            SKBitmap[] bitmaps = new SKBitmap[paths.Length - 1];

                            // Load and draw remaining images in parallel
                            Parallel.For(1, paths.Length, i =>
                            {
                                var img = SKBitmap.Decode(paths[i].FullName);
                                bitmaps[i - 1] = img; // Store the bitmap in the array
                            });

                            // Now draw the bitmaps onto the combined canvas
                            for (int i = 0; i < bitmaps.Length; i++)
                            {
                                canvas.DrawBitmap(bitmaps[i], new SKRect(w * (i + 1), 0, w * (i + 2), h));
                                bitmaps[i]?.Dispose();
                            }

                            // Save the combined image to the cache path
                            using (var image = SKImage.FromBitmap(combinedBitmap))
                            using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
                            using (var stream = File.OpenWrite(Path))
                            {
                                data.SaveTo(stream);
                            }
                        }
                    }
                }
                finally
                {
                    sem.Release();
                }

                if (FrameWidth == 0 || FrameHeight == 0)
                {
                    using (var firstImage = SKBitmap.Decode(paths[0].FullName))
                    {
                        FrameWidth = firstImage.Width;
                        FrameHeight = firstImage.Height;
                        if (FrameWidth > GraphCore.Resolution)
                        {
                            FrameWidth = GraphCore.Resolution;
                            FrameHeight = (int)(FrameHeight * (GraphCore.Resolution / (double)firstImage.Width));
                        }
                        if (paths.Length * FrameWidth >= 60000)
                        {
                            FrameWidth = 60000 / paths.Length;
                            FrameHeight = (int)(firstImage.Height * (FrameWidth / (double)firstImage.Width));
                        }
                    }
                }

                FrameRects = new Int32Rect[paths.Length];

                for (int i = 0; i < paths.Length; i++)
                {
                    FrameRects[i] = new Int32Rect(FrameWidth * i, 0, FrameWidth, FrameHeight);
                    var noExtFileName = System.IO.Path.GetFileNameWithoutExtension(paths[i].Name);
                    int time = int.Parse(noExtFileName.Substring(noExtFileName.LastIndexOf('_') + 1));
                    Animations.Add(new Animation(this, time, i));
                }
                //stream = new MemoryStream(File.ReadAllBytes(cp));
                IsReady = true;
            }
            catch (Exception e)
            {
                IsFail = true;
                FailMessage = $"--PNGAnimation--{GraphInfo}--\nPath: {path}\n{e.Message}";
            }
            finally
            {
                // Ensure the graph is marked as either ready or failed so
                // Load_2_WaitGraph doesn't loop forever.
                if (!IsReady && !IsFail)
                {
                    IsFail = true;
                    FailMessage = $"--PNGAnimation--{GraphInfo}--\nPath: {path}\nUnknown failure (neither ready nor failed)";
                }
            }
        }

        /// <summary>
        /// </summary>
        public class Animation
        {
            private PNGAnimation parent;
            public int FrameIndex;
            ///// <summary>
            ///// </summary>
            //public Action Visible;
            ///// <summary>
            ///// </summary>
            //public Action Hidden;
            /// <summary>
            /// </summary>
            public int Time;
            public Animation(PNGAnimation parent, int time, int frameIndex)//, Action hidden)
            {
                this.parent = parent;
                Time = time;
                //Visible = visible;
                //Hidden = hidden;
                FrameIndex = frameIndex;
            }
            /// <summary>
            /// </summary>
            public void Run(FrameworkElement This, TaskControl Control)
            {
                var frameSource = parent.GetFrameSource(FrameIndex);
                This.Dispatcher.Invoke(() =>
                {
                    if (This is System.Windows.Controls.Image image)
                    {
                        image.Source = frameSource;
                    }
                    This.Margin = new Thickness(0, 0, 0, 0);
                });
                Thread.Sleep(Time);
                switch (Control.Type)
                {
                    case TaskControl.ControlType.Stop:
                        Control.EndAction?.Invoke();
                        return;
                    case TaskControl.ControlType.Status_Stoped:
                        return;
                    case TaskControl.ControlType.Status_Quo:
                    case TaskControl.ControlType.Continue:
                        if (++parent.nowid >= parent.Animations.Count)
                            if (parent.IsLoop)
                            {
                                parent.nowid = 0;
                                Task.Run(() => parent.Animations[0].Run(This, Control));
                                return;
                            }
                            else if (Control.Type == TaskControl.ControlType.Continue)
                            {
                                Control.Type = TaskControl.ControlType.Status_Quo;
                                parent.nowid = 0;
                            }
                            else
                            {
                                Control.Type = TaskControl.ControlType.Status_Stoped;
                                Control.EndAction?.Invoke();
                                return;
                            }
                        parent.Animations[parent.nowid].Run(This, Control);
                        return;
                }
            }
        }
        /// <summary>
        /// </summary>
        public void Run(Decorator parant, Action EndAction = null)
        {
            Touch();
            if (!IsReady)
            {
                EndAction?.Invoke();
                return;
            }
            if (Control?.PlayState == true)
            {
                Control.Stop(() => Run(parant, EndAction));
                return;
            }
            nowid = 0;
            var NEWControl = new TaskControl(EndAction);
            Control = NEWControl;
            parant.Dispatcher.Invoke(() =>
            {
                if (parant.Tag == this)
                {
                    Task.Run(() => Animations[0].Run((System.Windows.Controls.Image)parant.Child, NEWControl));
                    return;
                }
                System.Windows.Controls.Image img;

                if (parant.Child == GraphCore.CommUIElements["Image1.PNGAnimation"])
                {
                    img = (System.Windows.Controls.Image)GraphCore.CommUIElements["Image1.PNGAnimation"];
                }
                else if (parant.Child == GraphCore.CommUIElements["Image3.PNGAnimation"])
                {
                    img = (System.Windows.Controls.Image)GraphCore.CommUIElements["Image3.PNGAnimation"];
                }
                else
                {
                    img = (System.Windows.Controls.Image)GraphCore.CommUIElements["Image2.PNGAnimation"];
                    if (parant.Child != GraphCore.CommUIElements["Image2.PNGAnimation"])
                    {
                        if (img.Parent == null)
                        {
                            parant.Child = img;
                        }
                        else
                        {
                            img = (System.Windows.Controls.Image)GraphCore.CommUIElements["Image1.PNGAnimation"];
                            if (img.Parent != null)
                                ((Decorator)img.Parent).Child = null;
                            parant.Child = img;
                        }
                    }
                }
                parant.Tag = this;
                img.Source = GetFrameSource(0);
                img.Width = 500;
                Task.Run(() => Animations[0].Run((System.Windows.Controls.Image)parant.Child, NEWControl));
            });
        }
        /// <summary>
        /// </summary>
        public Task Run(System.Windows.Controls.Image img, Action EndAction = null)
        {
            Touch();
            if (!IsReady)
            {
                EndAction?.Invoke();
                return Task.CompletedTask;
            }
            if (Control?.PlayState == true)
            {
                Control.EndAction = null;
                Control.Type = TaskControl.ControlType.Stop;
            }
            nowid = 0;
            Control = new TaskControl(EndAction);
            return img.Dispatcher.Invoke(() =>
            {
                if (img.Tag == this)
                {
                    return new Task(() => Animations[0].Run(img, Control));
                }
                img.Tag = this;
                img.Source = GetFrameSource(0);
                img.Width = 500;
                return new Task(() => Animations[0].Run(img, Control));
            });
        }

        private BitmapSource GetFrameSource(int frameIndex)
        {
            Touch();
            EnsureSpriteSheetLoaded();
            if (FrameRects == null || frameIndex < 0 || frameIndex >= FrameRects.Length || SpriteSheetSource == null)
                return null;
            lock (FrameCacheLock)
            {
                if (FrameCache.TryGetValue(frameIndex, out var cacheFrame))
                {
                    return cacheFrame;
                }

                var frame = new CroppedBitmap(SpriteSheetSource, FrameRects[frameIndex]);
                frame.Freeze();
                FrameCache[frameIndex] = frame;

                var keepKeys = GetForwardKeepKeys(frameIndex);
                var removeKeys = new List<int>();
                foreach (var key in FrameCache.Keys)
                {
                    if (!keepKeys.Contains(key))
                    {
                        removeKeys.Add(key);
                    }
                }
                foreach (var key in removeKeys)
                {
                    FrameCache.Remove(key);
                }

                return frame;
            }
        }

        private HashSet<int> GetForwardKeepKeys(int frameIndex)
        {
            var keep = new HashSet<int> { frameIndex };
            int cursor = frameIndex;
            for (int i = 0; i < FrameCacheAheadCount; i++)
            {
                cursor++;
                if (cursor >= FrameRects.Length)
                {
                    if (!IsLoop)
                        break;
                    cursor = 0;
                }
                keep.Add(cursor);
            }
            return keep;
        }

        private void EnsureSpriteSheetLoaded()
        {
            if (SpriteSheetSource != null)
                return;
            lock (SpriteSheetLock)
            {
                if (SpriteSheetSource != null)
                    return;
                BitmapImage spriteSheet = new BitmapImage();
                spriteSheet.BeginInit();
                spriteSheet.CacheOption = BitmapCacheOption.OnDemand;
                spriteSheet.CreateOptions = BitmapCreateOptions.DelayCreation;
                spriteSheet.UriSource = new Uri(Path);
                spriteSheet.EndInit();
                spriteSheet.Freeze();
                SpriteSheetSource = spriteSheet;
            }
        }
        /// <summary>
        /// </summary>
        public void Touch() => Interlocked.Exchange(ref LastUseTimeTicks, DateTime.UtcNow.Ticks);


        public void CleanupIdleCache(long cleanTicks)
        {
            if (Control?.PlayState == true)
                return;
            if (SpriteSheetSource == null)
                return;
            long lastUse = Interlocked.Read(ref LastUseTimeTicks);
            if (cleanTicks < lastUse)
                return;

            lock (SpriteSheetLock)
            {
                SpriteSheetSource = null;
            }
            lock (FrameCacheLock)
            {
                FrameCache.Clear();
            }
        }

        public void Dispose()
        {
            Animations = null;
            FrameRects = null;
            lock (SpriteSheetLock)
            {
                SpriteSheetSource = null;
            }
            lock (FrameCacheLock)
            {
                FrameCache.Clear();
            }
            GraphCore = null;
        }
    }
}
