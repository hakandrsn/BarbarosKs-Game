using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using BarbarosKs.Shared.DTOs;
using BarbarosKs.UI;

namespace BarbarosKs.Core
{
    /// <summary>
    /// Tüm sahne geçişlerini ve yükleme işlemlerini merkezi olarak yöneten sistem
    /// </summary>
    public class SceneController : MonoBehaviour
    {
        public static SceneController Instance { get; private set; }

        [Header("Scene Names")]
        [SerializeField] private string bootstrapScene = "Bootstrap";
        [SerializeField] private string loginScene = "Login";
        [SerializeField] private string registerScene = "Register";
        [SerializeField] private string loadingScene = "Loading";
        [SerializeField] private string shipSelectionScene = "SelectShipScene";
        [SerializeField] private string createShipScene = "CreateShip";
        [SerializeField] private string gameScene = "FisherSea";

        [Header("Loading Configuration")]
        [SerializeField] private float minimumLoadingTime = 1f;
        [SerializeField] private bool useLoadingScreen = false;

        // Events
        public static event Action<string> OnSceneChangeStarted;
        public static event Action<string> OnSceneChangeCompleted;
        public static event Action<float> OnLoadingProgress;

        // State
        private bool isLoading = false;
        private string currentTargetScene;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                Debug.Log("✅ [SCENE CONTROLLER] Initialized");
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        private void Start()
        {
            // Scene loading event'lerini dinle
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        #region Public Scene Transition Methods

        /// <summary>
        /// Login sonrası karakter verisi ile uygun sahneye yönlendirir
        /// </summary>
        public void HandleLoginSuccess(CharacterSelectionDto characterData)
        {
            Debug.Log($"🎯 [SCENE CONTROLLER] Login başarılı: {characterData.PlayerProfile.Username}");
            
            // PlayerManager'a veri yükle (PlayerDataManager yerine)
            if (PlayerManager.Instance != null)
            {
                PlayerManager.Instance.LoadPlayerData(characterData);
            }
            else
            {
                Debug.LogWarning("⚠️ [SCENE CONTROLLER] PlayerManager bulunamadı! Veri yüklenemedi.");
            }

            // Gemi durumuna göre yönlendir
            if (characterData.Ships == null || characterData.Ships.Count == 0)
            {
                LoadScene(createShipScene, "Gemi oluşturma");
            }
            else
            {
                LoadScene(shipSelectionScene, "Gemi seçimi");
            }
        }

        /// <summary>
        /// Gemi seçimi sonrası oyuna giriş
        /// </summary>
        public void HandleShipSelected(ShipSummaryDto selectedShip)
        {
            Debug.Log($"🚢 [SCENE CONTROLLER] Gemi seçildi: {selectedShip.Name}");
            
            // PlayerManager'a aktif gemi ayarla (PlayerDataManager yerine)
            if (PlayerManager.Instance != null)
            {
                PlayerManager.Instance.SetActiveShip(selectedShip);
            }
            else
            {
                Debug.LogWarning("⚠️ [SCENE CONTROLLER] PlayerManager bulunamadı! Aktif gemi ayarlanamadı.");
            }

            LoadScene(gameScene, "Oyun dünyası");
        }

        /// <summary>
        /// Genel sahne yükleme metodu
        /// </summary>
        public void LoadScene(string sceneName, string description = "")
        {
            if (isLoading)
            {
                Debug.LogWarning($"⚠️ [SCENE CONTROLLER] Zaten sahne yükleniyor: {currentTargetScene}");
                return;
            }

            currentTargetScene = sceneName;
            Debug.Log($"🎯 [SCENE CONTROLLER] Sahne yükleniyor: {sceneName} ({description})");

            // Loading screen geçici olarak kapatıldı - scene eksik olduğu için
            if (useLoadingScreen && sceneName != loadingScene && !string.IsNullOrEmpty(loadingScene))
            {
                StartCoroutine(LoadSceneWithLoadingScreen(sceneName, description));
            }
            else
            {
                // Direct loading - loading screen bypass
                Debug.LogWarning("⚠️ [SCENE CONTROLLER] Loading screen bypass - direct loading");
                StartCoroutine(LoadSceneDirectly(sceneName, description));
            }
        }

        // Specific scene loaders
        public void LoadBootstrap() => LoadScene(bootstrapScene, "Sistem başlatma");
        public void LoadLogin() => LoadScene(loginScene, "Giriş ekranı");
        public void LoadRegister() => LoadScene(registerScene, "Kayıt ekranı");
        public void LoadShipSelection() => LoadScene(shipSelectionScene, "Gemi seçimi");
        public void LoadCreateShip() => LoadScene(createShipScene, "Gemi oluşturma");
        public void LoadGame() => LoadScene(gameScene, "Oyun dünyası");

        #endregion

        #region Private Loading Implementation

        private IEnumerator LoadSceneWithLoadingScreen(string targetScene, string description)
        {
            isLoading = true;
            OnSceneChangeStarted?.Invoke(targetScene);

            // 1. Loading ekranını yükle
            yield return StartCoroutine(LoadSceneDirectly(loadingScene, "Loading ekranı"));

            // 2. Loading screen hazır olana kadar bekle
            yield return new WaitForSeconds(0.5f);

            // 3. Loading screen'e hedef sahneyi bildir (LoadingScreen sistemi varsa)
            var loadingScreen = FindObjectOfType<LoadingScreen>();
            // LoadingScreen metodları varsa kullan
            Debug.Log(loadingScreen
                ? $"📱 [SCENE CONTROLLER] Loading screen başlatılıyor: {targetScene}"
                : $"⚠️ [SCENE CONTROLLER] LoadingScreen bulunamadı, direct loading");

            // 4. Hedef sahneyi arka planda yükle
            var loadOperation = SceneManager.LoadSceneAsync(targetScene);
            if (loadOperation != null)
            {
                loadOperation.allowSceneActivation = false;

                var startTime = Time.time;
                var progress = 0f;

                // 5. Yükleme progress'ini takip et
                while (!loadOperation.isDone)
                {
                    // Gerçek progress
                    progress = Mathf.Clamp01(loadOperation.progress / 0.9f);

                    // Minimum süre kontrolü
                    var elapsedTime = Time.time - startTime;
                    var minTimeReached = elapsedTime >= minimumLoadingTime;

                    OnLoadingProgress?.Invoke(progress);

                    // Loading screen'e progress bildir (varsa)
                    if (loadingScreen)
                    {
                        Debug.Log($"📊 Loading progress: {progress:F2} - Min time: {minTimeReached}");
                    }

                    // Yükleme tamamlandı ve minimum süre geçti
                    if (loadOperation.progress >= 0.9f && minTimeReached)
                    {
                        loadOperation.allowSceneActivation = true;
                    }

                    yield return null;
                }
            }

            isLoading = false;
            OnSceneChangeCompleted?.Invoke(targetScene);
        }

        private IEnumerator LoadSceneDirectly(string sceneName, string description)
        {
            isLoading = true;
            OnSceneChangeStarted?.Invoke(sceneName);

            Debug.Log($"🔄 [SCENE CONTROLLER] Direct loading: {sceneName}");

            // Scene var mı kontrol et
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogError($"❌ [SCENE CONTROLLER] Scene name null veya boş!");
                isLoading = false;
                yield break;
            }

            AsyncOperation loadOperation;
            try 
            {
                loadOperation = SceneManager.LoadSceneAsync(sceneName);
                
                if (loadOperation == null)
                {
                    Debug.LogError($"❌ [SCENE CONTROLLER] Scene yüklenemedi: {sceneName}");
                    isLoading = false;
                    yield break;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"❌ [SCENE CONTROLLER] Scene loading exception: {ex.Message}");
                isLoading = false;
                yield break;
            }
            
            while (!loadOperation.isDone)
            {
                OnLoadingProgress?.Invoke(loadOperation.progress);
                yield return null;
            }

            isLoading = false;
            OnSceneChangeCompleted?.Invoke(sceneName);
        }

