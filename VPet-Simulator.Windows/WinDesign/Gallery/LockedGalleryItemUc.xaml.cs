using LinePutScript.Localization.WPF;
using NAudio.Gui;
using Panuon.WPF.UI;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using VPet_Simulator.Windows.Interface;

namespace VPet_Simulator.Windows.WinDesign.Gallery
{
    /// <summary>
    /// </summary>
    public partial class LockedGalleryItemUc : UserControl
    {
        Photo Photo;
        MainWindow mw;
        public LockedGalleryItemUc(Photo photo, MainWindow mw)
        {
            InitializeComponent();
            Photo = photo;
            this.mw = mw;
            tbTitle.Text = photo.TranslateName;

            string untxt = photo.UnlockAble.CheckReason(mw.GameSavesData);
            unlocktext.Text = untxt;


            if (Photo.UnlockAble.SellPrice > 0)
            {
                if (Photo.UnlockAble.SellBoth)
                {
                    if (Photo.UnlockAble.Check(mw.GameSavesData))
                    {
                        btnCan.Visibility = Visibility.Visible;
                        rAnd.Text = "AND".Translate();
                        clmoney.Text = convertk(photo.UnlockAble.SellPrice);
                    }
                    else
                    {
                        btnCannot.Visibility = Visibility.Visible;
                        nlmoney.Text = convertk(photo.UnlockAble.SellPrice);
                    }
                    ToolTip = "Spend ${0} and meet the following conditions".Translate(photo.UnlockAble.SellPrice) + '\n' + untxt;
                }
                else
                {
                    btnCan.Visibility = Visibility.Visible;
                    clmoney.Text = convertk(photo.UnlockAble.SellPrice);
                    ToolTip = "Spend ${0} or meet the following conditions".Translate(photo.UnlockAble.SellPrice) + '\n' + untxt;
                    rAnd.Text = "OR".Translate();
                }
            }
            else
                ToolTip = untxt;

        }
        private string convertk(double price)
        {
            if (price < 100000)
                return price.ToString("N0");
            return (price / 1000).ToString("N0") + "k";
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (mw.GameSavesData.GameSave.Money < Photo.UnlockAble.SellPrice)
            {
                mw.winGallery.Toast("Lack of money".Translate() + " " + convertk(Photo.UnlockAble.SellPrice),
                    icon: MessageBoxIcon.Warning);
                return;
            }

            mw.GameSavesData.GameSave.Money -= Photo.UnlockAble.SellPrice;
            Photo.Unlock(mw);
            var i = mw.winGallery.AutoUniformGridImages.Children.IndexOf(this);
            mw.winGallery.AutoUniformGridImages.Children.Remove(this);
            mw.winGallery.AutoUniformGridImages.Children.Insert(i, new UnLockedGalleryItemUc(Photo, mw));
            mw.winGallery.Toast(
            message: "Unlocked".Translate() + " " + Photo.TranslateName,
            icon: MessageBoxIcon.Info
            );
        }

        private void this_Loaded(object sender, RoutedEventArgs e)
        {
            displayimage.Source = Photo.ConvertToGrayScale(
                Photo.ConvertToThumbnail(Photo.GetImage(mw),
               (int)(bbd.ActualWidth * 2), (int)(bbd.ActualHeight * 2)));
        }
    }
}
