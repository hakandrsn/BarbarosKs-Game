using System.Collections.Generic;
using UnityEngine;
using BarbarosKs.Shared.DTOs;
using BarbarosKs.Core;
using Project.Scripts.Network;

namespace BarbarosKs.Combat
{
    /// <summary>
    /// Network'ten gelen gülle spawn mesajlarını yöneten singleton manager
    /// </summary>
    public class ProjectileManager : MonoBehaviour
    {
        public static ProjectileManager Instance { get; private set; }
        
        [Header("Projectile Prefabs")]
        [SerializeField] private GameObject cannonballPrefab; // Varsayılan gülle prefab'ı
        
        [Header("Settings")]
        [SerializeField] private Transform projectileParent; // Güllerin parent'ı (organizasyon için)
        
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
        }
        
        private void OnDestroy()
        {
            // Event'leri temizle
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.OnProjectileSpawn -= HandleProjectileSpawn;
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
        /// Gülle tipine göre uygun prefab'ı döndürür
        /// </summary>
        private GameObject GetProjectilePrefab(string projectileType)
        {
            switch (projectileType)
            {
                case "Cannonball":
                default:
                    return cannonballPrefab;
            }
        }
        
        /// <summary>
        /// Network'ten gelen verilerle gülle spawn eder
        /// </summary>
        private void SpawnNetworkProjectile(S2C_ProjectileSpawnData spawnData, GameObject prefab)
        {
            // Hedef transform'unu bul
            Transform targetTransform = FindTargetTransform(spawnData.TargetId);
            if (targetTransform == null)
            {
                Debug.LogError($"❌ [PROJECTILE MANAGER] Hedef bulunamadı: {spawnData.TargetId}");
                return;
            }
            
            // Gülle'yi oluştur
            Vector3 startPos = new Vector3(spawnData.StartPosition.X, spawnData.StartPosition.Y, spawnData.StartPosition.Z);
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
            activeProjectiles[spawnData.ProjectileId] = projectileObj;
            
            // Güvenlik için otomatik temizlik (flight time + buffer)
            StartCoroutine(CleanupProjectileAfterTime(spawnData.ProjectileId, spawnData.FlightTime + 2f));
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
    }
} 