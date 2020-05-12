﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameInterface.GameManagement;

namespace GameInterface.GameSettings
{
    /// <summary>
    /// Game Settings Structure
    /// </summary>
    public class SettingStructure : ViewModels.NotifyDataErrorInfoViewModel
    {
        private ushort limitTime = 5;

        public bool IsEnableGameConduct => BoardCreation != BoardCreation.Server;

        /// <summary>
        /// Limitation Time [Seconds]
        /// </summary>
        public ushort LimitTime {
            get => limitTime;
            set {
                ResetError();
                if (value == 0)
                    AddError("ターン時間は0秒以上でなければなりません．");
                RaisePropertyChanged(ref limitTime, value);
            }
        }

        private ushort port1P = 0;

        /// <summary>
        /// AI TCP/IP Port 1P.
        /// </summary>
        public ushort Port1P {
            get => port1P;
            set => RaisePropertyChanged(ref port1P, value);
        }

        private ushort port2P = 0;

        /// <summary>
        /// AI TCP/IP Port 2P.
        /// </summary>
        public ushort Port2P {
            get => port2P;
            set => RaisePropertyChanged(ref port2P, value);
        }

        /// <summary>
        /// Whether 1P is a user.
        /// </summary>
        public bool IsUser1P => port1P == 0;

        /// <summary>
        /// Whether 2P is a user.
        /// </summary>
        public bool IsUser2P => port2P == 0;

        private byte boardCreationState = 0;

        /// <summary>
        /// Board Creation State
        /// </summary>
        public byte _BoardCreationState {
            get => boardCreationState;
            set {
                RaisePropertyChanged(ref boardCreationState, value);
                RaisePropertyChanged(nameof(IsEnableGameConduct));
            }
        }

        internal BoardCreation BoardCreation => (BoardCreation)_BoardCreationState;

        //internal Cells.Cell[,] JsonCell { get; set; }
        //internal Agent[] JsonAgent { get; set; }

        private byte turns = 60;

        private int agentsCount = 6;
        public int AgentsCount {
            get => agentsCount;
            set {
                if (value < 6)
                    AddError("エージェントの数は6以上でなければなりません．");
                if (value > 16)
                    AddError("エージェントの数は16以下でなければなりません．");
                RaisePropertyChanged(ref agentsCount, value);
            }
        }

        /// <summary>
        /// Turn Counts
        /// </summary>
        public byte Turns {
            get => turns;
            set {
                ResetError();
                if (value < 1)
                    AddError("ターン数は1以上でないといけません");
                RaisePropertyChanged(ref turns, value);
            }
        }

        private byte boardWidth = 12;

        /// <summary>
        /// Width of Board
        /// </summary>
        public byte BoardWidth {
            get => boardWidth;
            set {
                ResetError();
                if (value < 12)
                    AddError("フィールドの幅は12以上でなければなりません");
                if(value > 24)
                    AddError("フィールドの幅は24以下でなければなりません");
                RaisePropertyChanged(ref boardWidth, value);
            }
        }

        private byte boardHeight = 12;

        /// <summary>
        /// Height of Bord
        /// </summary>
        public byte BoardHeight {
            get => boardHeight;
            set {
                ResetError();
                if(value < 12)
                    AddError("フィールドの高さは12以上でなければなりません");
                if (value > 24)
                    AddError("フィールドの高さは24以下でなければなりません");
                RaisePropertyChanged(ref boardHeight, value);
            }
        }

        private bool isAutoSkip = false;

        /// <summary>
        /// Whether every turn skip automatic.
        /// </summary>
        public bool IsAutoSkip {
            get => isAutoSkip;
            set => RaisePropertyChanged(ref isAutoSkip, value);
        }

        private bool isAutoGoNextGame = false;

        /// <summary>
        /// Whether go next game automatic when a game end.
        /// </summary>
        public bool IsAutoGoNextGame
        {
            get => isAutoGoNextGame;
            set => RaisePropertyChanged(ref isAutoGoNextGame, value);
        }

        public string HostName {
            get => Network.ProconAPIClient.Information.HostName;
            set {
                Network.ProconAPIClient.Information.HostName = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Token of Server
        /// </summary>
        public string ServerToken {
            get => Network.ProconAPIClient.Information.AuthenticationID;
            set {
                Network.ProconAPIClient.Information.AuthenticationID = value;
                RaisePropertyChanged();
            }
        }

        private MCTProcon31Protocol.Json.Match[] matches = null;
        public MCTProcon31Protocol.Json.Match[] Matches {
            get => matches;
            set {
                RaisePropertyChanged(ref matches, value);
                SelectedMatchIndex = value == null || value.Length <= 0 ? -1 : 0;
            }
        }

        private int selectedMatchIndex = -1;
        public int SelectedMatchIndex {
            get => selectedMatchIndex;
            set => RaisePropertyChanged(ref selectedMatchIndex, value);
        }

        /// <summary>
        /// Whether the AI used in before game use.
        /// </summary>
        public bool IsUseSameAI { get; set; } = false;

        private BoardSymmetry creationSymmetry = BoardSymmetry.XY;
        public BoardSymmetry CreationSymmetry {
            get => creationSymmetry;
            set {
                RaisePropertyChanged(ref creationSymmetry, value);
                RaisePropertyChanged(nameof(IsCreateX));
                RaisePropertyChanged(nameof(IsCreateY));
                RaisePropertyChanged(nameof(IsCreateRotate));
            }
        }

        public bool IsCreateX {
            get => (creationSymmetry & BoardSymmetry.X) != 0;
            set {
                if(value)
                {
                    if ((creationSymmetry & BoardSymmetry.Rotate) != 0)
                        CreationSymmetry = BoardSymmetry.X;
                    else
                        CreationSymmetry = creationSymmetry | BoardSymmetry.X;
                }
                else
                {
                    if ((creationSymmetry & ~BoardSymmetry.X) != 0)
                        CreationSymmetry = creationSymmetry & ~BoardSymmetry.X;
                }
            }
        }

        public bool IsCreateY {
            get => (creationSymmetry & BoardSymmetry.Y) != 0;
            set {
                if (value)
                {
                    if ((creationSymmetry & BoardSymmetry.Rotate) != 0)
                        CreationSymmetry = BoardSymmetry.Y;
                    else
                        CreationSymmetry = creationSymmetry | BoardSymmetry.Y;
                }
                else
                {
                    if ((creationSymmetry & ~BoardSymmetry.Y) != 0)
                        CreationSymmetry = creationSymmetry & ~BoardSymmetry.Y;
                }
            }
        }

        public bool IsCreateRotate {
            get => creationSymmetry == BoardSymmetry.Rotate;
            set {
                if (value)
                    CreationSymmetry = BoardSymmetry.Rotate;
            }
        }
    }

    public enum BoardCreation : byte
    {
        Random = 0, Server = 1, JsonFile = 2
    }
    
    public enum BoardSymmetry : byte
    {
        X = 1, Y = 2, XY = 3, Rotate = 4
    }
}
