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

        //UP_LEFT は左上だから 0 というように、
        //0 1 2                                 
        //3 4 5                                  
        //6 7 8
        //となるようなIDを定める(viewmodel内のボタンの処理をわかりやすくするため)
        readonly int[] directionId = new int[]
        {
            4,1,2,
            5,8,7,
            6,3,0,
        };
        public int GetDirectionIdFromDirection()
        {
            return directionId[(int)this.AgentDirection];
        }

        public enum State { MOVE, REMOVE_TILE };
        private State agentState;
        public State AgentState
        {
            get => agentState; 
            set
            {
                agentState = value;
            }
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
