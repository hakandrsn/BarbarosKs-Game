using System.Collections.Generic;
using BarbarosKs.Player;
using BarbarosKs.Shared.DTOs.Game; // Yeni ve doğru DTO namespace'i
using UnityEngine;

namespace Project.Scripts.Network
{
    /// <summary>
    /// NetworkManager'dan gelen olayları dinleyerek sahnedeki ağ nesnelerini (diğer oyuncular, NPC'ler vb.)
    /// oluşturan, güncelleyen ve yok eden merkezi sınıf.
    /// </summary>
    public class NetworkObjectSpawner : MonoBehaviour
    {
        [System.Serializable]
        public class NetworkPrefabMapping
        {
            public string prefabType; // DTO'dan gelen "PlayerShip_Sloop" gibi bir anahtar
            public GameObject prefab;
        }

        [Header("Prefab Ayarları")] [Tooltip("Eşleşme bulunamazsa kullanılacak varsayılan prefab.")] [SerializeField]
        private GameObject defaultPlayerPrefab;

        [SerializeField] private List<NetworkPrefabMapping> prefabMappings = new();

        [Header("Spawn Konteynerleri")] [SerializeField]
        private Transform playersContainer;

        [SerializeField] private Transform npcsContainer; // Gelecekteki NPC'ler için

        // Ağ nesneleri sözlüğü (EntityId'ye göre GameObject tutar)
        private readonly Dictionary<string, GameObject> _spawnedEntities = new();
        private readonly Dictionary<string, GameObject> _prefabLookup = new();

        public static NetworkObjectSpawner Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            // Prefab eşlemelerini daha hızlı erişim için bir sözlüğe dönüştür
            foreach (var mapping in prefabMappings)
            {
                if (!string.IsNullOrEmpty(mapping.prefabType) && mapping.prefab != null)
                {
                    _prefabLookup[mapping.prefabType] = mapping.prefab;
                }
            }

            if (playersContainer == null) playersContainer = new GameObject("NetworkPlayers").transform;
        }

        private void Start()
        {
            if (NetworkManager.Instance == null)
            {
                Debug.LogError("NetworkManager sahnede bulunamadı! Spawner çalışamaz.");
                enabled = false;
                return;
            }

            // NetworkManager'dan gelen yeni, temiz ve DTO-odaklı olayları dinliyoruz.
            NetworkManager.Instance.OnWorldStateReceived += HandleWorldState;
            NetworkManager.Instance.OnEntitySpawned += HandleEntitySpawned;
            NetworkManager.Instance.OnEntityDespawned += HandleEntityDespawned;
            NetworkManager.Instance.OnTransformUpdate += HandleTransformUpdate;
        }

