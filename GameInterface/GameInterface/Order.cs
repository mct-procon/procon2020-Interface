using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameInterface.GameManagement;

namespace GameInterface
{
    public class Order
    {
        public int agentNum; //1,2,3,4
        public Direction direction;
        public AgentState state;
        public Order(int agentNum_, Direction direction_, AgentState state_)
        {
            agentNum = agentNum_;
            direction = direction_;
            state = state_;
        }
    }
}
