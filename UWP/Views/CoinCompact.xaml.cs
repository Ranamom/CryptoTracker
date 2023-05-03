﻿using CommunityToolkit.Mvvm.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using UWP.Core.Constants;
using UWP.Helpers;
using UWP.Models;
using UWP.Services;
using UWP.Shared.Constants;
using UWP.UserControls;
using UWP.ViewModels;
using Windows.System.Threading;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;


namespace UWP.Views {
    public sealed partial class CoinCompact : Page {
		/// Variables to get historic
		private static int limit = 168;
		private static int aggregate = 1;
		private static string timeSpan = "4h";
		private static string timeUnit = "minute";

		private List<string> timeSpans = new List<string>{"1h", "4h", "1d"};

		/// Timer for auto-refresh
		private static ThreadPoolTimer PricePeriodicTimer;
		private static ThreadPoolTimer ChartPeriodicTimer;

		public CoinCompact() {
			this.InitializeComponent();
		}

		protected override void OnNavigatingFrom(NavigatingCancelEventArgs e) {
			vm.Chart.ChartStroke = ColorConstants.GetCoinBrush(vm.Info.Name);
		}

        protected override void OnNavigatedTo(NavigationEventArgs e) {
			var type = (e.Parameter.GetType()).Name;

			var coinDetailsVM = (CoinDetailsViewModel)e.Parameter;
			vm.CoinDetailsVM = coinDetailsVM;
			vm.Chart = coinDetailsVM.Chart;
			vm.Info = coinDetailsVM.Coin;
			vm.Chart.TimeSpan = vm.Chart.TimeSpan;
			if (!timeSpans.Contains(vm.Chart.TimeSpan)) {
				(timeUnit, limit, aggregate) = GraphHelper.TimeSpanParser[timeSpan];
				vm.Chart.TimeSpan = timeSpan;
				UpdateValues();
			}
			else
				timeSpan = vm.Chart.TimeSpan;


			/// Create the auto-refresh timer
			var autoRefresh = App._LocalSettings.Get<string>(UserSettings.AutoRefresh);
			TimeSpan period;
			if (autoRefresh != "None") {
				switch (autoRefresh) {
					case "30 sec":
					case "1 min":
						period = TimeSpan.FromSeconds(60);
						break;
					case "2 min":
						period = TimeSpan.FromSeconds(120);
						break;
				}
				PricePeriodicTimer = ThreadPoolTimer.CreatePeriodicTimer(async (source) => {
					await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
						UpdatePrice();
					});
				}, TimeSpan.FromSeconds(30));

				ChartPeriodicTimer = ThreadPoolTimer.CreatePeriodicTimer(async (source) => {
					await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
						TimeRangeButtons_Tapped(null, null);
					});
				}, period);
			}
		}

		private void Page_Unloaded(object sender, RoutedEventArgs e) {
			ChartPeriodicTimer?.Cancel();
			PricePeriodicTimer?.Cancel();
		}

		/// #########################################################################################
		private async void UpdatePrice()
			=> vm.Info.Price = await Ioc.Default.GetService<ICryptoCompare>().GetPrice_Extension(
				vm.Info.Name, App.currency);
		
		private async void UpdateValues() {
			var crypto = vm.Info.Name;

			/// Get historic values
			var histo = await Ioc.Default.GetService<ICryptoCompare>().GetHistoric_(crypto, timeUnit, limit, aggregate);

			var chartData = new List<ChartPoint>();
			foreach (var h in histo)
				chartData.Add(new ChartPoint() {
					Date = h.DateTime,
					Value = h.Average
				});
			vm.Chart.ChartData = chartData;

			/// Calculate diff based on historic prices
			double oldestPrice = histo.FirstOrDefault()?.Average ?? 0;
			double newestPrice = histo.LastOrDefault()?.Average ?? 0;
			vm.Info.Prices = (oldestPrice, newestPrice);

			var brush = (vm.Info.Diff > 0) ?
				(SolidColorBrush)Application.Current.Resources["pastelGreen"] :
				(SolidColorBrush)Application.Current.Resources["pastelRed"];

			vm.Chart.ChartStroke = brush;

			var MinMax = GraphHelper.GetMinMaxOfArray(chartData.Select(d => d.Value).ToList());
			vm.Chart.PricesMinMax = GraphHelper.OffsetMinMaxForChart(MinMax.Min, MinMax.Max, 0.25);
		}

        private async void FullScreen_btn_click(object sender, RoutedEventArgs e) {
			var view = ApplicationView.GetForCurrentView();

			await view.TryEnterViewModeAsync(ApplicationViewMode.Default);
			Frame.Navigate(typeof(CoinDetails), vm.CoinDetailsVM);
		}

        private void TimeRangeButtons_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e) {
			if (sender != null)
				timeSpan = ((TimeRangeRadioButtons)sender).TimeSpan;

			(timeUnit, limit, aggregate) = GraphHelper.TimeSpanParser[timeSpan];
			vm.Chart.TimeSpan = timeSpan;

			UpdateValues();
		}
    }
}
