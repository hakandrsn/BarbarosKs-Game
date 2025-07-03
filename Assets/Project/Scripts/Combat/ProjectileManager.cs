using System.Collections.Generic;
using UnityEngine;
using BarbarosKs.Shared.DTOs;
using BarbarosKs.Core;
using Project.Scripts.Network;

namespace BarbarosKs.Combat
{
    /// <summary>
    /// Network'ten gelen gülle spawn mesajlarını yöneten singleton manager
    /// PrefabManager entegrasyonlu versiyon
    /// </summary>
    public class ProjectileManager : MonoBehaviour
    {
        public static ProjectileManager Instance { get; private set; }
        
        [Header("Settings")]
        [SerializeField] private Transform projectileParent; // Güllerin parent'ı (organizasyon için)
        [SerializeField] private bool usePrefabManager = true; // PrefabManager kullanılsın mı
        
        [Header("Fallback Prefabs (PrefabManager yoksa)")]
        [SerializeField] private GameObject fallbackCannonballPrefab;
        [SerializeField] private GameObject fallbackShrapnelPrefab;
        
        // Aktif gülleler (network senkronizasyonu için)
        private Dictionary<string, GameObject> activeProjectiles = new Dictionary<string, GameObject>();
        
        private void Awake()
        {
            // Singleton pattern
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                Debug.Log("✅ [PROJECTILE MANAGER] Singleton oluşturuldu");
            }
            else
            {
                Debug.Log("⚠️ [PROJECTILE MANAGER] Duplicate instance destroy ediliyor");
                Destroy(gameObject);
                return;
            }
            
            // ProjectileParent yoksa oluştur
            if (projectileParent == null)
            {
                var parentObj = new GameObject("ProjectileContainer");
                parentObj.transform.SetParent(transform);
                projectileParent = parentObj.transform;
                Debug.Log("📦 [PROJECTILE MANAGER] ProjectileContainer oluşturuldu");
            }
        }
        
        private void Start()
        {
            // NetworkManager event'lerini dinle
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.OnProjectileSpawn += HandleProjectileSpawn;
                Debug.Log("📡 [PROJECTILE MANAGER] NetworkManager event'i dinleniyor");
            }
            else
            {
                Debug.LogError("❌ [PROJECTILE MANAGER] NetworkManager bulunamadı!");
            }

