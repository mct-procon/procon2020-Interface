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
        private Direction agentDirection;
        public Direction AgentDirection
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
            switch((Direction)((uint)AgentDirection & 0b11))
            {
                case Direction.Right:
                    x += 1;
                    break;
                case Direction.Left:
                    x -= 1;
                    break;
                default:
                    break;
            }
            switch ((Direction)((uint)AgentDirection & 0b1100))
            {
                case Direction.Up:
                    y -= 1;
                    break;
                case Direction.Down:
                    y += 1;
                    break;
                case Direction.None:
                    break;
            }
            return new Point(x, y);
        }
    }
}
