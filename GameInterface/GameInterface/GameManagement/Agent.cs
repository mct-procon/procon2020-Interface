using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using MCTProcon31Protocol;

namespace GameInterface.GameManagement
{
    public class Agent : ViewModels.ViewModelBase
    {
        public int PlayerNum { get; set; } //0,1
        public int AgentNum { get; set; }

        private Point point;
        public Point Point {
            get => point;
            set => RaisePropertyChanged(ref point, value);
        }
        private AgentDirection agentDirection;
        public AgentDirection AgentDirection
        {
            get => agentDirection;
            set => RaisePropertyChanged(ref agentDirection, value);
        }

        private AgentState state;
        public AgentState State
        {
            get => state;
            set => RaisePropertyChanged(ref state, value);
        }

        public bool IsMoved { get; set; } = false;

        public Point GetNextPoint()
        {
            byte x = this.Point.X, y = this.Point.Y;
            switch((AgentDirection)((uint)AgentDirection & 0b11))
            {
                case AgentDirection.Right:
                    x += 1;
                    break;
                case AgentDirection.Left:
                    x -= 1;
                    break;
                default:
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
                case AgentDirection.None:
                    break;
            }
            return new Point(x, y);
        }

    }
}
