using GameInterface.Cells;
using MCTProcon31Protocol.Methods;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MCTProcon31Protocol;
using GameInterface.ViewModels;
using static GameInterface.GameManagement.TeamColorUtil;
using MCTProcon31Protocol.Json.Matches;

namespace GameInterface.GameManagement
{
    public class GameData
    {
        public byte FinishTurn { get; set; } = 60;
        public int TimeLimitSeconds { get; set; } = 5;

        private MainWindowViewModel viewModel;
        private System.Random rand = new System.Random();
        //---------------------------------------
        //ViewModelと連動させるデータ(画面上に現れるデータ)
        private Cell[,] cellDataValue = null;
        public Cell[,] CellData
        {
            get => cellDataValue;
            private set
            {
                cellDataValue = value;
                viewModel.CellData = value;
            }
        }
        public Player[] Players { get; private set; } = null;

        //----------------------------------------
        public int SecondCount { get; set; }
        public bool IsGameStarted { get; set; } = false;
        public bool IsNextTurnStart { get; set; } = true;
        public bool IsAutoSkipTurn { get; set; }
        public bool IsEnableGameConduct { get; set; }
        public bool IsPause { get; set; }
        public int NowTurn { get; set; }
        public int BoardHeight { get; private set; }
        public int BoardWidth { get; private set; }
        public Agent SelectedAgent { get; set; }

        public int MaximumAgentsCount { get; set; } = 0;

        public GameSettings.SettingStructure CurrentGameSettings { get; set; }

        public GameData(MainWindowViewModel _viewModel)
        {
            viewModel = _viewModel;
            Players = new Player[App.PlayersCount];
            viewModel.Players = new PlayerWindowViewModel[Players.Length];
        }

        public Task<bool> InitGameData(GameSettings.SettingStructure settings)
        {
            IsEnableGameConduct = settings.IsEnableGameConduct;
            CurrentGameSettings = settings;
            SecondCount = 0;
            NowTurn = 1;

            return settings.BoardCreation switch
            {
                GameSettings.BoardCreation.Server => InitGameDataWithServer(settings),
                _ => Task.FromResult(InitGameDataWithRandom(settings))
            };
        }

        bool InitGameDataWithRandom(GameSettings.SettingStructure settings)
        {
            BoardHeight = settings.BoardHeight;
            BoardWidth = settings.BoardWidth;
            FinishTurn = settings.Turns;
            TimeLimitSeconds = settings.LimitTime;
            IsAutoSkipTurn = settings.IsAutoSkip;

            Players[0] = new Player();
            Players[1] = new Player();

            for (int i = 0; i < Players.Length; ++i)
                viewModel.Players[i] = new PlayerWindowViewModel(Players[i], i + 1);
            InitCellData(settings);
            InitAgents(settings);
            return true;
        }

        async Task<bool> InitGameDataWithServer(GameSettings.SettingStructure settings)
        {
            if (settings.Matches is null || !(0 <= settings.SelectedMatchIndex && settings.SelectedMatchIndex < settings.Matches.Length))
                throw new InvalidOperationException();
            var matchInfoResult = await GameSettings.WaitForServerDialog.ShowDialogEx(settings.ApiClient, settings.Matches[settings.SelectedMatchIndex]);
            if (!matchInfoResult.IsSuccess)
                return false;
            var matchInfo = matchInfoResult.Value;
            settings.Turns = (byte)matchInfo.Turn;
            settings.LimitTime = (ushort)(settings.Matches[settings.SelectedMatchIndex].OperationMilliseconds / 1000); // TODO:Improve
            settings.IsAutoSkip = true;
            settings.BoardHeight = (byte)matchInfo.Height;
            settings.BoardWidth = (byte)matchInfo.Width;
            settings.AgentsCount = matchInfo.Teams[0].AgentCount;

            BoardHeight = settings.BoardHeight;
            BoardWidth = settings.BoardWidth;
            FinishTurn = settings.Turns;
            TimeLimitSeconds = settings.LimitTime;
            IsAutoSkipTurn = settings.IsAutoSkip;

            Players[0] = new Player();
            Players[1] = new Player();

            for (int i = 0; i < Players.Length; ++i)
                viewModel.Players[i] = new PlayerWindowViewModel(Players[i], i + 1);

            SetCellData(settings, matchInfo);
            InitAgents(settings);
            return true;
        }

