using System;
using BarbarosKs.Combat;
using Project.Scripts.Network;
using UnityEngine;

namespace BarbarosKs.Core
{
    /// <summary>
    /// Oyun durumlarını merkezi olarak yöneten sistem
    /// </summary>
    public class GameStateManager : MonoBehaviour
    {
        public static GameStateManager Instance { get; private set; }

        [Header("Current State")]
        [SerializeField] private GameState currentState = GameState.Bootstrap;
        [SerializeField] private GameState previousState = GameState.Bootstrap;

        [Header("Debug")]
        [SerializeField] private bool verboseLogging = true;

        // Events
        public static event Action<GameState, GameState> OnGameStateChanged;
        public static event Action<GameState> OnGameStateEntered;
        public static event Action<GameState> OnGameStateExited;

        // Properties
        public GameState CurrentState => currentState;
        public GameState PreviousState => previousState;
        public bool IsInGame => currentState == GameState.InGame;
        public bool IsInMenu => currentState == GameState.Login || currentState == GameState.Register || currentState == GameState.ShipSelection;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                DebugLog("✅ GameStateManager initialized");
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        private void Start()
        {
            // SceneController event'lerini dinle (varsa)
            if (SceneController.Instance != null)
            {
                SceneController.OnSceneChangeCompleted += OnSceneChanged;
            }
            
            // İlk state'i ayarla
            SetGameState(GameState.Bootstrap);
        }

        private void OnDestroy()
        {
            if (SceneController.Instance != null)
            {
                SceneController.OnSceneChangeCompleted -= OnSceneChanged;
            }
        }

        #region State Management

        /// <summary>
        /// Oyun durumunu değiştirir
        /// </summary>
        public void SetGameState(GameState newState)
        {
            if (currentState == newState)
            {
                DebugLog($"⚠️ Aynı state'e geçiş: {newState}");
                return;
            }

            var oldState = currentState;
            previousState = currentState;
            currentState = newState;

            DebugLog($"🔄 State değişimi: {oldState} → {newState}");

            // Events
            OnGameStateExited?.Invoke(oldState);
            OnGameStateChanged?.Invoke(oldState, newState);
            OnGameStateEntered?.Invoke(newState);

            // State-specific actions
            HandleStateEntered(newState);
            HandleStateExited(oldState);
        }

        /// <summary>
        /// Önceki state'e geri döner
        /// </summary>
        public void RevertToPreviousState()
        {
            DebugLog($"🔙 Önceki state'e dönülüyor: {previousState}");
            SetGameState(previousState);
        }

        #endregion

        #region State Handlers

        private void HandleStateEntered(GameState state)
        {
            switch (state)
            {
                case GameState.Bootstrap:
                    HandleBootstrapEntered();
                    break;
                case GameState.Login:
                    HandleLoginEntered();
                    break;
                case GameState.Register:
                    HandleRegisterEntered();
                    break;
                case GameState.Loading:
                    HandleLoadingEntered();
                    break;
                case GameState.ShipSelection:
                    HandleShipSelectionEntered();
                    break;
                case GameState.CreateShip:
                    HandleCreateShipEntered();
                    break;
                case GameState.InGame:
                    HandleInGameEntered();
                    break;
                case GameState.Paused:
                    HandlePausedEntered();
                    break;
                case GameState.GameOver:
                    HandleGameOverEntered();
                    break;
            }
        }

        private void HandleStateExited(GameState state)
        {
            switch (state)
            {
                case GameState.InGame:
                    HandleInGameExited();
                    break;
                case GameState.Paused:
                    HandlePausedExited();
                    break;
            }
        }

        // Specific state handlers
        private void HandleBootstrapEntered()
        {
            DebugLog("🚀 Bootstrap state entered - Sistem başlatılıyor");
            
            // Core sistemlerin başlatılmasını bekle
            var systemCoordinator = FindObjectOfType<SystemCoordinator>();
            if (systemCoordinator != null)
            {
                DebugLog("✅ SystemCoordinator bulundu");
            }
        }

        private void HandleLoginEntered()
        {
            DebugLog("🔐 Login state entered");
            
            // Login UI'ı aktif et
            Time.timeScale = 1f;
        }

        private void HandleRegisterEntered()
        {
            DebugLog("📝 Register state entered");
        }

        private void HandleLoadingEntered()
        {
            DebugLog("⏳ Loading state entered");
        }

        private void HandleShipSelectionEntered()
        {
            DebugLog("🚢 Ship Selection state entered");
            
            // Gemi verilerinin yüklü olduğunu kontrol et
            if (PlayerManager.Instance?.OwnedShips == null)
            {
                Debug.LogWarning("⚠️ Gemi verileri yüklü değil!");
            }
        }

        private void HandleCreateShipEntered()
        {
            DebugLog("🔨 Create Ship state entered");
        }

        private void HandleInGameEntered()
        {
            DebugLog("🎮 In Game state entered");
            
            // Oyun sistemlerini aktif et
            Time.timeScale = 1f;
            
            // Player kontrollerini aktif et
            EnableGameplaySystemsForInGame();
        }

