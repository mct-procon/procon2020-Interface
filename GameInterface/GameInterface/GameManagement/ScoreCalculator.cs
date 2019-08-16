using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using GameInterface.Cells;
using MCTProcon30Protocol;

namespace GameInterface.GameManagement
{
    static class ScoreCalculator
    {
        private static byte height;
        private static byte width;
        private static readonly int[] DirectionX = new int[] { 1, 0, -1, 0 };
        private static readonly int[] DirectionY = new int[] { 0, 1, 0, -1 };

        public static void Init(byte height_, byte width_)
        {
            height = height_;
            width = width_;
        }

        public static int CalcScore(int playerNum, Cell[,] cells)
        {
            ColoredBoardNormalSmaller checker = new ColoredBoardNormalSmaller(width, height);
            int result = 0;
            var state = playerNum == 0 ? TeamColor.Area1P : TeamColor.Area2P;

            for (uint x = 0; x < width; ++x)
                for (uint y = 0; y < height; ++y)
                {
                    if (cells[x, y].AreaState_ == state)
                    {
                        result += cells[x, y].Score;
                        checker[x, y] = true;
                    }
                }
            BadSpaceFill(ref checker, width, height);

            for (uint x = 0; x < width; ++x)
                for (uint y = 0; y < height; ++y)
                    if (!checker[x, y])
                    {
                        result += Math.Abs(cells[x, y].Score);
                        cells[x, y].SurroundedState |= state;
                    }
            return result;
        }

        //uint[] myStack = new uint[1024];	//x, yの順で入れる. y, xの順で取り出す. width * height以上のサイズにする.
        public static unsafe void BadSpaceFill(ref ColoredBoardNormalSmaller Checker, byte width, byte height)
        {
            unchecked
            {
                MCTProcon30Protocol.Point* myStack = stackalloc MCTProcon30Protocol.Point[20 * 20];

                MCTProcon30Protocol.Point point;
                byte x, y, searchTo = 0, myStackSize = 0;

                searchTo = (byte)(height - 1);
                for (x = 0; x < width; x++)
                {
                    if (!Checker[x, 0])
                    {
                        myStack[myStackSize++] = new MCTProcon30Protocol.Point(x, 0);
                        Checker[x, 0] = true;
                    }
                    if (!Checker[x, searchTo])
                    {
                        myStack[myStackSize++] = new MCTProcon30Protocol.Point(x, searchTo);
                        Checker[x, searchTo] = true;
                    }
                }

                searchTo = (byte)(width - 1);
                for (y = 0; y < height; y++)
                {
                    if (!Checker[0, y])
                    {
                        myStack[myStackSize++] = new MCTProcon30Protocol.Point(0, y);
                        Checker[0, y] = true;
                    }
                    if (!Checker[searchTo, y])
                    {
                        myStack[myStackSize++] = new MCTProcon30Protocol.Point(searchTo, y);
                        Checker[searchTo, y] = true;
                    }
                }

                while (myStackSize > 0)
                {
                    point = myStack[--myStackSize];
                    x = point.X;
                    y = point.Y;

                    //左方向
                    searchTo = (byte)(x - 1);
                    if (searchTo < width && !Checker[searchTo, y])
                    {
                        myStack[myStackSize++] = new MCTProcon30Protocol.Point(searchTo, y);
                        Checker[searchTo, y] = true;
                    }

                    //下方向
                    searchTo = (byte)(y + 1);
                    if (searchTo < height && !Checker[x, searchTo])
                    {
                        myStack[myStackSize++] = new MCTProcon30Protocol.Point(x, searchTo);
                        Checker[x, searchTo] = true;
                    }

                    //右方向
                    searchTo = (byte)(x + 1);
                    if (searchTo < width && !Checker[searchTo, y])
                    {
                        myStack[myStackSize++] = new MCTProcon30Protocol.Point(searchTo, y);
                        Checker[searchTo, y] = true;
                    }

                    //上方向
                    searchTo = (byte)(y - 1);
                    if (searchTo < height && !Checker[x, searchTo])
                    {
                        myStack[myStackSize++] = new MCTProcon30Protocol.Point(x, searchTo);
                        Checker[x, searchTo] = true;
                    }
                }
            }
        }

    }
}
