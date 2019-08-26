using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using GameInterface.GameManagement;

namespace GameInterface.Cells
{
    public class Cell : ViewModels.ViewModelBase
    {
        private int score;
        public int Score
        {
            get => score; 
            set => RaisePropertyChanged(ref score, value);
        }

        private TeamColor areaState = TeamColor.Free;
        public TeamColor AreaState 
        {
            get => areaState;
            set => RaisePropertyChanged(ref areaState, value);
        }

        private TeamColor agentState = TeamColor.Free;
        public TeamColor AgentState 
        {
            get => agentState;
            set => RaisePropertyChanged(ref agentState, value);
        }

        private TeamColor surroundedState = TeamColor.Free;
        public TeamColor SurroundedState {
            get => surroundedState;
            set => RaisePropertyChanged(ref surroundedState, value);
        }

        private int agentNum = -1;
        public int AgentNum {
            get => agentNum;
            set {
                RaisePropertyChanged(ref agentNum, value);
                RaisePropertyChanged(nameof(AgentNumVisibility));
            }
        }

        public Visibility AgentNumVisibility {
            get => agentNum < 0 ? Visibility.Hidden : Visibility.Visible;
        }

        public Cell() { }
        public Cell(int _score)
        {
            Score = _score;
        }
    }
}
