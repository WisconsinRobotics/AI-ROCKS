using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AI_ROCKS.Drive.Utils
{
    public static class Speed
    {
        public const byte NORMAL_OPERATION = 50;
        public const byte SLOW_OPERATION = 30;
        public const byte CLEAR_OBSTACLE = 40;
        public const byte AVOID_OBSTACLE = 30;
        public const byte SLOW_TURN = 35;
        public const byte VISION = 50;
        public const byte VISION_REDETECT_BALL = 40;
        public const byte VISION_SCAN = 40;
        public const byte HALT = 0;
    }
}
