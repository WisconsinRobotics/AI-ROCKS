using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Net.Sockets;
using System.Net;

using AI_ROCKS.Drive.Models;

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
        // BCL opcodes
        const byte OPCODE_REPORT_GPS = 0x51;
        const byte OPCODE_REPORT_IMU = 0x55;

        // Constants for communicating with ROCKS
        const int AI_ROCKS_PORT = 15000;
        const int ROCKS_PORT = 10000;
        const string LAUNCHPAD_COM_PORT = "COM4";       //TODO make from param? Update after knowing COM port if nothing else

        // BCL constants
        const int ROCKS_ROBOT_ID = 15;
        const int ROCKS_AI_SERVICE_ID = 3;
        const int AI_ROCKS_ROBOT_ID = 16;
        const int AI_ROCKS_AI_SERVICE_ID = 0;

        private static IPEndPoint ascentControlsIPEndpoint;
        private UdpClient ai_rocksSocket;
        private SerialPort launchpad;
        private GPSHandler gpsHandler;
        private IMUHandler imuHandler;
        private DriveHandler driveHandler;

        private static AscentPacketHandler instance;


        /// <summary>
        /// Initialize the instance of AscentPacketHandler held within this class.
        /// </summary>
        /// <param name="destinationIP">Destination IP to communicate to ROCKS via. By default this is
        /// loopback, though an IP can be specified via command line args for Gazebo testing.
        /// </param>
        public static void Initialize(IPAddress destinationIP)
        {
            if (instance == null)
            {
                instance = new AscentPacketHandler(destinationIP);
            }
        }

        /// <summary>
        /// Get the instance of AscentPacketHandler held within this class. If the instance has not
        /// yet been initialized, this function creates a new instance that communicates on loopback,
        /// updates the class's instance, and returns it.
        /// </summary>
        /// <returns>AscentPacketHandler - the instance of AscentPacketHandler held within this class.
        /// If no such instance exists, this function returns a new instance, using loopback to 
        /// communicate.</returns>
        public static AscentPacketHandler GetInstance()
        {
            if (instance == null)
            {
                instance = new AscentPacketHandler(IPAddress.Loopback);
            }

            return instance;
        }

        /// <summary>
        /// Constructor for AscentPacketHandler. Only one instance should be made via Initialize() and accessed
        /// via GetInstance(). The destination IP is either loopback (127.0.0.1) for ROCKS or the address 
        /// specified via command line argument for Gazebo.
        /// </summary>
        /// <param name="destinationIP">IP address to communicate with. This is loopback for ROCKS by default,
        /// but can be specified via command line arguments for Gazebo usage with the "-g {address}" option.
        /// </param>
        private AscentPacketHandler(IPAddress destinationIP)
        {
            this.ai_rocksSocket = new UdpClient(AI_ROCKS_PORT);

            ascentControlsIPEndpoint = new IPEndPoint(destinationIP, ROCKS_PORT);

            // TODO delete once BCL communication works top-down
            //this.launchpad = new SerialPort(LAUNCHPAD_COM_PORT, 115200, Parity.None, 8, StopBits.One);
            //this.launchpad.Open();

            // Initialize handlers
            this.gpsHandler = new GPSHandler();
            this.imuHandler = new IMUHandler();
            this.driveHandler = new DriveHandler();

            // Initialize async receive
            ai_rocksSocket.BeginReceive(HandleSocketReceive, null);
        }

        /// <summary>
        /// Forms a BCL packet from the specified opcode and data[] and send this packet to ROCKS.
        /// This data will be the payload of a BCL packet, formed from the specified opcode.
        /// </summary>
        /// <param name="opcode">BCL opcode for the data and BCL packet to send to ROCKS.</param>
        /// <param name="data">Data, or payload, of the BCL packet being sent to ROCKS.</param>
        public static void SendPayloadToROCKS(byte opcode, byte[] data)
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

            // Send to Gazebo or ROCKS
            GetInstance().ai_rocksSocket.Send(bclPacket.ToArray(), bclPacket.Count, ascentControlsIPEndpoint);
        }
        
        /// <summary>
        /// Async receive function for ROCKS communication. This receives all data being sent from 
        /// ROCKS and updates the appropriate PacketHandler with the received data.
        /// </summary>
        /// <param name="result"></param>
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
                {
                    return;
                }
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
