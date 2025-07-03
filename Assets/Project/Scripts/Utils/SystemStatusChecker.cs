using UnityEngine;
using BarbarosKs.Core;
using BarbarosKs.Combat;
using BarbarosKs.Testing;
using Project.Scripts.Network;

namespace BarbarosKs.Utils
{
    /// <summary>
    /// Yeni architecture sistemlerinin durumunu kontrol eden yardımcı script
    /// </summary>
    public class SystemStatusChecker : MonoBehaviour
    {
        [Header("Kontrol Ayarları")]
        [SerializeField] private bool autoCheckOnStart = true;
        [SerializeField] private float checkInterval = 5f; // 5 saniyede bir kontrol et
        [SerializeField] private bool showOnScreenStatus = true;

        [Header("Status Display")]
        [SerializeField] private bool showCoreSystemsOnly = false;
        [SerializeField] private int statusDisplayX = 10;
        [SerializeField] private int statusDisplayY = 10;
        [SerializeField] private int statusDisplayWidth = 300;
        [SerializeField] private int statusDisplayHeight = 400;

        private void Start()
        {
            if (autoCheckOnStart)
            {
                CheckAllSystems();
                
                // Periyodik kontrol
                InvokeRepeating(nameof(CheckAllSystems), checkInterval, checkInterval);
            }
        }

        [ContextMenu("Check All Systems")]
        public void CheckAllSystems()
        {
            Debug.Log("=== 🔍 YENİ ARCHITECTURE SİSTEM DURUMU ===");
            
            // Core Systems (Yeni Architecture)
            Debug.Log("--- CORE SYSTEMS ---");
            CheckSystem("SystemCoordinator", SystemCoordinator.Instance != null);
            CheckSystem("SceneController", SceneController.Instance != null);
            CheckSystem("GameStateManager", GameStateManager.Instance != null);
            CheckSystem("GameSettings", GameSettings.Instance != null);
            CheckSystem("PrefabManager", PrefabManager.Instance != null);
            
            // Data Management Systems
            Debug.Log("--- DATA SYSTEMS ---");
            CheckSystem("PlayerManager", PlayerManager.Instance != null);
            CheckSystem("GameDataManager", GameDataManager.Instance != null);
            CheckSystem("DataInitializer", DataInitializer.Instance != null);

            // Network & API Systems
            Debug.Log("--- NETWORK SYSTEMS ---");
            CheckSystem("ApiManager", ApiManager.Instance != null);
            CheckSystem("CannonballService", CannonballService.Instance != null);
            CheckSystem("NetworkManager", NetworkManager.Instance != null); // Sahne-specific

            // Gameplay Systems
            Debug.Log("--- GAMEPLAY SYSTEMS ---");
            CheckSystem("ProjectileManager", ProjectileManager.Instance != null);
            CheckSystem("CombatManager", CombatManager.Instance != null);
            CheckSystem("AudioManager", AudioManager.Instance != null);

            // UI Systems
            Debug.Log("--- UI SYSTEMS ---");
            CheckSystem("MarketManager", MarketManager.Instance != null);
            CheckSystem("LoadingManager", LoadingManager.Instance != null);

            // Data Loading Status
            CheckDataLoadingStatus();

            // Game State Status
            CheckGameStateStatus();

            Debug.Log("=== 🔍 KONTROL TAMAMLANDI ===");
        }

        private void CheckSystem(string systemName, bool isActive)
        {
            string status = isActive ? "✅ AKTIF" : "❌ EKSİK";
            Debug.Log($"{status} {systemName}");
            
            if (!isActive)
            {
                Debug.LogWarning($"⚠️ {systemName} sistemi bulunamadı! SystemCoordinator çalıştığından emin olun.");
            }
        }

        private void CheckDataLoadingStatus()
        {
            Debug.Log("--- DATA STATUS ---");
            
            // PlayerManager data status
            if (PlayerManager.Instance != null)
            {
                Debug.Log($"📊 PlayerManager Status:");
                Debug.Log($"   - Has Player Data: {PlayerManager.Instance.HasPlayerData}");
                Debug.Log($"   - Player Name: {PlayerManager.Instance.PlayerProfile?.Username ?? "NULL"}");
                Debug.Log($"   - Ship Count: {PlayerManager.Instance.ShipCount}");
                Debug.Log($"   - Has Active Ship: {PlayerManager.Instance.HasActiveShip}");
                Debug.Log($"   - Active Ship: {PlayerManager.Instance.ActiveShip?.Name ?? "NULL"}");
                Debug.Log($"   - Is In Game: {PlayerManager.Instance.IsInGame}");
            }

            // GameDataManager status
            if (GameDataManager.Instance != null)
            {
                Debug.Log($"📊 GameDataManager Status:");
                Debug.Log($"   - IsInitialized: {GameDataManager.Instance.IsInitialized}");
                Debug.Log($"   - Cannonballs Count: {GameDataManager.Instance.Cannonballs?.Count ?? 0}");
                Debug.Log($"   - Items Count: {GameDataManager.Instance.Items?.Count ?? 0}");
                Debug.Log($"   - Active Cannonballs: {GameDataManager.Instance.ActiveCannonballs?.Count ?? 0}");
                Debug.Log($"   - Market Cannonballs: {GameDataManager.Instance.MarketCannonballs?.Count ?? 0}");
            }
        }

