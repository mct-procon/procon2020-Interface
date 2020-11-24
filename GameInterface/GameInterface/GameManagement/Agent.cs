using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using MCTProcon31Protocol;
using System.Diagnostics;

namespace GameInterface.GameManagement
{
    public class Agent : ViewModels.ViewModelBase
    {
        public Agent(TeamColor PlayerNum, int AgentNum, int AgentsCount)
        {
            this.PlayerNum = PlayerNum;
            this.AgentNum = AgentNum;
            this.AgentID = PlayerNum.ToPlayerNum() * AgentsCount + AgentNum;
        }

        public TeamColor PlayerNum { get; }
        public int AgentID { get; }
        public int AgentNum { get; }
        public bool IsPlaced => state.HasFlag(AgentState.Move);
        public bool IsPlacePending => state == AgentState.PlacePending;

        // IsOnField = false かつ State = AgentState.BePlacedのときは
        // 今から配置する予定の座標を示し, IsOnField = trueのときは現在座標を示す
        private Point point;
        public Point Point {
            get => point;
            set => RaisePropertyChanged(ref point, value);
        }
        private AgentDirection agentDirection;
        public AgentDirection AgentDirection
        {
            get => agentDirection;
            set { if (IsPlaced) RaisePropertyChanged(ref agentDirection, value); }
        }

        private AgentState state = AgentState.NonPlaced;
        public AgentState State
        {
            get => state;
            set {
                RaisePropertyChanged(ref state, value);
                RaisePropertyChanged(nameof(IsPlacePending));
            }
        }

        public Point GetNextPoint()
        {
            Debug.Assert(this.State != AgentState.NonPlaced);
            if(this.IsPlacePending)
                return this.Point;
            byte x = this.Point.X, y = this.Point.Y;
            switch((AgentDirection)((uint)AgentDirection & 0b11))
            {
                case AgentDirection.Right:
                    x += 1;
                    break;
                case AgentDirection.Left:
                    x -= 1;
                    break;
            }
            switch ((AgentDirection)((uint)AgentDirection & 0b1100))
            {
                case AgentDirection.Up:
                    y -= 1;
                    break;
                case AgentDirection.Down:
                    y += 1;
                    break;
            }
            return new Point(x, y);
        }
    }
}
