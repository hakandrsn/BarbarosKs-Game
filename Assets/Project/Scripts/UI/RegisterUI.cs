using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Örnek bir RegisterUI.cs script'i

public class RegisterUI : MonoBehaviour
{
    public TMP_InputField usernameInput; // YENİ: Inspector'dan bu alanı atayın
    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;
    public TMP_InputField confirmPasswordInput;
    public Button registerButton;
    public TextMeshProUGUI feedbackText;

    private void Start()
    {
        registerButton.onClick.AddListener(OnRegisterButtonClicked);
    }

    private async void OnRegisterButtonClicked()
    {
        feedbackText.text = "Kayıt olunuyor...";
        registerButton.interactable = false;

        // ApiManager'a artık username'i de gönderiyoruz
        var response = await ApiManager.Instance.Register(
            emailInput.text, 
            passwordInput.text, 
            confirmPasswordInput.text,
            usernameInput.text);

        if (response != null && response.Success)
        {
            feedbackText.text = "Kayıt başarılı! Oyuna giriş yapılıyor...";

            // Başarılı kayıttan sonra dönen tam veriyi GameManager'a iletiyoruz.
            // Bu metot, oyuncunun gemisi olup olmadığını kontrol edip doğru sahneye yönlendirecek.
            GameManager.Instance.OnCharacterDataReceived(response.User);
        }
        else
        {
            feedbackText.text = "Hata: " + (response?.Message ?? "Sunucuya bağlanılamadı.");
            registerButton.interactable = true;
        }
    }
}