        private void CheckGameStateStatus()
        {
            Debug.Log("--- GAME STATE ---");
            
            if (GameStateManager.Instance != null)
            {
                Debug.Log($"🎮 GameState Status:");
                Debug.Log($"   - Current State: {GameStateManager.Instance.CurrentState}");
                Debug.Log($"   - Previous State: {GameStateManager.Instance.PreviousState}");
                Debug.Log($"   - Is In Game: {GameStateManager.Instance.IsInGame}");
                Debug.Log($"   - Is In Menu: {GameStateManager.Instance.IsInMenu}");
            }

            // Scene info
            var currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            Debug.Log($"🎬 Scene Status:");
            Debug.Log($"   - Current Scene: {currentScene.name}");
            Debug.Log($"   - Build Index: {currentScene.buildIndex}");
        }

        [ContextMenu("Force Initialize All Systems")]
        public void ForceInitializeAllSystems()
        {
            Debug.Log("🔄 Tüm sistemler yeniden başlatılıyor...");
            
            // SystemCoordinator'ı bul ve yeniden başlat
            if (SystemCoordinator.Instance != null)
            {
                SystemCoordinator.Instance.ManualInitializeAllSystems();
            }
            else
            {
                Debug.LogError("❌ SystemCoordinator bulunamadı!");
                
                // Fallback - GameObject oluştur
                var coordinatorGO = new GameObject("SystemCoordinator");
                coordinatorGO.AddComponent<SystemCoordinator>();
            }
        }

        [ContextMenu("Start Data Loading")]
        public void StartDataLoading()
        {
            if (DataInitializer.Instance != null)
            {
                Debug.Log("🔄 Data loading manuel başlatılıyor...");
                _ = DataInitializer.Instance.StartDataInitializationAsync();
            }
            else
            {
                Debug.LogError("❌ DataInitializer bulunamadı!");
            }
        }

        [ContextMenu("Test Combat System")]
        public void TestCombatSystem()
        {
            if (CombatManager.Instance != null)
            {
                Debug.Log("⚔️ Combat sistem testi başlatılıyor...");
                
                // Test target oluştur
                var testTarget = GameObject.CreatePrimitive(PrimitiveType.Cube);
                testTarget.name = "TestCombatTarget";
                testTarget.transform.position = Vector3.forward * 8f;
                testTarget.AddComponent<TestEnemy>();

                // Target seç ve test et
                CombatManager.Instance.SetTarget(testTarget.transform);
                bool success = CombatManager.Instance.FireProjectile("CB1", 15);
                
                Debug.Log($"Combat test result: {(success ? "✅ Success" : "❌ Failed")}");
            }
            else
            {
                Debug.LogError("❌ CombatManager bulunamadı!");
            }
        }

        [ContextMenu("Test Scene Transition")]
        public void TestSceneTransition()
        {
            if (SceneController.Instance != null)
            {
                Debug.Log("🎬 Scene transition testi...");
                
                var currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                Debug.Log($"Current scene: {currentScene}");
                
                // Test olarak aynı sahneyi reload et
                SceneController.Instance.LoadScene(currentScene, "Test reload");
            }
            else
            {
                Debug.LogError("❌ SceneController bulunamadı!");
            }
        }

        [ContextMenu("Validate PrefabManager")]
        public void ValidatePrefabManager()
        {
            if (PrefabManager.Instance != null)
            {
                Debug.Log("🎯 PrefabManager validation...");
                PrefabManager.Instance.ValidateAllPrefabs();
                PrefabManager.Instance.RefreshCache();
            }
            else
            {
                Debug.LogError("❌ PrefabManager asset bulunamadı! Resources/PrefabManager.asset oluşturun.");
            }
        }

