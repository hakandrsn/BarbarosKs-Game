using System;
using System.Collections.Generic;
using UnityEngine;
using BarbarosKs.Shared.DTOs;

namespace Project.Scripts.Network
{
    /// <summary>
    /// Modern MMO oyuncu verisi - GameServer DTOs ile uyumlu
    /// GameServer'daki ShipStatsDto, AmmoStatus ve diğer modern yapıları destekler
    /// </summary>
    [Serializable]
    public class PlayerData
    {
        // Oyuncu tanımlama - GameServer uyumlu
        public Guid PlayerId;
        public string Username;
        public Guid ShipId;
        public string ConnectionId;

        // Oyuncu dönüşümü
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Velocity;

        // Oyuncu durumu
        public float Health;
        public float MaxHealth;
        public bool IsAlive;
        public float RespawnTime;

        // Oyuncu istatistikleri
        public int Score;
        public int Kills;
        public int Deaths;
        public DateTime LastActivity;

        // Modern MMO Data - GameServer'dan sync
        public ShipStatsData ShipStats;
        public AmmoData AmmoStatus;
        public CombatData CombatInfo;
        public PlayerProfile Profile;

        /// <summary>
        /// Yeni bir oyuncu verisi oluşturur
        /// </summary>
        public PlayerData()
        {
            PlayerId = Guid.NewGuid();
            Username = "Player";
            ShipId = Guid.Empty;
            ConnectionId = string.Empty;
            Health = 100f;
            MaxHealth = 100f;
            IsAlive = true;
            RespawnTime = 3f;
            Position = Vector3.zero;
            Rotation = Quaternion.identity;
            Velocity = Vector3.zero;
            Score = 0;
            Kills = 0;
            Deaths = 0;
            LastActivity = DateTime.UtcNow;
            
            // Modern MMO verilerini başlat
            ShipStats = new ShipStatsData();
            AmmoStatus = new AmmoData();
            CombatInfo = new CombatData();
            Profile = new PlayerProfile();
        }

        /// <summary>
        /// ID ile yeni bir oyuncu verisi oluşturur
        /// </summary>
        public PlayerData(Guid playerId, string playerName = null)
        {
            PlayerId = playerId;
            Username = playerName ?? "Player" + playerId.ToString().Substring(0, 8);
            ShipId = Guid.Empty;
            ConnectionId = string.Empty;
            Health = 100f;
            MaxHealth = 100f;
            IsAlive = true;
            RespawnTime = 3f;
            Position = Vector3.zero;
            Rotation = Quaternion.identity;
            Velocity = Vector3.zero;
            Score = 0;
            Kills = 0;
            Deaths = 0;
            LastActivity = DateTime.UtcNow;
            
            // Modern MMO verilerini başlat
            ShipStats = new ShipStatsData();
            AmmoStatus = new AmmoData();
            CombatInfo = new CombatData();
            Profile = new PlayerProfile();
        }

        /// <summary>
        /// GameServer'daki ShipStatsDto'yu Unity ShipStatsData'ya dönüştürür
        /// </summary>
        public void UpdateShipStats(ShipStatsDto serverStats)
        {
            if (serverStats == null) return;

            ShipStats.ShipId = serverStats.ShipId;
            ShipStats.LastCalculated = serverStats.LastCalculated;
            ShipStats.AttackPower = serverStats.AttackPower;
            ShipStats.Armor = serverStats.Armor;
            ShipStats.Speed = serverStats.Speed;
            ShipStats.Maneuverability = serverStats.Maneuverability;
            ShipStats.CannonPower = serverStats.CannonPower;
            ShipStats.HitRate = serverStats.HitRate;
            ShipStats.CriticalHitChance = serverStats.CriticalHitChance;
            ShipStats.CriticalHitDamage = serverStats.CriticalHitDamage;
            
            // Bonus breakdowns
            ShipStats.CrewBonuses = serverStats.CrewBonuses;
            ShipStats.EquipmentBonuses = serverStats.EquipmentBonuses;
            ShipStats.CannonballBonuses = serverStats.CannonballBonuses;
        }

