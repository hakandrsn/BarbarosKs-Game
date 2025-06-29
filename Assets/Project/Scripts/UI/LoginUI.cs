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
    public Button registerButton;   

    private void Start()
    {
        loginButton.onClick.AddListener(OnLoginButtonClicked);
        registerButton.onClick.AddListener(OnRegisterButtonClicked);       
    }

    private async void OnLoginButtonClicked()
    {
        feedbackText.text = "Giriş yapılıyor...";
        loginButton.interactable = false;

        var response = await ApiManager.Instance.Login(emailInput.text, passwordInput.text);

        if (response is { Success: true })
        {
            feedbackText.text = "Giriş başarılı! Veriler alınıyor...";
            // Başarılı giriş sonrası dönen tam veriyi GameManager'a iletiyoruz.
            GameManager.Instance.OnCharacterDataReceived(response.CharacterData);
        }
        else
        {
            feedbackText.text = "Hata: " + (response?.Message ?? "Sunucuya bağlanılamadı.");
            loginButton.interactable = true;
        }
    }

    private void OnRegisterButtonClicked()
    {
        GameManager.Instance.ToScene("Register");     
    }
}