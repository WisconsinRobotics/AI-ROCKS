using System;
using System.Net.Sockets;
using System.Net;
using AI_ROCKS.Drive;

namespace AI_ROCKS.PacketHandlers
{
    /// <summary>
    /// Interface to be implemented by all other PacketHandlers to implement 
    /// their own handling of receiving and parsing packets.
    /// </summary>
    interface PacketHandler
    {
        bool HandlePacket(byte opcode, byte[] payload);
    }


    class AscentPacketHandler
    {
        // Constants for communicating with ROCKS
        const int AI_PORT = 15000;
        const int ASCENT_CONTROLS_PORT = 10000;
        const int ASCENT_ROBOT_ID = 15;

        static readonly IPEndPoint ASCENT_CONTROLS_IP_ENDPOINT = 
            new IPEndPoint(IPAddress.Loopback, ASCENT_CONTROLS_PORT);

        UdpClient socket;
        GPSHandler gpsHandler;
        DriveHandler driveHandler;

        static AscentPacketHandler instance;


        public static AscentPacketHandler GetInstance()
        {
            if (instance == null)
            {
                instance = new AscentPacketHandler();
            }

            return instance;
        }

        public static void SendPayloadToAscentControlSystem(byte opcode, byte[] data)
        {
            // append header bytes
            // set src addr to {16, 0}
            // set dest addr to {15, AI_SHIM}
            // set size
            // compute crc
            // append data
            // append end byte
        }

        AscentPacketHandler()
        {
            // Initialize handlers
            this.socket = new UdpClient(AI_PORT);
            this.gpsHandler = new GPSHandler();
            this.driveHandler = new DriveHandler();

            // Initialize async receive
            socket.BeginReceive(HandleSocketReceive, null);
        }

        void HandleSocketReceive(IAsyncResult result)
        {
            IPEndPoint recvAddr = new IPEndPoint(IPAddress.Any, 0);
            byte[] data = socket.EndReceive(result, ref recvAddr);

            socket.BeginReceive(HandleSocketReceive, null);
            // (optional) check recv_addr against ASCENT_CONTROLS_IP_ENDPOINT
            // verify header, ignore crc as over loopback
            // parse opcode
            // ignore crc
            // route payload to appropriate handler based on parsed opcode
        }

        /// <summary>
        /// Property to get current GPS data.
        /// </summary>
        public GPS GPSData
        {
            get { return this.gpsHandler.Data; }
        }
    }
}