        private void OnDestroy()
        {
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.OnWorldStateReceived -= HandleWorldState;
                NetworkManager.Instance.OnEntitySpawned -= HandleEntitySpawned;
                NetworkManager.Instance.OnEntityDespawned -= HandleEntityDespawned;
                NetworkManager.Instance.OnTransformUpdate -= HandleTransformUpdate;
            }
        }

        #region Network Olay İşleyicileri

        /// <summary>
        /// Oyuna ilk girildiğinde, dünyadaki tüm varlıkları oluşturur.
        /// </summary>
        private void HandleWorldState(S2C_WorldStateData data)
        {
            Debug.Log($"Dünya durumu alınıyor. Sahnede oluşturulacak varlık sayısı: {data.Entities.Count}");

            // Önce mevcut tüm nesneleri temizle (sahne yeniden yüklendiğinde vb. durumlar için)
            foreach (var spawnedObject in _spawnedEntities.Values)
            {
                Destroy(spawnedObject);
            }

            _spawnedEntities.Clear();

            // Sunucudan gelen listedeki her varlığı oluştur.
            foreach (var entityData in data.Entities)
            {
                SpawnEntity(entityData);
            }
        }

        /// <summary>
        /// Oyun sırasında dünyaya yeni bir varlık girdiğinde onu oluşturur.
        /// </summary>
        private void HandleEntitySpawned(S2C_EntitySpawnData data)
        {
            Debug.Log($"Yeni varlık dünyaya giriyor: ID={data.Entity.EntityId}, Tip={data.Entity.PrefabType}");
            SpawnEntity(data.Entity);
        }

        /// <summary>
        /// Bir varlık dünyadan ayrıldığında onu yok eder.
        /// </summary>
        private void HandleEntityDespawned(S2C_EntityDespawnData data)
        {
            Debug.Log($"{data.EntityId} ID'li varlık dünyadan ayrılıyor. Sebep: {data.Reason}");
            if (_spawnedEntities.TryGetValue(data.EntityId, out GameObject entityObject))
            {
                Destroy(entityObject);
                _spawnedEntities.Remove(data.EntityId);
            }
        }

        /// <summary>
        /// Sunucudan gelen toplu pozisyon güncellemelerini işler.
        /// </summary>
        private void HandleTransformUpdate(S2C_TransformUpdateData data)
        {
            foreach (var transformUpdate in data.Transforms)
            {
                var entityId = transformUpdate.Key;
                var transformData = transformUpdate.Value;

                if (!_spawnedEntities.TryGetValue(entityId, out var entityObject)) continue;
                // TODO: Buraya yumuşak geçiş (interpolation) yapan bir NetworkTransformSync bileşeni eklenecek.
                // Şimdilik pozisyonu doğrudan atıyoruz.
                entityObject.transform.position = transformData.Position.ToUnity();
                entityObject.transform.rotation = transformData.Rotation.ToUnity();
            }
        }

        #endregion

        #region Yardımcı Metotlar

        /// <summary>
        /// Gelen varlık verisine göre sahnede bir GameObject oluşturur.
        /// </summary>
        private void SpawnEntity(WorldEntityData entityData)
        {
            if (entityData == null || string.IsNullOrEmpty(entityData.EntityId)) return;

            // Bu varlık zaten sahnede varsa tekrar oluşturma.
            if (_spawnedEntities.ContainsKey(entityData.EntityId)) return;

            // Prefab'ı bul
            if (!_prefabLookup.TryGetValue(entityData.PrefabType, out GameObject prefabToSpawn))
            {
                // Eşleşme bulunamazsa varsayılanı kullan
                prefabToSpawn = defaultPlayerPrefab;
                Debug.LogWarning($"'{entityData.PrefabType}' için prefab bulunamadı. Varsayılan kullanılıyor.");
            }

            if (prefabToSpawn == null)
            {
                Debug.LogError("Oluşturulacak prefab bulunamadı!");
                return;
            }

            // Nesneyi oluştur
            var newEntityObject = Instantiate(
                prefabToSpawn,
                entityData.Position.ToUnity(),
                entityData.Rotation.ToUnity(),
                playersContainer); // TODO: Gelen tipe göre doğru konteyneri seç

            newEntityObject.name = $"{entityData.PrefabType}_{entityData.EntityId[..8]}";

            // Varlığın yerel oyuncuya ait olup olmadığını kontrol et
            var isLocal = (GameManager.Instance.LocalPlayerId.HasValue &&
                           entityData.OwnerPlayerId == GameManager.Instance.LocalPlayerId.Value.ToString());

            // PlayerController gibi script'leri bu bilgiyle başlat
            var playerController = newEntityObject.GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerController.Initialize(isLocal, entityData.EntityId);
            }

            // Oluşturulan nesneyi takip listemize ekle
            _spawnedEntities.Add(entityData.EntityId, newEntityObject);
        }
        
        /// <summary>
        /// Verilen Entity ID'sine sahip, sahnede oluşturulmuş olan GameObject'i bulur ve döndürür.
        /// PlayerController'ın menzil kontrolü gibi işlemler için kullanılır.
        /// </summary>
        /// <param name="entityId">Aranan varlığın ağ kimliği.</param>
        /// <returns>Sahnede bulunan GameObject veya bulunamazsa null.</returns>
        public GameObject GetEntityById(string entityId)
        {
            if (string.IsNullOrEmpty(entityId)) return null;

            _spawnedEntities.TryGetValue(entityId, out GameObject entityObject);
            return entityObject;
        }

        #endregion
    }
}