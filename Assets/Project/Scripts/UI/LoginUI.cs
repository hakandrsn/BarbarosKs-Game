using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BarbarosKs.Core;

namespace BarbarosKs.UI
{
    public class LoginUI : MonoBehaviour
    {
        [Header("UI Elements")]
        public TMP_InputField emailInput;
        public TMP_InputField passwordInput;
        public Button loginButton;
        public TextMeshProUGUI feedbackText;
        public Button registerButton;

        [Header("Test Settings")]
        [SerializeField] private bool useTestCredentials = true;
        [SerializeField] private string testEmail = "hakan@gmail.com";
        [SerializeField] private string testPassword = "qwerdas";

        [Header("Debug")]
        [SerializeField] private bool verboseLogging = true;

        private void Start()
        {
            SetupUI();
            DebugLog("LoginUI initialized");
        }

        private void SetupUI()
        {
            // Button event'lerini ayarla
            if (loginButton != null)
                loginButton.onClick.AddListener(OnLoginButtonClicked);
            
            if (registerButton != null)
                registerButton.onClick.AddListener(OnRegisterButtonClicked);
            
            // Test için default değerler
            if (useTestCredentials)
            {
                if (emailInput != null) emailInput.text = testEmail;
                if (passwordInput != null) passwordInput.text = testPassword;
                DebugLog($"Test credentials loaded: {testEmail}");
            }

            // Başlangıç feedback'i temizle
            if (feedbackText != null)
                feedbackText.text = "";
        }

        private async void OnLoginButtonClicked()
        {
            DebugLog("Login button clicked");

            // Input validation
            if (!ValidateInputs())
                return;

            SetUIInteractable(false);
            ShowFeedback("Giriş yapılıyor...", Color.yellow);

            try
            {
                // ApiManager kontrolü
                if (ApiManager.Instance == null)
                {
                    Debug.LogError("❌ ApiManager Instance null!");
                    ShowFeedback("Hata: API yöneticisi bulunamadı.", Color.red);
                    SetUIInteractable(true);
                    return;
                }

                var response = await ApiManager.Instance.Login(emailInput.text, passwordInput.text);

                if (response is { Success: true })
                {
                    DebugLog("✅ Login response başarılı!");
                    
                    // Null kontrolleri
                    if (response.CharacterData == null)
                    {
                        Debug.LogError("❌ Login başarılı ama CharacterData null!");
                        ShowFeedback("Hata: Karakter verileri alınamadı.", Color.red);
                        SetUIInteractable(true);
                        return;
                    }

                    if (PlayerManager.Instance == null)
                    {
                        Debug.LogError("❌ PlayerManager Instance null!");
                        ShowFeedback("Hata: Oyuncu yöneticisi bulunamadı.", Color.red);
                        SetUIInteractable(true);
                        return;
                    }

                    ShowFeedback("Giriş başarılı! Veriler yükleniyor...", Color.green);

                    // PlayerManager'a login başarısını bildir
                    PlayerManager.Instance.HandleLoginSuccess(response.CharacterData);

                    DebugLog("🎉 Login süreci tamamlandı, sahne yönlendirmesi PlayerManager tarafından yapılacak");
                }
                else
                {
                    string errorMessage = response?.Message ?? "Bilinmeyen hata";
                    Debug.LogWarning($"❌ Login başarısız: {errorMessage}");
                    ShowFeedback($"Hata: {errorMessage}", Color.red);
                    SetUIInteractable(true);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"❌ Login sırasında exception: {ex.Message}");
                Debug.LogError($"❌ Stack trace: {ex.StackTrace}");
                ShowFeedback("Hata: Beklenmeyen bir sorun oluştu.", Color.red);
                SetUIInteractable(true);
            }
        }

