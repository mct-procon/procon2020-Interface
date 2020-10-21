﻿using GameInterface.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GameInterface.Controls
{
    /// <summary>
    /// UserOrderPanel.xaml の相互作用ロジック
    /// </summary>
    public partial class UserOrderPanel : UserControl
    {
        public UserOrderPanel()
        {
            InitializeComponent();
        }

        private void PlaceCancelClicked(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is UserOrderPanelViewModel)
                ((UserOrderPanelViewModel)this.DataContext).Data.State = GameManagement.AgentState.NonPlaced;
        }
    }
}
