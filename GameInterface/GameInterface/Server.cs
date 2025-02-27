﻿using System;
using System.Windows;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MCTProcon31Protocol.Methods;
using MCTProcon31Protocol;
using System.Threading;
using System.Diagnostics;
using GameInterface.GameManagement;

namespace GameInterface
{
    internal class ClientRennenend : IIPCServerReader
    {
        Server server;
        GameManager gameManager;
        private int managerNum;
        public ClientRennenend(Server server_, GameManager gameManager_, int managerNum_)
        {
            this.gameManager = gameManager_;
            this.managerNum = managerNum_;
            this.server = server_;
        }
        public void OnConnect(Connect connect)
        {
            gameManager.viewModel.MainWindowDispatcher.Invoke(connectMethod);
        }

        private void connectMethod()
        {
            if (managerNum == 0)
                server.IsConnected1P = true;
            else
                server.IsConnected2P = true;
        }

        public void OnDecided(Decided decided)
        {
            gameManager.viewModel.MainWindowDispatcher.Invoke(() =>
            {
                gameManager.SetDecisions(managerNum, decided);
            });
            gameManager.SendActionToAPIServer();
        }

        public void OnInterrupt(Interrupt interrupt)
        {
            gameManager.viewModel.MainWindowDispatcher.Invoke(__onInterrupt);
        }

        private void __onInterrupt()
        {
            MessageBox.Show($"{managerNum + 1}P is disconnected.");
        }

        public void OnAIProcessExited(IIPCServerReader sender, EventArgs e)
        {
            ClientRennenend cr = (ClientRennenend)sender;
            if (gameManager.Data.IsGameStarted)
                gameManager.viewModel.MainWindowDispatcher.BeginInvoke((Action)__onAIProcessExited);
        }

        private void __onAIProcessExited()
        {
            gameManager.PauseGame();
            if (managerNum == 0)
                gameManager.Server.IsConnected1P = false;
            else
                gameManager.Server.IsConnected2P = false;
            if ( MessageBox.Show($"{managerNum + 1}P' AI Process has exited, Do you want to Reconnect?", "Reconnection", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes )
            {

                GameSettings.ReconnectDialog dig = new GameSettings.ReconnectDialog(gameManager.Server, managerNum);
                Task.Run(() => server.Reconnect(managerNum, gameManager.Data.CurrentGameSettings));
                dig.ShowDialog();
                server.SendConnect(managerNum);
                server.SendGameInit(managerNum);
                int remaining = (int)(gameManager.Data.NextTime - DateTime.UtcNow).TotalMilliseconds;
                server.SendTurnStart(managerNum, Enumerable.Range(0, gameManager.Data.MaximumAgentsCount).Select(x => true).ToArray(), remaining > 0 ? remaining : 1000);
            }
            else
            {
                gameManager.Server.Shutdown(managerNum);
            }
            gameManager.RerunGame();
        }
    }

    class Server : ViewModels.ViewModelBase
    {
        IPCManager[] managers = new IPCManager[2];
        GameData data;
        GameManager gameManager;
        private int[] previousPort = new int[] { -1, -1 };
        private bool[] isConnected = new bool[] { false, false };
        public bool IsConnected1P
        {
            get => isConnected[0];
            set => RaisePropertyChanged(ref isConnected[0], value);
        }

        public bool IsConnected2P
        {
            get => isConnected[1];
            set => RaisePropertyChanged(ref isConnected[1], value);
        }

        public bool[] IsDecidedReceived = new bool[] { false, false };


        public Server(GameManager gameManager)
        {
            this.gameManager = gameManager;
            data = gameManager.Data;
            if(App.Current != null)
                App.Current.Exit += (obj, e) =>
                {
                    foreach (var man in managers)
                    {
                        try
                        {
                            man?.Shutdown();
                        }
                        catch { }
                    }
                };
        }

