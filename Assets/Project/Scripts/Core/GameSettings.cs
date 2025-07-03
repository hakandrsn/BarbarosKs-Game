using UnityEngine;

namespace BarbarosKs.Core
{
    /// <summary>
    /// Oyun genelinde kullanılan ayarları tutar.
    /// İleride sunucudan çekilecek ayarlar için hazırlanmış.
    /// </summary>
    [CreateAssetMenu(fileName = "GameSettings", menuName = "BarbarosKs/Game Settings")]
    public class GameSettings : ScriptableObject
    {
        [Header("Combat Settings")]
        [Tooltip("Projektil hızı (metre/saniye)")]
        public float projectileSpeed = 30f;
        
        [Tooltip("Projektil yörünge yüksekliği (metre)")]
        public float projectileArcHeight = 2f;
        
        [Tooltip("Maksimum projektil menzili (metre)")]
        public float maxProjectileRange = 10f;
        
        [Tooltip("Projektil maksimum yaşam süresi (saniye)")]
        public float projectileMaxLifetime = 10f;

        [Header("Visual Effects")]
        [Tooltip("Projektil döndürme hızı")]
        public float projectileRotationSpeed = 360f;
        
        [Tooltip("Hit effect kalma süresi (saniye)")]
        public float hitEffectDuration = 2f;

        [Header("Audio Settings")]
        [Range(0f, 1f)]
        [Tooltip("Combat sesleri ses seviyesi")]
        public float combatSoundVolume = 1f;

        [Header("Network Settings")]
        [Tooltip("Sunucudan ayarları çekme aktif mi?")]
        public bool useServerSettings = false;
        
        [Tooltip("Sunucu ayarları çekme URL'si")]
        public string serverSettingsUrl = "https://api.barbaros-ks.com/game-settings";

        /// <summary>
        /// Singleton instance - tüm projede aynı ayarları kullanmak için
        /// </summary>
        private static GameSettings _instance;
        public static GameSettings Instance
        {
            get
            {
                if (_instance) return _instance;
                _instance = Resources.Load<GameSettings>("GameSettings");
                if (_instance) return _instance;
                Debug.LogError("❌ [GAME SETTINGS] GameSettings asset bulunamadı! Resources/GameSettings.asset oluşturun.");
                // Fallback olarak default değerlerle geçici instance oluştur
                _instance = CreateInstance<GameSettings>();
                return _instance;
            }
        }

        /// <summary>
        /// Mesafeye göre projektil uçma süresini hesaplar
        /// </summary>
        public float CalculateFlightTime(float distance)
        {
            return distance / projectileSpeed;
        }

        /// <summary>
        /// Projektil hedefe erişebilir mi kontrol eder
        /// </summary>
        public bool IsWithinRange(float distance)
        {
            return distance <= maxProjectileRange;
        }

        /// <summary>
        /// İleride sunucudan ayarları çekmek için hazırlanan method
        /// </summary>
        public void LoadFromServer()
        {
            if (!useServerSettings) 
            {
                Debug.Log("📋 [GAME SETTINGS] Sunucu ayarları kullanımı devre dışı");
                return;
            }

            Debug.Log("🌐 [GAME SETTINGS] Sunucudan ayarlar çekiliyor...");
            // TODO: İleride sunucudan ayarları çekme implementasyonu
            // WebRequest ile serverSettingsUrl'den JSON çekip ayarları güncelle
        }

        /// <summary>
        /// Debug için ayarları logla
        /// </summary>
        [ContextMenu("Log Current Settings")]
        public void LogSettings()
        {
            Debug.Log($"🎮 [GAME SETTINGS] Current Settings:");
            Debug.Log($"  Projectile Speed: {projectileSpeed} m/s");
            Debug.Log($"  Arc Height: {projectileArcHeight} m");
            Debug.Log($"  Max Range: {maxProjectileRange} m");
            Debug.Log($"  Max Lifetime: {projectileMaxLifetime} s");
            Debug.Log($"  Use Server Settings: {useServerSettings}");
        }

        private void OnValidate()
        {
            // Editor'da değer kontrolü
            projectileSpeed = Mathf.Max(1f, projectileSpeed);
            projectileArcHeight = Mathf.Max(0f, projectileArcHeight);
            maxProjectileRange = Mathf.Max(10f, maxProjectileRange);
            projectileMaxLifetime = Mathf.Max(1f, projectileMaxLifetime);
        }
    }
} 