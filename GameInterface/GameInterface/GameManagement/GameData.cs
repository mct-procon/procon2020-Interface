using GameInterface.Cells;
using MCTProcon30Protocol.Methods;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MCTProcon30Protocol;
using GameInterface.ViewModels;

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

        public int AgentsCount { get; set; } = 0;

        public GameSettings.SettingStructure CurrentGameSettings { get; set; }

        public GameData(MainWindowViewModel _viewModel)
        {
            viewModel = _viewModel;
            Players = new Player[App.PlayersCount];
            viewModel.Players = new PlayerWindowViewModel[Players.Length];
        }

        public async Task<bool> InitGameData(GameSettings.SettingStructure settings)
        {
            IsEnableGameConduct = settings.IsEnableGameConduct;
            CurrentGameSettings = settings;
            SecondCount = 0;
            NowTurn = 1;

            if (settings.BoardCreation == GameSettings.BoardCreation.Server)
            {
                if ((await GameSettings.WaitForServerDialog.ShowDialogEx()) == false)
                    return false;
                settings.Turns = (byte)Network.ProconAPIClient.Instance.MatchData.Turns;
                settings.LimitTime = (ushort)(Network.ProconAPIClient.Instance.MatchData.IntervalMilliseconds / 1000);
                settings.IsAutoSkip = true;
                settings.BoardHeight = (byte)Network.ProconAPIClient.Instance.FieldState.Height;
                settings.BoardWidth = (byte)Network.ProconAPIClient.Instance.FieldState.Width;
                settings.AgentsCount = Network.ProconAPIClient.Instance.FieldState.Teams[0].Agents.Length;
            }

            BoardHeight = settings.BoardHeight;
            BoardWidth = settings.BoardWidth;
            FinishTurn = settings.Turns;
            TimeLimitSeconds = settings.LimitTime;
            IsAutoSkipTurn = settings.IsAutoSkip;

            Players[0] = new Player();
            Players[1] = new Player();

            for (int i = 0; i < Players.Length; ++i)
                viewModel.Players[i] = new PlayerWindowViewModel(Players[i], i + 1);

            if (settings.BoardCreation == GameSettings.BoardCreation.Random)
            {
                InitCellData(settings);
                InitAgents(settings);
                return true;
            }
            else
            {
                SetCellData(settings);
                SetAgents(settings);
                return true;
            }
        }

        void SetCellData(GameSettings.SettingStructure settings)
        {
            var fieldState = Network.ProconAPIClient.Instance.FieldState;
            CellData = new Cell[fieldState.Width, fieldState.Height];
            for (int i = 0; i < fieldState.Width; ++i)
                for (int j = 0; j < fieldState.Height; ++j)
                    CellData[i, j] = new Cell(fieldState.Point[j, i]) { AreaState = fieldState.Tiled[j, i] == Network.ProconAPIClient.Instance.MatchData.TeamId ? TeamColor.Area1P : (fieldState.Tiled[j, i] == 0 ? TeamColor.Free : TeamColor.Area2P) };
        }

        void InitCellData(GameSettings.SettingStructure settings)
        {
            if (settings.IsCreateRotate)
            {
                int randWidth = (BoardWidth + 1) / 2;
                CellData = new Cell[BoardWidth, BoardHeight];
                for (int i = 0; i < BoardWidth; i++)
                {
                    for (int j = 0; j < BoardHeight; j++)
                    {
                        if (i < randWidth)
                        {
                            //40%の確率で値を0未満にする
                            if (rand.Next(1, 100) > 40)
                                CellData[i, j] = new Cell(rand.Next(1, 16));
                            else
                                CellData[i, j] = new Cell(rand.Next(-16, 0));
                        }
                        else
                            CellData[i, j] = new Cell(CellData[i >= randWidth ? BoardWidth - 1 - i : i, BoardHeight - 1 - j].Score);
                    }
                }
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

        Point GenerateSymmetryPosition(Point p, Point boardSize, GameSettings.BoardSymmetry symmetry)
        {
            if(symmetry == GameSettings.BoardSymmetry.Rotate)
                return new Point( (byte)(boardSize.X - 1 - p.X), (byte)(boardSize.Y - 1 - p.Y));
            return new Point((symmetry & GameSettings.BoardSymmetry.Y) != 0 ? (byte)(boardSize.X - 1 - p.X) : p.X, (symmetry & GameSettings.BoardSymmetry.X) != 0 ? (byte)(boardSize.Y - 1 - p.Y) : p.Y);
        }

        void SetAgents(GameSettings.SettingStructure settings)
        {
            var fieldState = Network.ProconAPIClient.Instance.FieldState;
            AgentsCount = fieldState.Teams[0].Agents.Length;
            for (int p = 0; p < Players.Length; p++)
            {
                Players[p].Agents = new Agent[settings.AgentsCount];
                viewModel.Players[p].AgentViewModels = new UserOrderPanelViewModel[settings.AgentsCount];
                for (int i = 0; i < Players[p].Agents.Length; ++i)
                {
                    Players[p].Agents[i] = new Agent(PlayerNum: p, AgentNum: i, AgentsCount: settings.AgentsCount);
                    viewModel.Players[p].AgentViewModels[i] = new UserOrderPanelViewModel(Players[p].Agents[i]);
                }
            }
        }

        void InitAgents(GameSettings.SettingStructure settings)
        {
            AgentsCount = settings.AgentsCount;
            for (int p = 0; p < Players.Length; p++)
            {
                Players[p].Agents = new Agent[settings.AgentsCount];
                viewModel.Players[p].AgentViewModels = new UserOrderPanelViewModel[settings.AgentsCount];
                for (int i = 0; i < Players[p].Agents.Length; ++i)
                {
                    Players[p].Agents[i] = new Agent(PlayerNum: p, AgentNum: i, AgentsCount: settings.AgentsCount);
                    viewModel.Players[p].AgentViewModels[i] = new UserOrderPanelViewModel(Players[p].Agents[i]);
                }
            }
        }
    }
}
