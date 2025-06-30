using System;
using System.Collections.Generic;
using System.Linq;
using BarbarosKs.Player;
using BarbarosKs.Shared.DTOs;
using UnityEngine;
// Yeni ve doğru DTO namespace'i

namespace Project.Scripts.Network
{
    /// <summary>
    ///     NetworkManager'dan gelen olayları dinleyerek sahnedeki ağ nesnelerini (diğer oyuncular, NPC'ler vb.)
    ///     oluşturan, güncelleyen ve yok eden merkezi sınıf.
    /// </summary>
    public class NetworkObjectSpawner : MonoBehaviour
    {
        [Header("Prefab Ayarları")] [Tooltip("Eşleşme bulunamazsa kullanılacak varsayılan prefab.")] [SerializeField]
        private GameObject defaultPlayerPrefab;

        [SerializeField] private List<NetworkPrefabMapping> prefabMappings = new();

        [Header("Spawn Konteynerleri")] [SerializeField]
        private Transform playersContainer;

        [SerializeField] private Transform npcsContainer; // Gelecekteki NPC'ler için
        private readonly Dictionary<string, GameObject> _prefabLookup = new();

        // Ağ nesneleri sözlüğü (EntityId'ye göre GameObject tutar)
        private readonly Dictionary<string, GameObject> _spawnedEntities = new();

        public static NetworkObjectSpawner Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            // Prefab eşlemelerini daha hızlı erişim için bir sözlüğe dönüştür
            foreach (var mapping in prefabMappings.Where(mapping => !string.IsNullOrEmpty(mapping.prefabType) && mapping.prefab != null))
                _prefabLookup[mapping.prefabType] = mapping.prefab;

            if (playersContainer == null) playersContainer = new GameObject("NetworkPlayers").transform;
        }

        private void Start()
        {
            Debug.Log("🔍 [SPAWNER] NetworkObjectSpawner Start() çağrıldı");
            
            if (NetworkManager.Instance == null)
            {
                Debug.LogError("❌ [SPAWNER] NetworkManager sahnede bulunamadı! Spawner çalışamaz.");
                enabled = false;
                return;
            }

            Debug.Log("✅ [SPAWNER] NetworkManager bulundu, event'leri dinlemeye başlanıyor...");

            // NetworkManager'dan gelen yeni, temiz ve DTO-odaklı olayları dinliyoruz.
            NetworkManager.Instance.OnWorldStateReceived += HandleWorldState;
            NetworkManager.Instance.OnEntitySpawned += HandleEntitySpawned;
            NetworkManager.Instance.OnEntityDespawned += HandleEntityDespawned;
            NetworkManager.Instance.OnTransformUpdate += HandleTransformUpdate;
            
            Debug.Log("✅ [SPAWNER] Tüm event'ler başarıyla dinlenmeye başlandı");
            Debug.Log($"🔍 [SPAWNER] Prefab mapping sayısı: {_prefabLookup.Count}");
            
            foreach (var mapping in _prefabLookup)
            {
                Debug.Log($"🔍 [SPAWNER] Mapping: {mapping.Key} -> {mapping.Value?.name ?? "NULL"}");
            }
        }

        private void OnDestroy()
        {
            if (NetworkManager.Instance == null) return;
            NetworkManager.Instance.OnWorldStateReceived -= HandleWorldState;
            NetworkManager.Instance.OnEntitySpawned -= HandleEntitySpawned;
            NetworkManager.Instance.OnEntityDespawned -= HandleEntityDespawned;
            NetworkManager.Instance.OnTransformUpdate -= HandleTransformUpdate;
        }

        [Serializable]
        public class NetworkPrefabMapping
        {
            public string prefabType; // DTO'dan gelen "PlayerShip_Sloop" gibi bir anahtar
            public GameObject prefab;
        }

        #region Network Olay İşleyicileri

