using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Windows;
using GameInterface.Cells;
using System.Windows.Threading;
using MCTProcon31Protocol.Methods;
using System.Linq;
using GameInterface.GameManagement;
using MCTProcon31Protocol;
using Point = MCTProcon31Protocol.Point;

namespace GameInterface.ViewModels
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
            set => RaisePropertyChanged(ref timerStr, value);
        }
        public int TimeLimitMilliseconds {
            get => gameManager.Data.TimeLimitMilliseconds;
            set {
                gameManager.Data.TimeLimitMilliseconds = value;
                RaisePropertyChanged();
            }
        }

        private string turnStr;
        public string TurnStr
        {
            get => turnStr;
            set => RaisePropertyChanged(ref turnStr, value);
        }

        private PlayerWindowViewModel[] players;
        public PlayerWindowViewModel[] Players
        {
            get => players;
            set => RaisePropertyChanged(ref players, value);
        }
    }
}
