using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using BarbarosKs.UI;
using Project.Scripts.Network;

namespace BarbarosKs.Core
{
    /// <summary>
    /// Loading süreçlerini yöneten sistem
    /// Deprecated - SceneController ile değiştirildi, geriye uyumluluk için korunuyor
    /// </summary>
    [System.Obsolete("LoadingManager deprecated. Use SceneController instead.")]
    public class LoadingManager : MonoBehaviour
    {
        private static LoadingManager instance;
        public static LoadingManager Instance => instance;

        [Header("Scene Settings")] 
        [SerializeField] private string gameSceneName = "FisherSea";

        [Header("Loading Steps")] 
        [SerializeField] private float stepDuration = 0.5f; // Her step arasında minimum süre

        [Header("Debug")]
        [SerializeField] private bool verboseLogging = true;

        // Loading süreci için state flags
        private bool _loadingFailed = false;
        private string _loadingErrorMessage = "";

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                DebugLog("⚠️ LoadingManager deprecated - SceneController kullanın");
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Gemi seçildikten sonra tüm loading sürecini başlatır
        /// DEPRECATED: SceneController.HandleShipSelected() kullanın
        /// </summary>
        [System.Obsolete("Use SceneController.HandleShipSelected() instead")]
        public void StartShipLoadingProcess(Guid selectedShipId)
        {
            DebugLog($"⚠️ DEPRECATED: StartShipLoadingProcess çağrıldı - SceneController kullanın");
            DebugLog($"Gemi loading süreci başlatılıyor: {selectedShipId}");

            // SceneController varsa ona yönlendir
            if (SceneController.Instance != null)
            {
                // PlayerManager'dan ship'i al ve SceneController'a gönder
                if (PlayerManager.Instance != null && PlayerManager.Instance.HasActiveShip)
                {
                    var activeShip = PlayerManager.Instance.ActiveShip;
                    SceneController.Instance.HandleShipSelected(activeShip);
                    return;
                }
            }

            // Fallback: Eski sistem
            DebugLog("SceneController bulunamadı, eski loading sistemi kullanılıyor");
            
            // State'i sıfırla
            _loadingFailed = false;
            _loadingErrorMessage = "";

            StartCoroutine(LoadShipAndConnectToServerWrapper(selectedShipId));
        }

        private IEnumerator LoadShipAndConnectToServer(Guid shipId)
        {
            // Loading ekranını göster
            ShowLoadingMessage("Gemi hazırlanıyor...", 0.1f);
            yield return new WaitForSeconds(0.2f);

            // Step 1: Aktif gemiyi ayarla
            ShowLoadingMessage("Gemi seçiliyor...", 0.2f);
            yield return StartCoroutine(SetActiveShipCoroutine(shipId));

            // Hata kontrolü
            if (_loadingFailed)
            {
                ShowErrorAndReturn(_loadingErrorMessage);
                yield break;
            }

            yield return new WaitForSeconds(stepDuration);

            // Step 2: PlayerManager'a gemi seç
            ShowLoadingMessage("Gemi bilgileri yükleniyor...", 0.4f);
            if (PlayerManager.Instance != null)
            {
                var ship = PlayerManager.Instance.GetShipById(shipId);
                if (ship != null)
                {
                    PlayerManager.Instance.SetActiveShip(ship);
                    DebugLog("✅ Gemi PlayerManager'a ayarlandı");
                }
                else
                {
                    _loadingFailed = true;
                    _loadingErrorMessage = "Gemi bulunamadı!";
                    ShowErrorAndReturn(_loadingErrorMessage);
                    yield break;
                }
            }

            yield return new WaitForSeconds(stepDuration);

            // Step 3: Oyun sahnesini yükle
            ShowLoadingMessage("Oyun dünyası yükleniyor...", 0.7f);
            var sceneLoadOperation = SceneManager.LoadSceneAsync(gameSceneName);
            sceneLoadOperation.allowSceneActivation = false;

            // Sahne yüklenene kadar bekle
            while (sceneLoadOperation.progress < 0.9f)
            {
                float progress = 0.7f + (sceneLoadOperation.progress * 0.2f);
                ShowLoadingMessage("Oyun dünyası yükleniyor...", progress);
                yield return null;
            }

            // Sahneyi aktifleştir
            sceneLoadOperation.allowSceneActivation = true;
            yield return new WaitUntil(() => sceneLoadOperation.isDone);

            // Step 4: Sunucuya bağlan
            ShowLoadingMessage("Sunucuya bağlanılıyor...", 0.9f);
            yield return new WaitForSeconds(0.5f); // Sahnenin tam yüklenmesi için kısa bekleme

            // NetworkManager'ı bulup bağlantıyı başlat
            var networkManager = FindObjectOfType<NetworkManager>();
            if (networkManager != null)
            {
                networkManager.ConnectToGameServer();

                // Bağlantı kurulana kadar bekle (maksimum 10 saniye)
                float connectionTimeout = 10f;
                float connectionTimer = 0f;

                while (!networkManager.IsConnected && connectionTimer < connectionTimeout)
                {
                    connectionTimer += Time.deltaTime;
                    yield return null;
                }

                if (networkManager.IsConnected)
                {
                    DebugLog("✅ Sunucuya başarıyla bağlanıldı!");
                }
                else
                {
                    DebugLog("⚠️ Sunucu bağlantısı zaman aşımına uğradı, ama oyun devam ediyor.");
                }
            }
            else
            {
                DebugLog("⚠️ NetworkManager bulunamadı!");
            }

            // Step 5: Tamamlandı
            ShowLoadingMessage("Hazır!", 1.0f);
            yield return new WaitForSeconds(0.5f);

            // Loading ekranını gizle
            HideLoadingMessage();

            DebugLog("✅ Loading süreci başarıyla tamamlandı!");
        }

