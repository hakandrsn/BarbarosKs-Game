using System;
using System.Collections.Generic;
using UnityEngine;
using BarbarosKs.Shared.DTOs;

namespace BarbarosKs.Core
{
    /// <summary>
    /// Player ve Ship verilerini merkezi olarak yöneten sistem
    /// PlayerDataManager'ın iyileştirilmiş versiyonu
    /// </summary>
    public class PlayerManager : MonoBehaviour
    {
        public static PlayerManager Instance { get; private set; }

        [Header("Player Data")]
        [SerializeField] private PlayerProfileDto playerProfile;
        [SerializeField] private List<ShipSummaryDto> ownedShips = new List<ShipSummaryDto>();
        [SerializeField] private ShipSummaryDto activeShip;

        [Header("Ship State")]
        [SerializeField] private Vector3 lastKnownPosition;
        [SerializeField] private float lastKnownHealth;
        [SerializeField] private bool isInGame = false;

        [Header("Debug")]
        [SerializeField] private bool verboseLogging = true;

        // Events
        public static event Action<PlayerProfileDto> OnPlayerDataLoaded;
        public static event Action<List<ShipSummaryDto>> OnShipsLoaded;
        public static event Action<ShipSummaryDto> OnActiveShipChanged;
        public static event Action<Vector3> OnPlayerPositionUpdated;
        public static event Action<float> OnPlayerHealthUpdated;

        // Properties
        public PlayerProfileDto PlayerProfile => playerProfile;
        public List<ShipSummaryDto> OwnedShips => ownedShips;
        public ShipSummaryDto ActiveShip => activeShip;
        public Vector3 LastKnownPosition => lastKnownPosition;
        public float LastKnownHealth => lastKnownHealth;
        public bool IsInGame => isInGame;
        public bool HasPlayerData => playerProfile != null;
        public bool HasActiveShip => activeShip != null;
        public int ShipCount => ownedShips?.Count ?? 0;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                DebugLog("✅ PlayerManager initialized");
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        #region Data Loading

        /// <summary>
        /// Login/Register sonrası karakterr verilerini yükler
        /// </summary>
        public void LoadPlayerData(CharacterSelectionDto characterData)
        {
            if (characterData == null)
            {
                Debug.LogError("❌ [PLAYER MANAGER] CharacterSelectionDto null!");
                return;
            }

            if (characterData.PlayerProfile == null)
            {
                Debug.LogError("❌ [PLAYER MANAGER] PlayerProfile null!");
                return;
            }

            // Player profile'ı kaydet
            playerProfile = characterData.PlayerProfile;
            DebugLog($"✅ Player profile yüklendi: {playerProfile.Username}");

            // Ships listesini kaydet
            ownedShips.Clear();
            if (characterData.Ships != null)
            {
                ownedShips.AddRange(characterData.Ships);
                DebugLog($"✅ {ownedShips.Count} gemi yüklendi");
            }

            // Events
            OnPlayerDataLoaded?.Invoke(playerProfile);
            OnShipsLoaded?.Invoke(ownedShips);

            DebugLog($"🎯 Player data loading complete - Username: {playerProfile.Username}, Ships: {ownedShips.Count}");
        }

        /// <summary>
        /// Aktif gemi'yi ayarlar
        /// </summary>
        public void SetActiveShip(ShipSummaryDto ship)
        {
            if (ship == null)
            {
                Debug.LogError("❌ [PLAYER MANAGER] Ship null!");
                return;
            }

            // Geminin sahip olunan gemiler arasında olup olmadığını kontrol et
            if (!ownedShips.Exists(s => s.Id == ship.Id))
            {
                Debug.LogError($"❌ [PLAYER MANAGER] Gemi sahip olunan gemiler arasında değil: {ship.Name}");
                return;
            }

            activeShip = ship;
            DebugLog($"✅ Aktif gemi ayarlandı: {ship.Name}");

            // Event
            OnActiveShipChanged?.Invoke(activeShip);
        }

        /// <summary>
        /// Player profile'ı günceller
        /// </summary>
        public void UpdatePlayerProfile(PlayerProfileDto updatedProfile)
        {
            if (updatedProfile == null)
            {
                Debug.LogError("❌ [PLAYER MANAGER] Updated profile null!");
                return;
            }

            playerProfile = updatedProfile;
            DebugLog($"🔄 Player profile güncellendi: {playerProfile.Username}");

            OnPlayerDataLoaded?.Invoke(playerProfile);
        }

        /// <summary>
        /// Gemi listesini günceller
        /// </summary>
        public void UpdateOwnedShips(List<ShipSummaryDto> ships)
        {
            if (ships == null)
            {
                Debug.LogError("❌ [PLAYER MANAGER] Ships list null!");
                return;
            }

            ownedShips.Clear();
            ownedShips.AddRange(ships);
            DebugLog($"🔄 Gemi listesi güncellendi: {ownedShips.Count} gemi");

            // Aktif gemi hala listede var mı kontrol et
            if (activeShip != null && !ownedShips.Exists(s => s.Id == activeShip.Id))
            {
                Debug.LogWarning($"⚠️ Aktif gemi artık sahip olunan gemiler arasında değil: {activeShip.Name}");
                activeShip = null;
                OnActiveShipChanged?.Invoke(null);
            }

            OnShipsLoaded?.Invoke(ownedShips);
        }

        #endregion

        #region Game State Management

