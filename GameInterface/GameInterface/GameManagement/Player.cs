using MCTProcon31Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameInterface.GameManagement
{
    public class Player
    {
        public int Score { get; set; }
        public Agent[] Agents { get; set; }
        public List<Decision> Decisions { get; set; }
        public int DecisionsSelectedIndex { get; set; }
        public int AgentsCount { get; set; }
        public int BeforeAgentsCount { get; set; }

        public Player()
        {
            Score = 0;
            Agents = null;
            Decisions = new List<Decision>();
            DecisionsSelectedIndex = -1;
        }
    }
}
