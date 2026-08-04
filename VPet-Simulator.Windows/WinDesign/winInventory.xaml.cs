using LinePutScript;
using LinePutScript.Localization.WPF;
using Panuon.WPF;
using Panuon.WPF.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Threading;
using VPet_Simulator.Windows.Interface;

namespace VPet_Simulator.Windows
{
    /// <summary>
    /// </summary>
    public partial class winInventory : WindowX
    {
        MainWindow mw;
        private TextBox _searchTextBox;
        private Run rTotalValue;
        private Item _detailItem;
        private int _detailCount = 1;

        /// <summary>
        /// </summary>
        public winInventory(MainWindow mw)
        {
            InitializeComponent();
            this.mw = mw;
            Loaded += winInventory_Loaded;
            for (int i = 1; i < Item.ItemTypes.Count; i++)
            {
                LsbCategory.Items.Add(new ListBoxItem() { Content = ("Item_" + Item.ItemTypes[i]).Translate() });
            }
            //mw.ItemsAdd(mw.Foods[2].Clone());
        }

        /// <summary>
        /// </summary>
        public new void Show()
        {
            base.Show();
            Dispatcher.BeginInvoke(new Action(UpdateList), DispatcherPriority.Loaded);

        }

        private void winInventory_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateList();
        }

        /// <summary>
        /// </summary>
        /// <remarks>
        /// </remarks>
        private void UpdateList()
        {
            if (mw == null) return;

            IEnumerable<Item> items = mw.Items.Where(x => x.Visibility);

            // Search
            if (_searchTextBox != null && !string.IsNullOrWhiteSpace(_searchTextBox.Text))
            {
                items = items.Where(x => x.TranslateName.Contains(_searchTextBox.Text));
            }

            // Favorites
            if (_puswitch?.IsChecked == true)
            {
                items = items.Where(x => x.Star == true);
            }

            if (LsbCategory.SelectedIndex != 0)
            {
                items = items.Where(x => x.ItemType == Item.ItemTypes[LsbCategory.SelectedIndex]);
            }

            bool asc = LsbSortAsc.SelectedIndex == 0;
            switch (LsbSortRule.SelectedIndex)
            {
                default:
                    break;
                case 1:
                    items = asc ? items.OrderBy(x => x.Name) : items.OrderByDescending(x => x.Name);
                    break;
                case 2:
                    items = asc ? items.OrderBy(x => x.Count) : items.OrderByDescending(x => x.Count);
                    break;
                case 3: // Price
                    items = asc ? items.OrderBy(x => x.Price) : items.OrderByDescending(x => x.Price);
                    break;
            }

            IcCommodity.ItemsSource = items;

            double totalValue = mw.Items.Sum(x => x.Price * x.Count);
            if (rTotalValue != null)
                rTotalValue.Text = totalValue.ToString("f2");

            if (!items.Any())
                TbNone.Visibility = Visibility.Visible;
            else
                TbNone.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// </summary>
        private void UseItem(Item item, int count = 1)
        {
            if (item == null) return;
            while (count-- > 0)
                item.Use(mw);
            UpdateList();
        }

        /// <summary>
        /// </summary>
        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            UpdateList();
        }

        /// <summary>
        /// </summary>
        private void TbTitleSearch_Loaded(object sender, RoutedEventArgs e)
        {
            _searchTextBox = sender as TextBox;
        }

        /// <summary>
        /// </summary>
        private void rTotalValue_Loaded(object sender, RoutedEventArgs e)
        {
            rTotalValue = sender as Run;
            var totalValue = mw.Items.Sum(x => x.Price * x.Count);
            rTotalValue.Text = totalValue.ToString("f2");
        }


