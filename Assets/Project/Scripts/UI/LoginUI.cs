using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoginUI : MonoBehaviour
{
    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;
    public Button loginButton;
    public TextMeshProUGUI feedbackText;
    public Button registerButton;

    private void Start()
    {
        loginButton.onClick.AddListener(OnLoginButtonClicked);
        registerButton.onClick.AddListener(OnRegisterButtonClicked);
        
        // Test için default değerler
        emailInput.text = "hakan@gmail.com";
        passwordInput.text = "qwerdas";
    }

    private async void OnLoginButtonClicked()
    {
        feedbackText.text = "Giriş yapılıyor...";
        loginButton.interactable = false;

        try
        {
            var response = await ApiManager.Instance.Login(emailInput.text, passwordInput.text);

            if (response is { Success: true })
            {
                Debug.Log("✅ Login response başarılı!");
                
                // Null kontrolleri
                if (response.CharacterData == null)
                {
                    Debug.LogError("❌ Login başarılı ama CharacterData null!");
                    feedbackText.text = "Hata: Karakter verileri alınamadı.";
                    loginButton.interactable = true;
                    return;
                }

                if (GameManager.Instance == null)
                {
                    Debug.LogError("❌ GameManager Instance null!");
                    feedbackText.text = "Hata: Oyun yöneticisi bulunamadı.";
                    loginButton.interactable = true;
                    return;
                }

                feedbackText.text = "Giriş başarılı! Veriler alınıyor...";
                
                // Debug için response içeriğini logla
                Debug.Log($"🎯 Login Response içeriği:");
                Debug.Log($"   - Success: {response.Success}");
                Debug.Log($"   - CharacterData: {(response.CharacterData != null ? "MEVCUT" : "NULL")}");
                if (response.CharacterData != null)
                {
                    Debug.Log($"   - PlayerProfile: {(response.CharacterData.PlayerProfile != null ? "MEVCUT" : "NULL")}");
                    Debug.Log($"   - Ships: {(response.CharacterData.Ships != null ? "MEVCUT" : "NULL")}");
                }

                // Başarılı giriş sonrası dönen tam veriyi GameManager'a iletiyoruz.
                GameManager.Instance.OnCharacterDataReceived(response.CharacterData);
            }
            else
            {
                Debug.LogWarning($"❌ Login başarısız: {response?.Message ?? "Bilinmeyen hata"}");
                feedbackText.text = "Hata: " + (response?.Message ?? "Sunucuya bağlanılamadı.");
                loginButton.interactable = true;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"❌ Login sırasında exception: {ex.Message}");
            Debug.LogError($"❌ Stack trace: {ex.StackTrace}");
            feedbackText.text = "Hata: Beklenmeyen bir sorun oluştu.";
            loginButton.interactable = true;
        }
    }

    private void OnRegisterButtonClicked()
    {
        GameManager.Instance.ToScene("Register");
    }
}