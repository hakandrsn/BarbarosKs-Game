using System.Collections.Generic;
using UnityEngine; // System.Numerics.Vector3 yerine UnityEngine.Vector3 kullanıyoruz.
using Newtonsoft.Json; // Veri modellerinde Newtonsoft kullanacağız.

namespace Project.Scripts.Network.Models
{
    // Sunucu ile birebir aynı mesaj yapısı
    public class GameMessage
    {
        public MessageType Type { get; set; }
        public object Data { get; set; }
        public System.DateTime Timestamp { get; set; } = System.DateTime.UtcNow;
    }

    // Sunucu ile birebir aynı enum
    public enum MessageType
    {
        PlayerJoin,
        PlayerJoined,
        PlayerMove,
        PlayerMoved,
        PlayerAction,
        PlayerActionResult,
        WorldState,
        ChatMessage,
        Disconnect,
        Ping,
        Pong
    }

    // Sunucu ile birebir aynı Player sınıfı
    public class Player
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public Vector3 Position { get; set; } // UnityEngine.Vector3
        public float CurrentHullDurability { get; set; }
        public System.DateTime LastUpdate { get; set; }
    }

    // Gönderilecek veriler için sınıflar
    public class PlayerJoinData
    {
        public string PlayerName { get; set; }
    }

    public class PlayerMoveData
    {
        public Vector3 NewPosition { get; set; } // UnityEngine.Vector3
    }
    
    /// <summary>
    /// YENİ EKLENDİ: Oyuncu aksiyonu (saldırı, yetenek kullanımı vb.) verisi.
    /// </summary>
    public class PlayerActionData
    {
        public string ActionType { get; set; } = string.Empty;
        public Vector3 Position { get; set; }
        public string TargetId { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    // --- SUNUCUDAN GELEN VERİLER İÇİN MODELLER ---
    
    /// <summary>
    /// YENİ EKLENDİ: Sunucudan bir aksiyonun sonucunu bildiren veri.
    /// </summary>
    public class ActionResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public object Data { get; set; }
    }
    
    // Diğer tüm veri sınıfları (PlayerActionData, ActionResult vb.) buraya eklenebilir.
}