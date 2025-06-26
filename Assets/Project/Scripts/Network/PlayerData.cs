using System;
using UnityEngine;
using System;
using UnityEngine;

namespace Project.Scripts.Network
{
    /// <summary>
    /// Oyuncu verileri sınıfı
    /// </summary>
    [System.Serializable]
    public class PlayerData
    {
        // Oyuncu tanımlama
        public string id;
        public string username;
        public string shipId;

        // Oyuncu dönüşümü
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 velocity;

        // Oyuncu durumu
        public int health;
        public int maxHealth;
        public bool isAlive;
        public float respawnTime;

        // Oyuncu istatistikleri
        public int score;
        public int kills;
        public int deaths;

        // Aktif gemi verisi
        public ShipData activeShip;

        // Oyuncu profil bilgileri
        public PlayerStats stats;

        /// <summary>
        /// Yeni bir oyuncu verisi oluşturur
        /// </summary>
        public PlayerData()
        {
            id = Guid.NewGuid().ToString();
            username = "Player";
            health = 100;
            maxHealth = 100;
            isAlive = true;
            respawnTime = 3f;
            position = Vector3.zero;
            rotation = Quaternion.identity;
            velocity = Vector3.zero;
            score = 0;
            kills = 0;
            deaths = 0;
            stats = new PlayerStats();
        }

        /// <summary>
        /// ID ile yeni bir oyuncu verisi oluşturur
        /// </summary>
        public PlayerData(string playerId, string playerName = null)
        {
            id = playerId;
            username = playerName ?? "Player" + playerId.Substring(0, 4);
            health = 100;
            maxHealth = 100;
            isAlive = true;
            respawnTime = 3f;
            position = Vector3.zero;
            rotation = Quaternion.identity;
            velocity = Vector3.zero;
            score = 0;
            kills = 0;
            deaths = 0;
            stats = new PlayerStats();
        }

        /// <summary>
        /// Oyuncu verilerini başka bir oyuncudan kopyalar
        /// </summary>
        public void CopyFrom(PlayerData other)
        {
            if (other == null) return;

            // Temel bilgileri kopyala
            username = other.username;
            shipId = other.shipId;

            // Dönüşüm bilgilerini kopyala
            position = other.position;
            rotation = other.rotation;
            velocity = other.velocity;

            // Durum bilgilerini kopyala
            health = other.health;
            maxHealth = other.maxHealth;
            isAlive = other.isAlive;
            respawnTime = other.respawnTime;

            // İstatistikleri kopyala
            score = other.score;
            kills = other.kills;
            deaths = other.deaths;

            // Gemi verilerini kopyala
            if (other.activeShip != null)
            {
                if (this.activeShip == null)
                {
                    this.activeShip = new ShipData();
                }
                this.activeShip.CopyFrom(other.activeShip);
            }

            // İstatistikleri kopyala
            if (other.stats != null)
            {
                if (this.stats == null)
                {
                    this.stats = new PlayerStats();
                }
                this.stats.CopyFrom(other.stats);
            }
        }

        /// <summary>
        /// Oyuncu verilerini JSON formatına dönüştürür
        /// </summary>
        public string ToJson()
        {
            return JsonUtility.ToJson(this);
        }

        /// <summary>
        /// JSON verilerinden oyuncu verisi oluşturur
        /// </summary>
        public static PlayerData FromJson(string json)
        {
            try
            {
                return JsonUtility.FromJson<PlayerData>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"Oyuncu verisi ayrıştırma hatası: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gemi verilerini içeren sınıf
        /// </summary>
        [Serializable]
        public class ShipData
        {
            public string id;
            public string name;
            public int level;
            public int durability;
            public int maxDurability;
            public float speed;
            public float turnRate;
            public float fireRate;
            public string shipType;
            public int currentHullDurability;
            public int baseHullDurability;

            // Silah verileri
            public WeaponData[] weapons;

            /// <summary>
            /// Gemi verilerini başka bir gemiden kopyalar
            /// </summary>
            public void CopyFrom(ShipData other)
            {
                if (other == null) return;

                this.id = other.id;
                this.name = other.name;
                this.level = other.level;
                this.durability = other.durability;
                this.maxDurability = other.maxDurability;
                this.speed = other.speed;
                this.turnRate = other.turnRate;
                this.fireRate = other.fireRate;

                // Silah verilerini kopyala
                if (other.weapons != null && other.weapons.Length > 0)
                {
                    this.weapons = new WeaponData[other.weapons.Length];
                    for (int i = 0; i < other.weapons.Length; i++)
                    {
                        this.weapons[i] = new WeaponData();
                        this.weapons[i].CopyFrom(other.weapons[i]);
                    }
                }
            }

            /// <summary>
            /// Silah verilerini içeren sınıf
            /// </summary>
            [Serializable]
            public class WeaponData
            {
                public int id;
                public string type;
                public int damage;
                public float cooldown;
                public float lastFiredTime;
                public bool isActive;

                /// <summary>
                /// Silah verilerini başka bir silahtan kopyalar
                /// </summary>
                public void CopyFrom(WeaponData other)
                {
                    if (other == null) return;

                    this.id = other.id;
                    this.type = other.type;
                    this.damage = other.damage;
                    this.cooldown = other.cooldown;
                    this.lastFiredTime = other.lastFiredTime;
                    this.isActive = other.isActive;
                }
            }
        }

        /// <summary>
        /// Oyuncu istatistiklerini içeren sınıf
        /// </summary>
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

            /// <summary>
            /// Yeni bir istatistik nesnesi oluşturur
            /// </summary>
            public PlayerStats()
            {
                level = 1;
                experience = 0;
                requiredExperienceForNextLevel = 100;
                pearls = 0;
                artifacts = 0;
                gold = 0;
                victories = 0;
                defeats = 0;
            }

            /// <summary>
            /// İstatistikleri başka bir istatistik nesnesinden kopyalar
            /// </summary>
            public void CopyFrom(PlayerStats other)
            {
                if (other == null) return;

                this.level = other.level;
                this.experience = other.experience;
                this.requiredExperienceForNextLevel = other.requiredExperienceForNextLevel;
                this.pearls = other.pearls;
                this.artifacts = other.artifacts;
                this.gold = other.gold;
                this.victories = other.victories;
                this.defeats = other.defeats;
            }
        }
    }
}
