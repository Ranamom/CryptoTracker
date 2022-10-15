﻿using CommunityToolkit.Mvvm.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using UWP.Background;
using UWP.Core.Constants;
using UWP.Helpers;
using UWP.Models;
using UWP.Services;
using UWP.Shared.Constants;
using UWP.Shared.Interfaces;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.System.Threading;
using Windows.UI.Core;
using Windows.UI.StartScreen;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

namespace UWP.Views {
    public sealed partial class Home : Page, UpdatablePage {
        /// Variables to get historic
        private static int limit = 168;
        private static int aggregate = 1;
        private static string timeSpan = "1w";
        private static string timeUnit = "hour";

        /// Timers for auto-refresh
        private static ThreadPoolTimer ChartPeriodicTimer;
        private static ThreadPoolTimer PricePeriodicTimer;
        private static DateTime lastPriceUpdate = new DateTime(1990, 1, 1);

        public Home() {
            InitializeComponent();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e) {
            /// Get timespan before updating
            timeSpan = App._LocalSettings.Get<string>(UserSettings.Timespan);
            TimeRangeRadioButtons.TimeSpan = timeSpan;
            (timeUnit, limit, aggregate) = GraphHelper.TimeSpanParser[timeSpan];

            InitHome();

            /// Create the auto-refresh timer
            var autoRefresh = App._LocalSettings.Get<string>(UserSettings.AutoRefresh);
            TimeSpan period;
            if (!autoRefresh.Equals("none", StringComparison.InvariantCultureIgnoreCase)) {
                switch (autoRefresh) {
                    case "30 sec":
                        period = TimeSpan.FromSeconds(30);
                        break;
                    case "1 min":
                    default:
                        period = TimeSpan.FromSeconds(60);
                        break;
                    case "2 min":
                        period = TimeSpan.FromSeconds(120);
                        break;
                }
                PricePeriodicTimer = ThreadPoolTimer.CreatePeriodicTimer(async (source) => {
                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () => {
                        await UpdatePrices();
                    });
                }, TimeSpan.FromSeconds(30));

                ChartPeriodicTimer = ThreadPoolTimer.CreatePeriodicTimer(async (source) => {
                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                        if (timeUnit == "minute")
                            TimeRangeButtons_Tapped(null, null);
                    });
                }, period);
            }
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e) {
            ChartPeriodicTimer?.Cancel();
            PricePeriodicTimer?.Cancel();
        }

        public async Task UpdatePage() {
            foreach (var homeCard in vm.PriceCards)
                homeCard.Info.IsLoading = true;

            await UpdatePrices();
            for (int i = 0; i < vm.PriceCards.Count; i++)
                await UpdateCard(i);
        }

        public async Task UpdatePrices() {
            if ((DateTime.Now - lastPriceUpdate).TotalSeconds < 10)
                return;

            for (int i = 0; i < vm.PriceCards.Count; i++)
                await UpdateCardPrice(i);
            lastPriceUpdate = DateTime.Now;
        }

        /// #########################################################################################
        private async void InitHome() {
            /// See if there's any change
            var pinned = App.pinnedCoins.ToList();
            var current = vm.PriceCards.Select(x => x.Info.Name).ToList();

            // TODO: dont clear cards that haven't changed
            if(!pinned.SequenceEqual(current)) {
                vm.PriceCards.Clear();
                foreach (var coin in App.pinnedCoins)
                    AddCoinHome(coin);
            }

            var tiles = await SecondaryTile.FindAllAsync();
            for (int i = 0; i < App.pinnedCoins.Count; i++)
                vm.PriceCards[i].Info.IsPin = tiles.Any(tile => tile.TileId == App.pinnedCoins[i]);

            await UpdatePage();
        }

        /// #########################################################################################
        /// Add/remove coins from Home
        internal void AddCoinHome(string crypto) {
            var h = new HomeCard() { Info = new Coin() { Name = crypto } };
            vm.PriceCards.Add(h);

            /// Update pinnedCoin list
            App.UpdatePinnedCoins();
        }

        internal void RemoveCoinHome(string crypto) {
            if (App.pinnedCoins.Contains(crypto)) {
                var n = App.pinnedCoins.IndexOf(crypto);

                App.pinnedCoins.RemoveAt(n);
                vm.PriceCards.RemoveAt(n);

                /// Update pinnedCoin list
                App.UpdatePinnedCoins();
            }
        }

        /// #########################################################################################
        ///  Update a card's price
        private async Task UpdateCardPrice(int i) {
            /// Get price
            string crypto = App.pinnedCoins[i];
            var price = await Ioc.Default.GetService<ICryptoCompare>().GetPrice_Extension(
                crypto, App.currency);

            vm.PriceCards[i].Info.Price = price;

            var oldestPrice = vm.PriceCards[i].Chart.ChartData.FirstOrDefault()?.Value ?? price;
            vm.PriceCards[i].Info.Prices = (oldestPrice, price);
        }

        /// #########################################################################################
        ///  Update a card's charts
        private async Task UpdateCard(int i) {
            try {
                string crypto = App.pinnedCoins[i];
                vm.PriceCards[i].Info.CurrencySym = App.currencySymbol;

                /// Save the current timeSpan for navigating to another page
                vm.PriceCards[i].Chart.TimeSpan = timeSpan;

                /// Colors
                var brush = vm.PriceCards[i].Chart.ChartStroke;
                brush = ColorConstants.GetCoinBrush(crypto);
                vm.PriceCards[i].Chart.ChartStroke = brush;

                /// Get Historic and create List of ChartData for the chart
                var histo = await Ioc.Default.GetService<ICryptoCompare>().GetHistoric_(crypto, timeUnit, limit, aggregate);
                var chartData = new List<ChartPoint>();
                foreach (var h in histo) {
                    chartData.Add(new ChartPoint() {
                        Date = h.DateTime,
                        Value = h.Average,
                        Volume = h.volumeto,
                        High = h.high,
                        Low = h.low,
                        Open = h.open,
                        Close = h.close
                    });
                }
                if (chartData.Count == 0)
                    return;

                vm.PriceCards[i].Chart.ChartData = chartData;
                vm.PriceCards[i].Chart.ChartStyling = GraphHelper.AdjustLinearAxis(new ChartStyling(), timeSpan);

                /// Calculate min-max to adjust axis
                var MinMax = GraphHelper.GetMinMaxOfArray(chartData.Select(d => d.Value).ToList());
                vm.PriceCards[i].Chart.PricesMinMax = GraphHelper.OffsetMinMaxForChart(MinMax.Min, MinMax.Max);
                vm.PriceCards[i].Chart.VolumeMax = GraphHelper.GetMaxOfVolume(chartData);

                /// Calculate the price difference
                double oldestPrice = histo.FirstOrDefault()?.Average ?? 0;
                double newestPrice = histo.LastOrDefault()?.Average ?? 0;
                vm.PriceCards[i].Info.Prices = (oldestPrice, newestPrice);

                /// Sum total volume from historic
                vm.PriceCards[i].Info.VolumeToTotal = histo.Sum(h => h.volumeto);
                vm.PriceCards[i].Info.VolumeFromTotal = histo.Sum(h => h.volumefrom);
                double total = histo.Sum(h => h.volumeto);

                /// Show that loading is done
                vm.PriceCards[i].Info.IsLoading = false;
            } catch (Exception) {  }
            
        }

        /// #######################################################################################
        /// (Left/Right) Click handlers
        private void homeListView_Click(object sender, ItemClickEventArgs e) {
            /// Connected animation
            HomeGridView.PrepareConnectedAnimation("toCoinDetails", e.ClickedItem, "HomeGridView_Element");

            var card = ((HomeCard)e.ClickedItem);
            this.Frame.Navigate(typeof(CoinDetails), card);
        }

        private void UnfavCoin(object sender, RoutedEventArgs e) {
            string crypto = ((HomeCard)((FrameworkElement)sender).DataContext).Info.Name;
            RemoveCoinHome(crypto);
        }
        private async void PinUnpinCoin(object sender, RoutedEventArgs e) {
            var card = (HomeCard)((FrameworkElement)sender).DataContext;
            string crypto = card.Info.Name;

            var priceCard = vm.PriceCards.FirstOrDefault(c => c.Info.Name == crypto);
            if (priceCard == null)
                return;

            int i = vm.PriceCards.IndexOf(priceCard);
            bool success = false;
            if (card.Info.IsPin) {
                success = await LiveTileUpdater.RemoveSecondaryTileAction(crypto);
                vm.PriceCards[i].Info.IsPin = false;
                vm.InAppNotification($"Unpinned {crypto} from start screen.");
            }
            else {
                var grid = await LiveTileGenerator.SecondaryTileGridOperation(crypto);

                try {
                    RenderTargetBitmap rtb = new RenderTargetBitmap();
                    MainGrid.Children.Add(grid);
                    grid.Opacity = 0;
                    await rtb.RenderAsync(grid);
                    MainGrid.Children.Remove(grid);
                    var pixelBuffer = await rtb.GetPixelsAsync();
                    var pixels = pixelBuffer.ToArray();
                    var displayInformation = DisplayInformation.GetForCurrentView();
                    var file = await ApplicationData.Current.LocalFolder.CreateFileAsync($"tile-{crypto}.png",
                        CreationCollisionOption.ReplaceExisting);
                    using (var stream = await file.OpenAsync(FileAccessMode.ReadWrite)) {
                        var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
                        encoder.SetPixelData(BitmapPixelFormat.Bgra8,
                                             BitmapAlphaMode.Premultiplied,
                                             (uint)rtb.PixelWidth,
                                             (uint)rtb.PixelHeight,
                                             displayInformation.RawDpiX,
                                             displayInformation.RawDpiY,
                                             pixels);
                        await encoder.FlushAsync();
                    }
                }
                catch (Exception ex) {
                    var z = ex.Message;
                }


                success = await LiveTileUpdater.AddSecondaryTileAction(crypto);
                if (success) {
                    vm.PriceCards[i].Info.IsPin = true;
                    vm.InAppNotification($"Pinned {crypto} to start screen.");
                }
            }
        }

        /// #######################################################################################
        private async void TimeRangeButtons_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e) {
            timeSpan = ((UserControls.TimeRangeRadioButtons)sender)?.TimeSpan ?? timeSpan;
            (timeUnit, limit, aggregate) = GraphHelper.TimeSpanParser[timeSpan];

            await UpdatePage();
        }

        private void HomeGridView_DragCompleted(ListViewBase sender, DragItemsCompletedEventArgs args) {
            var cards = sender.ItemsSource as ObservableCollection<HomeCard>;
            App.pinnedCoins = cards.Select(x => x.Info.Name).ToList();

            /// Update pinnedCoin list
            App.UpdatePinnedCoins();
        }
    }
}