        private void Update()
        {
            // F1 tuşu ile hızlı sistem kontrolü
            if (Input.GetKeyDown(KeyCode.F1))
            {
                CheckAllSystems();
            }

            // F2 tuşu ile data loading
            if (Input.GetKeyDown(KeyCode.F2))
            {
                StartDataLoading();
            }

            // F3 tuşu ile combat test
            if (Input.GetKeyDown(KeyCode.F3))
            {
                TestCombatSystem();
            }

            // F4 tuşu ile prefab validation
            if (Input.GetKeyDown(KeyCode.F4))
            {
                ValidatePrefabManager();
            }
        }

        private void OnGUI()
        {
            if (!showOnScreenStatus) return;

            // Sol üst köşede sistem durumunu göster
            GUI.color = Color.white;
            GUILayout.BeginArea(new Rect(statusDisplayX, statusDisplayY, statusDisplayWidth, statusDisplayHeight));
            
            GUILayout.Label("🎮 NEW ARCHITECTURE STATUS", GUI.skin.box);

            // Core sistemler
            if (!showCoreSystemsOnly)
            {
                GUILayout.Label("=== CORE SYSTEMS ===");
            }
            
            DrawSystemStatus("SystemCoordinator", SystemCoordinator.Instance != null);
            DrawSystemStatus("SceneController", SceneController.Instance != null);
            DrawSystemStatus("GameStateManager", GameStateManager.Instance != null);
            DrawSystemStatus("PlayerManager", PlayerManager.Instance != null);
            DrawSystemStatus("PrefabManager", PrefabManager.Instance != null);

            if (!showCoreSystemsOnly)
            {
                GUILayout.Label("=== GAMEPLAY ===");
                DrawSystemStatus("ProjectileManager", ProjectileManager.Instance != null);
                DrawSystemStatus("CombatManager", CombatManager.Instance != null);
                DrawSystemStatus("GameDataManager", GameDataManager.Instance != null);
                
                GUILayout.Label("=== NETWORK ===");
                DrawSystemStatus("ApiManager", ApiManager.Instance != null);
                DrawSystemStatus("NetworkManager", NetworkManager.Instance != null);
            }

            // Game state
            if (GameStateManager.Instance != null)
            {
                GUI.color = Color.cyan;
                GUILayout.Label($"State: {GameStateManager.Instance.CurrentState}");
            }
            
            // Data loading durumu
            if (PlayerManager.Instance != null)
            {
                GUI.color = PlayerManager.Instance.HasPlayerData ? Color.green : Color.yellow;
                GUILayout.Label($"Player Data: {(PlayerManager.Instance.HasPlayerData ? "✅" : "❌")}");
                
                if (PlayerManager.Instance.HasActiveShip)
                {
                    GUI.color = Color.green;
                    GUILayout.Label($"Ship: {PlayerManager.Instance.ActiveShip.Name}");
                }
            }
            
            GUI.color = Color.white;
            GUILayout.Label("F1: Check | F2: Load | F3: Combat | F4: Prefabs");
            
            GUILayout.EndArea();
        }

        private void DrawSystemStatus(string name, bool isActive)
        {
            GUI.color = isActive ? Color.green : Color.red;
            string status = isActive ? "✅" : "❌";
            GUILayout.Label($"{status} {name}");
        }

        #region Legacy System Detection

        [ContextMenu("Check Legacy Systems")]
        public void CheckLegacySystems()
        {
            Debug.Log("=== 🕰️ LEGACY SYSTEM CHECK ===");
            
            // Deprecated GameManager
            var gameManager = FindObjectOfType<GameManager>();
            if (gameManager != null)
            {
                Debug.LogWarning("⚠️ DEPRECATED: GameManager hala aktif! Migration yapın.");
            }
            else
            {
                Debug.Log("✅ GameManager temizlenmiş");
            }

            // Eski PlayerDataManager
            var playerDataManager = FindObjectOfType<PlayerManager>();
            if (playerDataManager != null)
            {
                Debug.LogWarning("⚠️ DEPRECATED: PlayerDataManager hala aktif! PlayerManager'a geçin.");
            }
            else
            {
                Debug.Log("✅ PlayerDataManager temizlenmiş");
            }

            // Eski GameSystemInitializer
            var gameSystemInitializer = FindObjectOfType<SystemStatusChecker>();
            if (gameSystemInitializer != null)
            {
                Debug.LogWarning("⚠️ DEPRECATED: GameSystemInitializer hala aktiv! SystemCoordinator'a geçin.");
            }
            else
            {
                Debug.Log("✅ GameSystemInitializer temizlenmiş");
            }

            Debug.Log("=== LEGACY CHECK COMPLETE ===");
        }

        #endregion
    }
} 