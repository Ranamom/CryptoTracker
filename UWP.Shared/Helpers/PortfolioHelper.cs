﻿using Microsoft.Toolkit.Mvvm.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UWP.Core.Constants;
using UWP.Helpers;
using UWP.Models;
using UWP.Services;
using UWP.Shared.Constants;

namespace UWP.Shared.Helpers {
    public class PortfolioHelper {

        public async static Task<List<PurchaseModel>> GetPortfolio(string filterCrypto = "") {
            var portfolio = await LocalStorageHelper.ReadObject<List<PurchaseModel>>(UserStorage.Portfolio);

            if (filterCrypto != "")
                portfolio = portfolio.Where(p => p.Crypto == filterCrypto).ToList();

            return (portfolio.Count == 0) ? new List<PurchaseModel>() : portfolio;
        }

        public async static void AddPurchase(PurchaseModel purchase) {
            var portfolio = await LocalStorageHelper.ReadObject<List<PurchaseModel>>(UserStorage.Portfolio);
            portfolio.Add(purchase);
            LocalStorageHelper.SaveObject(UserStorage.Portfolio, portfolio);
        }

        public async static Task<PurchaseModel> UpdatePurchase(PurchaseModel purchase) {
            string crypto = purchase.Crypto;

            if (purchase.Current <= 0 || (DateTime.Now - purchase.LastUpdate).TotalSeconds > 20)
                purchase.Current = await Ioc.Default.GetService<ICryptoCompare>().GetPrice_Extension(
                    purchase.Crypto, purchase.Currency);

            var curr = purchase.Current;
            purchase.Worth = Math.Round(curr * purchase.CryptoQty, 2);

            /// If the user has also filled the invested quantity, we can calculate everything else
            if (purchase.InvestedQty >= 0) {
                double priceBought = (1 / purchase.CryptoQty) * purchase.InvestedQty;
                priceBought = Math.Round(priceBought, 4);

                var diff = Math.Round((curr - priceBought) * purchase.CryptoQty, 4);
                
                purchase.Delta = Math.Round(100 * diff / purchase.InvestedQty, 2);
                purchase.BoughtAt = priceBought;
                if (purchase.Delta > 100)
                    purchase.Delta -= 100;
                purchase.Profit = Math.Round(diff, 2);
                purchase.ProfitFG = (diff < 0) ?
                    ColorConstants.GetBrush("pastel_red") :
                    ColorConstants.GetBrush("pastel_green");
            }
            if (purchase.InvestedQty == 0)
                purchase.Delta = 0;

            return purchase;
        }
    }
}