        /// <summary>
        ///     Oyuna ilk girildiğinde, dünyadaki tüm varlıkları oluşturur.
        /// </summary>
        private void HandleWorldState(S2C_WorldStateData data)
        {
            Debug.Log($"🟢 [SPAWNER] HandleWorldState çağrıldı!");
            Debug.Log($"🔍 [SPAWNER] YourPlayerId: {data?.YourPlayerId ?? "NULL"}");
            Debug.Log($"🔍 [SPAWNER] YourShipId: {data?.YourShipId ?? "NULL"}");
            Debug.Log($"🔍 [SPAWNER] Entities Count: {data?.Entities?.Count ?? 0}");

            if (data?.Entities != null)
            {
                for (int i = 0; i < data.Entities.Count; i++)
                {
                    var entity = data.Entities[i];
                    Debug.Log($"🔍 [SPAWNER] Entity[{i}]: ID={entity?.EntityId ?? "NULL"}, Type={entity?.PrefabType ?? "NULL"}, Owner={entity?.OwnerPlayerId ?? "NULL"}");
                }
            }

            Debug.Log($"🔍 [SPAWNER] Dünya durumu alınıyor. Sahnede oluşturulacak varlık sayısı: {data?.Entities?.Count ?? 0}");

            // Önce mevcut tüm nesneleri temizle (sahne yeniden yüklendiğinde vb. durumlar için)
            Debug.Log($"🔍 [SPAWNER] Mevcut entity'ler temizleniyor. Mevcut sayı: {_spawnedEntities.Count}");
            foreach (var spawnedObject in _spawnedEntities.Values) 
            {
                if (spawnedObject != null)
                {
                    Debug.Log($"🔍 [SPAWNER] Entity siliniyor: {spawnedObject.name}");
                    Destroy(spawnedObject);
                }
            }

            _spawnedEntities.Clear();
            Debug.Log($"✅ [SPAWNER] Mevcut entity'ler temizlendi");

            // Sunucudan gelen listedeki her varlığı oluştur.
            if (data?.Entities != null)
            {
                Debug.Log($"🔍 [SPAWNER] {data.Entities.Count} entity spawn edilecek...");
                foreach (var entityData in data.Entities) 
                {
                    Debug.Log($"🔍 [SPAWNER] Entity spawn ediliyor: {entityData?.EntityId}");
                    SpawnEntity(entityData);
                }
                Debug.Log($"✅ [SPAWNER] Tüm entity'ler spawn edildi");
            }
            else
            {
                Debug.LogWarning("❌ [SPAWNER] Data.Entities NULL!");
            }
        }

        /// <summary>
        ///     Oyun sırasında dünyaya yeni bir varlık girdiğinde onu oluşturur.
        /// </summary>
        private void HandleEntitySpawned(S2C_EntitySpawnData data)
        {
            Debug.Log($"Yeni varlık dünyaya giriyor: ID={data.Entity.EntityId}, Tip={data.Entity.PrefabType}");
            SpawnEntity(data.Entity);
        }

        /// <summary>
        ///     Bir varlık dünyadan ayrıldığında onu yok eder.
        /// </summary>
        private void HandleEntityDespawned(S2C_EntityDespawnData data)
        {
            Debug.Log($"{data.EntityId} ID'li varlık dünyadan ayrılıyor. Sebep: {data.Reason}");
            if (!_spawnedEntities.TryGetValue(data.EntityId, out var entityObject)) return;
            Destroy(entityObject);
            _spawnedEntities.Remove(data.EntityId);
        }

        /// <summary>
        ///     Sunucudan gelen toplu pozisyon güncellemelerini işler.
        ///     🚢 SMOOTH MOVEMENT: Local player için server transform update'lerini ignore eder!
        /// </summary>
        private void HandleTransformUpdate(S2C_TransformUpdateData data)
        {
            foreach (var (key, transformData) in data.Transforms)
            {
                if (!_spawnedEntities.TryGetValue(key, out var entityObject)) continue;
                
                // 🚢 LOCAL PLAYER İÇİN SERVER TRANSFORM UPDATE'LERİNİ IGNORE ET!
                var playerController = entityObject.GetComponent<PlayerController>();
                if (playerController != null && playerController.IsLocalPlayer)
                {
                    // Debug.Log($"🚫 [TRANSFORM] Local player transform update ignore edildi: {key}");
                    continue; // Local player'ın kendi hareketi server tarafından override edilmesin!
                }
                
                // Remote player'lar için server transform update'lerini uygula
                entityObject.transform.position = transformData.Position.ToUnity();
                entityObject.transform.rotation = transformData.Rotation.ToUnity();
                // Debug.Log($"✅ [TRANSFORM] Remote player transform update uygulandı: {key}");
            }
        }

        #endregion

        #region Yardımcı Metotlar