        private void OnRegisterButtonClicked()
        {
            DebugLog("Register button clicked");
            
            if (SceneController.Instance != null)
            {
                SceneController.Instance.LoadRegister();
                DebugLog("Register sahnesine yönlendiriliyor");
            }
            else
            {
                Debug.LogError("❌ SceneController Instance null!");
                ShowFeedback("Hata: Sahne yöneticisi bulunamadı.", Color.red);
            }
        }

        #region Helper Methods

        private bool ValidateInputs()
        {
            // Email kontrolü
            if (emailInput == null || string.IsNullOrWhiteSpace(emailInput.text))
            {
                ShowFeedback("Lütfen email adresinizi girin.", Color.red);
                return false;
            }

            // Password kontrolü
            if (passwordInput == null || string.IsNullOrWhiteSpace(passwordInput.text))
            {
                ShowFeedback("Lütfen şifrenizi girin.", Color.red);
                return false;
            }

            // Basit email format kontrolü
            if (!emailInput.text.Contains("@") || !emailInput.text.Contains("."))
            {
                ShowFeedback("Lütfen geçerli bir email adresi girin.", Color.red);
                return false;
            }

            return true;
        }

        private void SetUIInteractable(bool interactable)
        {
            if (loginButton != null)
                loginButton.interactable = interactable;
            
            if (registerButton != null)
                registerButton.interactable = interactable;
            
            if (emailInput != null)
                emailInput.interactable = interactable;
            
            if (passwordInput != null)
                passwordInput.interactable = interactable;

            DebugLog($"UI interactable: {interactable}");
        }

        private void ShowFeedback(string message, Color color)
        {
            if (feedbackText != null)
            {
                feedbackText.text = message;
                feedbackText.color = color;
            }
            
            DebugLog($"Feedback: {message}");
        }

        private void DebugLog(string message)
        {
            if (verboseLogging)
            {
                Debug.Log($"[LoginUI] {message}");
            }
        }

        #endregion

        #region Context Menu Debug Methods

        [ContextMenu("Debug: Test Login")]
        private void DebugTestLogin()
        {
            if (useTestCredentials)
            {
                OnLoginButtonClicked();
            }
            else
            {
                DebugLog("Test credentials disabled, manuel giriş yapın");
            }
        }

        [ContextMenu("Debug: Check Manager Status")]
        private void DebugCheckManagerStatus()
        {
            Debug.Log("=== LOGIN UI MANAGER STATUS ===");
            Debug.Log($"ApiManager Instance: {(ApiManager.Instance != null ? "ACTIVE" : "NULL")}");
            Debug.Log($"PlayerManager Instance: {(PlayerManager.Instance != null ? "ACTIVE" : "NULL")}");
            Debug.Log($"SceneController Instance: {(SceneController.Instance != null ? "ACTIVE" : "NULL")}");
            
            if (ApiManager.Instance != null)
            {
                string token = ApiManager.Instance.GetAuthToken();
                Debug.Log($"Auth Token: {(string.IsNullOrEmpty(token) ? "NULL" : "PRESENT")}");
            }
        }

        [ContextMenu("Debug: Clear Inputs")]
        private void DebugClearInputs()
        {
            if (emailInput != null) emailInput.text = "";
            if (passwordInput != null) passwordInput.text = "";
            if (feedbackText != null) feedbackText.text = "";
            DebugLog("Inputs cleared");
        }

        [ContextMenu("Debug: Load Test Credentials")]
        private void DebugLoadTestCredentials()
        {
            if (emailInput != null) emailInput.text = testEmail;
            if (passwordInput != null) passwordInput.text = testPassword;
            DebugLog("Test credentials loaded");
        }

        #endregion

        #region Unity Events

        private void OnDestroy()
        {
            // Button event'lerini temizle
            if (loginButton != null)
                loginButton.onClick.RemoveListener(OnLoginButtonClicked);
            
            if (registerButton != null)
                registerButton.onClick.RemoveListener(OnRegisterButtonClicked);
        }

        #endregion
    }
}