            // PrefabManager kontrolü
            if (usePrefabManager)
            {
                ValidatePrefabManager();
            }
        }
        
        private void OnDestroy()
        {
            // Event'leri temizle
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.OnProjectileSpawn -= HandleProjectileSpawn;
            }
        }

        private void ValidatePrefabManager()
        {
            if (PrefabManager.Instance != null)
            {
                Debug.Log("✅ [PROJECTILE MANAGER] PrefabManager entegrasyonu aktif");
                
                // Validation
                var defaultPrefab = PrefabManager.Instance.GetDefaultCannonballPrefab();
                if (defaultPrefab == null)
                {
                    Debug.LogWarning("⚠️ [PROJECTILE MANAGER] PrefabManager'da varsayılan cannonball prefab yok!");
                }
            }
            else
            {
                Debug.LogWarning("⚠️ [PROJECTILE MANAGER] PrefabManager bulunamadı, fallback prefab'lar kullanılacak");
                usePrefabManager = false;
            }
        }
        
        /// <summary>
        /// Network'ten gelen gülle spawn mesajını handle eder
        /// </summary>
        private void HandleProjectileSpawn(S2C_ProjectileSpawnData spawnData)
        {
            Debug.Log($"🚀 [PROJECTILE MANAGER] Gülle spawn request alındı: {spawnData.ProjectileType} ID: {spawnData.ProjectileId}");
            
            // Prefab'ı bul
            GameObject prefab = GetProjectilePrefab(spawnData.ProjectileType);
            if (prefab == null)
            {
                Debug.LogError($"❌ [PROJECTILE MANAGER] Prefab bulunamadı: {spawnData.ProjectileType}");
                return;
            }
            
            // Gülle'yi spawn et
            SpawnNetworkProjectile(spawnData, prefab);
        }
        
        /// <summary>
        /// Gülle tipine göre uygun prefab'ı döndürür (PrefabManager entegrasyonlu)
        /// </summary>
        private GameObject GetProjectilePrefab(string projectileType)
        {
            // PrefabManager kullan
            if (usePrefabManager && PrefabManager.Instance != null)
            {
                var prefab = PrefabManager.Instance.GetCannonballPrefab(projectileType);
                if (prefab != null)
                {
                    Debug.Log($"✅ [PROJECTILE MANAGER] PrefabManager'dan prefab alındı: {projectileType}");
                    return prefab;
                }
            }

            // Fallback prefab'lar
            return projectileType switch
            {
                "Cannonball" or "CB1" => fallbackCannonballPrefab,
                "Shrapnel" or "SHRAPNEL" => fallbackShrapnelPrefab,
                _ => GetFallbackPrefab(projectileType)
            };
        }

        private GameObject GetFallbackPrefab(string projectileType)
        {
            Debug.LogWarning($"⚠️ [PROJECTILE MANAGER] Bilinmeyen projectile tipi: {projectileType}, varsayılan fallback kullanılıyor");
            
            return fallbackCannonballPrefab != null ? fallbackCannonballPrefab : fallbackShrapnelPrefab;
        }

        /// <summary>
        /// Manual gülle spawn (Test ve local kullanım için)
        /// </summary>
        public GameObject SpawnProjectile(string projectileType, Vector3 startPosition, Transform target, int damage)
        {
            var prefab = GetProjectilePrefab(projectileType);
            if (prefab == null)
            {
                Debug.LogError($"❌ [PROJECTILE MANAGER] Manual spawn başarısız - prefab bulunamadı: {projectileType}");
                return null;
            }

            // Gülle'yi oluştur
            GameObject projectileObj = Instantiate(prefab, startPosition, Quaternion.identity, projectileParent);
            
            // Projectile component'ını initialize et
            if (projectileObj.TryGetComponent<Projectile>(out var projectile))
            {
                projectile.Initialize(damage, target);
                
                Debug.Log($"✅ [PROJECTILE MANAGER] Manual gülle spawn edildi: {projectileType}");
                Debug.Log($"   Position: {startPosition}");
                Debug.Log($"   Target: {target?.name ?? "None"}");
                Debug.Log($"   Damage: {damage}");
                
                return projectileObj;
            }
            else
            {
                Debug.LogError($"❌ [PROJECTILE MANAGER] Projectile component bulunamadı!");
                Destroy(projectileObj);
                return null;
            }
        }

        /// <summary>
        /// CannonballTypeDto ile gülle spawn (GameDataManager entegrasyonu)
        /// </summary>
        public GameObject SpawnProjectile(CannonballTypeDto cannonballData, Vector3 startPosition, Transform target)
        {
            if (cannonballData == null)
            {
                Debug.LogError("❌ [PROJECTILE MANAGER] CannonballTypeDto null!");
                return null;
            }

            GameObject prefab = null;
            
            // PrefabManager ile dene
            if (usePrefabManager && PrefabManager.Instance != null)
            {
                prefab = PrefabManager.Instance.GetCannonballPrefab(cannonballData);
            }
            
            // Fallback
            if (prefab == null)
            {
                prefab = GetProjectilePrefab(cannonballData.Code.ToString());
            }

            if (prefab == null)
            {
                Debug.LogError($"❌ [PROJECTILE MANAGER] Prefab bulunamadı: {cannonballData.Code}");
                return null;
            }

            // Gülle'yi oluştur
            GameObject projectileObj = Instantiate(prefab, startPosition, Quaternion.identity, projectileParent);
            
            // Projectile component'ını initialize et
            if (projectileObj.TryGetComponent<Projectile>(out var projectile))
            {
                // CannonballTypeDto'dan damage al
                int damage = cannonballData.BaseDamage > 0 ? cannonballData.BaseDamage : 10;
                projectile.Initialize(damage, target);
                
                Debug.Log($"✅ [PROJECTILE MANAGER] CannonballTypeDto ile spawn: {cannonballData.Name}");
                return projectileObj;
            }
            else
            {
                Debug.LogError($"❌ [PROJECTILE MANAGER] Projectile component bulunamadı!");
                Destroy(projectileObj);
                return null;
            }
        }
        
        /// <summary>
        /// Network'ten gelen verilerle gülle spawn eder
        /// </summary>
        private void SpawnNetworkProjectile(S2C_ProjectileSpawnData spawnData, GameObject prefab)
        {
            // Hedef transform'unu bul
            Transform targetTransform = FindTargetTransform(spawnData.TargetId?.ToString());
            if (targetTransform == null)
            {
                Debug.LogError($"❌ [PROJECTILE MANAGER] Hedef bulunamadı: {spawnData.TargetId}");
                return;
            }
            
            // Gülle'yi oluştur
            Vector3 startPos = new Vector3(spawnData.SourcePosition.X, spawnData.SourcePosition.Y, spawnData.SourcePosition.Z);
            GameObject projectileObj = Instantiate(prefab, startPos, Quaternion.identity, projectileParent);
            
            // Projectile component'ını al ve network verilerini set et
            if (projectileObj.TryGetComponent<Projectile>(out var projectile))
            {
                // Network gülle initialize et (farklı metod)
                projectile.InitializeFromNetwork(spawnData.Damage, targetTransform, spawnData.FlightTime);
                
                Debug.Log($"✅ [PROJECTILE MANAGER] Network gülle oluşturuldu: {spawnData.ProjectileId}");
                Debug.Log($"   Başlangıç: {startPos}");
                Debug.Log($"   Hedef: {targetTransform.name}");
                Debug.Log($"   Damage: {spawnData.Damage}");
                Debug.Log($"   Flight Time: {spawnData.FlightTime:F2}s");
            }
            else
            {
                Debug.LogError($"❌ [PROJECTILE MANAGER] Projectile component bulunamadı!");
                Destroy(projectileObj);
                return;
            }
            
            // Aktif gülleler listesine ekle
            activeProjectiles[spawnData.ProjectileId.ToString()] = projectileObj;
            
            // Güvenlik için otomatik temizlik (flight time + buffer)
            StartCoroutine(CleanupProjectileAfterTime(spawnData.ProjectileId.ToString(), spawnData.FlightTime + 2f));
        }
        
        /// <summary>
        /// Target ID'sine göre transform bulur
        /// </summary>
        private Transform FindTargetTransform(string targetId)
        {
            // Önce GUID olarak parse etmeyi dene (PlayerController'lar için)
            if (System.Guid.TryParse(targetId, out _))
            {
                // NetworkIdentity ile ara
                var networkIdentities = FindObjectsOfType<Project.Scripts.Network.NetworkIdentity>();
                foreach (var identity in networkIdentities)
                {
                    if (identity.EntityId == targetId)
                    {
                        return identity.transform;
                    }
                }
            }
            
            // GameObject.name ile ara (TestEnemy gibi statik objeler için)
            GameObject target = GameObject.Find(targetId);
            if (target != null)
            {
                return target.transform;
            }
            
            Debug.LogWarning($"⚠️ [PROJECTILE MANAGER] Hedef bulunamadı: {targetId}");
            return null;
        }
        
        /// <summary>
        /// Belirli süre sonra gülle'yi temizler
        /// </summary>
        private System.Collections.IEnumerator CleanupProjectileAfterTime(string projectileId, float delay)
        {
            yield return new WaitForSeconds(delay);
            
            if (activeProjectiles.TryGetValue(projectileId, out var projectile))
            {
                if (projectile != null)
                {
                    Destroy(projectile);
                }
                activeProjectiles.Remove(projectileId);
                Debug.Log($"🧹 [PROJECTILE MANAGER] Gülle temizlendi: {projectileId}");
            }
        }
        
        #region Debug Methods

        /// <summary>
        /// Debug: Aktif gülle sayısını gösterir
        /// </summary>
        [ContextMenu("Show Active Projectiles")]
        public void ShowActiveProjectiles()
        {
            Debug.Log($"📊 [PROJECTILE MANAGER] Aktif gülle sayısı: {activeProjectiles.Count}");
            foreach (var kvp in activeProjectiles)
            {
                Debug.Log($"   - {kvp.Key}: {(kvp.Value != null ? kvp.Value.name : "NULL")}");
            }
        }

        [ContextMenu("Test Cannonball Spawn")]
        private void TestCannonballSpawn()
        {
            // Test target oluştur
            var testTarget = GameObject.CreatePrimitive(PrimitiveType.Cube);
            testTarget.name = "TestTarget";
            testTarget.transform.position = Vector3.forward * 5f;
            
            // Test spawn
            SpawnProjectile("CB1", transform.position, testTarget.transform, 25);
            
            Debug.Log("🧪 [PROJECTILE MANAGER] Test cannonball spawn edildi");
        }

        [ContextMenu("Validate PrefabManager Integration")]
        private void DebugValidatePrefabManager()
        {
            ValidatePrefabManager();
            
            if (PrefabManager.Instance != null)
            {
                Debug.Log("=== PREFAB MANAGER VALIDATION ===");
                var allPrefabs = PrefabManager.Instance.GetAllCannonballPrefabs();
                Debug.Log($"Kayıtlı cannonball prefab sayısı: {allPrefabs.Count}");
                
                foreach (var prefabData in allPrefabs)
                {
                    Debug.Log($"  - {prefabData.typeCode}: {prefabData.displayName}");
                }
            }
        }

        #endregion
    }
} 