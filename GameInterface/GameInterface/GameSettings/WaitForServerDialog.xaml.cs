﻿using MCTProcon31Protocol.Json;
using MCTProcon31Protocol.Json.Matches;
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
        public APIResult<Match> Result { get; set; } = default;
        public double CurrentRetryAfter { get; set; } = 0;
        public ProconAPIClient Client { get; set; }
        public MatchInformation SelectedMatch { get; set; }
        public WaitForServerDialog()
        {
            InitializeComponent();
        }
        public WaitForServerDialog(ProconAPIClient client, APIResult<Match> previousResult, MatchInformation selMatch) : this()
        {
            this.Result = previousResult;
            this.Client = client;
            this.SelectedMatch = selMatch;
        }

        public async static Task<APIResult<Match>> ShowDialogEx(ProconAPIClient client, MatchInformation match)
        {
            var data = await client.Match(match);
            if(data.IsSuccess)
                return data;
            else
            {
                if (data.HTTPReturnCode != 425)
                {
                    MessageBox.Show(data.HTTPReturnCode.ToString(), "Connection Failed", MessageBoxButton.OK, MessageBoxImage.Information);
                    return data;
                }
                else
                {
                    WaitForServerDialog dig = new WaitForServerDialog(client, data, match);
                    if (dig.ShowDialog() == true)
                    {
                        dig.Client = null;
                        return dig.Result;
                    }
                    dig.Client = null;
                    return data;
                }
            }
        }

        private async void Update()
        {
            this.CurrentRetryAfter -= 0.3;
            if (this.CurrentRetryAfter < 0) this.CurrentRetryAfter = 0;
            Progress.Value = this.Result.RetryAfter - this.CurrentRetryAfter;
            ProgressText.Text = $"残り時間：{this.CurrentRetryAfter:F2}秒";
            if (this.CurrentRetryAfter == 0)
            {
                this.Result = await Client.Match(SelectedMatch);
                if (Result.IsSuccess)
                {
                    timer.Stop();
                    DialogResult = true;
                }
                else if (Result.HTTPReturnCode != 425)
                {
                    timer.Stop();
                    MessageBox.Show("HTTP Connection error.\nError Code: " + Result.HTTPReturnCode.ToString(), "Connection Failed", MessageBoxButton.OK, MessageBoxImage.Information);
                    DialogResult = false;
                }
                else
                {
                    Progress.Maximum = this.Result.RetryAfter;
                    Progress.Value = 0;
                    ProgressText.Text = $"残り時間：{this.Result.RetryAfter:F2}秒";
                }
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.Client == null || this.SelectedMatch == null || this.Result.RetryAfter <= 0) return;
            Progress.Maximum = this.Result.RetryAfter;
            this.CurrentRetryAfter = this.Result.RetryAfter;
            Progress.Value = 0;
            ProgressText.Text = $"残り時間：{this.Result.RetryAfter:F2}秒";
            timer.Tick += (s, ee) => Update();
            timer.Interval = TimeSpan.FromMilliseconds(300);
            timer.Start();
        }
    }
}
