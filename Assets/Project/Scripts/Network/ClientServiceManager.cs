using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using BarbarosKs.Shared.DTOs;
using Project.Scripts.Network;
using Newtonsoft.Json;

namespace BarbarosKs.Client.Services
{
    /// <summary>
    /// Unity Client-Side Modern MMO Service Manager
    /// GameServer'daki MMO architecture ile tam entegrasyon
    /// Real-time stats, ammo, combat systems yönetir
    /// </summary>
    public class ClientServiceManager : MonoBehaviour
    {
        public static ClientServiceManager Instance { get; private set; }

        [Header("Service Configuration")]
        [SerializeField] private bool enableRealTimeStats = true;
        [SerializeField] private bool enableAmmoTracking = false;
        [SerializeField] private bool enableCombatSystem = true;
        [SerializeField] private bool enablePerformanceMetrics = true;

        [Header("Update Intervals")]
        [SerializeField] private float statsUpdateInterval = 1f;
        [SerializeField] private float ammoUpdateInterval = 0.5f;
        [SerializeField] private float combatUpdateInterval = 0.1f;

        [Header("Debug")]
        [SerializeField] private bool verboseLogging = true;

        // Client-side services
        private ClientStatsService _statsService;
        private ClientAmmoService _ammoService;
        private ClientCombatService _combatService;
        private ClientCacheService _cacheService;

        // Client state management
        private readonly Dictionary<Guid, PlayerData> _players = new();

        // Performance tracking
        private float _lastStatsUpdate;
        private float _lastAmmoUpdate;
        private float _lastCombatUpdate;
        private int _messagesSentThisSecond;
        private float _lastMessageCountReset;

        #region Events

        /// <summary>Real-time ship stats güncellendi</summary>
        public event Action<ShipStatsDto> OnShipStatsUpdated;

        /// <summary>Ammo durumu güncellendi</summary>
        public event Action<AmmoStatus> OnAmmoStatusUpdated;

        /// <summary>Combat event gerçekleşti</summary>
        public event Action<CombatResult> OnCombatEventReceived;

        /// <summary>Player spawn/despawn events</summary>
        public event Action<Guid, PlayerData> OnPlayerJoined;
        public event Action<Guid> OnPlayerLeft;

        /// <summary>Service başlatıldı/durdu</summary>
        public event Action OnServiceStarted;
        public event Action OnServiceStopped;

        #endregion

        #region Properties

        private bool IsInitialized { get; set; } = false;

        public bool HasLocalPlayer => LocalPlayer != null;
        private PlayerData LocalPlayer { get; set; }

        public int ConnectedPlayersCount => _players.Count;
        public float MessagesPerSecond { get; private set; }

