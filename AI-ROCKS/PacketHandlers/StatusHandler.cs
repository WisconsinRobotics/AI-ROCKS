using System.Text;
using System.Collections.Generic;

namespace AI_ROCKS.PacketHandlers
{
    public enum Status : byte
    {
        /* 0-50: General Updates */
        AIS_FOUND_GATE = 0,
        AIS_ACK = 1,
        AIS_LOG = 2,

        /* 51-100: State change/status update */
        AIS_SWITCH_TO_VISION = 51,
        AIS_SWITCH_TO_GPS = 52,
        AIS_OBS_DETECT = 53,
        AIS_OBS_AVOID = 54,
        AIS_IN_WATCHDOG = 55,
        AIS_OUT_WATCHDOG = 56,

        /* 101-150: GPS codes */
        AIS_DIST_FROM_GOAL = 101,

        /* 151-200: Vision codes */
        AIS_BALL_DETECT = 151, 
        AIS_DIST_DETECT_BALL = 152,
        AIS_BEGIN_SCAN = 153,
        AIS_DROP_BALL = 154,

        AIS_VERIFY_SUCCESS = 170,
        AIS_VERIFY_FAIL_TIMESTAMP = 171,
        AIS_VERIFY_FAIL_CAM_AVG = 172,
        AIS_VERIFY_FAIL_CAM_PERCENT = 173,
        AIS_VERIFY_FAIL_GPS_AVG = 174,
        AIS_VERIFY_FAIL_GPS_PERCENT = 175,

        /* 201-255: Error codes */
        AIS_FATAL_ERROR = 201,
        AIS_CONN_LOST = 202,
        AIS_CAM_ERR = 203
    }

    class StatusHandler : IPacketHandler
    {
        private static readonly byte[] EMPTY_OPTION = { 0, 0, 0, 0, 0, 0, 0, 0 };

        public bool HandlePacket(byte opcode, byte[] payload)
        {
            if (opcode == AscentPacketHandler.OPCODE_SIMPLE_AI)
            {
                // ACK packet
                if (payload[0] == (byte)Status.AIS_ACK)
                {
                    AscentPacketHandler.GetInstance().receivedAck = true;
                }
            }

            else if (opcode == AscentPacketHandler.OPCODE_DEBUG_AI)
            {
                //TODO
            }

            return false;
        }

        public static void SendSimpleAIPacket(Status status, byte[] option = null)
        {
            // check whether an additional 8 bytes are specified
            option = option ?? new byte[8];

            // truncate if option is larger than 8 bytes
            if(option.Length > 8)
                option = new byte[8] { option[0], option[1], option[2], option[3], option[4], option[5], option[6], option[7] };

            List<byte> payload = new List<byte>(new byte[] { (byte)status });
            payload.AddRange(option);

            AscentPacketHandler.SendPayloadToROCKS(AscentPacketHandler.OPCODE_SIMPLE_AI, payload.ToArray(), AscentPacketHandler.ROCKS_AI_SERVICE_ID);
        }
        
        public static void SendDebugAIPacket(Status status, string debugMessage)
        {
            List<byte> payload = new List<byte>();

            // if debug message is too long, truncate
            if (debugMessage.Length >= 150)
            {
                debugMessage = debugMessage.Substring(0, 150);
            }

            // convert everything to bytes
            payload.Add((byte)status);
            payload.AddRange(Encoding.ASCII.GetBytes(debugMessage));
            payload.AddRange(new byte[151 - payload.Count]); // zero extend

            AscentPacketHandler.SendPayloadToROCKS(AscentPacketHandler.OPCODE_DEBUG_AI, payload.ToArray(), AscentPacketHandler.ROCKS_AI_SERVICE_ID);
        }
    }
}
