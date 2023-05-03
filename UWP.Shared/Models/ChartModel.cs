﻿using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using Windows.UI;
using Windows.UI.Xaml.Media;

namespace UWP.Models {
    public partial class ChartModel : ObservableObject {
        /// <summary>
        /// List of ChartPoints containing the values for the chart
        /// </summary>
        [ObservableProperty]
        private List<ChartPoint> chartData = new List<ChartPoint>();


        /// <summary>
        /// Tuple with the minimum and maximum value to adjust the chart
        /// </summary>
        [ObservableProperty]
        private (double Min, double Max) pricesMinMax = (0, 100);


        [ObservableProperty]
        private double volumeMax = 0;

        
        [ObservableProperty]
        private bool isLoading = false;


        /// <summary>
        /// Stroke to paint the charts
        /// </summary>
        [ObservableProperty]
        private SolidColorBrush chartStroke = new SolidColorBrush(Color.FromArgb(224, 127, 127, 127));


        /// <summary>
        /// Attributes to adjust the axis to the plotted time interval
        /// </summary>
        [ObservableProperty]
        private ChartStyling chartStyling = new ChartStyling();


        [ObservableProperty]
        private DateTime minimum = DateTime.Now.AddHours(-1);


        /// <summary>
        /// 
        /// </summary>
        [ObservableProperty]
        private string timeSpan = "1w";
    }
}