        #endregion

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeServices();
                DebugLog("✅ ClientServiceManager initialized");
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            // NetworkManager events'lerine subscribe ol
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.OnConnectedToServer += OnConnectedToServer;
                NetworkManager.Instance.OnDisconnectedFromServer += OnDisconnectedFromServer;
                SubscribeToNetworkEvents();
            }
        }

        private void Update()
        {
            if (!IsInitialized) return;

            // Performance tracking
            TrackPerformanceMetrics();

            // Real-time updates
            HandleRealTimeUpdates();
        }

        private void OnDestroy()
        {
            StopServices();
        }

        #region Service Management

        private void InitializeServices()
        {
            try
            {
                // Cache service - her zaman ilk olarak başlat
                _cacheService = gameObject.AddComponent<ClientCacheService>();

                // Stats service
                if (enableRealTimeStats)
                {
                    _statsService = gameObject.AddComponent<ClientStatsService>();
                    _statsService.Initialize(statsUpdateInterval, _cacheService);
                }

                // Ammo service
                if (enableAmmoTracking)
                {
                    _ammoService = gameObject.AddComponent<ClientAmmoService>();
                    _ammoService.Initialize(ammoUpdateInterval, _cacheService);
                }

                // Combat service
                if (enableCombatSystem)
                {
                    _combatService = gameObject.AddComponent<ClientCombatService>();
                    _combatService.Initialize(combatUpdateInterval, _cacheService);
                }

                DebugLog("✅ All client services initialized successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ ClientServiceManager initialization failed: {ex.Message}");
            }
        }

        private void SubscribeToNetworkEvents()
        {
            // NetworkManager'daki temel events'lere subscribe ol
            // Modern MMO message handling için
        }

        private void StartServices()
        {
            try
            {
                _cacheService?.StartService();
                _statsService?.StartService();
                _ammoService?.StartService();
                _combatService?.StartService();

                IsInitialized = true;
                DebugLog("🚀 All client services started");
                OnServiceStarted?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Service start failed: {ex.Message}");
            }
        }

        private void StopServices()
        {
            try
            {
                _combatService?.StopService();
                _ammoService?.StopService();
                _statsService?.StopService();
                _cacheService?.StopService();

                IsInitialized = false;
                DebugLog("🛑 All client services stopped");
                OnServiceStopped?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Service stop failed: {ex.Message}");
            }
        }

        #endregion

        #region Network Event Handlers

        private void OnConnectedToServer()
        {
            DebugLog("🔗 Connected to MMO server, starting client services...");
            StartServices();

            // Local player'ı initialize et
            InitializeLocalPlayer();
        }

        private void OnDisconnectedFromServer()
        {
            DebugLog("🔌 Disconnected from MMO server, stopping client services...");
            StopServices();
            ClearAllPlayers();
        }

        private void InitializeLocalPlayer()
        {
            if (!BarbarosKs.Core.PlayerManager.Instance ||
                !BarbarosKs.Core.PlayerManager.Instance.HasPlayerData) return;
            var playerProfile = BarbarosKs.Core.PlayerManager.Instance.PlayerProfile;
            var activeShip = BarbarosKs.Core.PlayerManager.Instance.ActiveShip;
            LocalPlayer = new PlayerData(playerProfile.Id, playerProfile.Username);
            DebugLog($"[ LOCAL PLAYER ] Username: {LocalPlayer.Username} - Ship: [ {activeShip?.Id} ]");
            if (activeShip != null)
            {
                LocalPlayer.ShipId = activeShip.Id;
            }

            DebugLog($"✅ Local player initialized: {LocalPlayer.Username} - Ship: {LocalPlayer.ShipId}");
        }

        #endregion

        #region Real-Time Updates

        private void HandleRealTimeUpdates()
        {
            var currentTime = Time.time;

            // Stats update
            if (enableRealTimeStats && currentTime - _lastStatsUpdate >= statsUpdateInterval)
            {
                RequestStatsUpdate();
                _lastStatsUpdate = currentTime;
            }

            // Ammo update
            if (enableAmmoTracking && currentTime - _lastAmmoUpdate >= ammoUpdateInterval)
            {
                RequestAmmoUpdate();
                _lastAmmoUpdate = currentTime;
            }

            // Combat update
            if (enableCombatSystem && currentTime - _lastCombatUpdate >= combatUpdateInterval)
            {
                ProcessCombatUpdates();
                _lastCombatUpdate = currentTime;
            }
        }

        public void RequestStatsUpdate()
        {
            if (LocalPlayer == null || NetworkManager.Instance?.IsConnected != true) return;
            var request = new
            {
                Type = "REQUEST_SHIP_STATS",
                ShipId = LocalPlayer.ShipId,
                Timestamp = DateTime.UtcNow
            };

            SendMessageToServer(request);
            DebugLog($"📊 Requested ship stats for {LocalPlayer.ShipId}");
        }

        public void RequestAmmoUpdate()
        {
            if (LocalPlayer == null || NetworkManager.Instance?.IsConnected != true) return;
            var request = new
            {
                Type = "REQUEST_AMMO_STATUS",
                ShipId = LocalPlayer.ShipId,
                Timestamp = DateTime.UtcNow
            };

            SendMessageToServer(request);
            DebugLog($"🎯 Requested ammo status for {LocalPlayer.ShipId}");
        }

        private void ProcessCombatUpdates()
        {
            // Combat system real-time processing
            _combatService?.ProcessCombatTick();
        }

        #endregion

        #region Message Handling

        public void HandleServerMessage(string messageType, string messageData)
        {
            try
            {
                switch (messageType)
                {
                    case "SHIP_STATS_UPDATE":
                        HandleShipStatsUpdate(messageData);
                        break;
                    case "AMMO_STATUS_UPDATE":
                        HandleAmmoStatusUpdate(messageData);
                        break;
                    case "COMBAT_EVENT":
                        HandleCombatEvent(messageData);
                        break;
                    case "PLAYER_JOINED":
                        HandlePlayerJoined(messageData);
                        break;
                    case "PLAYER_LEFT":
                        HandlePlayerLeft(messageData);
                        break;
                    default:
                        DebugLog($"⚠️ Unhandled message type: {messageType}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Message handling error ({messageType}): {ex.Message}");
            }
        }

        private void HandleShipStatsUpdate(string data)
        {
            var statsDto = JsonConvert.DeserializeObject<ShipStatsDto>(data);
            if (statsDto != null && LocalPlayer != null)
            {
                LocalPlayer.UpdateShipStats(statsDto);
                _statsService?.UpdateStats(statsDto);
                OnShipStatsUpdated?.Invoke(statsDto);
                DebugLog($"📊 Ship stats updated: AttackPower={statsDto.AttackPower:F1}");
            }
        }

        private void HandleAmmoStatusUpdate(string data)
        {
            var ammoStatus = JsonConvert.DeserializeObject<AmmoStatus>(data);
            if (ammoStatus != null && LocalPlayer != null)
            {
                LocalPlayer.UpdateAmmoStatus(ammoStatus);
                _ammoService?.UpdateAmmoStatus(ammoStatus);
                OnAmmoStatusUpdated?.Invoke(ammoStatus);
                DebugLog($"🎯 Ammo updated: {ammoStatus.CurrentAmmo}/{ammoStatus.TotalAmmo}");
            }
        }

        private void HandleCombatEvent(string data)
        {
            var combatResult = JsonConvert.DeserializeObject<CombatResult>(data);
            if (combatResult != null)
            {
                _combatService?.ProcessCombatEvent(combatResult);
                OnCombatEventReceived?.Invoke(combatResult);
                DebugLog($"⚔️ Combat event: {combatResult.IsSuccess}");
            }
        }

        private void HandlePlayerJoined(string data)
        {
            var playerData = JsonConvert.DeserializeObject<PlayerData>(data);
            if (playerData != null)
            {
                _players[playerData.PlayerId] = playerData;
                OnPlayerJoined?.Invoke(playerData.PlayerId, playerData);
                DebugLog($"👤 Player joined: {playerData.Username}");
            }
        }

        private void HandlePlayerLeft(string data)
        {
            var playerId = JsonConvert.DeserializeObject<Guid>(data);
            if (_players.ContainsKey(playerId))
            {
                _players.Remove(playerId);
                OnPlayerLeft?.Invoke(playerId);
                DebugLog($"👤 Player left: {playerId}");
            }
        }

        #endregion

        #region Utility Methods

        private void SendMessageToServer(object message)
        {
            try
            {
                string json = JsonConvert.SerializeObject(message);
                // NetworkManager.Instance.SendMessage(json); // Bu method'u NetworkManager'da implement etmek gerekecek
                _messagesSentThisSecond++;
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Send message error: {ex.Message}");
            }
        }

        private void TrackPerformanceMetrics()
        {
            if (Time.time - _lastMessageCountReset >= 1f)
            {
                MessagesPerSecond = _messagesSentThisSecond;
                _messagesSentThisSecond = 0;
                _lastMessageCountReset = Time.time;
            }
        }

        private void ClearAllPlayers()
        {
            _players.Clear();
            LocalPlayer = null;
        }

        private void DebugLog(string message)
        {
            if (verboseLogging)
            {
                Debug.Log($"[ClientServiceManager] {message}");
            }
        }

        #endregion

        #region Public API

        /// <summary>Player verilerini al</summary>
        public PlayerData GetPlayer(Guid playerId)
        {
            return _players.TryGetValue(playerId, out var player) ? player : null;
        }

        /// <summary>Tüm player'ları al</summary>
        public Dictionary<Guid, PlayerData> GetAllPlayers()
        {
            return new Dictionary<Guid, PlayerData>(_players);
        }

        /// <summary>Combat action gönder</summary>
        public void SendCombatAction(string actionType, Guid targetId, Vector3 position)
        {
            if (LocalPlayer == null) return;

            var action = new
            {
                Type = "COMBAT_ACTION",
                ActionType = actionType,
                AttackerId = LocalPlayer.PlayerId,
                TargetId = targetId,
                Position = position,
                Timestamp = DateTime.UtcNow
            };

            SendMessageToServer(action);
            DebugLog($"⚔️ Combat action sent: {actionType} -> {targetId}");
        }

        /// <summary>Manual stats refresh</summary>
        public void RefreshStats()
        {
            RequestStatsUpdate();
        }

        /// <summary>Manual ammo refresh</summary>
        public void RefreshAmmo()
        {
            RequestAmmoUpdate();
        }

        #endregion
    }

    /// <summary>
    /// Client stats service - real-time stats tracking
    /// </summary>
    public class ClientStatsService : MonoBehaviour
    {
        private float _updateInterval;
        private ClientCacheService _cacheService;
        private ShipStatsDto _currentStats;

        public void Initialize(float updateInterval, ClientCacheService cacheService)
        {
            _updateInterval = updateInterval;
            _cacheService = cacheService;
        }

        public void StartService()
        {
            Debug.Log("📊 ClientStatsService started");
        }

        public void StopService()
        {
            Debug.Log("📊 ClientStatsService stopped");
        }

        public void UpdateStats(ShipStatsDto stats)
        {
            _currentStats = stats;
            _cacheService?.CacheStats(stats);
        }

        public ShipStatsDto GetCurrentStats()
        {
            return _currentStats ?? _cacheService?.GetCachedStats();
        }
    }

    /// <summary>
    /// Client ammo service - real-time ammo tracking
    /// </summary>
    public class ClientAmmoService : MonoBehaviour
    {
        private float _updateInterval;
        private ClientCacheService _cacheService;
        private AmmoStatus _currentAmmo;

        public void Initialize(float updateInterval, ClientCacheService cacheService)
        {
            _updateInterval = updateInterval;
            _cacheService = cacheService;
        }

        public void StartService()
        {
            Debug.Log("🎯 ClientAmmoService started");
        }

        public void StopService()
        {
            Debug.Log("🎯 ClientAmmoService stopped");
        }

        public void UpdateAmmoStatus(AmmoStatus ammo)
        {
            _currentAmmo = ammo;
            _cacheService?.CacheAmmo(ammo);
        }

        public AmmoStatus GetCurrentAmmo()
        {
            return _currentAmmo ?? _cacheService?.GetCachedAmmo();
        }
    }

    /// <summary>
    /// Client combat service - combat events processing
    /// </summary>
    public class ClientCombatService : MonoBehaviour
    {
        private float _updateInterval;
        private ClientCacheService _cacheService;
        private readonly List<CombatResult> _recentCombatEvents = new();

        public void Initialize(float updateInterval, ClientCacheService cacheService)
        {
            _updateInterval = updateInterval;
            _cacheService = cacheService;
        }

        public void StartService()
        {
            Debug.Log("⚔️ ClientCombatService started");
        }

        public void StopService()
        {
            Debug.Log("⚔️ ClientCombatService stopped");
        }

        public void ProcessCombatEvent(CombatResult combatResult)
        {
            _recentCombatEvents.Add(combatResult);
            
            // Keep only recent events (last 10 seconds)
            var cutoffTime = DateTime.UtcNow.AddSeconds(-10);
            _recentCombatEvents.RemoveAll(e => e.Timestamp < cutoffTime);
        }

        public void ProcessCombatTick()
        {
            // Real-time combat processing
        }

        public List<CombatResult> GetRecentCombatEvents()
        {
            return new List<CombatResult>(_recentCombatEvents);
        }
    }

    /// <summary>
    /// Client cache service - local data caching
    /// </summary>
    public class ClientCacheService : MonoBehaviour
    {
        private ShipStatsDto _cachedStats;
        private AmmoStatus _cachedAmmo;
        private DateTime _statsLastCached;
        private DateTime _ammoLastCached;

        public void StartService()
        {
            Debug.Log("💾 ClientCacheService started");
        }

        public void StopService()
        {
            Debug.Log("💾 ClientCacheService stopped");
        }

        public void CacheStats(ShipStatsDto stats)
        {
            _cachedStats = stats;
            _statsLastCached = DateTime.UtcNow;
        }

        public void CacheAmmo(AmmoStatus ammo)
        {
            _cachedAmmo = ammo;
            _ammoLastCached = DateTime.UtcNow;
        }

        public ShipStatsDto GetCachedStats()
        {
            // Cache expiry check (30 seconds)
            if (_cachedStats != null && (DateTime.UtcNow - _statsLastCached).TotalSeconds < 30)
            {
                return _cachedStats;
            }
            return null;
        }

        public AmmoStatus GetCachedAmmo()
        {
            // Cache expiry check (5 seconds)
            if (_cachedAmmo != null && (DateTime.UtcNow - _ammoLastCached).TotalSeconds < 5)
            {
                return _cachedAmmo;
            }
            return null;
        }
    }
}

/// <summary>
/// Unity main thread dispatcher for network callbacks
/// </summary>
public class UnityMainThreadDispatcher : MonoBehaviour
{
    public static UnityMainThreadDispatcher Instance { get; private set; }
    private readonly Queue<Action> _actions = new();
    private readonly object _lock = new();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        lock (_lock)
        {
            while (_actions.Count > 0)
            {
                _actions.Dequeue()?.Invoke();
            }
        }
    }

    public void Enqueue(Action action)
    {
        lock (_lock)
        {
            _actions.Enqueue(action);
        }
    }
} 