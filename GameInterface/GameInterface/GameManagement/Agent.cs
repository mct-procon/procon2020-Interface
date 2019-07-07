using System.Windows;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace GameInterface.GameManagement
{
    public class Agent : ViewModels.ViewModelBase
    {
        public int playerNum; //0,1
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
        public Point GetNextPoint()
        {
            int x = this.Point.X, y = this.Point.Y;
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
