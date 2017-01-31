using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AI_ROCKS.Drive
{
    class DriveCommand
    {
        private byte[] magnitude;
        private byte[] direction;


        public DriveCommand(byte[] magnitude, byte[] direction)
        {
            this.magnitude = magnitude;
            this.direction = direction;
        }


        public byte[] Magnitude
        {
            get { return this.magnitude; }
            set { this.magnitude = value; }
        }

        public byte[] Direction
        {
            get { return this.direction; }
            set { this.direction = value; }
        }

        public Tuple<byte[], byte[]> Command
        {
            get { return new Tuple<byte[], byte[]>(magnitude, direction); }
        }
    }
}
