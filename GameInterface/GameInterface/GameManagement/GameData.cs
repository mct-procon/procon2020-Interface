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

        public void InitGameData(GameSettings.SettingStructure settings)
        {
            CurrentGameSettings = settings;
            SecondCount = 0;
            NowTurn = 1;
            FinishTurn = settings.Turns;
            TimeLimitSeconds = settings.LimitTime;
            IsAutoSkipTurn = settings.IsAutoSkip;

            if(settings.BoardCreation == GameSettings.BoardCreation.JsonFile)
            {
                settings.BoardWidth = (byte)settings.JsonCell.GetLength(0);
                settings.BoardHeight = (byte)settings.JsonCell.GetLength(1);
            }
            Players[0] = new Player();
            Players[1] = new Player();

            for (int i = 0; i < Players.Length; ++i)
                viewModel.Players[i] = new PlayerWindowViewModel(Players[i]);

            InitCellData(settings);
            InitAgents(settings);
        }

        void InitCellData(GameSettings.SettingStructure settings)
        {
            if (settings.BoardCreation == GameSettings.BoardCreation.Random)
            {
                BoardHeight = settings.BoardHeight;
                BoardWidth = settings.BoardWidth;

                //水平方向か垂直方向のどちらかを対称にするフラグをランダムに立てる
                bool isVertical;
                bool isHorizontal;
                do
                {
                    isVertical = rand.Next(2) == 1 ? true : false;
                    isHorizontal = rand.Next(2) == 1 ? true : false;
                } while (!isVertical && !isHorizontal);

                int randWidth, randHeight;
                randWidth = (isHorizontal) ? (BoardWidth + 1) / 2 : BoardWidth;
                randHeight = (isVertical) ? (BoardHeight + 1) / 2 : BoardHeight;

                CellData = new Cell[BoardWidth, BoardHeight];
                for (int i = 0; i < BoardWidth; i++)
                {
                    for (int j = 0; j < BoardHeight; j++)
                    {
                        if (i < randWidth && j < randHeight)
                        {
                            //10%の確率で値を0以下にする
                            if (rand.Next(1, 100) > 10)
                                CellData[i, j] = new Cell(rand.Next(1, 14));
                            else
                                CellData[i, j] = new Cell(rand.Next(-14, 0));
                        }
                        else if (j >= randHeight)
                            CellData[i, j] = new Cell(CellData[i, BoardHeight - 1 - j].Score);
                        else
                            CellData[i, j] = new Cell(CellData[BoardWidth - 1 - i, j].Score);
                    }
                }
            }
            else
            {
                CellData = settings.JsonCell;
                BoardWidth = CellData.GetLength(0);
                BoardHeight = CellData.GetLength(1);
            }
        }

        void InitAgents(GameSettings.SettingStructure settings)
        {
            AgentsCount = settings.AgentsCount;
            Players[0].Agents = new Agent[settings.AgentsCount];
            Players[1].Agents = new Agent[settings.AgentsCount];
            viewModel.Players[0].AgentViewModels = new UserOrderPanelViewModel[settings.AgentsCount];
            viewModel.Players[1].AgentViewModels = new UserOrderPanelViewModel[settings.AgentsCount];
            for (int i = 0; i < Players[0].Agents.Length; ++i)
            {
                var a0 = new Point((byte)rand.Next(0, BoardWidth / 2), (byte)rand.Next(0, BoardWidth / 2));
                var a1 = new Point((byte)(BoardWidth - a0.X - 1), (byte)(BoardHeight - a0.Y - 1));
                Players[0].Agents[i] = new Agent() { Point = a0, PlayerNum = 0, AgentNum = i };
                Players[1].Agents[i] = new Agent() { Point = a1, PlayerNum = 1, AgentNum = i };
                CellData[a0.X, a0.Y].AreaState = TeamColor.Area1P;
                CellData[a0.X, a0.Y].AgentState = TeamColor.Area1P;
                CellData[a1.X, a1.Y].AreaState = TeamColor.Area2P;
                CellData[a1.X, a1.Y].AgentState = TeamColor.Area2P;

                viewModel.Players[0].AgentViewModels[i] = new UserOrderPanelViewModel(Players[0].Agents[i]);
                viewModel.Players[1].AgentViewModels[i] = new UserOrderPanelViewModel(Players[1].Agents[i]);
            }
        }
    }
}