        /// <summary>
        /// GameServer'daki AmmoStatus'u Unity AmmoData'ya dönüştürür
        /// </summary>
        public void UpdateAmmoStatus(AmmoStatus serverAmmo)
        {
            if (serverAmmo == null) return;

            AmmoStatus.ShipId = serverAmmo.ShipId;
            AmmoStatus.CurrentAmmo = serverAmmo.CurrentAmmo;
            AmmoStatus.TotalAmmo = serverAmmo.TotalAmmo;
            AmmoStatus.MaxCapacity = serverAmmo.MaxCapacity;
            AmmoStatus.SelectedCannonballType = serverAmmo.SelectedCannonballType;
            AmmoStatus.CannonballTypes = serverAmmo.CannonballTypes;
            AmmoStatus.IsReloading = serverAmmo.IsReloading;
            AmmoStatus.ReloadTimeRemaining = serverAmmo.ReloadTimeRemaining;
            AmmoStatus.AmmoPercentage = serverAmmo.AmmoPercentage;
        }

        /// <summary>
        /// Oyuncu verilerini başka bir oyuncudan kopyalar
        /// </summary>
        public void CopyFrom(PlayerData other)
        {
            if (other == null) return;

            // Temel bilgileri kopyala
            Username = other.Username;
            ShipId = other.ShipId;
            ConnectionId = other.ConnectionId;

            // Dönüşüm bilgilerini kopyala
            Position = other.Position;
            Rotation = other.Rotation;
            Velocity = other.Velocity;

            // Durum bilgilerini kopyala
            Health = other.Health;
            MaxHealth = other.MaxHealth;
            IsAlive = other.IsAlive;
            RespawnTime = other.RespawnTime;

            // İstatistikleri kopyala
            Score = other.Score;
            Kills = other.Kills;
            Deaths = other.Deaths;
            LastActivity = other.LastActivity;

            // Modern MMO verilerini kopyala
            if (other.ShipStats != null)
            {
                if (ShipStats == null) ShipStats = new ShipStatsData();
                ShipStats.CopyFrom(other.ShipStats);
            }

            if (other.AmmoStatus != null)
            {
                if (AmmoStatus == null) AmmoStatus = new AmmoData();
                AmmoStatus.CopyFrom(other.AmmoStatus);
            }

            if (other.CombatInfo != null)
            {
                if (CombatInfo == null) CombatInfo = new CombatData();
                CombatInfo.CopyFrom(other.CombatInfo);
            }

            if (other.Profile != null)
            {
                if (Profile == null) Profile = new PlayerProfile();
                Profile.CopyFrom(other.Profile);
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
                Debug.LogError($"PlayerData ayrıştırma hatası: {e.Message}");
                return null;
            }
        }
    }

    /// <summary>
    /// Modern MMO gemi istatistikleri - GameServer ShipStatsDto ile uyumlu
    /// </summary>
    [Serializable]
    public class ShipStatsData
    {
        public Guid ShipId = Guid.Empty;
        public DateTime LastCalculated = DateTime.UtcNow;
        
        // Ana istatistikler
        public float AttackPower;
        public float Armor;
        public float Speed;
        public float Maneuverability;
        public float CannonPower;
        public float HitRate;
        public float CriticalHitChance;
        public float CriticalHitDamage;
        
        // Bonus ayrıntıları
        public StatsBonusDto CrewBonuses = new StatsBonusDto();
        public StatsBonusDto EquipmentBonuses = new StatsBonusDto();
        public StatsBonusDto CannonballBonuses = new StatsBonusDto();

        public void CopyFrom(ShipStatsData other)
        {
            if (other == null) return;

            ShipId = other.ShipId;
            LastCalculated = other.LastCalculated;
            AttackPower = other.AttackPower;
            Armor = other.Armor;
            Speed = other.Speed;
            Maneuverability = other.Maneuverability;
            CannonPower = other.CannonPower;
            HitRate = other.HitRate;
            CriticalHitChance = other.CriticalHitChance;
            CriticalHitDamage = other.CriticalHitDamage;

            CrewBonuses = other.CrewBonuses;
            EquipmentBonuses = other.EquipmentBonuses;
            CannonballBonuses = other.CannonballBonuses;
        }
    }

