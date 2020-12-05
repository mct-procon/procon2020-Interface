using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using GameInterface.GameManagement;
using AgentState = MCTProcon31Protocol.AgentState;

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
                RaisePropertyChanged(nameof(IsRemoveMode));
            }
        }
        public UserOrderPanelViewModel(Agent agent)
        {
            Data = agent;
            Data.PropertyChanged += DataPropertyChanged;
        }

        public bool IsNotPlaced {
            get => !Data.IsPlaced;
        }

        public bool IsUpLeft {
            get => Data.AgentDirection == AgentDirection.UpLeft;
            set => Data.AgentDirection = AgentDirection.UpLeft;
        }

        public bool IsLeft {
            get => Data.AgentDirection == AgentDirection.Left;
            set => Data.AgentDirection = AgentDirection.Left;
        }
        public bool IsDownLeft {
            get => Data.AgentDirection == AgentDirection.DownLeft;
            set => Data.AgentDirection = AgentDirection.DownLeft;
        }

        public bool IsUp {
            get => Data.AgentDirection == AgentDirection.Up;
            set => Data.AgentDirection = AgentDirection.Up;
        }

        public bool IsNone {
            get => Data.AgentDirection == AgentDirection.None;
            set => Data.AgentDirection = AgentDirection.None;
        }

        public bool IsDown {
            get => Data.AgentDirection == AgentDirection.Down;
            set => Data.AgentDirection = AgentDirection.Down;
        }

        public bool IsUpRight {
            get => Data.AgentDirection == AgentDirection.UpRight;
            set => Data.AgentDirection = AgentDirection.UpRight;
        }

        public bool IsRight {
            get => Data.AgentDirection == AgentDirection.Right;
            set => Data.AgentDirection = AgentDirection.Right;
        }

        public bool IsDownRight {
            get => Data.AgentDirection == AgentDirection.DownRight;
            set => Data.AgentDirection = AgentDirection.DownRight;
        }

        public bool IsRemoveMode {
            get => Data.State == AgentState.RemoveTile;
            set { if (value) Data.State = AgentState.RemoveTile; else if (Data.IsPlaced) Data.State = AgentState.Move; }
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
            else if (args.PropertyName == nameof(data.State))
            {
                RaisePropertyChanged(nameof(IsRemoveMode));
                RaisePropertyChanged(nameof(IsNotPlaced));
            }
        }

        ~UserOrderPanelViewModel()
        {
            if (!(data is null))
                data.PropertyChanged -= DataPropertyChanged;
        }
    }
}
