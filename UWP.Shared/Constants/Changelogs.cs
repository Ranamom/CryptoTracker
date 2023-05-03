﻿using System.Collections.Generic;
using System.Linq;

namespace UWP.Core.Constants {
    /// <summary>
	/// Class with the changelogs of the latest versions
	/// </summary>
    public class Changelogs {

		public static readonly Dictionary<string, List<string>> LatestChangelogs = new Dictionary<string, List<string>>(){
            {
                "6.5.0",
                new List<string>() {
                    "Coin details: new toggle for vertical axis",
                    "General: Improved historic chart accuracy by removing outlier data points"
                }
            },
            {
                "6.4.0",
                new List<string>() {
                    "General: added 4 days and 2 year time spans in charts",
                    "Coin details: new optional horizontal and vertical annotations (for purchase dates and alert prices)",
					"General: bug fixes"
                }
            },
            {
				"6.3.X",
				new List<string>() {
					"General: more custom crypto assetes and colors",
					"General: bug fixes",
					"General: improved decimal commas/dots",
					"Coin details: added total invested values",
					"Portfolio: auto-refresh each 2 mins",
					"Settings: new minimal appearance",
				}
			},
			{
				"6.3.0",
				new List<string>() {
					"Purchases: add transaction fees",
					"Portfolio: new private mode",
					"Portfolio: new sorting mechanism",
					"Settings: Windows 11 styled",
					"Settings: startup task",
				}
            },
			{
				"6.2.0",
				new List<string>() {
					"Coin details: Added sort button",
					"Coin details: Added 'Buy price' column",
					"Settings: new layout + close button"
				}
			},
			{
				"6.1.X",
				new List<string>() {
					"General: bug fixes"
				}
			},
			{
				"6.1.0",
				new List<string>() {
					"General: added CZK currency",
					"General: fixed auto-refresh",
					"Settings: set the chart's default timespan and optional Back button",
					"Home: adaptive layout",
					"News: removed AmbCrypto (it crashed)",
					"Portfolio: fixed rounding errors with ROI",
				}
			},
			{
				"6.0.X",
				new List<string>() {
					"General: fixed casting error on startup",
					"Coins: SHIB and other alt-coins' empty graphs fixed",
					"Settings: set the currency to all purchases",
					"Portfolio: fixed rounding errors",
					"Portfolio: group button shown only when multiple same coin entries exist",
					"Portfolio: fixed error saving the Portfolio",
				}
			},
			{
				"6.0.0",
				new List<string>(){
					"Refreshed UI inline with Windows 11",
					"New volume and candle charts",
					"New price alerts",
					"Import/Export portfolio backups",
					"More granular time filters (1h/4h/1d...)",
					"Pin any coin as a Live Tile (Windows 10)",
					"Configurable auto-refresh and startup page",
					"Support for Jump Lists",
					"New Compact Overlay",
					"Portfolio: diversification ratios, group or duplicate entries, add notes and change purchases' currencies"
				}
			}
		};

		public static readonly List<string> CurrentChangelog = LatestChangelogs[LatestChangelogs.FirstOrDefault().Key];
		public static readonly List<string> MajorChangelog = LatestChangelogs["6.5.0"];

		public static string FormatChangelog(List<string> changelog) {
			string message = "";
			foreach (var change in changelog)
				message += $"  • {change} \n";
			return message.TrimEnd('\n');
		}
	}
}