        #endregion

        #region Scene Events

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log($"✅ [SCENE CONTROLLER] Sahne yüklendi: {scene.name}");

            // Sahne-specific initialization
            switch (scene.name)
            {
                case "Bootstrap":
                    HandleBootstrapLoaded();
                    break;
                case "Login":
                    HandleLoginSceneLoaded();
                    break;
                case "FisherSea":
                    HandleGameSceneLoaded();
                    break;
            }
        }

        private void HandleBootstrapLoaded()
        {
            Debug.Log("🚀 [SCENE CONTROLLER] Bootstrap scene loaded - Sistem başlatılıyor");
            
            // Sistem başlatma sonrası login'e git
            StartCoroutine(BootstrapSequence());
        }

        private IEnumerator BootstrapSequence()
        {
            // Sistemlerin başlatılmasını bekle
            yield return new WaitForSeconds(1f);
            
            // Login'e geç
            LoadLogin();
        }

        private void HandleLoginSceneLoaded()
        {
            Debug.Log("🔐 [SCENE CONTROLLER] Login scene loaded");
        }

        private void HandleGameSceneLoaded()
        {
            Debug.Log("🎮 [SCENE CONTROLLER] Game scene loaded - Oyun başlatılıyor");
            
            // Game state'i aktif yap (GameStateManager sistemi varsa)
            // Not: GameStateManager henüz oluşturulmadı, bu yüzden check yapıyoruz
            var gameStateManager = FindObjectOfType<GameStateManager>();
            if (gameStateManager != null)
            {
                Debug.Log("🎯 [SCENE CONTROLLER] GameStateManager found - setting InGame state");
                // gameStateManager.SetGameState(GameState.InGame); // Bu method henüz yok
            }
            else
            {
                Debug.Log("⚠️ [SCENE CONTROLLER] GameStateManager bulunamadı");
            }

            // PlayerManager'ı game mode'a geçir
            if (PlayerManager.Instance != null)
            {
                PlayerManager.Instance.EnterGame();
                Debug.Log("🎮 [SCENE CONTROLLER] PlayerManager game mode aktif");
            }
        }

