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

        var response = await ApiManager.Instance.Login(emailInput.text, passwordInput.text);

        if (response != null && response.Success)
        {
            feedbackText.text = "Giriş başarılı! " + response.Message;
        
            // YENİ: Başarılı giriş bilgisini GameManager'a aktar ve sahneyi yükle.
            GameManager.Instance.OnLoginSuccess(response.User);
        }
        else
        {
            feedbackText.text = "Hata: " + (response?.Message ?? "Sunucuya bağlanılamadı.");
            loginButton.interactable = true;
        }
    }
}