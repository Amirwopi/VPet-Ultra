using LinePutScript;
using LinePutScript.Dictionary;
using System;
using System.Windows.Media.Imaging;

namespace VPet_Simulator.Windows.Interface
{
    /// <summary>
    /// </summary>
    public class Resources : LPS_D
    {
        public Resources() { }
        /// <summary>
        /// </summary>
        public void AddSource(ILine line, string modpath)
        {
            ISub source = line.Find("source");
            if (source == null)
                return;
            //else if (!source.Info.Contains(":\\"))
            source.Info = modpath + '\\' + source.Info;
            line.Name = line.Name.ToLowerInvariant();
            AddorReplaceLine(line);
        }
        /// <summary>
        /// </summary>
        public void AddSource(ILine line)
        {
            ISub source = line.Find("source");
            if (source == null)
                return;
            //else if (!source.Info.Contains(":\\"))
            line.Name = line.Name.ToLowerInvariant();
            AddorReplaceLine(line);
        }
        /// <summary>
        /// </summary>
        public void AddSources(ILPS lps)
        {
            foreach (ILine line in lps)
            {
                line.Name = line.Name.ToLowerInvariant();
                AddSource(line);
            }
        }
        /// <summary>
        /// </summary>
        public void AddSources(ILPS lps, string modpath = "")
        {
            foreach (ILine line in lps)
            {
                AddSource(line, modpath);
            }
        }
        /// <summary>
        /// </summary>
        public void AddSource(string name, string path)
        {
            AddorReplaceLine(new Line(name.ToLowerInvariant(), "", "", new Sub("source", path)));
        }
        /// <summary>
        /// </summary>
        public string FindSource(string name, string nofind = null)
        {
            ILine line = FindLine(name.ToLowerInvariant());
            if (line == null)
                return nofind;
            return line.Find("source").Info;
        }
        /// <summary>
        /// </summary>
        public Uri FindSourceUri(string name, string nofind = null)
        {
            ILine line = FindLine(name.ToLowerInvariant());
            if (line == null)
                if (nofind != null)
                    return new Uri(nofind);
                else
                    return null;
            return new Uri(line.Find("source").Info);
        }
    }

    /// <summary>
    /// </summary>
    public class ImageResources : Resources
    {
        public ImageResources()
        {

        }
        /// <summary>
        /// </summary>
        public void AddImages(LpsDocument lps, string modpath = "") => AddSources(lps, modpath);
        /// <summary>
        /// </summary>
        public void AddImage(ILine line, string modpath = "") => AddSource(line, modpath);
        /// <summary>
        /// </summary>
        public BitmapImage FindImage(string imagename) => NewSafeBitmapImage(FindImageUri(imagename));

        public Uri FindImageUri(string imagename)
        {
#if DEBUGs
            var v = FindSourceUri(imagename, "pack://application:,,,/Res/Image/system/error.png");
            if (v.AbsoluteUri == "pack://application:,,,/Res/Image/system/error.png")
                throw new Exception($"image nofound {imagename}");
            return v;
#else
            return FindSourceUri(imagename, "pack://application:,,,/Res/img/error.png");
#endif
        }

        /// <summary>
        /// </summary>
        public BitmapImage FindImage(string imagename, string superior)
        {
            string source = FindSource(imagename);
            if (source == null)
            {
                return NewSafeBitmapImage(FindImageUri(superior));
            }
            return NewSafeBitmapImage(source);
        }
        /// <summary>
        /// </summary>
        public LpsDocument ImageSetting = new LpsDocument();
        /// <summary>
        /// </summary>
        /// <returns>BitmapImage</returns>
        public static BitmapImage NewSafeBitmapImage(string source) => NewSafeBitmapImage(new Uri(source));
        /// <summary>
        /// </summary>
        /// <returns>BitmapImage</returns>
        public static BitmapImage NewSafeBitmapImage(Uri source)
        {
            BitmapImage bi = new BitmapImage();
            bi.BeginInit();
            bi.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
            bi.UriSource = source;
            bi.EndInit();
            return bi;
        }
    }
}