        #endregion

        #region Debug Methods

        [ContextMenu("Debug: Current Scene Info")]
        private void DebugCurrentSceneInfo()
        {
            var currentScene = SceneManager.GetActiveScene();
            Debug.Log($"=== SCENE INFO ===");
            Debug.Log($"Current Scene: {currentScene.name}");
            Debug.Log($"Is Loading: {isLoading}");
            Debug.Log($"Target Scene: {currentTargetScene ?? "None"}");
        }

        [ContextMenu("Debug: Reload Current Scene")]
        private void DebugReloadCurrentScene()
        {
            var currentScene = SceneManager.GetActiveScene();
            LoadScene(currentScene.name, "Debug reload");
        }

        [ContextMenu("Debug: Test Scene Transitions")]
        private void DebugTestSceneTransitions()
        {
            Debug.Log("🧪 Testing scene transitions...");
            Debug.Log($"Current Scene: {SceneManager.GetActiveScene().name}");
            
            // Available scenes listesi
            Debug.Log("Available scene transitions:");
            Debug.Log($"- LoadBootstrap() → {bootstrapScene}");
            Debug.Log($"- LoadLogin() → {loginScene}");
            Debug.Log($"- LoadRegister() → {registerScene}");
            Debug.Log($"- LoadShipSelection() → {shipSelectionScene}");
            Debug.Log($"- LoadCreateShip() → {createShipScene}");
            Debug.Log($"- LoadGame() → {gameScene}");
        }

        #endregion
    }
} 