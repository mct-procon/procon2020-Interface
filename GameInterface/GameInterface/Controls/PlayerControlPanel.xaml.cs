using MCTProcon31Protocol;
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
using System.Windows.Shapes;

namespace GameInterface.Controls
{
    /// <summary>
    /// PlayerControlPanel.xaml の相互作用ロジック
    /// </summary>
    public partial class PlayerControlPanel : Window
    {
        GameManagement.GameManager gameManager { get; set; }

        private ViewModels.PlayerWindowViewModel vm = null;
        public new ViewModels.PlayerWindowViewModel DataContext {
            get => vm;
            set {
                vm = value;
                base.DataContext = value;
            }
        }

        public PlayerControlPanel(GameManagement.GameManager gameMan, ViewModels.PlayerWindowViewModel viewModel)
        {
            InitializeComponent();
            gameManager = gameMan;
            DataContext = viewModel;

            MainGrid.Background = new SolidColorBrush(viewModel.PlayerNum == 1 ? Colors.Blue : Colors.Red);

            MainGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });
            if (viewModel.AgentViewModels.Length >= 3)
            {
                MainGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });
                this.Height *= 2;
            }
            if (viewModel.AgentViewModels.Length >= 6)
            {
                MainGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });
                this.Height = this.Height * 3 / 2;
            }
            int i = 0;
            for (; i < viewModel.AgentViewModels.Length; ++i)
            {
                var orderButtonUserControl = new Controls.UserOrderPanel() { DataContext = viewModel.AgentViewModels[i] };
                MainGrid.Children.Add(orderButtonUserControl);
                Grid.SetRow(orderButtonUserControl, (i / 3) + 1);
                Grid.SetColumn(orderButtonUserControl, i % 3);
            }
            Grid.SetRow(Decisions, (i / 3) + 1);
            Grid.SetColumn(Decisions, i % 3);
        }

        private void Decisions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var decided = (Decision)((ListBox)sender).SelectedItem;
            if (decided == null) return;
            gameManager.SetDecision(DataContext.PlayerNum-1, decided);
        }
    }
}
