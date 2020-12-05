using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Threading;
using MCTProcon31Protocol.Methods;
using MCTProcon31Protocol;
using GameInterface.ViewModels;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;

namespace GameInterface.GameManagement
{
    public class GameManager
    {
        public GameData Data;
        public MCTProcon31Protocol.Json.Matches.Match CurrentMatchState;
        internal Server Server;
        private Thread CommunicationThread;
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

        public MCTProcon31Protocol.Json.Matches.MatchInformation CurrentMatchInfo => Data.CurrentGameSettings.Matches[Data.CurrentGameSettings.SelectedMatchIndex];

        public void StartGame()
        {
            Server.SendGameInit();
            InitDispatcherTimer();
            for (int p = 0; p < viewModel.Players.Length; ++p)
                for (int i = 0; i < viewModel.Players[p].AgentViewModels.Length; ++i)
                {
                    viewModel.Players[p].AgentViewModels[i].Data.AgentDirection = AgentDirection.None;
                    viewModel.Players[p].AgentViewModels[i].Data.State = AgentState.NonPlaced;
                }
            StartTurn();
            GetScore();
            Data.IsGameStarted = true;
        }

        public void EndGame() => TimerStop();
        public void PauseGame() => Data.IsPause = true;

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

        public void TimerStop() => dispatcherTimer?.Stop();
        public void TimerResume() => dispatcherTimer?.Start();

        private void InitDispatcherTimer()
        {
            dispatcherTimer = new DispatcherTimer(DispatcherPriority.Normal);
            dispatcherTimer.Interval = TimeSpan.FromMilliseconds(60);
            dispatcherTimer.Tick += new EventHandler(DispatcherTimer_Tick);
            Data.LastTime = DateTime.Now - TimeSpan.FromMilliseconds(500);
            dispatcherTimer.Start();
            viewModel.TurnStr = $"TURN:{Data.NowTurn}/{Data.FinishTurn}";
            viewModel.TimerStr = $"TIME:0.00/{Data.TimeLimitMilliseconds/1000.0:F2}";
        }

        //一秒ごとに呼ばれる
        private void DispatcherTimer_Tick(object sender, EventArgs e)
        {
            Update();
            Draw((int)(DateTime.Now - Data.LastTime).TotalMilliseconds);
        }

        private void Update()
        {
            if (!Data.IsNextTurnStart) return;
            if ((DateTime.Now - Data.LastTime).TotalMilliseconds >= Data.TimeLimitMilliseconds || Server.IsDecidedReceived.All(b => b))
            {
                Data.IsNextTurnStart = false;
                EndTurn();
            }
        }

        public void StartTurn()
        {
            if (Data.IsEnableGameConduct)
            {
                Data.IsNextTurnStart = true;
                var movable = MoveAgents();
                GetScore();
                Data.LastTime = DateTime.Now;
                Data.NowTurn++;
                Server.SendTurnStart(movable);
            }
            else
            {
                dispatcherTimer.Stop();
                if (!(CommunicationThread is null) && CommunicationThread.ThreadState == System.Threading.ThreadState.Running) return; // thread is running
                var ts = new ThreadStart(async () =>
                {
                    int retries = 0;
                retry:
                    {
                        var res = await Data.CurrentGameSettings.ApiClient.Match(this.CurrentMatchInfo);
                        if (res.IsSuccess && res.Value.Turn >= Data.NowTurn) // SUCCESS and turn is NOT past.
                        {
                            this.CurrentMatchState = res.Value;
                            goto end;
                        }
                        retries++;
                        if (retries > 10)
                        {
                            bool abort = false;
                            await mainWindow.Dispatcher.BeginInvoke(() =>
                            {
                                abort = System.Windows.MessageBox.Show("Have failed connection.\nDo you want to disconnect?\nError Code:" + res.HTTPReturnCode, "Failed communication 10 times.", System.Windows.MessageBoxButton.YesNo) == System.Windows.MessageBoxResult.Yes;
                            });
                            if (abort)
                                return;
                            retries = 0;
                        }
                        await Task.Delay(100);
                        goto retry;
                    }
                end:
                    Data.LastTime = DateTime.Now;
                    Data.IsNextTurnStart = true;
                    await mainWindow.Dispatcher.BeginInvoke(() =>
                    {
                        dispatcherTimer.Start();
                        var movable = MoveAgents();
                        GetScore();
                        Data.NowTurn++;
                        Server.SendTurnStart(movable);
                    });
                });
                CommunicationThread = new Thread(ts);
                CommunicationThread.Start();
            }
        }