        /// <summary>
        /// Ana loading coroutine'ini try-catch ile sarmalayan wrapper
        /// </summary>
        private IEnumerator LoadShipAndConnectToServerWrapper(Guid shipId)
        {
            bool hasError = false;
            string errorMessage = "";

            // Ana coroutine'i try-catch ile sarmalamak için bir wrapper kullanıyoruz
            yield return StartCoroutine(ExecuteWithErrorHandling(
                LoadShipAndConnectToServer(shipId),
                (error) =>
                {
                    hasError = true;
                    errorMessage = error;
                }
            ));

            // Eğer beklenmeyen bir hata oluştuysa
            if (hasError)
            {
                Debug.LogError($"❌ LoadingManager Wrapper: {errorMessage}");
                ShowErrorAndReturn("Beklenmeyen bir hata oluştu. Lütfen tekrar deneyin.");
            }
        }

        /// <summary>
        /// Coroutine'i try-catch ile sarmalayan generic metod
        /// </summary>
        private IEnumerator ExecuteWithErrorHandling(IEnumerator coroutine, System.Action<string> onError)
        {
            bool completed = false;
            Exception caughtException = null;

            // Coroutine'i çalıştırmak için StartCoroutine kullanıyoruz
            StartCoroutine(RunCoroutineWithErrorCapture(coroutine,
                () => completed = true,
                (ex) =>
                {
                    completed = true;
                    caughtException = ex;
                }));

            // Tamamlanana kadar bekle
            while (!completed)
            {
                yield return null;
            }

            // Hata varsa callback'i çağır
            if (caughtException != null)
            {
                onError?.Invoke($"Coroutine hatası: {caughtException.Message}");
            }
        }

        /// <summary>
        /// Coroutine'i çalıştırıp hataları yakalayan helper metod
        /// </summary>
        private IEnumerator RunCoroutineWithErrorCapture(IEnumerator coroutine, System.Action onComplete,
            System.Action<Exception> onError)
        {
            yield return coroutine;
            onComplete?.Invoke();
        }

