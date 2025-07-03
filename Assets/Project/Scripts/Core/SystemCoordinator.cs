using System.Collections;
using BarbarosKs.Combat;
using BarbarosKs.Utils;
using Project.Scripts.Network;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BarbarosKs.Core
{
    /// <summary>
    /// Tüm sistemlerin koordinasyonunu ve başlatılmasını yöneten merkezi sistem
    /// GameSystemInitializer'ın iyileştirilmiş versiyonu
    /// </summary>
    public class SystemCoordinator : MonoBehaviour
    {
        public static SystemCoordinator Instance { get; private set; }

        [Header("Core Systems")]
        [SerializeField] private bool autoInitializeOnAwake = true;
        [SerializeField] private bool createMissingSystemsFromCode = true;

        [Header("Scene-Specific Systems")]
        [SerializeField] private bool enableBootstrapSystems = true;
        [SerializeField] private bool enableGameplaySystems = true;
        [SerializeField] private bool enableUISystems = true;

        [Header("Initialization Order")]
        [SerializeField] private float systemInitializationDelay = 0.1f;
        [SerializeField] private float sceneSystemsDelay = 0.5f;

        [Header("Debug")]
        [SerializeField] private bool verboseLogging = true;
        [SerializeField] private bool showInitializationProgress = true;

        // System status
        private bool isInitialized = false;
        private bool isInitializing = false;
        private string currentScene;

        // Properties
        public bool IsInitialized => isInitialized;
        public bool IsInitializing => isInitializing;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                DebugLog("✅ SystemCoordinator initialized");

                if (autoInitializeOnAwake)
                {
                    StartCoroutine(InitializeAllSystems());
                }
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        private void Start()
        {
            // Scene değişim event'lerini dinle
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
        }

        #region System Initialization

        /// <summary>
        /// Tüm sistemleri başlatır
        /// </summary>
        public IEnumerator InitializeAllSystems()
        {
            if (isInitializing)
            {
                DebugLog("⚠️ Sistem başlatma zaten devam ediyor");
                yield break;
            }

            isInitializing = true;
            DebugLog("🚀 Sistem başlatma işlemi başlıyor...");

            // 1. Core sistemleri başlat
            yield return StartCoroutine(InitializeCoreSystem());
            yield return new WaitForSeconds(systemInitializationDelay);

            // 2. Data sistemlerini başlat
            yield return StartCoroutine(InitializeDataSystems());
            yield return new WaitForSeconds(systemInitializationDelay);

            // 3. Network sistemlerini başlat
            yield return StartCoroutine(InitializeNetworkSystems());
            yield return new WaitForSeconds(systemInitializationDelay);

            // 4. Gameplay sistemlerini başlat
            if (enableGameplaySystems)
            {
                yield return StartCoroutine(InitializeGameplaySystems());
                yield return new WaitForSeconds(systemInitializationDelay);
            }

            // 5. UI sistemlerini başlat
            if (enableUISystems)
            {
                yield return StartCoroutine(InitializeUISystems());
                yield return new WaitForSeconds(systemInitializationDelay);
            }

            // 6. Scene-specific sistemleri başlat
            yield return StartCoroutine(InitializeSceneSpecificSystems());

            isInitializing = false;
            isInitialized = true;
            DebugLog("✅ Tüm sistemler başarıyla başlatıldı!");
        }

        private IEnumerator InitializeCoreSystem()
        {
            DebugLog("🔧 Core sistemler başlatılıyor...");

            // GameSettings
            if (!ValidateSystem("GameSettings", () => GameSettings.Instance != null))
            {
                DebugLog("⚠️ GameSettings asset eksik - Resources/GameSettings.asset oluşturun");
            }

            // PrefabManager
            if (!ValidateSystem("PrefabManager", () => PrefabManager.Instance != null))
            {
                DebugLog("⚠️ PrefabManager asset eksik - Resources/PrefabManager.asset oluşturun");
            }

            // SceneController
            InitializeOrCreateSystem<SceneController>("SceneController");

            // GameStateManager
            InitializeOrCreateSystem<GameStateManager>("GameStateManager");

            yield return null;
        }

        private IEnumerator InitializeDataSystems()
        {
            DebugLog("📊 Data sistemleri başlatılıyor...");

            // PlayerManager (PlayerDataManager'ın yerine)
            InitializeOrCreateSystem<PlayerManager>("PlayerManager");

            // GameDataManager
            InitializeOrCreateSystem<GameDataManager>("GameDataManager");

            // DataInitializer
            InitializeOrCreateSystem<DataInitializer>("DataInitializer");

            yield return null;
        }

        private IEnumerator InitializeNetworkSystems()
        {
            DebugLog("🌐 Network sistemleri başlatılıyor...");

            // ApiManager
            InitializeOrCreateSystem<ApiManager>("ApiManager");

            // CannonballService
            InitializeOrCreateSystem<CannonballService>("CannonballService");

            // NetworkManager (zaten sahne-specific olabilir)
            if (FindObjectOfType<NetworkManager>() is null && createMissingSystemsFromCode)
            {
                DebugLog("⚠️ NetworkManager sahne-specific, manuel olarak eklenmeli");
            }

            yield return null;
        }

        private IEnumerator InitializeGameplaySystems()
        {
            DebugLog("🎮 Gameplay sistemleri başlatılıyor...");

            // ProjectileManager
            InitializeOrCreateSystem<ProjectileManager>("ProjectileManager");

            // CombatManager
            InitializeOrCreateSystem<CombatManager>("CombatManager");

            // AudioManager
            InitializeOrCreateSystem<AudioManager>("AudioManager");

            yield return null;
        }

        private IEnumerator InitializeUISystems()
        {
            DebugLog("🖼️ UI sistemleri başlatılıyor...");

            // MarketManager
            InitializeOrCreateSystem<MarketManager>("MarketManager");

            // LoadingManager
            InitializeOrCreateSystem<LoadingManager>("LoadingManager");

            yield return null;
        }

        private IEnumerator InitializeSceneSpecificSystems()
        {
            DebugLog("🎬 Scene-specific sistemler başlatılıyor...");

            string sceneName = SceneManager.GetActiveScene().name;
            currentScene = sceneName;

            switch (sceneName)
            {
                case "Bootstrap":
                    yield return StartCoroutine(InitializeBootstrapSystems());
                    break;
                case "Login":
                case "Register":
                    yield return StartCoroutine(InitializeAuthSystems());
                    break;
                case "SelectShipScene":
                case "CreateShip":
                    yield return StartCoroutine(InitializeShipSystems());
                    break;
                case "FisherSea":
                    yield return StartCoroutine(InitializeGameSystems());
                    break;
                case "Loading":
                    yield return StartCoroutine(InitializeLoadingSystems());
                    break;
            }

            yield return null;
        }

        #endregion

        #region Scene-Specific Initialization

        private IEnumerator InitializeBootstrapSystems()
        {
            DebugLog("🚀 Bootstrap sistemleri başlatılıyor...");

            // Sadece temel sistemler, UI minimum
            // Otomatik login'e geçiş
            if (SceneController.Instance != null)
            {
                yield return new WaitForSeconds(1f);
                SceneController.Instance.LoadLogin();
            }

            yield return null;
        }

        private IEnumerator InitializeAuthSystems()
        {
            DebugLog("🔐 Auth sistemleri başlatılıyor...");

            // Login/Register için UI sistemleri
            // Network sistemleri aktif olmalı

            yield return null;
        }

        private IEnumerator InitializeShipSystems()
        {
            DebugLog("🚢 Ship sistemleri başlatılıyor...");

            // Gemi seçimi için sistemler
            // PlayerManager aktif olmalı
            // GameDataManager'da ship verileri

            yield return null;
        }

        private IEnumerator InitializeGameSystems()
        {
            DebugLog("🎮 Game sistemleri başlatılıyor...");

            // Tüm gameplay sistemleri aktif
            // Combat, Network, UI sistemleri

            // PlayerManager'ı game mode'a geçir
            if (PlayerManager.Instance != null)
            {
                PlayerManager.Instance.EnterGame();
            }

            yield return null;
        }

        private IEnumerator InitializeLoadingSystems()
        {
            DebugLog("⏳ Loading sistemleri başlatılıyor...");

            // Loading screen sistemleri
            // Minimal resource kullanımı

            yield return null;
        }

        #endregion

        #region System Management

        /// <summary>
        /// Sistem'i kontrol eder, yoksa oluşturur
        /// </summary>
        private void InitializeOrCreateSystem<T>(string systemName) where T : MonoBehaviour
        {
            if (FindObjectOfType<T>() == null)
            {
                if (createMissingSystemsFromCode)
                {
                    var systemObj = new GameObject(systemName);
                    systemObj.AddComponent<T>();
                    DebugLog($"🏗️ {systemName} koddan oluşturuldu");
                }
                else
                {
                    DebugLog($"⚠️ {systemName} bulunamadı ve koddan oluşturma kapalı");
                }
            }
            else
            {
                DebugLog($"✅ {systemName} zaten mevcut");
            }
        }

        /// <summary>
        /// Sistem'in varlığını kontrol eder
        /// </summary>
        private bool ValidateSystem(string systemName, System.Func<bool> validationFunc)
        {
            bool isValid = validationFunc();
            if (isValid)
            {
                DebugLog($"✅ {systemName} geçerli");
            }
            else
            {
                DebugLog($"❌ {systemName} geçersiz");
            }
            return isValid;
        }

        #endregion

        #region Scene Events

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            DebugLog($"🎬 Sahne yüklendi: {scene.name}");
            
            if (isInitialized && scene.name != currentScene)
            {
                // Sahne değişti, scene-specific sistemleri yeniden başlat
                StartCoroutine(InitializeSceneSpecificSystems());
            }
        }

        private void OnSceneUnloaded(Scene scene)
        {
            DebugLog($"🚪 Sahne kaldırıldı: {scene.name}");
        }

        #endregion

        #region Public Interface

        /// <summary>
        /// Manuel sistem başlatma
        /// </summary>
        public void ManualInitializeAllSystems()
        {
            if (!isInitializing)
            {
                StartCoroutine(InitializeAllSystems());
            }
        }

        /// <summary>
        /// Belirli sistem'i yeniden başlatır
        /// </summary>
        public void ReinitializeSystem<T>(string systemName) where T : MonoBehaviour
        {
            var existingSystem = FindObjectOfType<T>();
            if (existingSystem != null)
            {
                Destroy(existingSystem.gameObject);
            }
            
            InitializeOrCreateSystem<T>(systemName);
            DebugLog($"🔄 {systemName} yeniden başlatıldı");
        }

        /// <summary>
        /// Sistem durumunu kontrol eder
        /// </summary>
        public void ValidateAllSystems()
        {
            DebugLog("=== SYSTEM VALIDATION ===");

            // Core systems
            ValidateSystem("SceneController", () => SceneController.Instance != null);
            ValidateSystem("GameStateManager", () => GameStateManager.Instance != null);
            ValidateSystem("GameSettings", () => GameSettings.Instance != null);
            ValidateSystem("PrefabManager", () => PrefabManager.Instance != null);

            // Data systems
            ValidateSystem("PlayerManager", () => PlayerManager.Instance != null);
            ValidateSystem("GameDataManager", () => GameDataManager.Instance != null);
            ValidateSystem("DataInitializer", () => DataInitializer.Instance != null);

            // Network systems
            ValidateSystem("ApiManager", () => ApiManager.Instance != null);
            ValidateSystem("CannonballService", () => CannonballService.Instance != null);

            // Gameplay systems
            ValidateSystem("ProjectileManager", () => ProjectileManager.Instance != null);
            ValidateSystem("CombatManager", () => CombatManager.Instance != null);
            ValidateSystem("AudioManager", () => AudioManager.Instance != null);

            // UI systems
            ValidateSystem("MarketManager", () => MarketManager.Instance != null);
            ValidateSystem("LoadingManager", () => LoadingManager.Instance != null);

            DebugLog("=== VALIDATION COMPLETE ===");
        }

        #endregion

        #region Debug Methods

        private void DebugLog(string message)
        {
            if (verboseLogging)
            {
                Debug.Log($"[SystemCoordinator] {message}");
            }
        }

        [ContextMenu("Debug: Initialize All Systems")]
        private void DebugInitializeAllSystems()
        {
            ManualInitializeAllSystems();
        }

        [ContextMenu("Debug: Validate All Systems")]
        private void DebugValidateAllSystems()
        {
            ValidateAllSystems();
        }

        [ContextMenu("Debug: Show System Status")]
        private void DebugShowSystemStatus()
        {
            Debug.Log("=== SYSTEM STATUS ===");
            Debug.Log($"Is Initialized: {isInitialized}");
            Debug.Log($"Is Initializing: {isInitializing}");
            Debug.Log($"Current Scene: {currentScene}");
            Debug.Log($"Auto Initialize: {autoInitializeOnAwake}");
            Debug.Log($"Create Missing Systems: {createMissingSystemsFromCode}");
        }

        #endregion
    }
} 