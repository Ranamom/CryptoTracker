﻿using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Microsoft.Extensions.DependencyInjection;
using CommunityToolkit.Mvvm.DependencyInjection;
using Refit;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;
using UWP.Core.Constants;
using UWP.Helpers;
using UWP.Services;
using UWP.Views;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.System.UserProfile;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.StartScreen;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace UWP {
    sealed partial class App : Application {
        /// <summary>
        /// Efficient socket usage
        /// https://www.aspnetmonsters.com/2016/08/2016-08-27-httpclientwrong/
        /// </summary>
        internal static HttpClient Client = new HttpClient();

        internal static string currency;
        internal static string currencySymbol;

        internal static LocalSettings _LocalSettings = new LocalSettings();
        internal static string CurrentPage = "";
        internal static List<CoinPaprikaCoin> coinListPaprika = new List<CoinPaprikaCoin>();
        internal static List<CoinGeckoCoin> coinListGecko = new List<CoinGeckoCoin>();
        internal static List<string> pinnedCoins;

        internal static CultureInfo UserCulture = new CultureInfo(GlobalizationPreferences.Languages[0]);

        public App() {
            currency = _LocalSettings.Get<string>(UserSettings.Currency);
            currencySymbol = _LocalSettings.Get<string>(UserSettings.CurrencySymbol);
            string _theme = _LocalSettings.Get<string>(UserSettings.Theme);
            string _pinned = _LocalSettings.Get<string>(UserSettings.PinnedCoins);

            pinnedCoins = new List<string>(_pinned.Split(new char[] { '|' }));
            pinnedCoins.Remove("");

            switch (_theme) {
                case "Light":
                    RequestedTheme = ApplicationTheme.Light;
                    break;
                case "Dark":
                    RequestedTheme = ApplicationTheme.Dark;
                    break;
                default:
                    RequestedTheme = (new UISettings().GetColorValue(UIColorType.Background) == Colors.Black) ?
                        ApplicationTheme.Dark : ApplicationTheme.Light;
                    break;
            }

            /// Register services
            Ioc.Default.ConfigureServices(
                new ServiceCollection()
                .AddSingleton(RestService.For<ICryptoCompare>("https://min-api.cryptocompare.com/"))
                .AddSingleton(RestService.For<ICoinGecko>("https://api.coingecko.com/api/v3/"))
                .AddSingleton(RestService.For<ICoinPaprika>("https://api.coinpaprika.com/"))
                .AddSingleton(RestService.For<IGithub>("https://raw.githubusercontent.com/ismaelestalayo/CryptoTracker/API/"))
                .AddTransient<LocalSettings>()
                .BuildServiceProvider());


            this.InitializeComponent();
            this.Suspending += OnSuspending;
            this.UnhandledException += OnUnhandledException;
        }
        // #########################################################################################
        protected override void OnLaunched(LaunchActivatedEventArgs e) {
            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content
            if (rootFrame == null) {
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated) {
                    //TODO: Cargar el estado de la aplicación suspendida previamente
                }

                Window.Current.Content = rootFrame;
            }

            if (e.PrelaunchActivated == false) {
                Windows.ApplicationModel.Core.CoreApplication.EnablePrelaunch(true);

                // Navigate to the root page if one isn't loaded already
                if (rootFrame.Content == null)
                    rootFrame.Navigate(typeof(MainPage), e.Arguments);

                ApplicationView.GetForCurrentView().SetPreferredMinSize(new Windows.Foundation.Size(900, 550));
                Window.Current.Activate();
#if !DEBUG
            AppCenter.Start("37e61258-8639-47d6-9f6a-d47d54cd8ad5", typeof(Analytics), typeof(Crashes));
#endif
            }
            SetJumpList();
        }

        protected override void OnActivated(IActivatedEventArgs args) {
            if (args.Kind == ActivationKind.ToastNotification) {
                var toastArgs = args as ToastNotificationActivatedEventArgs;
                var arg = toastArgs.Argument;

                if (!string.IsNullOrEmpty(arg)) {
                    Frame rootFrame = Window.Current.Content as Frame;
                    if (rootFrame == null) {
                        rootFrame = new Frame();
                        Window.Current.Content = rootFrame;
                    }
                    rootFrame.Navigate(typeof(MainPage), arg);
                    Window.Current.Activate();
                }
            }
            else {
                Frame rootFrame = Window.Current.Content as Frame;
                if (rootFrame == null) {
                    rootFrame = new Frame();
                    Window.Current.Content = rootFrame;
                }
                rootFrame.Navigate(typeof(MainPage));
                Window.Current.Activate();
            }
        }

        void OnNavigationFailed(object sender, NavigationFailedEventArgs e) {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }
        private void OnSuspending(object sender, SuspendingEventArgs e) {
            var deferral = e.SuspendingOperation.GetDeferral();

            deferral.Complete();
        }
        private async void OnUnhandledException(object sender, Windows.UI.Xaml.UnhandledExceptionEventArgs e) {
            e.Handled = true;
            Analytics.TrackEvent($"UNHANDLED-{CurrentPage}",
                new Dictionary<string, string>() { { "Exception", e.Message } });

            var messageDialog = new MessageDialog(e.Message, "An error occurred");
            await messageDialog.ShowAsync();
        }

        // ###############################################################################################
        /// Set the JumpList of the app
        private async Task SetJumpList() {
            if (!JumpList.IsSupported())
                return;

            try {
                var jumpList = await JumpList.LoadCurrentAsync();
                jumpList.Items.Clear();

                JumpListItem taskItem;
                taskItem = JumpListItem.CreateWithArguments("/News", "News");
                taskItem.Description = "See the latest news.";
                taskItem.DisplayName = "News";
                taskItem.Logo = new Uri("ms-appx:///Assets/Icons/News.png");
                jumpList.Items.Add(taskItem);

                taskItem = JumpListItem.CreateWithArguments("/Portfolio", "Portfolio");
                taskItem.Description = "Check your crypto-portfolio.";
                taskItem.DisplayName = "Portfolio";
                taskItem.Logo = new Uri("ms-appx:///Assets/Icons/Portfolio.png");
                jumpList.Items.Add(taskItem);

                await jumpList.SaveAsync();
            } catch (SystemException ex) {
                Analytics.TrackEvent("Jumplist-errorCatched",
                    new Dictionary<string, string>() { { "Exception", ex.Message } });
            } catch (Exception ex) {
                Analytics.TrackEvent($"Jumplist-error",
                    new Dictionary<string, string>() { { "Exception", ex.Message } });
            }
        }

        internal static void UpdatePinnedCoins() {
            if (App.pinnedCoins.Count > 0) { 
                string s = "";
                foreach (var item in App.pinnedCoins) {
                    s += item + "|";
                }
                s = s.Remove(s.Length - 1);
                App._LocalSettings.Set(UserSettings.PinnedCoins, s);
            }
        }

        // ###############################################################################################
        internal async static Task GetCoinList() {
            // check cache before sending an unnecesary request
            var date = _LocalSettings.Get<double>(UserSettings.CoinListsDate);
            
            DateTime lastUpdate = DateTime.FromOADate((double)date);
            var days = DateTime.Today.CompareTo(lastUpdate);

            coinListPaprika = await LocalStorageHelper.ReadObject<List<CoinPaprikaCoin>>(UserStorage.CacheCoinPaprika);
            coinListGecko = await LocalStorageHelper.ReadObject<List<CoinGeckoCoin>>(UserStorage.CacheCoinGecko);

            // if empty list OR old cache -> refresh
            if (coinListPaprika.Count == 0 || coinListGecko.Count == 0 || days > 7) {
                coinListPaprika = await Ioc.Default.GetService<ICoinPaprika>().GetCoinList_();
                coinListGecko = await Ioc.Default.GetService<ICoinGecko>().GetCoinList_();

                _LocalSettings.Set(UserSettings.CoinListsDate, DateTime.Today.ToOADate());
                await LocalStorageHelper.SaveObject(UserStorage.CacheCoinPaprika, coinListPaprika);
                await LocalStorageHelper.SaveObject(UserStorage.CacheCoinGecko, coinListGecko);
            }

        }

        
        // ###############################################################################################
        //  (GET) coin description
        internal static async Task<string> GetCoinDescription(string crypto, int lines = 5) {
            String URL = string.Format("https://krausefx.github.io/crypto-summaries/coins/{0}-{1}.txt", crypto.ToLower(), lines);
            Uri uri = new Uri(URL);

            try {
                string data = await GetStringAsync(uri);
                return data;

            } catch (Exception) {
                return "No description found for this coin.";
            }
        }

        /// ###############################################################################################
        /// <summary>
        /// do NOT mess with async methods...
        /// 
        /// Thank god I found this article (thanks Stephen Cleary)
        /// http://blog.stephencleary.com/2012/07/dont-block-on-async-code.html
        /// 
        /// </summary>
        // TODO: removing ConfigureAwait breaks everything... why?
        internal static async Task<string> GetStringAsync(Uri uri) {
            return await Client.GetStringAsync(uri).ConfigureAwait(false);
        }
        internal static async Task<string> GetStringFromUrlAsync(string url) {
            return await Client.GetStringAsync(new Uri(url)).ConfigureAwait(false);

        }
    }

}

