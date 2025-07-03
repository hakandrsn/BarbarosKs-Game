using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using BarbarosKs.Shared.DTOs;

namespace BarbarosKs.Core
{
    /// <summary>
    /// Tüm prefab referanslarını merkezi olarak yöneten sistem
    /// </summary>
    [CreateAssetMenu(fileName = "PrefabManager", menuName = "BarbarosKs/Prefab Manager")]
    public class PrefabManager : ScriptableObject
    {
        private static PrefabManager _instance;
        public static PrefabManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<PrefabManager>("PrefabManager");
                    if (_instance == null)
                    {
                        Debug.LogError("❌ [PREFAB MANAGER] PrefabManager asset bulunamadı! Resources/PrefabManager.asset oluşturun.");
                        // Fallback olarak empty instance oluştur
                        _instance = CreateInstance<PrefabManager>();
                    }
                }
                return _instance;
            }
        }

        [Header("Cannonball Prefabs")]
        [SerializeField] private List<CannonballPrefabData> cannonballPrefabs = new List<CannonballPrefabData>();

        [Header("Ship Prefabs")]
        [SerializeField] private List<ShipPrefabData> shipPrefabs = new List<ShipPrefabData>();

        [Header("Effect Prefabs")]
        [SerializeField] private GameObject hitEffectPrefab;
        [SerializeField] private GameObject explosionEffectPrefab;
        [SerializeField] private GameObject lightningEffectPrefab;

        [Header("UI Prefabs")]
        [SerializeField] private GameObject damageTextPrefab;
        [SerializeField] private GameObject loadingScreenPrefab;
        [SerializeField] private GameObject notificationPrefab;

        [Header("Network Prefabs")]
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private GameObject networkShipPrefab;

        // Runtime cache
        private Dictionary<string, GameObject> cannonballPrefabCache;
        private Dictionary<int, GameObject> cannonballIdCache;
        private Dictionary<string, GameObject> shipPrefabCache;

        private void OnEnable()
        {
            InitializeCaches();
        }

        private void InitializeCaches()
        {
            // Cannonball cache
            cannonballPrefabCache = new Dictionary<string, GameObject>();
            cannonballIdCache = new Dictionary<int, GameObject>();
            
            foreach (var data in cannonballPrefabs)
            {
                if (data.prefab != null)
                {
                    cannonballPrefabCache[data.typeCode] = data.prefab;
                    cannonballIdCache[data.id] = data.prefab;
                }
            }

            // Ship cache
            shipPrefabCache = new Dictionary<string, GameObject>();
            
            foreach (var data in shipPrefabs)
            {
                if (data.prefab != null)
                {
                    shipPrefabCache[data.shipCode] = data.prefab;
                }
            }

            Debug.Log($"✅ [PREFAB MANAGER] Cache initialized - Cannonballs: {cannonballPrefabCache.Count}, Ships: {shipPrefabCache.Count}");
        }

        #region Cannonball Prefabs

        /// <summary>
        /// Type code'a göre cannonball prefab'ını döndürür
        /// </summary>
        public GameObject GetCannonballPrefab(string typeCode)
        {
            if (cannonballPrefabCache == null) InitializeCaches();

            if (cannonballPrefabCache.TryGetValue(typeCode, out var prefab))
            {
                return prefab;
            }

            Debug.LogWarning($"⚠️ [PREFAB MANAGER] Cannonball prefab bulunamadı: {typeCode}");
            return GetDefaultCannonballPrefab();
        }

        /// <summary>
        /// ID'ye göre cannonball prefab'ını döndürür
        /// </summary>
        public GameObject GetCannonballPrefab(int id)
        {
            if (cannonballIdCache == null) InitializeCaches();

            if (cannonballIdCache.TryGetValue(id, out var prefab))
            {
                return prefab;
            }

            Debug.LogWarning($"⚠️ [PREFAB MANAGER] Cannonball prefab bulunamadı (ID: {id})");
            return GetDefaultCannonballPrefab();
        }

        /// <summary>
        /// CannonballTypeDto'dan prefab döndürür
        /// </summary>
        public GameObject GetCannonballPrefab(CannonballTypeDto cannonballData)
        {
            if (cannonballData == null)
            {
                Debug.LogWarning("⚠️ [PREFAB MANAGER] CannonballTypeDto null!");
                return GetDefaultCannonballPrefab();
            }

            // Önce ID ile dene
            var prefab = GetCannonballPrefab(cannonballData.Id);
            if (prefab != GetDefaultCannonballPrefab())
            {
                return prefab;
            }

            // ID ile bulunamadıysa code ile dene
            return GetCannonballPrefab(cannonballData.Code);
        }

        /// <summary>
        /// Varsayılan cannonball prefab'ını döndürür
        /// </summary>
        public GameObject GetDefaultCannonballPrefab()
        {
            if (cannonballPrefabs.Count > 0 && cannonballPrefabs[0].prefab != null)
            {
                return cannonballPrefabs[0].prefab;
            }

            Debug.LogError("❌ [PREFAB MANAGER] Varsayılan cannonball prefab bulunamadı!");
            return null;
        }

        /// <summary>
        /// Tüm cannonball prefab verilerini döndürür
        /// </summary>
        public List<CannonballPrefabData> GetAllCannonballPrefabs()
        {
            return cannonballPrefabs.ToList();
        }

        #endregion

        #region Ship Prefabs

        /// <summary>
        /// Ship code'a göre ship prefab'ını döndürür
        /// </summary>
        public GameObject GetShipPrefab(string shipCode)
        {
            if (shipPrefabCache == null) InitializeCaches();

            if (shipPrefabCache.TryGetValue(shipCode, out var prefab))
            {
                return prefab;
            }

            Debug.LogWarning($"⚠️ [PREFAB MANAGER] Ship prefab bulunamadı: {shipCode}");
            return GetDefaultShipPrefab();
        }

        /// <summary>
        /// Varsayılan ship prefab'ını döndürür
        /// </summary>
        public GameObject GetDefaultShipPrefab()
        {
            if (shipPrefabs.Count > 0 && shipPrefabs[0].prefab != null)
            {
                return shipPrefabs[0].prefab;
            }

            Debug.LogError("❌ [PREFAB MANAGER] Varsayılan ship prefab bulunamadı!");
            return null;
        }

        #endregion

        #region Effect Prefabs

        public GameObject GetHitEffectPrefab() => hitEffectPrefab;
        public GameObject GetExplosionEffectPrefab() => explosionEffectPrefab;
        public GameObject GetLightningEffectPrefab() => lightningEffectPrefab;

        #endregion

        #region UI Prefabs

        public GameObject GetDamageTextPrefab() => damageTextPrefab;
        public GameObject GetLoadingScreenPrefab() => loadingScreenPrefab;
        public GameObject GetNotificationPrefab() => notificationPrefab;

        #endregion

        #region Network Prefabs

        public GameObject GetPlayerPrefab() => playerPrefab;
        public GameObject GetNetworkShipPrefab() => networkShipPrefab;

        #endregion

        #region Validation and Debug

        /// <summary>
        /// Tüm prefab referanslarının geçerli olup olmadığını kontrol eder
        /// </summary>
        [ContextMenu("Validate All Prefabs")]
        public void ValidateAllPrefabs()
        {
            Debug.Log("=== PREFAB VALIDATION ===");
            
            int validCannonballs = 0;
            int invalidCannonballs = 0;

            foreach (var data in cannonballPrefabs)
            {
                if (data.prefab != null && !string.IsNullOrEmpty(data.typeCode))
                {
                    validCannonballs++;
                    Debug.Log($"✅ Cannonball: {data.typeCode} - {data.prefab.name}");
                }
                else
                {
                    invalidCannonballs++;
                    Debug.LogError($"❌ Invalid Cannonball: {data.typeCode}");
                }
            }

            int validShips = 0;
            int invalidShips = 0;

            foreach (var data in shipPrefabs)
            {
                if (data.prefab != null && !string.IsNullOrEmpty(data.shipCode))
                {
                    validShips++;
                    Debug.Log($"✅ Ship: {data.shipCode} - {data.prefab.name}");
                }
                else
                {
                    invalidShips++;
                    Debug.LogError($"❌ Invalid Ship: {data.shipCode}");
                }
            }

            Debug.Log($"=== VALIDATION COMPLETE ===");
            Debug.Log($"Valid Cannonballs: {validCannonballs}, Invalid: {invalidCannonballs}");
            Debug.Log($"Valid Ships: {validShips}, Invalid: {invalidShips}");
        }

        /// <summary>
        /// Prefab cache'ini yeniler
        /// </summary>
        [ContextMenu("Refresh Cache")]
        public void RefreshCache()
        {
            InitializeCaches();
            Debug.Log("🔄 [PREFAB MANAGER] Cache refreshed");
        }

        #endregion
    }

    /// <summary>
    /// Cannonball prefab verisi
    /// </summary>
    [System.Serializable]
    public class CannonballPrefabData
    {
        [Header("Identification")]
        public int id;                      // Database ID
        public string typeCode;             // Type code (CB1, SHRAPNEL, etc.)
        public string displayName;          // Display name

        [Header("Prefab")]
        public GameObject prefab;           // Prefab reference

        [Header("Network")]
        public bool isNetworkEnabled = true; // Network'te kullanılabilir mi

        [Header("Properties")]
        public int baseDamage = 10;
        public float baseSpeed = 30f;
        public float baseRange = 10f;
        public string description;
    }

    /// <summary>
    /// Ship prefab verisi
    /// </summary>
    [System.Serializable]
    public class ShipPrefabData
    {
        [Header("Identification")]
        public string shipCode;             // Ship code (SHIP001, etc.)
        public string displayName;          // Display name

        [Header("Prefab")]
        public GameObject prefab;           // Prefab reference

        [Header("Properties")]
        public int baseHealth = 100;
        public float baseSpeed = 10f;
        public string description;
    }
} 