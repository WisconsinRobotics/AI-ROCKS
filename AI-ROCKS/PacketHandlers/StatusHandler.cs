﻿using System;
//using AI_ROCKS.Drive.Models;
using System.Text;
using System.Linq;

namespace AI_ROCKS.PacketHandlers
{
    public enum Status
    {
        /* 0-50: General Updates */
        AIS_FOUND_GATE = 0,
        AIS_RELPY = 1, //what should this actually be called?? @ MATT
        AIS_LOG = 2,

        /* 51-100: State change/status update */
        AIS_SWITCH_VISION = 51,
        AIS_SWITCH_GPS = 52,
        AIS_OBS_DETECT = 53,
        AIS_OBS_AVOID = 54,

        /* 101-150: GPS codes */
        AIS_DIST_FROM_GOAL = 101,

        /* 151-200: Vision codes */
        AIS_BALL_DETECT = 151, 
        AIS_DIST_DETECT_BALL = 152,
        AIS_BEGAN_SCAN = 153,

        /* 251-255: Error codes */
        AIS_FATAL_ERROR = 251,
        AIS_CONN_LOST = 252,
        AIS_CAM_ERR = 253
    }

    class StatusHandler : PacketHandler
    {
        public bool HandlePacket(byte opcode, byte[] payload)
        {
            if (opcode == AscentPacketHandler.OPCODE_SIMPLE_AI)
            {
                //TODO
            }

            if (opcode == AscentPacketHandler.OPCODE_DEBUG_AI)
            {
                //TODO
            }
            return false;
        }

        void sendSimpleAIPacket(Status status)
        {
            byte[] statusInBytes = new byte[] { (byte)status };

            AscentPacketHandler.SendPayloadToROCKS(AscentPacketHandler.OPCODE_SIMPLE_AI, statusInBytes, 
                AI_ROCKS.PacketHandlers.AscentPacketHandler.AI_ROCKS_AI_SERVICE_ID);
        }
        
        void sendDebugAIPacket(Status status, string debugMessage)
        {
            //if debug message is too long, truncate
            if (debugMessage.Length >= 150)
            {
                debugMessage = debugMessage.Substring(0, 150);
            }

            //convert everything to bytes
            byte statusInBytes = (byte)status;

            byte[] debugMessageInBytes;
            debugMessageInBytes = Encoding.ASCII.GetBytes(debugMessage);

            byte[] payload = new byte[statusInBytes];
            payload.Concat(debugMessageInBytes);

            AscentPacketHandler.SendPayloadToROCKS(AscentPacketHandler.OPCODE_DEBUG_AI, payload, 
                AI_ROCKS.PacketHandlers.AscentPacketHandler.AI_ROCKS_AI_SERVICE_ID);
        }
    }
}