        void SetCellData(GameSettings.SettingStructure settings, Match matchInfo)
        {
            CellData = new Cell[matchInfo.Width, matchInfo.Height];
            for (int i = 0; i < CellData.GetLength(0); ++i)
                for (int j = 0; j < CellData.GetLength(1); ++j)
                    CellData[i, j] = new Cell(matchInfo.Areas[j, i]);
        }

        void InitCellData(GameSettings.SettingStructure settings)
        {
            if (settings.IsCreateRotate)
            {
                int randWidth = (BoardWidth + 1) / 2;
                CellData = new Cell[BoardWidth, BoardHeight];
                for (int i = 0; i < BoardWidth; i++)
                    for (int j = 0; j < BoardHeight; j++)
                        if (i < randWidth)
                            //40%の確率で値を0未満にする
                            CellData[i, j] = new Cell(rand.Next(1, 100) > 40 ? rand.Next(1, 16) : rand.Next(-16, 0));
                        else
                            //対称
                            CellData[i, j] = new Cell(CellData[i >= randWidth ? BoardWidth - 1 - i : i, BoardHeight - 1 - j].Score);
            }
            else
            {
                int randWidth, randHeight;
                randWidth = settings.IsCreateY ? (BoardWidth + 1) / 2 : BoardWidth;
                randHeight = settings.IsCreateX ? (BoardHeight + 1) / 2 : BoardHeight;

                CellData = new Cell[BoardWidth, BoardHeight];
                for (int i = 0; i < BoardWidth; i++)
                {
                    for (int j = 0; j < BoardHeight; j++)
                    {
                        if (i < randWidth && j < randHeight)
                        {
                            //40%の確率で値を0未満にする
                            if (rand.Next(1, 100) > 40)
                                CellData[i, j] = new Cell(rand.Next(1, 16));
                            else
                                CellData[i, j] = new Cell(rand.Next(-16, 0));
                        }
                        else
                            CellData[i, j] = new Cell(CellData[i >= randWidth ? BoardWidth - 1 - i : i, j >= randHeight ? BoardHeight - 1 - j : j].Score);
                    }
                }
            }
        }

        /// <summary>
        /// Symmetric agent placing, not used on 2020 rule.
        /// </summary>
        Point GenerateSymmetryPosition(Point p, Point boardSize, GameSettings.BoardSymmetry symmetry)
        {
            if(symmetry == GameSettings.BoardSymmetry.Rotate)
                return new Point( (byte)(boardSize.X - 1 - p.X), (byte)(boardSize.Y - 1 - p.Y));
            return new Point((symmetry & GameSettings.BoardSymmetry.Y) != 0 ? (byte)(boardSize.X - 1 - p.X) : p.X, (symmetry & GameSettings.BoardSymmetry.X) != 0 ? (byte)(boardSize.Y - 1 - p.Y) : p.Y);
        }

        void InitAgents(GameSettings.SettingStructure settings)
        {
            MaximumAgentsCount = settings.AgentsCount;
            for (int p = 0; p < Players.Length; p++)
            {
                Players[p].Agents = new Agent[settings.AgentsCount];
                viewModel.Players[p].AgentViewModels = new UserOrderPanelViewModel[settings.AgentsCount];
                for (int i = 0; i < Players[p].Agents.Length; ++i)
                {
                    Players[p].Agents[i] = new Agent(PlayerNum: p.ToTeamColor(), AgentNum: i, AgentsCount: settings.AgentsCount);
                    viewModel.Players[p].AgentViewModels[i] = new UserOrderPanelViewModel(Players[p].Agents[i]);
                }
            }
        }
    }
}
