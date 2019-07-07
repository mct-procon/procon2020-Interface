namespace GameInterface.GameManagement
{
    public enum Direction : uint {
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
        static public Direction CastPointToDir(Point p)
        {
            int x = p.X, y = p.Y;
            uint result = 0;
            if (x == 1)
                result = (uint)Direction.Right;
            else if (x == -1)
                result = (uint)Direction.Left;
            if (y == 1)
                result |= (uint)Direction.Down;
            else if (y == -1)
                result |= (uint)Direction.Up;
            return (Direction)result;
        }
    }
}
