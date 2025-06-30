using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Scripts.Network
{
    [Serializable]
    public class NetworkMessage
    {
        public string type;
    }

    [Serializable]
    public class AuthRequestMessage : NetworkMessage
    {
        public string username;
        public string deviceId;
        public string authToken;
        public string clientVersion;

        public AuthRequestMessage()
        {
            type = "auth_request";
        }
    }

    [Serializable]
    public class AuthResponseMessage : NetworkMessage
    {
        public bool success;
        public string sessionId;
        public string error;

        public AuthResponseMessage()
        {
            type = "auth_response";
        }
    }

    [Serializable]
    public class PlayerSyncMessage : NetworkMessage
    {
        public string sessionId;
        public PlayerData player;

        public PlayerSyncMessage()
        {
            type = "player_sync";
        }
    }

    [Serializable]
    public class GameStateMessage : NetworkMessage
    {
        public List<PlayerData> players;
        public float gameTime;
        public string currentPhase;

        public GameStateMessage()
        {
            type = "game_state";
        }
    }

    [Serializable]
    public class ChatMessage : NetworkMessage
    {
        public string sessionId;
        public string username;
        public string message;
        public string channel;

        public ChatMessage()
        {
            type = "chat";
        }
    }

    [Serializable]
    public class HeartbeatMessage : NetworkMessage
    {
        public string sessionId;
        public long timestamp;

        public HeartbeatMessage()
        {
            type = "heartbeat";
        }
    }

    [Serializable]
    public class PingMessage : NetworkMessage
    {
        public long timestamp;

        public PingMessage()
        {
            type = "ping";
        }
    }

    [Serializable]
    public class PongMessage : NetworkMessage
    {
        public long timestamp;

        public PongMessage()
        {
            type = "pong";
        }
    }

    [Serializable]
    public struct Vector3Position
    {
        public float x;
        public float y;
        public float z;

        public Vector3Position(Vector3 vector)
        {
            x = vector.x;
            y = vector.y;
            z = vector.z;
        }

        public Vector3 ToVector3()
        {
            return new Vector3(x, y, z);
        }

        public static implicit operator Vector3(Vector3Position position)
        {
            return position.ToVector3();
        }

        public static implicit operator Vector3Position(Vector3 vector)
        {
            return new Vector3Position(vector);
        }
    }

    [Serializable]
    public struct QuaternionData
    {
        public float x;
        public float y;
        public float z;
        public float w;

        public QuaternionData(Quaternion quaternion)
        {
            x = quaternion.x;
            y = quaternion.y;
            z = quaternion.z;
            w = quaternion.w;
        }

        public Quaternion ToQuaternion()
        {
            return new Quaternion(x, y, z, w);
        }

        public static implicit operator Quaternion(QuaternionData data)
        {
            return data.ToQuaternion();
        }

        public static implicit operator QuaternionData(Quaternion quaternion)
        {
            return new QuaternionData(quaternion);
        }
    }
}