        public void EndTurn()
        {
            if (!Data.IsGameStarted) return;
            Server.SendTurnEnd();
            if (Data.NowTurn <= Data.FinishTurn)
            {
                if (Data.IsAutoSkipTurn)
                    StartTurn();
            }
            else
            {
                EndGame();
                Server.SendGameEnd();
                if (Data.CurrentGameSettings.IsAutoGoNextGame && Data.CurrentGameSettings.IsEnableGameConduct)
                {
                    Data.CurrentGameSettings.IsUseSameAI = true;
                    mainWindow.ShotAndSave();
                    mainWindow.InitGame(Data.CurrentGameSettings).Wait();
                    StartGame();
                }
            }
        }

        public void PlaceAgent(int playerNum, Point point)
        {
            var agent = Data.Players[playerNum].Agents.FirstOrDefault(x => x.State == AgentState.NonPlaced);
            if(!(agent is null)){
                agent.State = AgentState.PlacePending;
                agent.Point = point;
            }
        }

        public void ChangeCellToNextColor(Point point)
        {
            //エージェントがいる場合、エージェントの移動処理へ
            Agent onAgent = GetOnAgent(point);
            if (onAgent != null)
            {
                if (Data.SelectedAgent != null)
                    ExchangeAgent(Data.SelectedAgent, onAgent);
                else
                    Data.SelectedAgent = onAgent;
            }
            else if (Data.SelectedAgent != null)
            {
                WarpAgent(Data.SelectedAgent, point);
                Data.SelectedAgent = null;
            }
            else
            {
                var color = Data.CellData[point.X, point.Y].AreaState;
                var nextColor = (TeamColor)(((int)color + 1) % 3);
                Data.CellData[point.X, point.Y].AreaState = nextColor;
                Data.CellData[point.X, point.Y].SurroundedState = TeamColor.Free;
            }
        }

        private Agent GetOnAgent(Point point)
        {
            for(int m = 0; m < App.PlayersCount; ++m)
                for (int i = 0; i < Data.MaximumAgentsCount; i++)
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
            var nextPointColor = agent.PlayerNum;
            Data.CellData[dest.X, dest.Y].AreaState = nextPointColor;
            Data.CellData[dest.X, dest.Y].AgentState = nextPointColor;
            return;
        }

        private void ExchangeAgent(Agent agent0, Agent agent1)
        {
            if(agent0.PlayerNum != agent1.PlayerNum)
            {
                Data.CellData[agent0.Point.X, agent0.Point.Y].AgentState = agent1.PlayerNum;
                Data.CellData[agent1.Point.X, agent1.Point.Y].AgentState = agent0.PlayerNum;
                Data.CellData[agent0.Point.X, agent0.Point.Y].AreaState = agent1.PlayerNum;
                Data.CellData[agent1.Point.X, agent1.Point.Y].AreaState = agent0.PlayerNum;
            }
            var swp = agent0.Point;
            agent0.Point = agent1.Point;
            agent1.Point = swp;

        }

        private bool[] MoveAgents()
        {
            var retVal = new bool[App.PlayersCount * Data.MaximumAgentsCount];
            if (Data.IsEnableGameConduct)
            {
                List<Agent> ActionableAgents = GetActionableAgents();

                foreach (var a in ActionableAgents)
                {
                    Data.CellData[a.Point.X, a.Point.Y].AgentState = TeamColor.Free;
                    Data.CellData[a.Point.X, a.Point.Y].AgentNum = -1;
                    var nextP = a.GetNextPoint();

                    TeamColor nextAreaState = Data.CellData[nextP.X, nextP.Y].AreaState;
                    ActionAgentToNextP(a, nextP, nextAreaState);

                    Data.CellData[a.Point.X, a.Point.Y].AgentState = a.PlayerNum;
                    Data.CellData[a.Point.X, a.Point.Y].AreaState = a.PlayerNum;
                    Data.CellData[a.Point.X, a.Point.Y].SurroundedState = TeamColor.Free;
                    Data.CellData[a.Point.X, a.Point.Y].AgentNum = a.AgentNum;
                    retVal[a.PlayerNum.ToPlayerNum() * Data.MaximumAgentsCount + a.AgentNum] = true;
                }

                foreach (var p in Data.Players)
                    foreach (var a in p.Agents)
                        if(a.State.HasFlag(AgentState.Move)) a.State = AgentState.Move;
            }
            else
            {
                for(int i = 0; i < App.PlayersCount; ++i)
                    for(int j = 0; j < Data.MaximumAgentsCount; ++j)
                    {
                        if (Data.Players[i].Agents[j].AgentDirection == AgentDirection.None)
                        {
                            retVal[i * Data.MaximumAgentsCount + j] = true;
                            continue;
                        }
                        //TODO
                        //retVal[i * Data.MaximumAgentsCount + j] = (Data.Players[i].Agents[j].GetNextPoint() == ToPoint(Network.ProconAPIClient.Instance.FieldState.Teams[i].Agents[j]));
                    }
            }
            return retVal;
        }