        private void HandleInGameExited()
        {
            DebugLog("🎮 In Game state exited");
            
            // Oyun sistemlerini pasif et
            DisableGameplaySystemsForInGame();
        }

        private void HandlePausedEntered()
        {
            DebugLog("⏸️ Paused state entered");
            
            // Oyunu duraklat
            Time.timeScale = 0f;
        }

        private void HandlePausedExited()
        {
            DebugLog("▶️ Paused state exited");
            
            // Oyunu devam ettir
            Time.timeScale = 1f;
        }

        private void HandleGameOverEntered()
        {
            DebugLog("💀 Game Over state entered");
        }

        #endregion

        #region Scene Integration

        private void OnSceneChanged(string sceneName)
        {
            // Sahne adına göre otomatik state değişimi
            var targetState = GetStateForScene(sceneName);
            if (targetState != currentState)
            {
                SetGameState(targetState);
            }
        }

        private GameState GetStateForScene(string sceneName)
        {
            return sceneName switch
            {
                "Bootstrap" => GameState.Bootstrap,
                "Login" => GameState.Login,
                "Register" => GameState.Register,
                "Loading" => GameState.Loading,
                "SelectShipScene" => GameState.ShipSelection,
                "CreateShip" => GameState.CreateShip,
                "FisherSea" => GameState.InGame,
                _ => currentState // Unknown scene, keep current state
            };
        }

        #endregion

        #region Gameplay Systems Management

        private void EnableGameplaySystemsForInGame()
        {
            // Combat sistemlerini aktif et
            if (ProjectileManager.Instance != null)
            {
                ProjectileManager.Instance.gameObject.SetActive(true);
            }

            // Player kontrollerini aktif et (varsa)
            var playerControllers = FindObjectsOfType<Player.PlayerController>();
            foreach (var controller in playerControllers)
            {
                controller.enabled = true;
            }

            // Combat Manager aktif et
            if (CombatManager.Instance != null)
            {
                CombatManager.Instance.gameObject.SetActive(true);
            }

            // Network sistemlerini aktif et (varsa)
            var networkManager = FindObjectOfType<NetworkManager>();
            if (networkManager != null)
            {
                DebugLog("🌐 NetworkManager found and active");
            }
        }

        private void DisableGameplaySystemsForInGame()
        {
            // Gerektiğinde oyun sistemlerini pasif et
            // (Şu an için boş - gerekirse implement edilir)
            DebugLog("🔇 Gameplay systems disabled");
        }

        #endregion

        #region Public Utility Methods

        /// <summary>
        /// Oyunu duraklatır/devam ettirir
        /// </summary>
        public void TogglePause()
        {
            if (currentState == GameState.InGame)
            {
                SetGameState(GameState.Paused);
            }
            else if (currentState == GameState.Paused)
            {
                SetGameState(GameState.InGame);
            }
        }

        /// <summary>
        /// Belirli bir state'te olup olmadığını kontrol eder
        /// </summary>
        public bool IsInState(GameState state) => currentState == state;

        /// <summary>
        /// Belirli state'lerden birinde olup olmadığını kontrol eder
        /// </summary>
        public bool IsInAnyState(params GameState[] states)
        {
            foreach (var state in states)
            {
                if (currentState == state) return true;
            }
            return false;
        }

        #endregion

        #region Debug Methods

        private void DebugLog(string message)
        {
            if (verboseLogging)
            {
                Debug.Log($"[GameStateManager] {message}");
            }
        }

        [ContextMenu("Debug: Current State Info")]
        private void DebugCurrentStateInfo()
        {
            Debug.Log($"=== GAME STATE INFO ===");
            Debug.Log($"Current State: {currentState}");
            Debug.Log($"Previous State: {previousState}");
            Debug.Log($"Is In Game: {IsInGame}");
            Debug.Log($"Is In Menu: {IsInMenu}");
            Debug.Log($"Time Scale: {Time.timeScale}");
        }

        [ContextMenu("Debug: Toggle Pause")]
        private void DebugTogglePause()
        {
            TogglePause();
        }

        [ContextMenu("Debug: Force In Game State")]
        private void DebugForceInGameState()
        {
            SetGameState(GameState.InGame);
        }

        [ContextMenu("Debug: Force Login State")]
        private void DebugForceLoginState()
        {
            SetGameState(GameState.Login);
        }

        #endregion
    }

    /// <summary>
    /// Oyun durumları enum'u
    /// </summary>
    public enum GameState
    {
        Bootstrap,      // Sistem başlatma
        Login,          // Giriş ekranı
        Register,       // Kayıt ekranı
        Loading,        // Yükleme ekranı
        ShipSelection,  // Gemi seçimi
        CreateShip,     // Gemi oluşturma
        InGame,         // Oyun içinde
        Paused,         // Oyun duraklatıldı
        GameOver        // Oyun bitti
    }
} 