        public void StartListening(GameSettings.SettingStructure settings)
        {
            if (settings.IsUseSameAI) return;
            if (!settings.IsUser1P)
            {
                if (isConnected[0])
                {
                    if (settings.Port1P != previousPort[0])
                    {
                        Shutdown(0);
                        isConnected[0] = false;
                        managers[0] = new IPCManager(new ClientRennenend(this, gameManager, 0));
                        Task.Run(async () =>
                        {
                            await managers[0].Connect(settings.Port1P, false);
                            previousPort[0] = settings.Port1P;
                            await managers[0].StartAsync();
                        });
                    }
                }
                else
                {
                    managers[0] = new IPCManager(new ClientRennenend(this, gameManager, 0));
                    Task.Run(async () =>
                    {
                        await managers[0].Connect(settings.Port1P, false);
                        previousPort[0] = settings.Port1P;
                        await managers[0].StartAsync();
                    });
                }
            }
            else
                Shutdown(0);
            if (!settings.IsUser2P)
            {
                if (isConnected[1])
                {
                    if (settings.Port2P != previousPort[1])
                    {
                        Shutdown(1);
                        isConnected[1] = false;
                        managers[1] = new IPCManager(new ClientRennenend(this, gameManager, 0));
                        Task.Run(async () =>
                        {
                            await managers[1].Connect(settings.Port2P, false);
                            previousPort[1] = settings.Port2P;
                            await managers[1].StartAsync();
                        });
                    }
                }
                else
                {
                    managers[1] = new IPCManager(new ClientRennenend(this, gameManager, 1));
                    Task.Run(async () =>
                    {
                        await managers[1].Connect(settings.Port2P, false);
                        previousPort[1] = settings.Port2P;
                        await managers[1].StartAsync();
                    });
                }
            }
            else
                Shutdown(1);
        }

        private void startListening(int playerNum, int portId)
        {
            managers[playerNum] = new IPCManager(new ClientRennenend(this, gameManager, playerNum));
            Task.Run(async () =>
            {
                await managers[playerNum].Connect(portId, false);
                await managers[playerNum].StartAsync();
            });
        }

        public void Reconnect(int playerNum, GameSettings.SettingStructure settings)
        {
            managers[playerNum].Shutdown();
            startListening(playerNum, playerNum == 0 ? settings.Port1P : settings.Port2P);
        }

        public void SendGameInit()
        {
            SendGameInit(0);
            SendGameInit(1);
        }

        public void SendConnect(int playerNum)
        {
            if (!isConnected[playerNum]) return;
            using (System.Diagnostics.Process proc = System.Diagnostics.Process.GetCurrentProcess())
                managers[playerNum].Write(DataKind.Connect, new Connect(ProgramKind.Interface) { ProcessId = proc.Id });
        } 

        public void SendGameInit(int playerNum)
        {
            if (!isConnected[playerNum]) return;
            sbyte[,] board = new sbyte[data.BoardWidth, data.BoardHeight];
            for (int i = 0; i < data.BoardWidth; i++)
            {
                for (int j = 0; j < data.BoardHeight; j++)
                {
                    board[i, j] = (sbyte)data.CellData[i, j].Score;
                }
            }
            managers[playerNum].Write(DataKind.GameInit, new GameInit((byte)data.BoardHeight, (byte)data.BoardWidth, board, (byte)data.MaximumAgentsCount, data.FinishTurn));
        }

        public void SendTurnStart(bool[] movable, int remainingMilliseconds)
        {
            SendTurnStart(0, movable.Take(movable.Length / 2).ToArray(), remainingMilliseconds);
            SendTurnStart(1, movable.Skip(movable.Length / 2).ToArray(), remainingMilliseconds);
        }

