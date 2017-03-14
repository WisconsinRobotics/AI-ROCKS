using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Net.Sockets;
using System.Net;

using AI_ROCKS.Drive.Utils;

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
        // Opcodes
        const byte OPCODE_REPORT_GPS = 0x51;
        const byte OPCODE_REPORT_IMU = 0x55;

        // Constants for communicating with ROCKS
        const int AI_ROCKS_PORT = 15000;
        const int ROCKS_PORT = 10000;
        const int ROCKS_ROBOT_ID = 15;
        const int ROCKS_AI_SERVICE_ID = 3;
        const int AI_ROCKS_ROBOT_ID = 16;
        const int AI_ROCKS_AI_SERVICE_ID = 0;
        
        // For ROCKS - receive using BCL from ROCKS
        //static readonly IPEndPoint ASCENT_CONTROLS_IP_ENDPOINT = new IPEndPoint(IPAddress.Loopback, ROCKS_PORT);

        // For Gazebo
        static readonly IPEndPoint ASCENT_CONTROLS_IP_ENDPOINT = new IPEndPoint(IPAddress.Parse("192.168.1.80"), ROCKS_PORT);
        const string LAUNCHPAD_COM_PORT = "COM4";       //TODO make from param? Update after knowing COM port if nothing else

        private UdpClient ai_rocksSocket;
        private SerialPort launchpad;
        private GPSHandler gpsHandler;
        private IMUHandler imuHandler;
        private DriveHandler driveHandler;

        static AscentPacketHandler instance;

        public static void Initialize()
        {
            if (instance == null)
            {
                instance = new AscentPacketHandler();
            }
        }

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
            // header = [header] [opcode] [source robot ID = 16] [source service ID = 0] [dest robot ID = 15] [dest service ID = 3] [payload size] [checksum] [payload] [footer]

            // BCL packet to be formed and sent
            List<byte> bclPacket = new List<byte>();

            // Header
            bclPacket.Add(0xBA);
            bclPacket.Add(0xAD);

            // Opcode - all wheel speed
            bclPacket.Add(opcode);

            // Source addr - robot ID and service ID
            bclPacket.Add(AI_ROCKS_ROBOT_ID);
            bclPacket.Add(AI_ROCKS_AI_SERVICE_ID);

            // Dest addr - robot ID and service ID
            bclPacket.Add(ROCKS_ROBOT_ID);
            bclPacket.Add(ROCKS_AI_SERVICE_ID);

            // Payload size and payload
            bclPacket.Add((byte)data.Length);
           
            // CRC
            int crc = 0;
            for (int i = 0; i < data.Length; i++)
            {
                // Set up the dividend
                crc ^= data[i];

                for (int j = 0; j < 8; j++)
                {
                    // Does the divisor go into the dividend?
                    if ((crc & 0x80) > 1)
                    {
                        // Dividend -= divisor
                        crc = (crc << 1) ^ 0x07;
                    }
                    else
                    {
                        // Move to the next bit
                        crc <<= 1;
                    }
                }
            }
            bclPacket.Add((byte)crc);

            // Add data[] to payload
            foreach (byte b in data)
            {
                bclPacket.Add(b);
            }

            // End
            bclPacket.Add(0xFE);

            // Send over Serial on the COM port of the launchpad
            //GetInstance().launchpad.Write(bclPacket.ToArray(), 0, bclPacket.Count);

            // Send over UDP to Gazebo or ROCKS
            GetInstance().ai_rocksSocket.Send(bclPacket.ToArray(), bclPacket.Count, ASCENT_CONTROLS_IP_ENDPOINT);
        }

        private AscentPacketHandler()
        {
            this.ai_rocksSocket = new UdpClient(AI_ROCKS_PORT);
            //this.launchpad = new SerialPort(LAUNCHPAD_COM_PORT, 115200, Parity.None, 8, StopBits.One);
            //this.launchpad.Open();

            // Initialize handlers
            this.gpsHandler = new GPSHandler();
            this.imuHandler = new IMUHandler();
            this.driveHandler = new DriveHandler();

            // Initialize async receive
            ai_rocksSocket.BeginReceive(HandleSocketReceive, null);
        }

        void HandleSocketReceive(IAsyncResult result)
        {
            IPEndPoint recvAddr = new IPEndPoint(IPAddress.Any, 0);
            byte[] data = ai_rocksSocket.EndReceive(result, ref recvAddr);

            ai_rocksSocket.BeginReceive(HandleSocketReceive, null);

            // (optional) check recv_addr against ASCENT_CONTROLS_IP_ENDPOINT
            // verify header, ignore crc as over loopback
            // parse opcode
            // ignore crc
            // route payload to appropriate handler based on parsed opcode

            // Check header bytes
            if (data[0] != 0xBA || data[1] != 0xAD)
            {
                return;
            }

            // Ignore 3, 4, 5, 6, -> dest, src IDs and 8 -> CRC

            // Get payload size and start index of payload
            int payloadSize = data[7];
            int payloadIndex = 9;

            if (data[payloadIndex + payloadSize + 1] != 0xFE)
            {
                return;
            }
            byte[] payload = data.Skip(payloadIndex).Take(payloadSize).ToArray();

            // Switch on opcode, route payload to appropriate handler
            byte opcode = data[2];
            switch (opcode)
            {
                case OPCODE_REPORT_GPS:
                {
                    gpsHandler.HandlePacket(OPCODE_REPORT_GPS, payload);
                    break;
                }
                case OPCODE_REPORT_IMU:
                {
                    imuHandler.HandlePacket(OPCODE_REPORT_IMU, payload);
                    break;
                }
                default:
                    return;
            }
        }

        /// <summary>
        /// Property to get current GPS data.
        /// </summary>
        public static GPS GPSData
        {
            get { return GetInstance().gpsHandler.Data; }
        }

        /// <summary>
        /// Property to get current IMU data.
        /// </summary>
        public static IMU IMUData
        {
            get { return GetInstance().imuHandler.Data; }
        }

        /// <summary>
        /// Property to get current Compass data, which is the ZOrient of the IMU.
        /// </summary>
        public static short Compass
        {
            get { return GetInstance().imuHandler.Compass; }
        }
    }
}
