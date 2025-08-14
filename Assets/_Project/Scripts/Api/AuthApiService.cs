// Filename: AuthApiService.cs

using System.Threading.Tasks;
using System;
using BarbarosKs.Shared.DTOs;

public class AuthApiService : BaseApiService, IGameService
{
    public event Action OnLoginSuccess;
    public event Action<string> OnLoginFailed;

    public async Task LoginAsync(string email, string password)
    {
        var requestData = new LoginRequest { email = email, password = password };
        var authResponse = await PostAsync<LoginRequest, AuthResponseDto>("api/auth/login", requestData, false);

        if (authResponse is { Success: true })
        {
            SetToken(authResponse.Token); // Token'ı temel sınıftaki static değişkene kaydet.
            OnLoginSuccess?.Invoke();
        }
        else
        {
            OnLoginFailed?.Invoke(authResponse?.Message ?? "Bilinmeyen bir giriş hatası.");
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