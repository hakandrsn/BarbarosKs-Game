using System.Collections.Generic;
using BarbarosKs.core.DTOs;
using BarbarosKs.Player;
using UnityEngine;

namespace Project.Scripts.Network
{
    /// <summary>
    /// NetworkManager'dan gelen olayları dinleyerek sahnedeki ağ nesnelerini (diğer oyuncuları)
    /// oluşturan, güncelleyen ve yok eden merkezi sınıf.
    /// </summary>
    public class NetworkObjectSpawner : MonoBehaviour
    {
        [System.Serializable]
        public class NetworkPrefabMapping
        {
            public string prefabType;
            public GameObject prefab;
        }

        [Header("Prefab Ayarları")]
        [SerializeField] private GameObject defaultPlayerPrefab;
        [SerializeField] private NetworkPrefabMapping[] prefabMappings;

        [Header("Spawn Konteynerleri")]
        [SerializeField] private Transform playersContainer;

        // Ağ nesneleri sözlüğü (ID'ye göre GameObject tutar)
        private readonly Dictionary<string, GameObject> _spawnedPlayerObjects = new Dictionary<string, GameObject>();
        private readonly Dictionary<string, NetworkPrefabMapping> _prefabLookup = new Dictionary<string, NetworkPrefabMapping>();
        
        // Bu script artık kendi başına bir Singleton veya statik olabilir,
        // böylece diğer scriptler kolayca erişebilir.
        public static NetworkObjectSpawner Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            // Prefab eşlemelerini daha hızlı erişim için bir sözlüğe dönüştür
            foreach (var mapping in prefabMappings)
            {
                if (!string.IsNullOrEmpty(mapping.prefabType) && mapping.prefab != null)
                {
                    _prefabLookup[mapping.prefabType] = mapping;
                }
            }

            // Oyuncular için bir konteyner yoksa, sahnede oluştur
            if (playersContainer == null)
            {
                playersContainer = new GameObject("NetworkPlayers").transform;
            }
        }

