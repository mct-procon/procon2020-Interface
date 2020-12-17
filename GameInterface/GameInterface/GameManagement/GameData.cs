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

namespace GameInterface.GameManagement
{
    public class GameData
    {
        public byte FinishTurn { get; set; } = 60;
        public int TimeLimitMilliseconds { get; set; } = 5000;
        public DateTime NextTime { get; set; }

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
            NowTurn = -1;

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
            TimeLimitMilliseconds = settings.LimitTime;
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
            settings.Turns = (byte)settings.Matches[settings.SelectedMatchIndex].Turns;
            settings.LimitTime = settings.Matches[settings.SelectedMatchIndex].OperationMilliseconds;
            settings.IsAutoSkip = true;
            settings.BoardHeight = (byte)matchInfo.Height;
            settings.BoardWidth = (byte)matchInfo.Width;
            settings.AgentsCount = matchInfo.Teams[0].AgentCount;

            BoardHeight = settings.BoardHeight;
            BoardWidth = settings.BoardWidth;
            FinishTurn = settings.Turns;
            TimeLimitMilliseconds = settings.LimitTime;
            IsAutoSkipTurn = settings.IsAutoSkip;

            Players[0] = new Player();
            Players[1] = new Player();

            for (int i = 0; i < Players.Length; ++i)
                viewModel.Players[i] = new PlayerWindowViewModel(Players[i], i + 1);

            SetCellData(settings, matchInfo);
            InitAgents(settings);
            return true;
        }

        public void UpdateData(MCTProcon31Protocol.Json.Matches.Match matchState, int myTeamID, bool[] moved)
        {
            void UpdateAgent(Agent agent, MCTProcon31Protocol.Json.Matches.Agent val, bool success)
            {
                if (val.X == 0)
                {
                    agent.State = AgentState.NonPlaced;
                    return;
                }
                if (success)
                {
                    agent.State = AgentState.Move;
                    agent.AgentDirection = AgentDirection.None;
                }
                agent.Point = new Point((byte)(val.X - 1), (byte)(val.Y - 1));
            }

            var (myTeam, enemyTeam) = matchState.Teams[0].Id == myTeamID ?
                (matchState.Teams[0], matchState.Teams[1]) : (matchState.Teams[1], matchState.Teams[0]);

            for (int a = 0; a < MaximumAgentsCount; ++a)
                UpdateAgent(Players[0].Agents[a], myTeam.Agents[a], moved[a]);
            for (int a = 0; a < MaximumAgentsCount; ++a)
                UpdateAgent(Players[1].Agents[a], enemyTeam.Agents[a], moved[MaximumAgentsCount + a]);

            for (int x = 0; x < CellData.GetLength(0); ++x)
                for (int y = 0; y < CellData.GetLength(1); ++y)
                {
                    CellData[x, y].AreaState = matchState.Walls[y, x] == myTeamID ? TeamColor.Player1 : (matchState.Walls[y, x] == 0 ? TeamColor.Free : TeamColor.Player2);
                    CellData[x, y].SurroundedState = matchState.Areas[y, x] == myTeamID ? TeamColor.Player1 : (matchState.Areas[y, x] == 0 ? TeamColor.Free : TeamColor.Player2);
                    CellData[x, y].AgentState = TeamColor.Free;
                    CellData[x, y].AgentNum = -1;
                }

            foreach (var p in Players)
                foreach (var a in p.Agents)
                    if (a.State != AgentState.NonPlaced)
                    {
                        CellData[a.Point.X, a.Point.Y].AgentState = a.PlayerNum;
                        CellData[a.Point.X, a.Point.Y].AgentNum = a.AgentNum;
                    }
            viewModel.Players[0].Score = myTeam.WallPoint + myTeam.AreaPoint;
            viewModel.Players[1].Score = enemyTeam.WallPoint + enemyTeam.AreaPoint;
        }

        void SetCellData(GameSettings.SettingStructure settings, MCTProcon31Protocol.Json.Matches.Match matchInfo)
        {
            CellData = new Cell[matchInfo.Width, matchInfo.Height];
            for (int i = 0; i < CellData.GetLength(0); ++i)
                for (int j = 0; j < CellData.GetLength(1); ++j)
                    CellData[i, j] = new Cell(matchInfo.FieldPoint[j, i]);
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
                            //20%の確率で値を0未満にする
                            CellData[i, j] = new Cell(rand.Next(1, 100) > 20 ? rand.Next(1, 16) : rand.Next(-16, 0));
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
                            //40%の確率で値を0未満にする
                            CellData[i, j] = new Cell(rand.Next(0, 100) > 40 ? rand.Next(1, 16) : rand.Next(-16, 0));
                        else
                            CellData[i, j] = new Cell(CellData[i >= randWidth ? BoardWidth - 1 - i : i, j >= randHeight ? BoardHeight - 1 - j : j].Score);
                    }
                }
            }
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
