using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using BarbarosKs.UI;
using Project.Scripts.Network;

namespace BarbarosKs.Core
{
    public class LoadingManager : MonoBehaviour
    {
        private static LoadingManager instance;
        public static LoadingManager Instance => instance;

        [Header("Scene Settings")] [SerializeField]
        private string gameSceneName = "FisherSea";

        [Header("Loading Steps")] [SerializeField]
        private float stepDuration = 0.5f; // Her step arasında minimum süre

        // Loading süreci için state flags
        private bool _loadingFailed = false;
        private string _loadingErrorMessage = "";

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Gemi seçildikten sonra tüm loading sürecini başlatır
        /// </summary>
        public void StartShipLoadingProcess(Guid selectedShipId)
        {
            Debug.Log($"[LoadingManager] Gemi loading süreci başlatılıyor: {selectedShipId}");

            // State'i sıfırla
            _loadingFailed = false;
            _loadingErrorMessage = "";

            StartCoroutine(LoadShipAndConnectToServerWrapper(selectedShipId));
        }

        private IEnumerator LoadShipAndConnectToServer(Guid shipId)
        {
            // Loading ekranını göster
            LoadingScreen.Instance?.ShowLoading("Gemi hazırlanıyor...");
            yield return new WaitForSeconds(0.2f);

            // Step 1: Aktif gemiyi ayarla
            LoadingScreen.Instance?.UpdateLoadingStep("Gemi seçiliyor...", 0.1f);
            yield return StartCoroutine(SetActiveShipCoroutine(shipId));

            // Hata kontrolü
            if (_loadingFailed)
            {
                ShowErrorAndReturn(_loadingErrorMessage);
                yield break;
            }

            yield return new WaitForSeconds(stepDuration);

            // Step 2: Gemi detaylarını çek
            LoadingScreen.Instance?.UpdateLoadingStep("Gemi bilgileri alınıyor...", 0.3f);
            yield return StartCoroutine(GetShipDetailsCoroutine(shipId));

            // Hata kontrolü
            if (_loadingFailed)
            {
                ShowErrorAndReturn(_loadingErrorMessage);
                yield break;
            }

            yield return new WaitForSeconds(stepDuration);

            // Step 4: Oyun sahnesini yükle
            LoadingScreen.Instance?.UpdateLoadingStep("Oyun dünyası yükleniyor...", 0.7f);
            var sceneLoadOperation = SceneManager.LoadSceneAsync(gameSceneName);
            sceneLoadOperation.allowSceneActivation = false;

            // Sahne yüklenene kadar bekle
            while (sceneLoadOperation.progress < 0.9f)
            {
                float progress = 0.7f + (sceneLoadOperation.progress * 0.2f);
                LoadingScreen.Instance?.SetProgress(progress);
                yield return null;
            }

            // Sahneyi aktifleştir
            sceneLoadOperation.allowSceneActivation = true;
            yield return new WaitUntil(() => sceneLoadOperation.isDone);

            // Step 5: Sunucuya bağlan
            LoadingScreen.Instance?.UpdateLoadingStep("Sunucuya bağlanılıyor...", 0.9f);
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
                    Debug.Log("[LoadingManager] Sunucuya başarıyla bağlanıldı!");
                }
                else
                {
                    Debug.LogWarning("[LoadingManager] Sunucu bağlantısı zaman aşımına uğradı, ama oyun devam ediyor.");
                }
            }
            else
            {
                Debug.LogWarning("[LoadingManager] NetworkManager bulunamadı!");
            }

            // Step 6: Tamamlandı
            LoadingScreen.Instance?.UpdateLoadingStep("Hazır!", 1.0f);
            yield return new WaitForSeconds(0.5f);

            // Loading ekranını gizle
            LoadingScreen.Instance?.HideLoading();

            Debug.Log("[LoadingManager] Loading süreci başarıyla tamamlandı!");
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
                Debug.LogError($"[LoadingManager] Wrapper: {errorMessage}");
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
                Debug.LogError($"[LoadingManager] SetActiveShip API hatası: {errorMessage}");
                _loadingFailed = true;
                _loadingErrorMessage = "Gemi seçilemedi. Lütfen tekrar deneyin.";
                yield break;
            }

            bool result = setActiveTask.Result;

            if (!result)
            {
                Debug.LogError("[LoadingManager] Aktif gemi ayarlanamadı!");
                _loadingFailed = true;
                _loadingErrorMessage = "Gemi seçilemedi. Lütfen tekrar deneyin.";
                yield break;
            }

            Debug.Log("[LoadingManager] SetActiveShip başarılı!");
        }

        /// <summary>
        /// GetShipDetails API çağrısını coroutine olarak sarmalayan metod
        /// </summary>
        private IEnumerator GetShipDetailsCoroutine(Guid shipId)
        {
            // Async metodu background'da çalıştır
            var getDetailsTask = ApiManager.Instance.GetShipDetails(shipId);

            // Task tamamlanana kadar bekle
            while (!getDetailsTask.IsCompleted)
            {
                yield return null;
            }

            if (getDetailsTask.IsFaulted)
            {
                string errorMessage = getDetailsTask.Exception?.GetBaseException().Message ?? "Bilinmeyen hata";
                Debug.LogError($"[LoadingManager] GetShipDetails API hatası: {errorMessage}");
                _loadingFailed = true;
                _loadingErrorMessage = "Gemi bilgileri alınamadı. Lütfen tekrar deneyin.";
                yield break;
            }

            var shipDetails = getDetailsTask.Result;

            if (shipDetails == null)
            {
                Debug.LogError("[LoadingManager] Gemi detayları alınamadı!");
                _loadingFailed = true;
                _loadingErrorMessage = "Gemi bilgileri alınamadı. Lütfen tekrar deneyin.";
                yield break;
            }

            // Step 3: PlayerDataManager'a verileri yükle
            LoadingScreen.Instance?.UpdateLoadingStep("Veriler hazırlanıyor...", 0.5f);
            PlayerDataManager.Instance.LoadActiveShipDetails(shipDetails);

            Debug.Log("[LoadingManager] GetShipDetails başarılı!");
        }

        private void ShowErrorAndReturn(string errorMessage)
        {
            Debug.LogError($"[LoadingManager] Hata: {errorMessage}");

            LoadingScreen.Instance?.UpdateLoadingStep($"Hata: {errorMessage}", 0f);

            // 3 saniye sonra loading'i gizle ve gemi seçim ekranına dön
            StartCoroutine(HideLoadingAfterDelay(3f));
        }

        private IEnumerator HideLoadingAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            LoadingScreen.Instance?.HideLoading();

            // Gemi seçim ekranına geri dön
            SceneManager.LoadScene("SelectShipScene");
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }
    }
}