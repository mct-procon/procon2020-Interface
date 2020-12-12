using System;
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
        public MCTProcon31Protocol.Json.ProconAPIClient ApiClient { get; private set; } = null;
        public void CreateClient()
        {
            if (ApiClient is null)
                ApiClient = new MCTProcon31Protocol.Json.ProconAPIClient(serverToken, endPoint);
            else
                ApiClient.ChangeTokenAndEndPoint(serverToken, endPoint);
        }
        public void DeleteClient()
        {
            Task.Run(() =>
            {
                if (ApiClient == null) return;
                var client = ApiClient;
                ApiClient = null;
                client.Dispose();
            });
        }

        private int limitTime = 5000;

        /// <summary>
        /// Whether enable self game conduction.
        /// </summary>
        public bool IsEnableGameConduct => BoardCreation != BoardCreation.Server;

        /// <summary>
        /// Limitation Time [MilliSeconds]
        /// </summary>
        public int LimitTime {
            get => limitTime;
            set {
                ResetError();
                if (value < 0)
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

        private string hostname1P = "localhost";

        /// <summary>
        /// AI Hostname 1P.
        /// </summary>
        public string Hostname1P {
            get => hostname1P;
            set => RaisePropertyChanged(ref hostname1P, value);
        }

        private ushort port2P = 0;

        /// <summary>
        /// AI TCP/IP Port 2P.
        /// </summary>
        public ushort Port2P {
            get => port2P;
            set => RaisePropertyChanged(ref port2P, value);
        }

        private string hostname2P = "localhost";

        /// <summary>
        /// AI Hostname 2P.
        /// </summary>
        public string Hostname2P {
            get => hostname2P;
            set => RaisePropertyChanged(ref hostname2P, value);
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

        private byte turns = 60;

        private int agentsCount = 6;
        /// <summary>
        /// Count of Agents
        /// </summary>
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

        private string endPoint = "http://";
        public string EndPoint {
            get => endPoint;
            set {
                if (value.StartsWith("http://"))
                    endPoint = value;
                else
                    endPoint = "http://" + value;
                if (!endPoint.EndsWith("/"))
                    endPoint = endPoint + "/";
                RaisePropertyChanged();
            }
        }

        private string serverToken = "";
        /// <summary>
        /// Token of Server
        /// </summary>
        public string ServerToken {
            get => serverToken;
            set => RaisePropertyChanged(ref serverToken, value);
        }

        private MCTProcon31Protocol.Json.Matches.MatchInformation[] matches = null;
        /// <summary>
        /// Matches information
        /// </summary>
        public MCTProcon31Protocol.Json.Matches.MatchInformation[] Matches {
            get => matches;
            set {
                RaisePropertyChanged(ref matches, value);
                SelectedMatchIndex = value == null || value.Length <= 0 ? -1 : 0;
            }
        }

        private int selectedMatchIndex = -1;
        /// <summary>
        /// Selected match index
        /// </summary>
        public int SelectedMatchIndex {
            get => selectedMatchIndex;
            set => RaisePropertyChanged(ref selectedMatchIndex, value);
        }

        /// <summary>
        /// Whether the AI used in before game use.
        /// </summary>
        public bool IsUseSameAI { get; set; } = false;

        private BoardSymmetry creationSymmetry = BoardSymmetry.None;
        /// <summary>
        /// Board creation mode
        /// </summary>
        public BoardSymmetry CreationSymmetry {
            get => creationSymmetry;
            set {
                RaisePropertyChanged(ref creationSymmetry, value);
                RaisePropertyChanged(nameof(IsCreateX));
                RaisePropertyChanged(nameof(IsCreateY));
                RaisePropertyChanged(nameof(IsCreateRotate));
            }
        }

        /// <summary>
        /// Create x-axis symmetric
        /// </summary>
        public bool IsCreateX {
            get => creationSymmetry.HasFlag(BoardSymmetry.X);
            set {
                if(value)
                {
                    if (!creationSymmetry.HasFlag(BoardSymmetry.Rotate))
                        CreationSymmetry = BoardSymmetry.X;
                    else
                        CreationSymmetry |= BoardSymmetry.X;
                }
                else
                {
                    CreationSymmetry = creationSymmetry & ~BoardSymmetry.X;
                }
            }
        }

        /// <summary>
        /// Create y-axis symmetric
        /// </summary>
        public bool IsCreateY {
            get => creationSymmetry.HasFlag(BoardSymmetry.Y);
            set {
                if (value)
                {
                    if (!creationSymmetry.HasFlag(BoardSymmetry.Rotate))
                        CreationSymmetry = BoardSymmetry.Y;
                    else
                        CreationSymmetry |= BoardSymmetry.Y;
                }
                else
                {
                    CreationSymmetry = creationSymmetry & ~BoardSymmetry.Y;
                }
            }
        }

        /// <summary>
        /// Create rotation symmetric
        /// </summary>
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
    
    [Flags]
    public enum BoardSymmetry : byte
    {
        None = 0, X = 1, Y = 2, XY = 3, Rotate = 4
    }
}
