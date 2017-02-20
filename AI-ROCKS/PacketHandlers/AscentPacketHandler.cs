using System;
using System.Collections.Generic;
using System.IO.Ports;
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
        const int ASCENT_AI_SERVICE_ID = 3;
        const int AI_ROCKS_ROBOT_ID = 16;
        const int AI_ROCKS_AI_SERVICE_ID = 0;
        
        const string LAUNCHPAD_COM_PORT = "COM4";       //TODO make from param? Update after knowing COM port if nothing else

        // For ROCKS - send to Launchpad
        //static readonly IPEndPoint ASCENT_CONTROLS_IP_ENDPOINT = 
        //new IPEndPoint(IPAddress.Loopback, ASCENT_CONTROLS_PORT);
        
        // For Gazebo
        static readonly IPEndPoint ASCENT_CONTROLS_IP_ENDPOINT = new IPEndPoint(IPAddress.Parse("192.168.1.22"), ASCENT_CONTROLS_PORT);

        UdpClient socket;
        SerialPort launchpad;
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
            
            // [header] [payload] [end]
            // header = [ opcode ][ source ][ dest ][ payload size ][ checksum ]
            // source/dest addr - [ robot ID] [service ID]


            byte leftSpeed = data[0];       // TODO
            byte rightSpeed = data[1];            // TODO

            List<byte> bclPacket = new List<byte>();

            // Header
            bclPacket.Add(0xBA);
            bclPacket.Add(0xAD);

            // Opcode - all wheel speed
            bclPacket.Add(opcode);

            // Source addr - robot ID and service ID
            bclPacket.Add(AI_ROCKS_ROBOT_ID);                  //TODO robot ID
            bclPacket.Add(AI_ROCKS_AI_SERVICE_ID);                   //TODO Service ID

            // Dest addr - robot ID and service ID
            bclPacket.Add(ASCENT_ROBOT_ID);
            bclPacket.Add(ASCENT_AI_SERVICE_ID);                   //TODO Service ID

            // Payload size and payload
            bclPacket.Add((byte)data.Length);
            foreach (byte b in data)
            {
                bclPacket.Add(b);
            }
            
            // CRC
            byte crc = 0;
            crc ^= leftSpeed;
            if ((crc & 0x80) != 0)
            {
                // Dividend -= divisor
                crc = (byte) ((crc << 1) ^ 0x07);
            }
            else
            {
                // Move to the next bit
                crc <<= 1;
            }

            crc ^= rightSpeed;
            if ((crc & 0x80) != 0)
            {
                // Dividend -= divisor
                crc = (byte)((crc << 1) ^ 0x07);
            }
            else
            {
                // Move to the next bit
                crc <<= 1;
            }

            // End
            bclPacket.Add(0xFE);

            // For Gazebo debugging
            Console.WriteLine("left: " + (sbyte)leftSpeed + " | right: " + (sbyte)rightSpeed);

            // Send over Serial on the COM port of the launchpad
            //GetInstance().launchpad.WriteLine(bclPacket.ToString());

            // Send over UDP to Gazebo
            GetInstance().socket.Send(data, 2, ASCENT_CONTROLS_IP_ENDPOINT);
        }

        AscentPacketHandler()
        {
            this.socket = new UdpClient(AI_PORT);
            //this.launchpad = new SerialPort(LAUNCHPAD_COM_PORT, 115200);
            //this.launchpad.Open();

            // Initialize handlers
            this.gpsHandler = new GPSHandler();
            this.driveHandler = new DriveHandler();

            // Initialize async receive
            //socket.BeginReceive(HandleSocketReceive, null);
        }

        void HandleSocketReceive(IAsyncResult result)
        {
            IPEndPoint recvAddr = new IPEndPoint(IPAddress.Any, 0);
            //byte[] data = socket.EndReceive(result, ref recvAddr);

            //socket.BeginReceive(HandleSocketReceive, null);
            
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