        public void SendTurnStart(int playerNum, bool[] isAgentsMoved, int remainingMilliseconds)
        {
            IsDecidedReceived[playerNum] = false;
            if (!isConnected[playerNum]) return;

            ColoredBoardNormalSmaller colorBoardMe = new ColoredBoardNormalSmaller((uint)data.BoardWidth, (uint)data.BoardHeight);
            ColoredBoardNormalSmaller colorBoardEnemy = new ColoredBoardNormalSmaller((uint)data.BoardWidth, (uint)data.BoardHeight);
            ColoredBoardNormalSmaller surroundedBoardMe = new ColoredBoardNormalSmaller((uint)data.BoardWidth, (uint)data.BoardHeight);
            ColoredBoardNormalSmaller surroundedBoardEnemy = new ColoredBoardNormalSmaller((uint)data.BoardWidth, (uint)data.BoardHeight);


            for (int i = 0; i < data.BoardWidth; i++)
                for (int j = 0; j < data.BoardHeight; j++)
                {
                    if (data.CellData[i, j].AreaState == TeamColor.Player1)
                        colorBoardMe[(uint)i, (uint)j] = true;
                    else if (data.CellData[i, j].AreaState == TeamColor.Player2)
                        colorBoardEnemy[(uint)i, (uint)j] = true;
                    else if (data.CellData[i, j].SurroundedState == TeamColor.Player1)
                        surroundedBoardMe[(uint)i, (uint)j] = true;
                    else if (data.CellData[i, j].SurroundedState == TeamColor.Player2)
                        surroundedBoardEnemy[(uint)i, (uint)j] = true;
                }

            if (playerNum == 1) Swap(ref colorBoardMe, ref colorBoardEnemy);
            managers[playerNum].Write(DataKind.TurnStart, new TurnStart((byte)data.NowTurn, remainingMilliseconds,
                Unsafe16Array.Create(data.Players[playerNum == 0 ? 0 : 1].Agents.Select(item => item.Point).ToArray()),
                Unsafe16Array.Create(data.Players[playerNum == 0 ? 1 : 0].Agents.Select(item => item.Point).ToArray()),
                colorBoardMe,
                colorBoardEnemy,
                Unsafe16Array.Create(isAgentsMoved),
                surroundedBoardMe,
                surroundedBoardEnemy,
                Unsafe16Array.Create(data.Players[playerNum == 0 ? 0 : 1].Agents.Select(item => item.State == AgentState.Move ? AgentState.Move : AgentState.NonPlaced).ToArray()),
                Unsafe16Array.Create(data.Players[playerNum == 0 ? 1 : 0].Agents.Select(item => item.State == AgentState.Move ? AgentState.Move : AgentState.NonPlaced).ToArray())
                ));
        }

        public void SendRequestAnswer(){
            SendRequestAnswer(0);
            SendRequestAnswer(1);
        }

        public void SendRequestAnswer(int playerNum){
            if (!isConnected[playerNum]) return;
            managers[playerNum].Write(DataKind.RequestAnswer, new RequestAnswer((byte)data.NowTurn));
        }

        public void SendTurnEnd()
        {
            SendTurnEnd(0);
            SendTurnEnd(1);
        }

        private void SendTurnEnd(int playerNum)
        {
            IsDecidedReceived[playerNum] = true;
            if (!isConnected[playerNum]) return;
            managers[playerNum].Write(DataKind. TurnEnd, new TurnEnd((byte)data.NowTurn));
        }

        public void SendGameEnd()
        {
            int score = data.Players[0].Score, enemyScore = data.Players[1].Score;
            for (int i = 0; i < App.PlayersCount; i++)
            {
                if (!isConnected[i]) continue;
                managers[i].Write(DataKind.GameEnd, new GameEnd(score, enemyScore));
                Swap<int>(ref score, ref enemyScore);
            }
        }

        public void Shutdown(int playerNum) {
            if (!isConnected[playerNum]) return;
            managers[playerNum]?.Shutdown();
        }

        private void Swap<T>(ref T lhs, ref T rhs)
        {
            T temp;
            temp = lhs;
            lhs = rhs;
            rhs = temp;
        }
    }
}
