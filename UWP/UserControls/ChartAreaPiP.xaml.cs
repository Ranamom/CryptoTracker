﻿using UWP.Models;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UWP.UserControls {
    public sealed partial class ChartAreaPiP : UserControl {
        public ChartAreaPiP() {
            this.InitializeComponent();
        }

        public static readonly DependencyProperty ChartModelProperty =
        DependencyProperty.Register(
            nameof(ChartModel),
            typeof(ChartModel),
            typeof(ChartAreaPiP),
            null);

        public ChartModel ChartModel {
            get => (ChartModel)GetValue(ChartModelProperty);
            set => SetValue(ChartModelProperty, value);
        }
    }
}
