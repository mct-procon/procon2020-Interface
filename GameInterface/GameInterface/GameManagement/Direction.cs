using MCTProcon30Protocol;

namespace GameInterface.GameManagement
{
    public enum AgentDirection : uint {
        None = 0,
        Up = 0b0100,
        UpRight = 0b0101,
        Right = 0b0001,
        DownRight = 0b1001,
        Down = 0b1000,
        DownLeft = 0b1010,
        Left = 0b0010,
        UpLeft = 0b0110
    }

    public static class DirectionExtensions
    {
        static public AgentDirection CastPointToDir(VelocityPoint p)
        {
            int x = p.X, y = p.Y;
            uint result = 0;
            if (x == 1)
                result = (uint)AgentDirection.Right;
            else if (x == -1)
                result = (uint)AgentDirection.Left;
            if (y == 1)
                result |= (uint)AgentDirection.Down;
            else if (y == -1)
                result |= (uint)AgentDirection.Up;
            return (AgentDirection)result;
        }
    }
}
