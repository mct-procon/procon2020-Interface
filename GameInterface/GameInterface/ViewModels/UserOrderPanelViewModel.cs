using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameInterface.ViewModels
{
    public class UserOrderPanelViewModel : ViewModels.ViewModelBase
    {
        private Agent data;
        public Agent Data {
            get => data;
            set {
                if (!(data is null))
                    data.PropertyChanged -= DataPropertyChanged;
                RaisePropertyChanged(ref data, value);
                data.PropertyChanged += DataPropertyChanged;
                RaisePropertyChanged(nameof(IsUpLeft));
                RaisePropertyChanged(nameof(IsLeft));
                RaisePropertyChanged(nameof(IsDownLeft));
                RaisePropertyChanged(nameof(IsUp));
                RaisePropertyChanged(nameof(IsNone));
                RaisePropertyChanged(nameof(IsDown));
                RaisePropertyChanged(nameof(IsUpRight));
                RaisePropertyChanged(nameof(IsRight));
                RaisePropertyChanged(nameof(IsDownRight));
            }
        }
        public UserOrderPanelViewModel(Agent agent)
        {
            Data = agent;
            Data.PropertyChanged += DataPropertyChanged;
        }

        public bool IsUpLeft {
            get => Data.AgentDirection == Agent.Direction.UP_LEFT;
            set => Data.AgentDirection = Agent.Direction.UP_LEFT;
        }

        public bool IsLeft {
            get => Data.AgentDirection == Agent.Direction.LEFT;
            set => Data.AgentDirection = Agent.Direction.LEFT;
        }
        public bool IsDownLeft {
            get => Data.AgentDirection == Agent.Direction.DOWN_LEFT;
            set => Data.AgentDirection = Agent.Direction.DOWN_LEFT;
        }

        public bool IsUp {
            get => Data.AgentDirection == Agent.Direction.UP;
            set => Data.AgentDirection = Agent.Direction.UP;
        }

        public bool IsNone {
            get => Data.AgentDirection == Agent.Direction.NONE;
            set => Data.AgentDirection = Agent.Direction.NONE;
        }

        public bool IsDown {
            get => Data.AgentDirection == Agent.Direction.DOWN;
            set => Data.AgentDirection = Agent.Direction.DOWN;
        }

        public bool IsUpRight {
            get => Data.AgentDirection == Agent.Direction.UP_RIGHT;
            set => Data.AgentDirection = Agent.Direction.UP_RIGHT;
        }

        public bool IsRight {
            get => Data.AgentDirection == Agent.Direction.RIGHT;
            set => Data.AgentDirection = Agent.Direction.RIGHT;
        }

        public bool IsDownRight {
            get => Data.AgentDirection == Agent.Direction.DOWN_RIGHT;
            set => Data.AgentDirection = Agent.Direction.DOWN_RIGHT;
        }

        private void DataPropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            if(args.PropertyName == nameof(data.AgentDirection))
            {
                RaisePropertyChanged(nameof(IsUpLeft));
                RaisePropertyChanged(nameof(IsLeft));
                RaisePropertyChanged(nameof(IsDownLeft));
                RaisePropertyChanged(nameof(IsUp));
                RaisePropertyChanged(nameof(IsNone));
                RaisePropertyChanged(nameof(IsDown));
                RaisePropertyChanged(nameof(IsUpRight));
                RaisePropertyChanged(nameof(IsRight));
                RaisePropertyChanged(nameof(IsDownRight));
            }
        }

        ~UserOrderPanelViewModel()
        {
            if (!(data is null))
                data.PropertyChanged -= DataPropertyChanged;
        }
    }
}
