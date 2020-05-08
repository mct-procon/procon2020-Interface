using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Threading;
using MCTProcon30Protocol.Methods;
using MCTProcon30Protocol;
using GameInterface.ViewModels;
using System.Threading.Tasks;

namespace GameInterface.GameManagement
{
    public class GameManager
    {
        public GameData Data;
        internal Server Server;
        private DispatcherTimer dispatcherTimer;
        public MainWindowViewModel viewModel;
        private MainWindow mainWindow;
        public GameManager(MainWindowViewModel _viewModel,MainWindow _mainWindow)
        {
            this.viewModel = _viewModel;
            this.mainWindow = _mainWindow;
            this.Data = new GameData(_viewModel);
            this.Server = new Server(this);
        }

        public void StartGame()
        {
            Server.SendGameInit();
            InitDispatcherTimer();
            for (int p = 0; p < viewModel.Players.Length; ++p)
                for (int i = 0; i < viewModel.Players[p].AgentViewModels.Length; ++i)
                {
                    viewModel.Players[p].AgentViewModels[i].Data.AgentDirection = AgentDirection.None;
                    viewModel.Players[p].AgentViewModels[i].Data.State = AgentState.Move;
                }
            StartTurn();
            GetScore();
            Data.IsGameStarted = true;
        }

        public void EndGame()
        {
            TimerStop();
        }

        public void PauseGame()
        {
            Data.IsPause = true;
        }

        public void RerunGame()
        {
            Data.IsPause = false;
        }

        public async Task<bool> InitGameData(GameSettings.SettingStructure settings)
        {
            if (!(await Data.InitGameData(settings)))
                return false;
            Data.IsPause = false;
            Server.StartListening(settings);
            return true;
        }

        public void TimerStop()
        {
            dispatcherTimer?.Stop();
        }

        public void TimerResume()
        {
            dispatcherTimer?.Start();
        }

        private void InitDispatcherTimer()
        {
            dispatcherTimer = new DispatcherTimer(DispatcherPriority.Normal);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
            dispatcherTimer.Tick += new EventHandler(DispatcherTimer_Tick);
            dispatcherTimer.Start();
            viewModel.TurnStr = $"TURN:{Data.NowTurn}/{Data.FinishTurn}";
            viewModel.TimerStr = $"TIME:{Data.SecondCount}/{Data.TimeLimitSeconds}";
        }

        //一秒ごとに呼ばれる
        private void DispatcherTimer_Tick(object sender, EventArgs e)
        {
            Update().ContinueWith((t) => Draw(), TaskScheduler.Current);
        }

        private async Task Update()
        {
            if (!Data.IsNextTurnStart) return;
            Data.SecondCount++;
            if (Data.SecondCount == Data.TimeLimitSeconds || Server.IsDecidedReceived.All(b => b))
            {
                Data.IsNextTurnStart = false;
                await EndTurn();
            }
        }

        public void StartTurn()
        {
            Data.IsNextTurnStart = true;
            var movable = MoveAgents();
            GetScore();
            Data.SecondCount = 0;
            Server.SendTurnStart(movable);
        }

        public async Task EndTurn()
        {
            if (!Data.IsGameStarted) return;
            Server.SendTurnEnd();
            if (Data.NowTurn <= Data.FinishTurn)
            {
                Data.NowTurn++;
                if (Data.IsAutoSkipTurn)
                {
                    if(!Data.IsEnableGameConduct)
                        await Network.ProconAPIClient.Instance.GetState();
                    StartTurn();
                }
            }
            else
            {
                EndGame();
                Server.SendGameEnd();
                if (Data.CurrentGameSettings.IsAutoGoNextGame && Data.CurrentGameSettings.IsEnableGameConduct)
                {
                    Data.CurrentGameSettings.IsUseSameAI = true;
                    var settings = Data.CurrentGameSettings;
                    mainWindow.ShotAndSave();
                    mainWindow.InitGame(settings).Wait();
                    StartGame();
                }
            }
        }

        public void ChangeCellToNextColor(Point point)
        {
            //エージェントがいる場合、エージェントの移動処理へ
            Agent onAgent = GetOnAgent(point);
            if (onAgent != null)
            {
                if(Data.SelectedAgent != null)
                {
                    ExchangeAgent(Data.SelectedAgent, onAgent);
                }
                else
                    Data.SelectedAgent = onAgent;
                return;
            }
            if (Data.SelectedAgent != null)
            {
                WarpAgent(Data.SelectedAgent, point);
                Data.SelectedAgent = null;
                return;
            }
            var color = Data.CellData[point.X, point.Y].AreaState;
            var nextColor = (TeamColor)(((int)color + 1) % 3);
            Data.CellData[point.X, point.Y].AreaState = nextColor;
        }