        //naotti: 行動可能なエージェントのId(1p{0,1}, 2p{2,3})を返す。
        private List<Agent> GetActionableAgents()
        {
            bool[] canMove = new bool[Data.MaximumAgentsCount * App.PlayersCount];     // canMove[i] = エージェントiは移動または配置をするか？
            bool[] canAction = new bool[canMove.Length];    // canAction[i] = エージェントiは移動または配置またはタイル除去をするか？
            List<Agent> existAgents = new List<Agent>();    // 配置されるor配置されているエージェント

            // まだフィールドに配置されておらず、かつ配置される予定のないエージェントは計算に含める必要がないため、
            // ・まだフィールドに存在していない
            // ・このターンに置かれる予定がない
            // エージェント以外でexistAgentsを構成し、以降はこれについて考える。
            foreach (var player in Data.Players)
                for (int i = 0; i < player.Agents.Length; i++)
                    if(player.Agents[i].State != AgentState.NonPlaced)
                        existAgents.Add(player.Agents[i]);

            //まずは、各エージェントの移動先を知りたいので、canMoveを求める。
            //最初, canMove[i] = trueとしておき、移動不可なエージェントを振るい落とす方式を取る。このループでは以下の2点をチェックする。
            //・相手陣を指しているエージェントはタイル除去なので、移動しない
            //・範囲外を指しているエージェントは移動できない。
            foreach (var agent in existAgents)
            {
                canMove[agent.AgentID] = true;
                var nextP = agent.GetNextPoint();
                if (!CheckIsPointInBoard(nextP)) { canMove[agent.AgentID] = false; continue; }
                TeamColor nextAreaState = Data.CellData[nextP.X, nextP.Y].AreaState;
                if ((agent.PlayerNum == TeamColor.Player1 && nextAreaState == TeamColor.Player2) || (agent.PlayerNum == TeamColor.Player2 && nextAreaState == TeamColor.Player1))
                    canMove[agent.AgentID] = false;
            }

            //次に、「指示先(agent.GetNextPoint()の位置)が被っているエージェントは移動不可」とする。
            foreach (var agent1 in existAgents)
            {
                var nextP1 = agent1.GetNextPoint();
                foreach (var agent2 in existAgents)
                {
                    if (agent1.AgentID <= agent2.AgentID) { break; }
                    var nextP2 = agent2.GetNextPoint();
                    if (nextP1 == nextP2)
                    {
                        canMove[agent1.AgentID] = false;
                        canMove[agent2.AgentID] = false;
                    }
                }
            }

            //次に、canMove[]の更新が起きなくなるまで、以下を繰り返す
            //・移動先に移動不可な(orタイル除去をする)エージェントがいる場合、移動不可とする
            bool updateFlag;
            do
            {
                updateFlag = false;
                foreach (var agent1 in existAgents)
                {
                    if (!canMove[agent1.AgentID]) { continue; }
                    var nextP1 = agent1.GetNextPoint();
                    foreach (var agent2 in existAgents)
                    {
                        if (agent1.AgentID == agent2.AgentID || canMove[agent2.AgentID]) { continue; }
                        var nextP2 = agent2.Point;
                        if (nextP1 == nextP2)
                        {
                            canMove[agent1.AgentID] = false;
                            updateFlag = true;
                            break;
                        }
                    }
                }
            }
            while (updateFlag);

            //この時点でcanMove[i] == trueならば、エージェントiは移動することになる。
            //次は、行動(移動またはタイル除去)が可能なエージェントを求める。
            //最初, canAction[i] = trueとしておき、行動不可なエージェントを振るい落とす方式を取る。このループでは以下の2点をチェックする。
            //・範囲外を指しているエージェントは行動できない。
            //・相手陣を指しているエージェントは配置できない。
            foreach (var agent in existAgents)
            {
                canAction[agent.AgentID] = true;
                var nextP = agent.GetNextPoint();
                if (!CheckIsPointInBoard(nextP))
                    canAction[agent.AgentID] = false;
                TeamColor nextAreaState = Data.CellData[nextP.X, nextP.Y].AreaState;
                if(agent.State == AgentState.Move)
                    if ((agent.PlayerNum == TeamColor.Player1 && nextAreaState == TeamColor.Player2) || (agent.PlayerNum == TeamColor.Player2 && nextAreaState == TeamColor.Player1))
                        canAction[agent.AgentID] = false;
            }

            //次に、「指示先(agent.GetNextPoint()の位置)が被っているエージェントは行動不可」とする。
            foreach (var agent1 in existAgents)
            {
                var nextP1 = agent1.GetNextPoint();
                foreach (var agent2 in existAgents)
                {
                    if (agent1.AgentID <= agent2.AgentID)
                        break;
                    var nextP2 = agent2.GetNextPoint();
                    if (nextP1 == nextP2)
                    {
                        canAction[agent1.AgentID] = false;
                        canAction[agent2.AgentID] = false;
                    }
                }
            }

            //次に、「指示先に移動不可な(orタイル除去をする)エージェントがいる場合、行動不可」とする。
            //このチェックは, 先ほどのように何回もwhileループで回す必要がない。
            foreach (var agent1 in existAgents)
            {
                if (canAction[agent1.AgentID]) { continue; }
                var nextP1 = agent1.GetNextPoint();
                foreach (var agent2 in existAgents)
                {
                    if (
                        agent1.AgentID == agent2.AgentID ||
                        canMove[agent2.AgentID]
                    ) { continue; }
                    var nextP2 = agent2.GetNextPoint();
                    if (nextP1 == nextP2)
                    {
                        canAction[agent1.AgentID] = false;
                        break;
                    }
                }
            }

            //この時点でcanAction[i] == trueならば、エージェントiは行動可能である
            //よって、行動可能なエージェントの番号を返すことができる
            List<Agent> ret = new List<Agent>();
            foreach (var agent in existAgents)
                if (canAction[agent.AgentID])
                    ret.Add(agent);
            return ret;
        }