        /// <summary>
        /// SetActiveShip API çağrısını coroutine olarak sarmalayan metod
        /// </summary>
        private IEnumerator SetActiveShipCoroutine(Guid shipId)
        {
            // ApiManager kontrolü
            if (ApiManager.Instance == null)
            {
                DebugLog("⚠️ ApiManager bulunamadı, API çağrısı atlanıyor");
                yield break;
            }

            // Async metodu background'da çalıştır
            var setActiveTask = ApiManager.Instance.SetActiveShip(shipId);

            // Task tamamlanana kadar bekle
            while (!setActiveTask.IsCompleted)
            {
                yield return null;
            }

            if (setActiveTask.IsFaulted)
            {
                string errorMessage = setActiveTask.Exception?.GetBaseException().Message ?? "Bilinmeyen hata";
                Debug.LogError($"❌ SetActiveShip API hatası: {errorMessage}");
                _loadingFailed = true;
                _loadingErrorMessage = "Gemi seçilemedi. Lütfen tekrar deneyin.";
                yield break;
            }

            bool result = setActiveTask.Result;

            if (!result)
            {
                Debug.LogError("❌ Aktif gemi ayarlanamadı!");
                _loadingFailed = true;
                _loadingErrorMessage = "Gemi seçilemedi. Lütfen tekrar deneyin.";
                yield break;
            }

            DebugLog("✅ SetActiveShip başarılı!");
        }

        #region UI Helpers

        private void ShowLoadingMessage(string message, float progress)
        {
            // LoadingScreen sistemini kullan
            var loadingScreen = FindObjectOfType<LoadingScreen>();
            if (loadingScreen != null)
            {
                loadingScreen.UpdateProgress(progress, progress >= 1.0f);
                DebugLog($"📊 Loading: {message} ({progress * 100:F0}%)");
            }
            else
            {
                DebugLog($"📊 Loading (no UI): {message} ({progress * 100:F0}%)");
            }
        }

        private void HideLoadingMessage()
        {
            var loadingScreen = FindObjectOfType<LoadingScreen>();
            if (loadingScreen != null)
            {
                loadingScreen.CompleteLoading();
            }
        }

        private void ShowErrorAndReturn(string errorMessage)
        {
            Debug.LogError($"❌ LoadingManager Hata: {errorMessage}");

            ShowLoadingMessage($"Hata: {errorMessage}", 0f);

            // 3 saniye sonra loading'i gizle ve gemi seçim ekranına dön
            StartCoroutine(HideLoadingAfterDelay(3f));
        }

        private IEnumerator HideLoadingAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            HideLoadingMessage();

            // SceneController varsa onu kullan
            if (SceneController.Instance != null)
            {
                SceneController.Instance.LoadShipSelection();
            }
            else
            {
                // Fallback
                SceneManager.LoadScene("SelectShipScene");
            }
        }

        #endregion

        #region Debug Methods

        private void DebugLog(string message)
        {
            if (verboseLogging)
            {
                Debug.Log($"[LoadingManager] {message}");
            }
        }

        [ContextMenu("Debug: Test Loading Process")]
        private void DebugTestLoadingProcess()
        {
            if (PlayerManager.Instance?.HasActiveShip == true)
            {
                var shipId = PlayerManager.Instance.ActiveShip.Id;
                DebugLog($"🧪 Test loading process başlatılıyor: {shipId}");
                #pragma warning disable CS0618 // Type or member is obsolete
                StartShipLoadingProcess(shipId);
                #pragma warning restore CS0618 // Type or member is obsolete
            }
            else
            {
                DebugLog("❌ Test için aktif gemi gerekli");
            }
        }

        [ContextMenu("Debug: Show Manager Status")]
        private void DebugShowManagerStatus()
        {
            Debug.Log("=== LOADING MANAGER STATUS ===");
            Debug.Log($"LoadingManager Instance: {(Instance != null ? "ACTIVE" : "NULL")}");
            Debug.Log($"SceneController Instance: {(SceneController.Instance != null ? "ACTIVE" : "NULL")}");
            Debug.Log($"PlayerManager Instance: {(PlayerManager.Instance != null ? "ACTIVE" : "NULL")}");
            Debug.Log($"ApiManager Instance: {(ApiManager.Instance != null ? "ACTIVE" : "NULL")}");
            Debug.Log($"NetworkManager Instance: {(NetworkManager.Instance != null ? "ACTIVE" : "NULL")}");
            
            if (PlayerManager.Instance != null)
            {
                Debug.Log($"Has Player Data: {PlayerManager.Instance.HasPlayerData}");
                Debug.Log($"Has Active Ship: {PlayerManager.Instance.HasActiveShip}");
                if (PlayerManager.Instance.HasActiveShip)
                {
                    Debug.Log($"Active Ship: {PlayerManager.Instance.ActiveShip.Name}");
                }
            }
        }

        #endregion

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }
    }
}