        private Agent GetOnAgent(Point point)
        {
            for(int m = 0; m < App.PlayersCount; ++m)
                for (int i = 0; i < Data.AgentsCount; i++)
                {
                    var agent = Data.Players[m].Agents[i];
                    if (agent.Point == point)
                        return agent;
                }
            return null;
        }

        private void WarpAgent(Agent agent, Point dest)
        {
            Data.CellData[agent.Point.X, agent.Point.Y].AgentState = TeamColor.Free;
            agent.Point = dest;
            var nextPointColor =
                agent.PlayerNum == 0 ? TeamColor.Area1P : TeamColor.Area2P;
            Data.CellData[dest.X, dest.Y].AreaState = nextPointColor;
            Data.CellData[dest.X, dest.Y].AgentState = nextPointColor;
            return;
        }

        private void ExchangeAgent(Agent agent0, Agent agent1)
        {
            if(agent0.PlayerNum != agent1.PlayerNum)
            {
                Data.CellData[agent0.Point.X, agent0.Point.Y].AgentState = agent1.PlayerNum == 0 ? TeamColor.Area1P : TeamColor.Area2P;
                Data.CellData[agent1.Point.X, agent1.Point.Y].AgentState = agent0.PlayerNum == 0 ? TeamColor.Area1P : TeamColor.Area2P;
                Data.CellData[agent0.Point.X, agent0.Point.Y].AreaState = agent1.PlayerNum == 0 ? TeamColor.Area1P : TeamColor.Area2P;
                Data.CellData[agent1.Point.X, agent1.Point.Y].AreaState = agent0.PlayerNum == 0 ? TeamColor.Area1P : TeamColor.Area2P;
            }
            var swp = agent0.Point;
            agent0.Point = agent1.Point;
            agent1.Point = swp;

        }

        private Point ToPoint(MCTProcon30Protocol.Json.Agent agent) => new Point((byte)agent.X, (byte)agent.Y);

        private bool[] MoveAgents()
        {
            if (Data.IsEnableGameConduct)
            {
                List<Agent> ActionableAgents = GetActionableAgents();

                // Erase Agent Location's data from cells.
                foreach (var p in Data.Players)
                    foreach (var a in p.Agents)
                    {
                        Data.CellData[a.Point.X, a.Point.Y].AgentState = TeamColor.Free;
                        Data.CellData[a.Point.X, a.Point.Y].AgentNum = -1;
                        a.IsMoved = false;
                    }

                foreach (var a in ActionableAgents)
                {
                    var nextP = a.GetNextPoint();

                    TeamColor nextAreaState = Data.CellData[nextP.X, nextP.Y].AreaState;
                    ActionAgentToNextP(a, nextP, nextAreaState);
                    a.State = AgentState.Move;
                    a.IsMoved = true;
                }

                // Reset Agent Location's data to cells.
                foreach (var p in Data.Players) foreach (var a in p.Agents)
                    {
                        Data.CellData[a.Point.X, a.Point.Y].AgentState = a.PlayerNum == 0 ? TeamColor.Area1P : TeamColor.Area2P;
                        Data.CellData[a.Point.X, a.Point.Y].AreaState = a.PlayerNum == 0 ? TeamColor.Area1P : TeamColor.Area2P;
                        Data.CellData[a.Point.X, a.Point.Y].AgentNum = a.AgentNum;
                    }

                bool[] retVal = new bool[App.PlayersCount * Data.AgentsCount];
                for (int p = 0; p < Data.Players.Length; ++p)
                    for (int i = 0; i < Data.Players[p].Agents.Length; ++i)
                        retVal[p * Data.AgentsCount + i] = Data.Players[p].Agents[i].IsMoved;
                return retVal;
            }
            else
            {
                var retVal = new bool[App.PlayersCount * Data.AgentsCount];
                for(int i = 0; i < App.PlayersCount; ++i)
                    for(int j = 0; j < Data.AgentsCount; ++j)
                    {
                        if (Data.Players[i].Agents[j].AgentDirection == AgentDirection.None)
                        {
                            retVal[i * Data.AgentsCount + j] = true;
                            continue;
                        }
                        retVal[i * Data.AgentsCount + j] = (Data.Players[i].Agents[j].GetNextPoint() == ToPoint(Network.ProconAPIClient.Instance.FieldState.Teams[i].Agents[j]));
                    }
                return retVal;
            }
        }

