using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using GameInterface.Cells;
using MCTProcon31Protocol;

namespace GameInterface.GameManagement
{
    static class ScoreCalculator
    {
        public static int CalcScore(int playerNum, Cell[,] cells)
        {
            byte width = (byte)cells.GetLength(0), height = (byte)cells.GetLength(1);
            ColoredBoardNormalSmaller checker = new ColoredBoardNormalSmaller(width, height);
            int result = 0;
            var state = playerNum == 0 ? TeamColor.Area1P : TeamColor.Area2P;

            for (uint x = 0; x < width; ++x)
                for (uint y = 0; y < height; ++y)
                {
                    if (cells[x, y].AreaState == state)
                    {
                        result += cells[x, y].Score;
                        checker[x, y] = true;
                    }
                }
            // TODO: Update BadSpaceFill.
            ScoreEvaluation.BadSpaceFill(ref checker, width, height);

            for (uint x = 0; x < width; ++x)
                for (uint y = 0; y < height; ++y)
                {
                    if (!checker[x, y])
                        cells[x, y].SurroundedState |= state;
                    if((cells[x, y].SurroundedState & state) != 0) result += Math.Abs(cells[x, y].Score);
                }
            return result;
        }
    }
}