        private void GetScore()
        {
            for (int i = 0; i < App.PlayersCount; i++)
                viewModel.Players[i].Score = ScoreCalculator.CalcScore(i, Data.CellData);
        }

        private void ActionAgentToNextP(Agent agent, Point nextP, TeamColor nextAreaState)
        {
            switch (agent.State)
            {
                case AgentState.Move:
                    var against = agent.PlayerNum == TeamColor.Player1 ? TeamColor.Player2 : TeamColor.Player1;

                    if(nextAreaState != against)
                        agent.Point = nextP;
                    break;
                case AgentState.RemoveTile:
                    Data.CellData[nextP.X, nextP.Y].AreaState = TeamColor.Free;
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
            for (int i = 0; i < Data.MaximumAgentsCount; ++i)
            {
                if (viewModel.Players[index].AgentViewModels[i].Data.State == AgentState.NonPlaced)
                {
                    viewModel.Players[index].AgentViewModels[i].Data.Point = decide.Agents[i];
                    viewModel.Players[index].AgentViewModels[i].Data.State = AgentState.PlacePending;
                }
                else
                {
                    sbyte x = (sbyte)(decide.Agents[i].X - Data.Players[index].Agents[i].Point.X);
                    sbyte y = (sbyte)(decide.Agents[i].Y - Data.Players[index].Agents[i].Point.Y);
                    viewModel.Players[index].AgentViewModels[i].Data.AgentDirection = DirectionExtensions.CastPointToDir((x, y));
                    viewModel.Players[index].AgentViewModels[i].Data.State = decide.AgentsState[i];
                }
            }
        }

        public void RequestAnswer(){
            Server.SendRequestAnswer();
        }

        private void Draw(int remainingTime)
        {
            viewModel.TimerStr = Data.IsNextTurnStart ? $"TIME:{remainingTime/1000.0:F2}/{Data.TimeLimitMilliseconds/1000.0:F2}" : "NEXT TURN";
            viewModel.TurnStr = $"TURN:{Data.NowTurn}/{Data.FinishTurn}";
        }

        private bool CheckIsPointInBoard(Point p) => (uint)p.X < Data.BoardWidth && (uint)p.Y < Data.BoardHeight;
    }
}
