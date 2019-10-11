using System;
using System.Collections.Generic;
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
using System.Windows.Threading;

namespace GameInterface.GameSettings
{
    /// <summary>
    /// WaitForServerDialog.xaml の相互作用ロジック
    /// </summary>
    public partial class WaitForServerDialog : Window
    {
        DispatcherTimer timer { get; set; } = new DispatcherTimer();
        public WaitForServerDialog()
        {
            InitializeComponent();
        }

        public async static Task<bool> ShowDialogEx()
        {
            if(await Network.ProconAPIClient.Instance.GetState())
            {
                return true;
            }
            else
            {
                if (Network.ProconAPIClient.Instance.LastError.StartAt == null)
                {
                    MessageBox.Show(Network.ProconAPIClient.Instance.LastError.Status, "Connection Failed", MessageBoxButton.OK, MessageBoxImage.Information);
                    return false;
                }
                else
                {
                    WaitForServerDialog dig = new WaitForServerDialog();
                    if(dig.ShowDialog() == true)
                        return true;
                    return false;
                }
            }
        }

        private async void Update()
        {
            Progress.Value = (Network.ProconAPIClient.Instance.LastError.StartAt.Value - System.DateTime.Now).TotalMilliseconds;
            ProgressText.Text = $"ゲーム開始時間：{Network.ProconAPIClient.Instance.LastError.StartAt.Value} 残り時間：{(Network.ProconAPIClient.Instance.LastError.StartAt.Value - System.DateTime.Now).Seconds}秒";
            if (Network.ProconAPIClient.Instance.LastError.StartAt.Value <= System.DateTime.Now)
            {
                if (await Network.ProconAPIClient.Instance.GetState())
                    DialogResult = true;
                else if (Network.ProconAPIClient.Instance.LastError.StartAt == null)
                {
                    MessageBox.Show(Network.ProconAPIClient.Instance.LastError.Status, "Connection Failed", MessageBoxButton.OK, MessageBoxImage.Information);
                    DialogResult = false;
                }
                else
                {
                    Progress.Maximum = (Network.ProconAPIClient.Instance.LastError.StartAt.Value - System.DateTime.Now).TotalMilliseconds;
                    Progress.Value = 0;
                    ProgressText.Text = $"ゲーム開始時間：{Network.ProconAPIClient.Instance.LastError.StartAt.Value} 残り時間：{(Network.ProconAPIClient.Instance.LastError.StartAt.Value - System.DateTime.Now).Seconds}秒";
                }
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Progress.Maximum = (Network.ProconAPIClient.Instance.LastError.StartAt.Value - System.DateTime.Now).TotalMilliseconds;
            Progress.Value = 0;
            ProgressText.Text = $"ゲーム開始時間：{Network.ProconAPIClient.Instance.LastError.StartAt.Value} 残り時間：{(Network.ProconAPIClient.Instance.LastError.StartAt.Value - System.DateTime.Now).Seconds}秒";
            timer.Tick += (s, ee) => Update();
            timer.Interval = TimeSpan.FromMilliseconds(300);
            timer.Start();
        }
    }
}
