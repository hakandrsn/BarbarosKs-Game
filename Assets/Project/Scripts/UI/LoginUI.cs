using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class LoginUI : MonoBehaviour
{
    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;
    public Button loginButton;
    public TextMeshProUGUI feedbackText;

    private void Start()
    {
        loginButton.onClick.AddListener(OnLoginButtonClicked);
    }

    private async void OnLoginButtonClicked()
    {
        feedbackText.text = "Giriş yapılıyor...";
        loginButton.interactable = false;

        var loginResponse = await ApiManager.Instance.Login(emailInput.text, passwordInput.text);

        if (loginResponse != null && loginResponse.Success)
        {
            feedbackText.text = "Karakter verileri alınıyor...";
        
            // Login başarılı, şimdi karakter ve gemi verilerini çekiyoruz.
            var characterData = await ApiManager.Instance.GetCharacterData();
        
            if (characterData != null)
            {
                GameManager.Instance.OnAccountReceived(loginResponse.User);
                // Veri başarıyla alındı, GameManager'a devrediyoruz.
                GameManager.Instance.OnCharacterDataReceived(characterData);
            }
            else
            {
                feedbackText.text = "Hata: Karakter verileri alınamadı.";
                loginButton.interactable = true;
            }
        }
        else
        {
            feedbackText.text = "Hata: " + (loginResponse?.Message ?? "Sunucuya bağlanılamadı.");
            loginButton.interactable = true;
        }
    }
}