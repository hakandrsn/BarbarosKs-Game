// Filename: LoginTester.cs

using UnityEngine;

public class LoginTester : MonoBehaviour
{
    private async void Start()
    {
        // Önemli: Web API projenizin çalıştığından emin olun!
        // Kayıt olmak için /api/Auth/register endpoint'ini kullanabilirsiniz (örn: Postman ile).
        // Veya mevcut bir kullanıcı ile test edin.
        
        var apiManager = ServiceLocator.Current.Get<APIManager>();

        // Olayları dinlemeye başla
        apiManager.OnLoginSuccess += HandleLoginSuccess;
        apiManager.OnLoginFailed += HandleLoginFailed;

        // Login metodunu çağır
        await apiManager.LoginAsync("egoist@egoist.com", "egoist");
    }

    private async void HandleLoginSuccess()
    {
        Debug.Log("TESTER: Giriş başarılı olayı yakalandı!");
        await ServiceLocator.Current.Get<APIManager>().GetMyCharacterDataAsync();
    }

    private void HandleLoginFailed(string errorMessage)
    {
        Debug.Log($"TESTER: Giriş başarısız olayı yakalandı! Hata: {errorMessage}");
    }

    private void OnDestroy()
    {
        // Bellek sızıntılarını önlemek için olayları dinlemeyi bırak
        if (ServiceLocator.Current != null)
        {
            var apiManager = ServiceLocator.Current.Get<APIManager>();
            apiManager.OnLoginSuccess -= HandleLoginSuccess;
            apiManager.OnLoginFailed -= HandleLoginFailed;
        }
    }
}