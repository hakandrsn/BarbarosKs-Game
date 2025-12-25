// Filename: AuthApiService.cs

using System.Threading.Tasks;
using System;
using BarbarosKs.Shared.DTOs;

public class AuthApiService : BaseApiService, IGameService
{
    public event Action OnLoginSuccess;
    public event Action<string> OnLoginFailed;

    // Register olayları (Yeni ekledik)
    public event Action OnRegisterSuccess;
    public event Action<string> OnRegisterFailed;

    /// <summary>
    /// Kullanıcı girişi yapar.
    /// API Endpoint: POST api/auth/login
    /// </summary>
    public async Task LoginAsync(string email, string password)
    {
        // LoginRequest yerine Shared projesindeki "LoginRequestDto"yu kullanıyoruz.
        var requestData = new LoginRequestDto 
        { 
            Email = email, 
            Password = password 
        };

        // BaseApiService'in 429 (Too Many Requests) gibi durumları handle ettiğini varsayıyoruz.
        // Eğer hata alırsak authResponse null gelebilir.
        var authResponse = await PostAsync<LoginRequestDto, AuthResponseDto>("api/auth/login", requestData, false);

        if (authResponse != null && authResponse.Success)
        {
            SetToken(authResponse.Token); // Token'ı kaydet
            OnLoginSuccess?.Invoke();
        }
        else
        {
            // API'den gelen mesajı veya varsayılan mesajı göster
            string msg = authResponse?.Message ?? "Giriş başarısız. (Sunucu hatası veya hız sınırı)";
            OnLoginFailed?.Invoke(msg);
        }
    }
    
    /// <summary>
    /// Yeni kullanıcı kaydı oluşturur.
    /// API Endpoint: POST api/auth/register
    /// </summary>
    public async Task RegisterAsync(string email, string password, string confirmPassword)
    {
        // Basit validasyon
        if (password != confirmPassword)
        {
            OnRegisterFailed?.Invoke("Şifreler eşleşmiyor.");
            return;
        }

        var requestData = new RegisterRequestDto
        {
            Email = email,
            Password = password,
            ConfirmPassword = confirmPassword
        };

        // Register endpoint'i AuthResponseDto dönmüyor, generic bir Success/Message objesi dönüyor.
        // Ancak AuthResponseDto yapısı (Success ve Message alanları olduğu için) burayı karşılar.
        // Veya BaseResponseDto gibi basit bir yapı da kullanabilirsin.
        var response = await PostAsync<RegisterRequestDto, AuthResponseDto>("api/auth/register", requestData, false);

        if (response != null && response.Success)
        {
            OnRegisterSuccess?.Invoke();
        }
        else
        {
            string msg = response?.Message ?? "Kayıt başarısız.";
            OnRegisterFailed?.Invoke(msg);
        }
    }

    public async Task<bool> CheckServerStatusAsync()
    {
        // Bu metot token gerektirmez.
        try
        {
            var response = await HttpClient.GetAsync("/api/auth/health");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}