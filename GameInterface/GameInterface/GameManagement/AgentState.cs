using System;

namespace GameInterface.GameManagement
{
    [Flags]
    public enum AgentState {
        NonPlaced    = 0,
        PlacePending = 1,
        Move         = 2,
        RemoveTile   = 3
    }
}