        private void Start()
        {
            // NetworkManager'ın olaylarına abone oluyoruz.
            // Bu script artık ağdan doğrudan mesaj dinlemiyor, sadece NetworkManager'dan komut alıyor.
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.OnWorldStateReceived += HandleWorldState;
                NetworkManager.Instance.OnPlayerJoined += HandlePlayerJoined;
                NetworkManager.Instance.OnPlayerMoved += HandlePlayerMoved;
                // NetworkManager'a OnPlayerLeft olayı eklendikten sonra bu satırı aktif edeceğiz:
                // NetworkManager.Instance.OnPlayerLeft += HandlePlayerLeft;
            }
            else
            {
                Debug.LogError("NetworkManager sahnede bulunamadı! NetworkObjectSpawner çalışamaz.");
            }
        }

        private void OnDestroy()
        {
            // Bellek sızıntılarını önlemek için olay aboneliklerini iptal et
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.OnWorldStateReceived -= HandleWorldState;
                NetworkManager.Instance.OnPlayerJoined -= HandlePlayerJoined;
                NetworkManager.Instance.OnPlayerMoved -= HandlePlayerMoved;
                // NetworkManager.Instance.OnPlayerLeft -= HandlePlayerLeft;
            }
        }
        
        #region Network Olay İşleyicileri

        /// <summary>
        /// Oyuna ilk girildiğinde sunucudan gelen tüm oyuncu listesini işler.
        /// </summary>
        private void HandleWorldState(List<Player> allPlayers)
        {
            Debug.Log($"Dünya durumu alındı. {allPlayers.Count} oyuncu mevcut.");
            foreach (var player in allPlayers)
            {
                // Bu metot hem oyuncu oluşturur hem de varsa günceller
                SpawnOrUpdatePlayer(player);
            }
        }

        /// <summary>
        /// Oyuna yeni bir oyuncu katıldığında çalışır.
        /// </summary>
        private void HandlePlayerJoined(Player newPlayer)
        {
            Debug.Log($"{newPlayer.Name} oyuna katıldı.");
            SpawnOrUpdatePlayer(newPlayer);
        }
        
        /// <summary>
        /// Bir oyuncu hareket ettiğinde çalışır ve o oyuncunun sahnedeki temsilcisini günceller.
        /// </summary>
        private void HandlePlayerMoved(string playerId, Vector3 newPosition)
        {
            if (_spawnedPlayerObjects.TryGetValue(playerId, out GameObject playerObject))
            {
                // Burada pozisyonu direkt atamak yerine daha yumuşak bir geçiş (interpolation)
                // sağlayan bir component (ileride yazacağımız NetworkTransformSync gibi) kullanılabilir.
                // Şimdilik direkt atama yapıyoruz.
                playerObject.transform.position = newPosition;
            }
        }

        /// <summary>
        /// Bir oyuncu oyundan ayrıldığında çalışır.
        /// </summary>
        private void HandlePlayerLeft(string playerId)
        {
            Debug.Log($"{playerId} ID'li oyuncu oyundan ayrıldı.");
            if (_spawnedPlayerObjects.TryGetValue(playerId, out GameObject playerObject))
            {
                Destroy(playerObject);
                _spawnedPlayerObjects.Remove(playerId);
            }
        }

        #endregion

        #region Yardımcı Metotlar

        /// <summary>
        /// Gelen oyuncu verisine göre sahnede bir karakter oluşturur veya mevcut olanı günceller.
        /// </summary>
        // NetworkObjectSpawner.cs içinde
        private void SpawnOrUpdatePlayer(Player playerData)
        {
            // Bu metot artık çok daha temiz ve güvenilir.
            // Gelen oyuncu verisinin ID'si, NetworkManager'da sakladığımız yerel ID ile aynı mı?
            bool isLocal = (playerData.Id == NetworkManager.Instance.LocalPlayerId);

            // Eğer bu ID'ye sahip bir oyuncu zaten sahnede varsa, onu güncelleyip çıkıyoruz.
            if (_spawnedPlayerObjects.TryGetValue(playerData.Id, out GameObject existingPlayerObject))
            {
                // Uzak oyuncuların pozisyonu burada güncellenecek.
                if (!isLocal)
                {
                    // TODO: Buraya yumuşak geçiş (interpolation) eklenecek.
                    existingPlayerObject.transform.position = playerData.Position.ToUnity();
                }
                return;
            }

            // --- YENİ OYUNCU OLUŞTURMA ---
    
            // TODO: Yerel ve uzak oyuncu için farklı prefab'lar kullanabilirsiniz.
            // GameObject prefabToSpawn = isLocal ? localPlayerPrefab : remotePlayerPrefab;
            GameObject prefabToSpawn = defaultPlayerPrefab; 

            GameObject newPlayerObject = Instantiate(prefabToSpawn, playerData.Position.ToUnity(), Quaternion.identity, playersContainer);
            newPlayerObject.name = $"Player_{playerData.Name} ({(isLocal ? "Yerel" : "Uzak")})";

            // Controller'a yerel mi uzak mı olduğunu ve network kimliğini bildir.
            var playerController = newPlayerObject.GetComponent<PlayerController>();
            if(playerController != null)
            {
                playerController.Initialize(isLocal, playerData.Id);
            }

            var playerHealth = newPlayerObject.GetComponent<PlayerHealth>();
            if(playerHealth != null)
            {
                // playerHealth.Initialize(isLocal); // Gerekirse PlayerHealth'e de bir Initialize metodu ekleyebilirsiniz.
            }

            // Oluşturulan nesneyi takip listemize ekle.
            _spawnedPlayerObjects.Add(playerData.Id, newPlayerObject);
            Debug.Log(newPlayerObject.name + " başarıyla oluşturuldu ve listeye eklendi.");
        }
        public GameObject GetPlayerObjectById(string playerId)
        {
            _spawnedPlayerObjects.TryGetValue(playerId, out GameObject playerObject);
            return playerObject;
        }

        #endregion
    }
}