        /// <summary>
        /// Oyuna giriş yapar
        /// </summary>
        public void EnterGame()
        {
            if (activeShip == null)
            {
                Debug.LogError("❌ [PLAYER MANAGER] Aktif gemi olmadan oyuna giriş yapılamaz!");
                return;
            }

            isInGame = true;
            lastKnownHealth = activeShip.CurrentHull;
            DebugLog($"🎮 Oyuna giriş yapıldı - Gemi: {activeShip.Name}");
        }

        /// <summary>
        /// Oyundan çıkış yapar
        /// </summary>
        public void ExitGame()
        {
            isInGame = false;
            DebugLog("🚪 Oyundan çıkış yapıldı");
        }

        /// <summary>
        /// Player pozisyonunu günceller
        /// </summary>
        public void UpdatePosition(Vector3 position)
        {
            lastKnownPosition = position;
            OnPlayerPositionUpdated?.Invoke(position);
        }

        /// <summary>
        /// Player sağlığını günceller
        /// </summary>
        public void UpdateHealth(float health)
        {
            lastKnownHealth = health;
            OnPlayerHealthUpdated?.Invoke(health);
            
            // Aktif gemi'nin sağlığını da güncelle
            if (activeShip != null)
            {
                activeShip.CurrentHull = (int)health;
            }
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// ID'ye göre gemi bulur
        /// </summary>
        public ShipSummaryDto GetShipById(Guid shipId)
        {
            return ownedShips.Find(s => s.Id == shipId);
        }

        /// <summary>
        /// Player'ın belirli bir gemi'ye sahip olup olmadığını kontrol eder
        /// </summary>
        public bool OwnsShip(Guid shipId)
        {
            return ownedShips.Exists(s => s.Id == shipId);
        }

        /// <summary>
        /// Player'ın player ID'sini döndürür
        /// </summary>
        public Guid? GetPlayerId()
        {
            return playerProfile?.Id;
        }

        /// <summary>
        /// Tüm player verilerini temizler
        /// </summary>
        public void ClearAllData()
        {
            playerProfile = null;
            ownedShips.Clear();
            activeShip = null;
            lastKnownPosition = Vector3.zero;
            lastKnownHealth = 0f;
            isInGame = false;

            DebugLog("🧹 Tüm player verileri temizlendi");
        }

        #endregion

        #region SceneController Integration

        /// <summary>
        /// Login başarılı olduğunda çağrılır
        /// </summary>
        public void HandleLoginSuccess(CharacterSelectionDto characterData)
        {
            LoadPlayerData(characterData);
            
            // SceneController'a sahne yönlendirmesi için bilgi ver
            if (SceneController.Instance != null)
            {
                SceneController.Instance.HandleLoginSuccess(characterData);
            }
        }

        /// <summary>
        /// Gemi seçimi yapıldığında çağrılır
        /// </summary>
        public void HandleShipSelection(ShipSummaryDto selectedShip)
        {
            SetActiveShip(selectedShip);
            
            // SceneController'a sahne yönlendirmesi için bilgi ver
            if (SceneController.Instance != null)
            {
                SceneController.Instance.HandleShipSelected(selectedShip);
            }
        }

        #endregion

        #region Debug Methods

        private void DebugLog(string message)
        {
            if (verboseLogging)
            {
                Debug.Log($"[PlayerManager] {message}");
            }
        }

        [ContextMenu("Debug: Show Player Info")]
        private void DebugShowPlayerInfo()
        {
            Debug.Log("=== PLAYER INFO ===");
            Debug.Log($"Player Profile: {(playerProfile != null ? playerProfile.Username : "NULL")}");
            Debug.Log($"Player ID: {GetPlayerId()?.ToString() ?? "NULL"}");
            Debug.Log($"Owned Ships: {ShipCount}");
            Debug.Log($"Active Ship: {(activeShip != null ? activeShip.Name : "NULL")}");
            Debug.Log($"Is In Game: {isInGame}");
            Debug.Log($"Last Position: {lastKnownPosition}");
            Debug.Log($"Last Health: {lastKnownHealth}");
        }

        [ContextMenu("Debug: List All Ships")]
        private void DebugListAllShips()
        {
            Debug.Log("=== OWNED SHIPS ===");
            for (int i = 0; i < ownedShips.Count; i++)
            {
                var ship = ownedShips[i];
                string activeMarker = ship.Id == activeShip?.Id ? " [ACTIVE]" : "";
                Debug.Log($"{i + 1}. {ship.Name} (ID: {ship.Id}){activeMarker}");
            }
        }

        [ContextMenu("Debug: Clear All Data")]
        private void DebugClearAllData()
        {
            ClearAllData();
        }

        [ContextMenu("Debug: Test Login Success")]
        private void DebugTestLoginSuccess()
        {
            // Test data oluştur
            var testProfile = new PlayerProfileDto
            {
                Id = Guid.NewGuid(),
                Username = "TestPlayer",
                AvatarUrl = "test@test.com"
            };

            var testShip = new ShipSummaryDto
            {
                Id = Guid.NewGuid(),
                Name = "Test Ship",
                CurrentHull = 100,
                MaxHull = 100
            };

            var testCharacterData = new CharacterSelectionDto
            {
                PlayerProfile = testProfile,
                Ships = new List<ShipSummaryDto> { testShip }
            };

            HandleLoginSuccess(testCharacterData);
        }

        #endregion
    }
} 