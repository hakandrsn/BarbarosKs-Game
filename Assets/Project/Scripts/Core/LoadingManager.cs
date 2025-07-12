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
        public static LoadingManager Instance { get; private set; }

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
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                DebugLog("⚠️ LoadingManager deprecated - SceneController kullanın");
            }
            else
            {
                Destroy(gameObject);
            }
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
            if (loadingScreen)
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
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}