        //naotti: 行動可能なエージェントのId(1p{0,1}, 2p{2,3})を返す。
        private List<Agent> GetActionableAgents()
        {
            bool[] canMove = new bool[Data.AgentsCount * App.PlayersCount];     //canMove[i] = エージェントiは移動するか？
            bool[] canAction = new bool[canMove.Length];   //canAction[i] = エージェントiは移動またはタイル除去をするか？

            //まずは、各エージェントの移動先を知りたいので、canMoveを求める。
            //最初, canMove[i] = trueとしておき、移動不可なエージェントを振るい落とす方式を取る。このループでは以下の2点をチェックする。
            //・相手陣を指しているエージェントはタイル除去なので、移動しない
            //・範囲外を指しているエージェントは移動できない。
            for(int p = 0; p < Data.Players.Length; ++p)
                for (int i = 0; i < Data.Players[p].Agents.Length; i++)
                {
                    canMove[p * Data.AgentsCount + i] = true;
                    var agent = Data.Players[p].Agents[i];
                    var nextP = agent.GetNextPoint();
                    if (CheckIsPointInBoard(nextP) == false) { canMove[p * Data.AgentsCount + i] = false; continue; }
                    TeamColor nextAreaState = Data.CellData[nextP.X, nextP.Y].AreaState;
                    if ((agent.PlayerNum == 0 && nextAreaState == TeamColor.Area2P) || (agent.PlayerNum == 1 && nextAreaState == TeamColor.Area1P))
                        canMove[p * Data.AgentsCount + i] = false;
                }

            //次に、「指示先(agent.GetNextPoint()の位置)が被っているエージェントは移動不可」とする。
            for (int p = 0; p < Data.Players.Length; ++p)
                for (int i = 0; i < Data.Players[p].Agents.Length; i++)
                {
                    var agent1 = Data.Players[p].Agents[i];
                    var nextP1 = agent1.GetNextPoint();
                    int j = i + 1;
                    for (int q = p; q < Data.Players.Length; ++q)
                    {
                        for (; j < Data.Players[q].Agents.Length; j++)
                        {
                            var agent2 = Data.Players[q].Agents[j];
                            var nextP2 = agent2.GetNextPoint();
                            if (nextP1 == nextP2)
                            {
                                canMove[p * Data.AgentsCount + i] = false;
                                canMove[q * Data.AgentsCount + j] = false;
                            }
                        }
                        j = 0;
                    }
                }

            //次に、canMove[]の更新が起きなくなるまで、以下を繰り返す
            //・移動先に移動不可な(orタイル除去をする)エージェントがいる場合、移動不可とする
            bool updateFlag;
            do
            {
                updateFlag = false;
                for(int p = 0; p < Data.Players.Length; ++p)
                for (int i = 0; i < Data.Players[p].Agents.Length; i++)
                {
                    if (canMove[p * Data.AgentsCount + i] == false) { continue; }
                    var agent1 = Data.Players[p].Agents[i];
                    var nextP1 = agent1.GetNextPoint();
                    for(int q = 0; q  <Data.Players.Length; ++q)
                    for (int j = 0; j < Data.Players[q].Agents.Length; j++)
                    {
                        if (p * Data.AgentsCount + i == q * Data.AgentsCount + j) { continue; }
                        if (canMove[ q * Data.AgentsCount + j] == true) { continue; }
                        var agent2 = Data.Players[q].Agents[j];
                        var nextP2 = agent2.Point;
                        if (nextP1 == nextP2)
                        {
                            canMove[p * Data.AgentsCount + i] = false;
                            updateFlag = true;
                            break;
                        }
                    }
                }
            }
            while (updateFlag) ;

            //この時点でcanMove[i] == trueならば、エージェントiは移動することになる。
            //次は、行動(移動またはタイル除去)が可能なエージェントを求める。
            //最初, canAction[i] = trueとしておき、行動不可なエージェントを振るい落とす方式を取る。このループでは以下の1点をチェックする。
            //・範囲外を指しているエージェントは移動できない。
            for (int p = 0; p < Data.Players.Length; ++p)
            for (int i = 0; i < Data.Players[p].Agents.Length; i++)
            {
                canAction[p * Data.AgentsCount + i] = true;
                var agent = Data.Players[p].Agents[i];
                var nextP = agent.GetNextPoint();
                if (CheckIsPointInBoard(nextP) == false)
                    canAction[p * Data.AgentsCount + i] = false;
            }

            //次に、「指示先(agent.GetNextPoint()の位置)が被っているエージェントは行動不可」とする。
            for(int p = 0; p < Data.Players.Length; ++p)
            for (int i = 0; i < Data.Players[p].Agents.Length; i++)
            {
                var agent1 = Data.Players[p].Agents[i];
                var nextP1 = agent1.GetNextPoint();
                int j = i + 1;
                    for (int q = p; q < Data.Players.Length; ++q)
                    {
                        for (; j < Data.Players[q].Agents.Length; j++)
                        {
                            var agent2 = Data.Players[q].Agents[j];
                            var nextP2 = agent2.GetNextPoint();
                            if (nextP1 == nextP2)
                            {
                                canAction[p * Data.AgentsCount + i] = false;
                                canAction[q * Data.AgentsCount + j] = false;
                            }
                        }
                        j = 0;
                    }
            }

            //次に、「指示先に移動不可な(orタイル除去をする)エージェントがいる場合、行動不可」とする。
            //このチェックは, 先ほどのように何回もwhileループで回す必要がない。
            for(int p = 0; p < Data.Players.Length; ++p)
            for (int i = 0; i < Data.AgentsCount; i++)
            {
                if (canAction[p * Data.AgentsCount + i] == false) { continue; }
                var agent1 = Data.Players[p].Agents[i];
                var nextP1 = agent1.GetNextPoint();

                for(int q = 0; q < Data.Players.Length; ++q)
                for (int j = 0; j < Data.Players[q].Agents.Length; j++)
                {
                    if (p * Data.AgentsCount + i == q * Data.AgentsCount + j) { continue; }
                    if (canMove[q * Data.AgentsCount + j] == true) { continue; }
                    var agent2 = Data.Players[q].Agents[j];
                    var nextP2 = agent2.Point;

                    if (nextP1 == nextP2)
                    {
                        canAction[p * Data.AgentsCount + i] = false;
                        break;
                    }
                }
            }

            //この時点でcanAction[i] == trueならば、エージェントiは行動可能である
            //よって、行動可能なエージェントの番号を返すことができる
            List<Agent> ret = new List<Agent>();
            for(int p = 0; p < Data.Players.Length; ++p)
            for (int i = 0; i < Data.Players[p].Agents.Length; i++)
                if (canAction[p * Data.AgentsCount + i])
                        ret.Add(Data.Players[p].Agents[i]);
            return ret;
        }

