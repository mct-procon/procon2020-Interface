using System;

namespace GameInterface.GameManagement
{
    [Flags]
    public enum TeamColor : byte {
        Free   = 0b000,
        Player1 = 0b001,
        Player2 = 0b010,
        Both   = 0b011
    }

    public static class TeamColorUtil
    {
        public static int ToPlayerNum(this TeamColor col) => (byte)col - 1;
        public static TeamColor ToTeamColor(this int playerNum) => (TeamColor)(playerNum + 1);
    }
}