        /// <summary>
        /// </summary>
        private void LsbSortRule_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded)
                UpdateList();
        }

        ///// <summary>
        ///// </summary>
        //private void WindowX_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        //{
        //    IcCommodity.ItemsSource = null;
        //    HideDetail();
        //    Hide();
        //    e.Cancel = true;
        //}

        private void BtnHoverUse_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            var btn = sender as Button;
            var item = btn?.DataContext as Item;
            if (item == null)
                return;
            UseItem(item);
        }
        private void BtnHoverView_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            var btn = sender as Button;
            var item = btn?.DataContext as Item;
            if (item == null)
                return;
            DisplayDetail(item);
        }

        /// <summary>
        /// </summary>
        private void CellRoot_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var bdr = sender as Border;
            var item = bdr?.DataContext as Item;
            if (item == null)
                return;
            DisplayDetail(item);
        }

        /// <summary>
        /// </summary>
        private void DisplayDetail(Item item)
        {
            _detailItem = item;
            _detailCount = 1;

            TextItemName.Text = item.TranslateName;
            ImageItemDetail.Source = item.ImageSource;
            TextItemPrice.Text = $"$ {item.Price:f2}";
            TextItemDesc.Text = item.Description ?? "";

            runMax.Text = item.Count.ToString();

            TbDetailCount.Text = _detailCount.ToString();
            TbtnStar.IsChecked = item.Star;
            IsMaskVisible = true;
            IsOverlayerVisible = true;

            if (item.IsSingle)
            {
                spUseEnter.Visibility = Visibility.Collapsed;
                BorderIsSingle.Visibility = Visibility.Visible;
            }
            else
            {
                spUseEnter.Visibility = Visibility.Visible;
                BorderIsSingle.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// </summary>
        private void HideDetail()
        {
            IsMaskVisible = false;
            IsOverlayerVisible = false;
        }

        /// <summary>
        /// </summary>
        /// <remarks>
        /// </remarks>
        private void BorderOutDetail_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            HideDetail();
        }

        /// <summary>
        /// </summary>
        private void ButtonCloseDetail_Click(object sender, RoutedEventArgs e)
        {
            HideDetail();
        }

        /// <summary>
        /// </summary>
        private void RbtnDetailDecrease_Click(object sender, RoutedEventArgs e)
        {
            if (_detailItem == null)
                return;
            _detailCount = Math.Max(1, _detailCount - 1);
            TbDetailCount.Text = _detailCount.ToString();
        }

        /// <summary>
        /// </summary>
        private void RbtnDetailIncrease_Click(object sender, RoutedEventArgs e)
        {
            if (_detailItem == null)
                return;
            _detailCount = Math.Min(Math.Max(1, _detailItem.Count), _detailCount + 1);
            TbDetailCount.Text = _detailCount.ToString();
        }

        /// <summary>
        /// </summary>
        /// <remarks>
        /// </remarks>
        private void TbDetailCount_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ApplyDetailCountFromText();
                e.Handled = true;
            }
        }

        /// <summary>
        /// </summary>
        private void TbDetailCount_LostFocus(object sender, RoutedEventArgs e)
        {
            ApplyDetailCountFromText();
        }

        /// <summary>
        /// </summary>
        /// <remarks>
        /// </remarks>
        private void ApplyDetailCountFromText()
        {
            if (_detailItem == null)
                return;
            if (!int.TryParse(TbDetailCount.Text?.Trim(), out var v))
                v = _detailCount;
            v = Math.Max(1, v);
            v = Math.Min(Math.Max(1, _detailItem.Count), v);
            _detailCount = v;
            TbDetailCount.Text = _detailCount.ToString();
        }

        /// <summary>
        /// </summary>
        private void BtnDetailUse_Click(object sender, RoutedEventArgs e)
        {
            if (_detailItem == null)
                return;
            ApplyDetailCountFromText();
            UseItem(_detailItem, _detailCount);
            HideDetail();
        }

        private CheckBox _puswitch;
        private void Switch_Loaded(object sender, RoutedEventArgs e)
        {
            _puswitch = sender as CheckBox;
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (IsLoaded)
                UpdateList();
        }

        private void TbtnStar_Checked(object sender, RoutedEventArgs e)
        {
            if (_detailItem != null)
                _detailItem.Star = TbtnStar.IsChecked == true;
        }
    }
}