        private void GetScore()
        {
            //for (int x = 0; x < Data.CellData.GetLength(0); ++x)
            //    for (int y = 0; y < Data.CellData.GetLength(1); ++y)
            //        Data.CellData[x, y].SurroundedState = TeamColor.Free;
            for (int i = 0; i < App.PlayersCount; i++)
                viewModel.Players[i].Score = ScoreCalculator.CalcScore(i, Data.CellData);
        }

        private void ActionAgentToNextP(Agent agent, Point nextP, TeamColor nextAreaState)
        {
            switch (agent.State)
            {
                case AgentState.Move:
                    switch (agent.PlayerNum)
                    {
                        case 0:
                            if (nextAreaState != TeamColor.Area2P)
                            {
                                agent.Point = nextP;
                                Data.CellData[nextP.X, nextP.Y].AreaState = TeamColor.Area1P;
                            }
                            else
                            {
                                Data.CellData[nextP.X, nextP.Y].AreaState = TeamColor.Free;
                            }
                            break;
                        case 1:
                            if (nextAreaState != TeamColor.Area1P)
                            {
                                agent.Point = nextP;
                                Data.CellData[nextP.X, nextP.Y].AreaState = TeamColor.Area2P;
                            }
                            else
                            {
                                Data.CellData[nextP.X, nextP.Y].AreaState = TeamColor.Free;
                            }
                            break;
                    }
                    break;
                case AgentState.RemoveTile:
                    Data.CellData[nextP.X, nextP.Y].AreaState = TeamColor.Free;
                    break;
                default:
                    break;
            }
            agent.AgentDirection = AgentDirection.None;
            agent.State = AgentState.Move;
        }

        public void ClearDecisions(int index)
        {
            viewModel?.Players[index]?.Decisions.Clear();
            viewModel?.Players[index]?.RaiseDecisionsChanged();
        }

        public void SetDecisions(int index, Decided decideds)
        {
            viewModel.Players[index].Decisions = decideds.Data;
            viewModel.Players[index].DecisionsSelectedIndex = 0;
            var decided = decideds[0];
            SetDecision(index, decided);
        }

        public void SetDecision(int index, Decision decide)
        {
            for (int i = 0; i < Data.AgentsCount; ++i)
            {
                AgentDirection dir = DirectionExtensions.CastPointToDir(decide.Agents[i]);
                viewModel.Players[index].AgentViewModels[i].Data.AgentDirection = dir;
                viewModel.Players[index].AgentViewModels[i].Data.State = AgentState.Move;
            }
        }

        private void Draw()
        {
            viewModel.TimerStr = $"TIME:{Data.SecondCount}/{Data.TimeLimitSeconds}";
            viewModel.TurnStr = $"TURN:{Data.NowTurn}/{Data.FinishTurn}";
        }

        private bool CheckIsPointInBoard(Point p)
        {
            return (p.X >= 0 && p.X < Data.BoardWidth &&
                p.Y >= 0 && p.Y < Data.BoardHeight);
        }
    }
}
