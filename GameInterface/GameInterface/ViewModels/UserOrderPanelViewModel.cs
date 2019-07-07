using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using GameInterface.GameManagement;

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
                RaiseAll();
            }
        }
        public UserOrderPanelViewModel(Agent agent)
        {
            Data = agent;
            Data.PropertyChanged += DataPropertyChanged;
        }

        public bool IsUpLeft {
            get => Data.AgentDirection == Direction.UpLeft;
            set => Data.AgentDirection = Direction.UpLeft;
        }

        public bool IsLeft {
            get => Data.AgentDirection == Direction.Left;
            set => Data.AgentDirection = Direction.Left;
        }
        public bool IsDownLeft {
            get => Data.AgentDirection == Direction.DownLeft;
            set => Data.AgentDirection = Direction.DownLeft;
        }

        public bool IsUp {
            get => Data.AgentDirection == Direction.Up;
            set => Data.AgentDirection = Direction.Up;
        }

        public bool IsNone {
            get => Data.AgentDirection == Direction.None;
            set => Data.AgentDirection = Direction.None;
        }

        public bool IsDown {
            get => Data.AgentDirection == Direction.Down;
            set => Data.AgentDirection = Direction.Down;
        }

        public bool IsUpRight {
            get => Data.AgentDirection == Direction.UpRight;
            set => Data.AgentDirection = Direction.UpRight;
        }

        public bool IsRight {
            get => Data.AgentDirection == Direction.Right;
            set => Data.AgentDirection = Direction.Right;
        }

        public bool IsDownRight {
            get => Data.AgentDirection == Direction.DownRight;
            set => Data.AgentDirection = Direction.DownRight;
        }

        private void RaiseAll()
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

        private void DataPropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            if (args.PropertyName == nameof(data.AgentDirection))
                RaiseAll();
        }

        ~UserOrderPanelViewModel()
        {
            if (!(data is null))
                data.PropertyChanged -= DataPropertyChanged;
        }
    }
}
