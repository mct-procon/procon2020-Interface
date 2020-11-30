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
using GameInterface.GameManagement;

namespace GameInterface.GameSettings
{
    /// <summary>
    /// GameSettingDialog.xaml の相互作用ロジック
    /// </summary>
    public partial class GameSettingDialog : Window
    {
        new SettingStructure DataContext { get; set; }

        Random randomer = new Random();

        public GameSettingDialog()
        {
            DataContext = new SettingStructure();
            base.DataContext = DataContext;
            InitializeComponent();
        }

        internal static bool ShowDialog(out SettingStructure setting)
        {
            GameSettingDialog dialog = new GameSettingDialog();
            dialog.ShowDialog();
            if (dialog.DialogResult == true)
            {
                setting = dialog.DataContext;
                return true;
            }
            else
            {
                setting = null;
                return false;
            }
        }

        private void P1UserToggle_Checked(object sender, RoutedEventArgs e) => DataContext.Port1P = 0;
        private void P1AIToggle_Checked(object sender, RoutedEventArgs e) => Set1PPort();

        private void Set1PPort()
        {
            if (!this.IsInitialized) return;
            P1PortBox.Background = new SolidColorBrush(Colors.White);
            P1PortBoxErrorMessage.Text = "";
            P1PortBoxErrorMessage.Visibility = Visibility.Collapsed;
            if(ushort.TryParse(P1PortBox.Text, out var number))
            {
                if (number <= 9999)
                    ErrorSet(P1PortBox, P1PortBoxErrorMessage, "ポート番号は10000以上でなければなりません.");
                else if(number == DataContext.Port2P)
                    ErrorSet(P1PortBox, P1PortBoxErrorMessage, "2Pのポート番号と異なる番号を指定する必要があります.");
                DataContext.Port1P = number;
            }
            else
                ErrorSet(P1PortBox, P1PortBoxErrorMessage, "10000～65535の整数を入力してください．");
        }

        private void P2UserToggle_Checked(object sender, RoutedEventArgs e) => DataContext.Port2P = 0;
        private void P2AIToggle_Checked(object sender, RoutedEventArgs e) => Set2PPort();

        private void Set2PPort()
        {
            if (!this.IsInitialized) return;
            P2PortBox.Background = new SolidColorBrush(Colors.White);
            P2PortBoxErrorMessage.Text = "";
            P2PortBoxErrorMessage.Visibility = Visibility.Collapsed;
            if (ushort.TryParse(P2PortBox.Text, out var number))
            {
                if (number <= 9999)
                    ErrorSet(P2PortBox, P2PortBoxErrorMessage, "ポート番号は10000以上でなければなりません.");
                else if (number == DataContext.Port1P)
                    ErrorSet(P2PortBox, P2PortBoxErrorMessage, "1Pのポート番号と異なる番号を指定する必要があります.");
                DataContext.Port2P = number;
            }
            else
                ErrorSet(P2PortBox, P2PortBoxErrorMessage, "10000～65535の整数を入力してください．");
        }

        private void ErrorSet(TextBox portBox, TextBlock messageBlock, string message)
        {
            portBox.Background = new SolidColorBrush(Colors.LightPink);
            messageBlock.Text = message;
            messageBlock.Visibility = Visibility.Visible;
        }

        private void P1PortBox_TextChanged(object sender, TextChangedEventArgs e) => Set1PPort();
        private void P2PortBox_TextChanged(object sender, TextChangedEventArgs e) => Set2PPort();

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            switch (DataContext.BoardCreation)
            {
                case BoardCreation.Server:
                    if(DataContext.Matches is null)
                    {
                        MessageBox.Show("試合情報を取得して，試合を選択しましたか？", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }
                    if (DataContext.SelectedMatchIndex < 0 || DataContext.SelectedMatchIndex >= DataContext.Matches.Length)
                    {
                        MessageBox.Show("試合IDを正しく選択してください．", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }
                    break;
                default:
                    DataContext.DeleteClient();
                    break;
            }
            DialogResult = true;
            Close();
        }

        private void WidthBox_RandomButton_Click(object sender, RoutedEventArgs e) => DataContext.BoardWidth = (byte)randomer.Next(4, 13);
        private void HeightBox_RandomButton_Click(object sender, RoutedEventArgs e) => DataContext.BoardHeight = (byte)randomer.Next(4, 13);
        private void AgentBox_RandomButton_Click(object sender, RoutedEventArgs e) => DataContext.AgentsCount = randomer.Next(2, 8);

        private async void GetBattles(object sender, RoutedEventArgs e)
        {
            try
            {
                DataContext.CreateClient();
                var me = await DataContext.ApiClient.Me();
                if (!me.IsSuccess)
                {
                    MessageBox.Show("Failed to get team information.", "Error Occured!", MessageBoxButton.OK);
                    return;
                }
                var matches = await DataContext.ApiClient.TeamMatches(me.Value);
                if (matches.IsSuccess)
                    DataContext.Matches = matches.Value._value;
                else
                    MessageBox.Show("Http Action failed.\nStatus Code:" + matches.HTTPReturnCode.ToString(), "Error Occured!", MessageBoxButton.OK);
            }
            catch(Exception ex)
            {
                MessageBox.Show($"Exception is thrown.\n{ex}", "Error Occured!", MessageBoxButton.OK);
            }
        }
    }
}
