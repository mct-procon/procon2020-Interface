using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Windows;
using GameInterface.Cells;
using System.Windows.Threading;
using MCTProcon30Protocol.Methods;
using System.Linq;
using GameInterface.GameManagement;
using MCTProcon30Protocol;
using Point = MCTProcon30Protocol.Point;

namespace GameInterface
{
    public class MainWindowViewModel : ViewModels.ViewModelBase
    {
        public GameManager gameManager;

        public Dispatcher MainWindowDispatcher;

        //---------------------------------------
        //画面に表示する変数
        private Cell[,] cellData = new Cell[12,12];
        public Cell[,] CellData
        {
            get => cellData;
            set => RaisePropertyChanged(ref cellData, value);
        }
        private string timerStr;
        public string TimerStr
        {
            get => timerStr; 
            set
            {
                timerStr = value;
                RaisePropertyChanged("TimerStr");
            }
        }
        public int TimeLimitSeconds {
            get => gameManager.Data.TimeLimitSeconds;
            set {
                gameManager.Data.TimeLimitSeconds = value;
                RaisePropertyChanged();
            }
        }

        private ViewModels.UserOrderPanelViewModel[] agentViewModels;
        public ViewModels.UserOrderPanelViewModel[] AgentViewModels {
            get => agentViewModels;
            set => RaisePropertyChanged(ref agentViewModels, value);
        }

        private int[] playerScores = new int[2];
        public int[] PlayerScores
        {
            get => playerScores;
            set
            {
                playerScores = value;
                RaisePropertyChanged();
            }
        }

        private string turnStr;
        public string TurnStr
        {
            get => turnStr;
            set => RaisePropertyChanged(ref turnStr, value);
        }

        public void RaiseDecisionsChanged()
        {
            RaisePropertyChanged("Decisions1P");
            RaisePropertyChanged("Decisions2P");
        }
        public List<Decision> Decisions1P {
            get => gameManager.Data.Decisions[0];
            set {
                gameManager.Data.Decisions[0] = value;
                RaisePropertyChanged();
            }
        }

        private int decisions1PSelectedIndex = -1;
        public int Decisions1PSelectedIndex {
            get => decisions1PSelectedIndex;
            set => RaisePropertyChanged(ref decisions1PSelectedIndex, value);
        }

        public List<Decision> Decisions2P {
            get => gameManager.Data.Decisions[1];
            set {
                gameManager.Data.Decisions[1] = value;
                RaisePropertyChanged();
            }
        }
        private int decisions2PSelectedIndex = -1;
        public int Decisions2PSelectedIndex {
            get => decisions2PSelectedIndex;
            set => RaisePropertyChanged(ref decisions2PSelectedIndex, value);
        }

        public DelegateCommand<Point> ChangeColorCommand { get; private set; }

        public MainWindowViewModel()
        {
            agentViewModels = Enumerable.Range(0,4).Select(x => new ViewModels.UserOrderPanelViewModel(new Agent())).ToArray();
            ChangeColorCommand = new DelegateCommand<Point>(ChangeColor);
        }

        private void ChangeColor(Point point)
        {
            gameManager.ChangeCellToNextColor(point);
        }
    }
}
