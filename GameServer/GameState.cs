using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer
{
    class GameState
    {
        private int x, y; // in [0, 10[

        public GameState()
        {
            x = y = 1;
        }

        public void Move(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public void GetPosition(out int x, out int y)
        {
            x = this.x;
            y = this.y;
        }
    }
}
