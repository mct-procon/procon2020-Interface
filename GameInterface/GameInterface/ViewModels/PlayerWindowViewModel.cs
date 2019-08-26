using GameInterface.GameManagement;
using MCTProcon30Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameInterface.ViewModels
{
    public class PlayerWindowViewModel : ViewModelBase
    {
        public GameManagement.Player Data { get; set; }
        public int PlayerNum { get; set; }
        public int Score {
            get => Data.Score;
            set {
                Data.Score = value;
                RaisePropertyChanged();
            }
        }

        public Agent[] Agents {
            get => Data.Agents;
            set {
                Data.Agents = value;
                RaisePropertyChanged();
            }
        }

        public List<Decision> Decisions {
            get => Data.Decisions;
            set {
                Data.Decisions = value;
                RaisePropertyChanged();
            }
        }

        public int DecisionsSelectedIndex {
            get => Data.DecisionsSelectedIndex;
            set {
                Data.DecisionsSelectedIndex = value;
                RaisePropertyChanged();
            }
        }

        private UserOrderPanelViewModel[] agentViewModels;
        public UserOrderPanelViewModel[] AgentViewModels {
            get => agentViewModels;
            set => RaisePropertyChanged(ref agentViewModels, value);
        }

        public void RaiseDecisionsChanged()
        {
            RaisePropertyChanged(nameof(Decisions));
            RaisePropertyChanged(nameof(DecisionsSelectedIndex));
        }

        public PlayerWindowViewModel(Player data, int playerNum)
        {
            Data = data;
            PlayerNum = playerNum;
        }
    }
}
