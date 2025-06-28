using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json; // Projenizde Newtonsoft.Json paketinin kurulu olması gerekir
using BarbarosKs.core.DTOs; // Sunucudaki DTO'ları Unity projenizde de tanımlamanız gerekir

public class ApiManager : MonoBehaviour
{
    // Singleton Pattern: Projenin her yerinden kolayca erişim için
    public static ApiManager Instance { get; private set; }

    [SerializeField] private string baseApiUrl = "https://localhost:5001/api"; // WebApi'nizin adresini buraya yazın

    private string _authToken;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadToken(); // Uygulama açıldığında kayıtlı token'ı yükle
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public string GetAuthToken() => _authToken;
    public bool IsLoggedIn => !string.IsNullOrEmpty(_authToken);

    /// <summary>
    /// API'ye giriş isteği gönderir.
    /// </summary>
    public async Task<AuthResponseDto> Login(string email, string password)
    {
        var loginRequest = new LoginRequestDto { Email = email, Password = password };
        var response = await PostRequest<AuthResponseDto>("/Auth/login", loginRequest);

        if (response is not { Success: true }) return response;
        SetToken(response.Token);
        Debug.Log("Giriş başarılı. Token kaydedildi.");

        return response;
    }

    /// <summary>
    /// API'ye kayıt olma isteği gönderir.
    /// </summary>
    public async Task<AuthResponseDto> Register(string email, string password, string confirmPassword)
    {
        var registerRequest = new RegisterRequestDto
        {
            Email = email,
            Password = password,
            ConfirmPassword = confirmPassword
        };
        // Kayıt işlemi de token döndürebilir, bu yüzden aynı mantığı kullanıyoruz.
        var response = await PostRequest<AuthResponseDto>("/Auth/register", registerRequest);

        if (response is not { Success: true }) return response;
        SetToken(response.Token);
        Debug.Log("Kayıt başarılı. Token kaydedildi.");

        return response;
    }
    
    /// <summary>
    /// Oturum kapatma işlemi yapar.
    /// </summary>
    public void Logout()
    {
        _authToken = null;
        PlayerPrefs.DeleteKey("AuthToken");
        Debug.Log("Oturum kapatıldı. Token silindi.");
    }


    /// <summary>
    /// API'ye POST isteği göndermek için genel bir metot.
    /// </summary>
    private async Task<T> PostRequest<T>(string endpoint, object payload) where T : class
    {
        var url = baseApiUrl + endpoint;
        var jsonPayload = JsonConvert.SerializeObject(payload);
        var bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);

        using var request = new UnityWebRequest(url, "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        // Eğer token varsa, Authorization header'ını ekle
        if (IsLoggedIn)
        {
            request.SetRequestHeader("Authorization", "Bearer " + _authToken);
        }
            
        var operation = request.SendWebRequest();

        while (!operation.isDone)
        {
            await Task.Yield();
        }

        if (request.result == UnityWebRequest.Result.Success)
            return JsonConvert.DeserializeObject<T>(request.downloadHandler.text);
        Debug.LogError($"API Hatası ({request.responseCode}): {request.error} - {request.downloadHandler.text}");
        // Sunucudan gelen hata mesajını da deserialize etmeye çalışabiliriz.
        try
        {
            return JsonConvert.DeserializeObject<T>(request.downloadHandler.text);
        }
        catch
        {
            return null; // Eğer hata mesajı JSON formatında değilse null dön.
        }

        return JsonConvert.DeserializeObject<T>(request.downloadHandler.text);
    }

    private void SetToken(string token)
    {
        _authToken = token;
        PlayerPrefs.SetString("AuthToken", token); // Token'ı cihaz hafızasına kaydet
        PlayerPrefs.Save();
    }

    private void LoadToken()
    {
        _authToken = PlayerPrefs.GetString("AuthToken", null);
        if (IsLoggedIn)
        {
            Debug.Log("Geçerli bir oturum bulundu.");
        }
    }
}