    /// <summary>
    /// Modern MMO ammo sistemi - GameServer AmmoStatus ile uyumlu
    /// </summary>
    [Serializable]
    public class AmmoData
    {
        public Guid ShipId;
        public int CurrentAmmo;
        public int TotalAmmo;
        public int MaxCapacity;
        public int SelectedCannonballType;
        public Dictionary<int, int> CannonballTypes;
        public bool IsReloading;
        public double ReloadTimeRemaining;
        public float AmmoPercentage;
        
        public AmmoData()
        {
            ShipId = Guid.Empty;
            CurrentAmmo = 0;
            TotalAmmo = 0;
            MaxCapacity = 100;
            SelectedCannonballType = 0;
            CannonballTypes = new Dictionary<int, int>();
            IsReloading = false;
            ReloadTimeRemaining = 0;
            AmmoPercentage = 0;
        }

        public void CopyFrom(AmmoData other)
        {
            if (other == null) return;

            ShipId = other.ShipId;
            CurrentAmmo = other.CurrentAmmo;
            TotalAmmo = other.TotalAmmo;
            MaxCapacity = other.MaxCapacity;
            SelectedCannonballType = other.SelectedCannonballType;
            CannonballTypes = new Dictionary<int, int>(other.CannonballTypes);
            IsReloading = other.IsReloading;
            ReloadTimeRemaining = other.ReloadTimeRemaining;
            AmmoPercentage = other.AmmoPercentage;
        }
    }

    /// <summary>
    /// Modern MMO combat sistemi
    /// </summary>
    [Serializable]
    public class CombatData
    {
        public DateTime LastAttackTime;
        public DateTime LastDamageTaken;
        public float AttackCooldownRemaining;
        public bool IsInCombat;
        public List<DamageEvent> RecentDamage;
        
        public CombatData()
        {
            LastAttackTime = DateTime.MinValue;
            LastDamageTaken = DateTime.MinValue;
            AttackCooldownRemaining = 0f;
            IsInCombat = false;
            RecentDamage = new List<DamageEvent>();
        }

        public void CopyFrom(CombatData other)
        {
            if (other == null) return;

            LastAttackTime = other.LastAttackTime;
            LastDamageTaken = other.LastDamageTaken;
            AttackCooldownRemaining = other.AttackCooldownRemaining;
            IsInCombat = other.IsInCombat;
            RecentDamage = new List<DamageEvent>(other.RecentDamage);
        }
    }

    /// <summary>
    /// Modern MMO player profili
    /// </summary>
    [Serializable]
    public class PlayerProfile
    {
        public int Level;
        public int Experience;
        public int Pearls;
        public int Artifacts;
        public int Victories;
        public int Defeats;
        public DateTime CreatedAt;
        public DateTime LastLogin;
        
        public PlayerProfile()
        {
            Level = 1;
            Experience = 0;
            Pearls = 0;
            Artifacts = 0;
            Victories = 0;
            Defeats = 0;
            CreatedAt = DateTime.UtcNow;
            LastLogin = DateTime.UtcNow;
        }

        public void CopyFrom(PlayerProfile other)
        {
            if (other == null) return;

            Level = other.Level;
            Experience = other.Experience;
            Pearls = other.Pearls;
            Artifacts = other.Artifacts;
            Victories = other.Victories;
            Defeats = other.Defeats;
            CreatedAt = other.CreatedAt;
            LastLogin = other.LastLogin;
        }
    }

    /// <summary>
    /// Damage event tracker
    /// </summary>
    [Serializable]
    public class DamageEvent
    {
        public Guid AttackerId;
        public Guid TargetId;
        public float Damage;
        public bool IsCritical;
        public string DamageType;
        public DateTime Timestamp;
        
        public DamageEvent()
        {
            AttackerId = Guid.Empty;
            TargetId = Guid.Empty;
            Damage = 0f;
            IsCritical = false;
            DamageType = "Unknown";
            Timestamp = DateTime.UtcNow;
        }
    }
} 