        /// <summary>
        ///     Gelen varlık verisine göre sahnede bir GameObject oluşturur.
        /// </summary>
        private void SpawnEntity(WorldEntityData entityData)
        {
            Debug.Log($"🔍 [SPAWN ENTITY] SpawnEntity çağrıldı");
            Debug.Log($"🔍 [SPAWN ENTITY] EntityId: {entityData?.EntityId ?? "NULL"}");
            Debug.Log($"🔍 [SPAWN ENTITY] PrefabType: {entityData?.PrefabType ?? "NULL"}"); 
            Debug.Log($"🔍 [SPAWN ENTITY] OwnerPlayerId: {entityData?.OwnerPlayerId ?? "NULL"}");
            Debug.Log($"🔍 [SPAWN ENTITY] Position: {entityData?.Position.ToString() ?? "NULL"}");
            
            if (entityData == null || string.IsNullOrEmpty(entityData.EntityId)) 
            {
                Debug.LogError("❌ [SPAWN ENTITY] EntityData null veya EntityId boş!");
                return;
            }

            // Bu varlık zaten sahnede varsa tekrar oluşturma.
            if (_spawnedEntities.ContainsKey(entityData.EntityId)) 
            {
                Debug.LogWarning($"❌ [SPAWN ENTITY] Entity {entityData.EntityId} zaten sahnede var!");
                return;
            }

            // Prefab'ı bul
            Debug.Log($"🔍 [SPAWN ENTITY] Prefab aranıyor: '{entityData.PrefabType}'");
            Debug.Log($"🔍 [SPAWN ENTITY] Mevcut mapping sayısı: {_prefabLookup.Count}");
            
            // Debug için tüm mevcut mapping'leri listele
            foreach (var kvp in _prefabLookup)
            {
                Debug.Log($"🔍 [SPAWN ENTITY] Available mapping: '{kvp.Key}' -> {kvp.Value?.name ?? "NULL"}");
            }
            
            GameObject prefabToSpawn = null;
            bool prefabFound = _prefabLookup.TryGetValue(entityData.PrefabType, out prefabToSpawn);
            
            if (!prefabFound)
            {
                // Eşleşme bulunamazsa varsayılanı kullan
                prefabToSpawn = defaultPlayerPrefab;
                Debug.LogWarning($"❌ [SPAWN ENTITY] '{entityData.PrefabType}' için prefab bulunamadı. Varsayılan kullanılıyor: {defaultPlayerPrefab?.name ?? "NULL"}");
            }
            else
            {
                Debug.Log($"✅ [SPAWN ENTITY] Prefab bulundu: {prefabToSpawn?.name ?? "NULL"}");
            }

            if (prefabToSpawn == null)
            {
                Debug.LogError("❌ [SPAWN ENTITY] Oluşturulacak prefab bulunamadı! DefaultPlayerPrefab Inspector'da ayarlanmış mı?");
                return;
            }

            // Nesneyi oluştur
            Debug.Log($"🔍 [SPAWN ENTITY] Prefab instantiate ediliyor...");
            Debug.Log($"🔍 [SPAWN ENTITY] Prefab: {prefabToSpawn.name}");
            Debug.Log($"🔍 [SPAWN ENTITY] Position: {entityData.Position.ToUnity()}");
            Debug.Log($"🔍 [SPAWN ENTITY] Rotation: {entityData.Rotation.ToUnity()}");
            
            var newEntityObject = Instantiate(
                prefabToSpawn,
                entityData.Position.ToUnity(),
                entityData.Rotation.ToUnity(),
                playersContainer); // TODO: Gelen tipe göre doğru konteyneri seç

            newEntityObject.name = $"{entityData.PrefabType}_{entityData.EntityId[..8]}";
            Debug.Log($"✅ [SPAWN ENTITY] GameObject oluşturuldu: {newEntityObject.name}");

            // Varlığın yerel oyuncuya ait olup olmadığını kontrol et
            var isLocal = GameManager.Instance.LocalPlayerId.HasValue &&
                          entityData.OwnerPlayerId == GameManager.Instance.LocalPlayerId.Value.ToString();
            Debug.Log($"🔍 [SPAWN ENTITY] Is Local Player: {isLocal}");
            Debug.Log($"🔍 [SPAWN ENTITY] LocalPlayerId: {GameManager.Instance.LocalPlayerId?.ToString() ?? "NULL"}");
            Debug.Log($"🔍 [SPAWN ENTITY] OwnerPlayerId: {entityData.OwnerPlayerId}");

            // PlayerController gibi script'leri bu bilgiyle başlat
            var playerController = newEntityObject.GetComponent<PlayerController>();
            if (playerController != null) 
            {
                Debug.Log($"🔍 [SPAWN ENTITY] PlayerController bulundu, initialize ediliyor...");
                playerController.Initialize(isLocal, entityData.EntityId);
                Debug.Log($"✅ [SPAWN ENTITY] PlayerController initialize edildi: EntityId={entityData.EntityId}");
            }
            else
            {
                Debug.LogWarning($"❌ [SPAWN ENTITY] PlayerController component bulunamadı: {newEntityObject.name}");
            }

            // Oluşturulan nesneyi takip listemize ekle
            _spawnedEntities.Add(entityData.EntityId, newEntityObject);
            Debug.Log($"✅ [SPAWN ENTITY] Entity spawn işlemi tamamlandı: {newEntityObject.name}");
        }

        /// <summary>
        ///     Verilen Entity ID'sine sahip, sahnede oluşturulmuş olan GameObject'i bulur ve döndürür.
        ///     PlayerController'ın menzil kontrolü gibi işlemler için kullanılır.
        /// </summary>
        /// <param name="entityId">Aranan varlığın ağ kimliği.</param>
        /// <returns>Sahnede bulunan GameObject veya bulunamazsa null.</returns>
        public GameObject GetEntityById(string entityId)
        {
            if (string.IsNullOrEmpty(entityId)) return null;

            _spawnedEntities.TryGetValue(entityId, out var entityObject);
            return entityObject;
        }

        #endregion
    }
}