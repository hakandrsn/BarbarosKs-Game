// Filename: APIManager.cs (Refactored Version)
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using BarbarosKs.Shared.DTOs; // Bu using ifadesi sizde hata verirse silebilirsiniz, DTO'larımızı kendimiz tanımlıyoruz.
using Newtonsoft.Json;
using UnityEngine;

public class APIManager : IGameService
{
    private const string API_BASE_URL = "https://localhost:7272";
    private readonly HttpClient _httpClient;
    private string _jwtToken;

    public CharacterSelectionDto PlayerData { get; private set; }

    public event Action OnLoginSuccess;
    public event Action<string> OnLoginFailed;
    public event Action OnPlayerDataReceived;

    public APIManager()
    {
        _httpClient = new HttpClient { BaseAddress = new Uri(API_BASE_URL) };
    }

    // =====================================================================
    // PUBLIC API METOTLARI (Dışarıdan Çağrılanlar)
    // =====================================================================

    public async Task LoginAsync(string email, string password)
    {
        Debug.Log($"Giriş isteği gönderiliyor: {email}");
        var requestData = new LoginRequest { email = email, password = password };
        
        // Yeni PostAsync yardımcımızı kullanıyoruz.
        var authResponse = await PostAsync<LoginRequest, AuthResponseDto>("api/Auth/login", requestData, requireAuth: false);

        if (authResponse != null && authResponse.Success)
        {
            _jwtToken = authResponse.Token;
            // Token'ı aldıktan sonra gelecekteki istekler için Authorization Header'ını ayarla.
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _jwtToken);
            
            Debug.Log("Giriş başarılı! Token alındı.");
            OnLoginSuccess?.Invoke();
        }
        else
        {
            OnLoginFailed?.Invoke(authResponse?.Message ?? "Bilinmeyen bir giriş hatası.");
        }
    }

    public async Task GetMyCharacterDataAsync()
    {
        Debug.Log("Oyuncu verileri isteniyor...");
        // Yeni GetAsync yardımcımızı kullanıyoruz.
        var characterData = await GetAsync<CharacterSelectionDto>("/api/Players/me");

        if (characterData != null)
        {
            PlayerData = characterData;
            Debug.Log($"Veri alındı! Oyuncu: {PlayerData.PlayerProfile.Username}, Gemi Sayısı: {PlayerData.Ships.Count}");
            OnPlayerDataReceived?.Invoke();
        }
        else
        {
            Debug.LogError("Oyuncu verisi alınamadı veya işlenemedi.");
        }
    }
    
    public async Task<bool> CheckServerStatusAsync()
    {
        // Burada basit bir GET isteği yeterli. Cevabın içeriğiyle ilgilenmiyoruz, sadece başarılı olup olmadığıyla.
        try
        {
            var response = await _httpClient.GetAsync("/api/Auth/health");
            return response.IsSuccessStatusCode;
        }
        catch (Exception e)
        {
            Debug.LogError($"Sunucu durumu kontrol edilirken hata oluştu: {e.Message}");
            return false;
        }
    }

    public async Task<AttackResult> ProcessAttackAsync(Guid attackerId, Guid targetId)
    {
        var payload = new AttackRequestPayload
        {
            AttackerShipId = attackerId.ToString(),
            TargetShipId = targetId.ToString()
        };
        
        // Yeni PostAsync yardımcımızı kullanıyoruz.
        var attackResult = await PostAsync<AttackRequestPayload, AttackResult>("/api/gateway/attack", payload);
        
        if(attackResult == null) Debug.LogError("Saldırı işlenemedi. API yanıtı boş veya hatalı.");
        
        return attackResult;
    }

    // =====================================================================
    // PRIVATE YARDIMCI METOTLAR (Merkezi Mantık)
    // =====================================================================

    /// <summary>
    /// Belirtilen endpoint'e GET isteği atar ve dönen JSON'ı belirtilen tipe dönüştürür.
    /// </summary>
    private async Task<T> GetAsync<T>(string endpoint, bool requireAuth = true)
    {
        if (requireAuth && string.IsNullOrEmpty(_jwtToken))
        {
            Debug.LogError($"Kimlik doğrulaması gerektiren istek yapılamadı: Token bulunamadı. ({endpoint})");
            return default;
        }

        try
        {
            var response = await _httpClient.GetAsync(endpoint);
            var responseJson = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                // Artık Newtonsoft.Json kullanarak daha esnek bir deserialize işlemi yapıyoruz.
                return JsonConvert.DeserializeObject<T>(responseJson);
            }
            else
            {
                Debug.LogError($"API GET hatası: {response.StatusCode} - {responseJson} ({endpoint})");
                return default;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"API isteği sırasında istisna oluştu: {e.Message} ({endpoint})");
            return default;
        }
    }

    /// <summary>
    /// Belirtilen endpoint'e bir payload ile POST isteği atar ve dönen cevabı belirtilen tipe dönüştürür.
    /// </summary>
    private async Task<TResponse> PostAsync<TRequest, TResponse>(string endpoint, TRequest payload, bool requireAuth = true)
    {
        if (requireAuth && string.IsNullOrEmpty(_jwtToken))
        {
            Debug.LogError($"Kimlik doğrulaması gerektiren istek yapılamadı: Token bulunamadı. ({endpoint})");
            return default;
        }

        try
        {
            // JsonUtility yerine daha güvenilir olan Newtonsoft.Json kullanıyoruz.
            var jsonPayload = JsonConvert.SerializeObject(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(endpoint, content);
            var responseJson = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<TResponse>(responseJson);
            }
            else
            {
                Debug.LogError($"API POST hatası: {response.StatusCode} - {responseJson} ({endpoint})");
                // Hata durumunda da API bir mesaj dönebilir, onu da TResponse olarak okumayı deneyebiliriz.
                return JsonConvert.DeserializeObject<TResponse>(responseJson);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"API isteği sırasında istisna oluştu: {e.Message} ({endpoint})");
            return default;
        }
    }
}