using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Scripts.Network
{
    /// <summary>
    ///     Sunucudan gelen oyun durumu verilerini içeren sınıf
    /// </summary>
    [Serializable]
    public class GameState
    {
        // Oyundaki oyuncular listesi
        public List<PlayerData> players;

        // Sunucu saati (senkronizasyon için)
        public long serverTimeMs;

        // Oyun durumu (lobby, battle, vs.)
        public string gamePhase;

        // Oyuncunun kendisiyle ilgili ek veriler
        public string localPlayerId;
        public PlayerStats playerStats;

        /// <summary>
        ///     Yeni bir oyun durumu oluşturur
        /// </summary>
        public GameState()
        {
            players = new List<PlayerData>();
            serverTimeMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            gamePhase = "lobby";
            playerStats = new PlayerStats();
        }

        /// <summary>
        ///     Oyun durumu bileşenlerini Unity formatına dönüştürür
        /// </summary>
        public void ConvertToUnityFormats()
        {
            foreach (var player in players)
            {
                // Pozisyon ve rotasyon değerlerini Unity formatına çevir
                if (player.Position != Vector3.zero)
                {
                    // Server tarafında farklı koordinat sistemi kullanılıyorsa
                    // gerekli dönüşümleri burada yapabilirsiniz
                }

                // Gemi verilerini kontrol et
                if (player.ShipStats != null)
                {
                    // Gemi pozisyonu, rotasyonu, vs. için ek dönüşümler yapılabilir
                }
            }
        }

        /// <summary>
        ///     Yerel oyuncuyu oyuncu listesinden bulur
        /// </summary>
        public PlayerData GetLocalPlayer()
        {
            if (string.IsNullOrEmpty(localPlayerId)) return null;

            foreach (var player in players)
                if (player.PlayerId.ToString() == localPlayerId)
                    return player;

            return null;
        }

        /// <summary>
        ///     Verilen ID'ye sahip oyuncuyu bulur
        /// </summary>
        public PlayerData GetPlayerById(string playerId)
        {
            if (string.IsNullOrEmpty(playerId)) return null;

            foreach (var player in players)
                if (player.PlayerId.ToString() == playerId)
                    return player;

            return null;
        }

        /// <summary>
        ///     Oyun durumunu JSON formatına dönüştürür
        /// </summary>
        public string ToJson()
        {
            return JsonUtility.ToJson(this);
        }

        /// <summary>
        ///     JSON verilerinden oyun durumu oluşturur
        /// </summary>
        public static GameState FromJson(string json)
        {
            try
            {
                return JsonUtility.FromJson<GameState>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"Oyun durumu ayrıştırma hatası: {e.Message}");
                return new GameState();
            }
        }

        [Serializable]
        public class PlayerStats
        {
            public int level;
            public int experience;
            public int requiredExperienceForNextLevel;
            public int pearls;
            public int artifacts;
            public int gold;
            public int victories;
            public int defeats;
        }
    }
}