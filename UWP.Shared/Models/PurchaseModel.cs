﻿using CommunityToolkit.Mvvm.DependencyInjection;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using UWP.Core.Constants;
using UWP.Helpers;
using UWP.Services;
using UWP.Shared.Constants;
using Windows.UI;
using Windows.UI.Xaml.Media;

namespace UWP.Models {
    /// <summary>
    /// Had to not implement ObservableObject to make it serializable
    /// </summary>
    [DataContract()]
    [KnownType(typeof(PurchaseModel))]
    [KnownType(typeof(List<PurchaseModel>))]
    public class PurchaseModel : INotifyPropertyChanged {
        public PurchaseModel() { }

        /// <summary>
        /// DataMembers that are saved locally
        /// </summary>
        private string crypto;
        private string cryptoName;
        private string cryptoLogo;
        private double cryptoQty;

        [DataMember()]
        public string Crypto {
            get => crypto;
            set {
                SetProperty(ref crypto, value);
                CryptoLogo = IconsHelper.GetIcon(crypto);
            }
        }

        [DataMember()]
        public string CryptoName {
            get => cryptoName;
            set => SetProperty(ref cryptoName, value);
        }

        [DataMember()]
        public string CryptoLogo {
            get => cryptoLogo;
            set => SetProperty(ref cryptoLogo, value);
        }
        
        [DataMember()]
        public double CryptoQty {
            get => cryptoQty;
            set => SetProperty(ref cryptoQty, value);
        }

        /// #######################################################################################
        private string currency;
        public string Currency {
            get => currency ?? Ioc.Default.GetService<LocalSettings>().Get<string>(UserSettings.Currency);
            set {
                SetProperty(ref currency, value);
                CurrencySymbol = Currencies.GetCurrencySymbol(Currency);
            }
        }

        private string id = Guid.NewGuid().ToString("N");
        [DataMember()]
        public string Id {
            get => id;
            set => id = value;
        }

        private string type;
        [DataMember()]
        public string Type {
            get => string.IsNullOrEmpty(type) ? "Purchase" : type;
            set => type = value;
        }

        private double investedQty = 0;
        [DataMember()]
        public double InvestedQty {
            get => investedQty;
            set => SetProperty(ref investedQty, value);
        }

        private double transactionFee = 0;
        [DataMember()]
        public double TransactionFee {
            get => transactionFee;
            set => SetProperty(ref transactionFee, value);
        }

        /// #######################################################################################
        /// Dates, notes...
        private DateTimeOffset date = DateTime.Today;
        private string exchange = "";
        private string notes = "";

        [DataMember()]
        public DateTimeOffset Date {
            get => date;
            set => SetProperty(ref date, value);
        }
        
        [DataMember()]
        public string Exchange {
            get => exchange;
            set => SetProperty(ref exchange, value);
        }
        
        [DataMember()]
        public string Notes {
            get => notes;
            set => SetProperty(ref notes, value);
        }


        /// #######################################################################################
        /// <summary>
        /// Atrtibutes calculated on load
        /// </summary>
        private double delta = 0;
        public double Delta {
            get => delta;
            set => SetProperty(ref delta, value);
        }

        private double boughtAt = 0;
        public double BoughtAt {
            get => boughtAt;
            set => SetProperty(ref boughtAt, value);
        }

        private string currencySymbol;
        public string CurrencySymbol {
            get => currencySymbol ?? Currencies.GetCurrencySymbol(Currency);
            set => SetProperty(ref currencySymbol, value);
        }

        private bool isComplete = false;
        public bool IsComplete {
            get => isComplete;
            set => SetProperty(ref isComplete, value);
        }

        private DateTime lastUpdate = DateTime.Now;
        public DateTime LastUpdate {
            get => lastUpdate;
            set => SetProperty(ref lastUpdate, value);
        }

        private SolidColorBrush profitFG = new SolidColorBrush(Color.FromArgb(255, 128, 128, 128));
        public SolidColorBrush ProfitFG {
            get => profitFG;
            set => SetProperty(ref profitFG, value);
        }

        private double current = 0;
        public double Current {
            get => current;
            set => SetProperty(ref current, value);
        }

        private double profit = 0;
        public double Profit {
            get => profit;
            set => SetProperty(ref profit, value);
        }

        private double worth = 0;
        public double Worth {
            get => worth;
            set => SetProperty(ref worth, value);
        }

        private int groupedQty = 1;
        public int GroupedQty {
            get => groupedQty;
            set => SetProperty(ref groupedQty, value);
        }


        /// #######################################################################################
        /// <summary>
        /// Similar SetProperty to that of the WCT's MVVM approach
        /// </summary>
        private void SetProperty<T>(ref T field, T newValue, [CallerMemberName] string propertyName = null) {
            if (newValue == null)
                return;
            if (field == null || !newValue.Equals(field)) {
                field = newValue;
                NotifyPropertyChanged(propertyName);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged([CallerMemberName] string propertyName = "") {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
