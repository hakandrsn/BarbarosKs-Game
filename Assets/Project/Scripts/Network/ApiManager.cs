using System.Text;
using System.Threading.Tasks;
using BarbarosKs.Shared.DTOs;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
// Projenizde Newtonsoft.Json paketinin kurulu olması gerekir

// Sunucudaki DTO'ları Unity projenizde de tanımlamanız gerekir

public class ApiManager : MonoBehaviour
{
    [SerializeField] private string baseApiUrl = "https://localhost:5001/api"; // WebApi'nizin adresini buraya yazın

    private string _authToken;

    // Singleton Pattern: Projenin her yerinden kolayca erişim için
    public static ApiManager Instance { get; private set; }
    public bool IsLoggedIn => !string.IsNullOrEmpty(_authToken);

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

    public string GetAuthToken()
    {
        return _authToken;
    }

    /// <summary>
    ///     API'ye giriş isteği gönderir.
    /// </summary>
    public async Task<AuthResponseDto> Login(string email, string password)
    {
        var loginRequest = new LoginRequestDto { Email = email, Password = password };
        // Bu metot artık AuthResponseDto içinde CharacterData'yı da getiriyor.
        var response = await PostRequest<AuthResponseDto>("/Auth/login", loginRequest);

        if (response != null && response.Success)
        {
            SetToken(response.Token);
            Debug.Log("Giriş başarılı. Token kaydedildi.");
        }

        return response;
    }

    /// <summary>
    ///     API'ye kayıt olma isteği gönderir.
    /// </summary>
    public async Task<AuthResponseDto>
        Register(string email, string password, string confirmPassword, string username) // username parametresi eklendi
    {
        var registerRequest = new RegisterRequestDto
        {
            Email = email,
            Password = password,
            ConfirmPassword = confirmPassword,
            Username = username // Yeni parametre DTO'ya eklendi
        };

        var response = await PostRequest<AuthResponseDto>("/Auth/register", registerRequest);
        return response;
    }

    /// <summary>
    ///     Oturum kapatma işlemi yapar.
    /// </summary>
    public void Logout()
    {
        _authToken = null;
        PlayerPrefs.DeleteKey("AuthToken");
        Debug.Log("Oturum kapatıldı. Token silindi.");
    }

    /// <summary>
    ///     API'ye GET isteği göndermek için genel bir metot.
    /// </summary>
    private async Task<T> GetRequest<T>(string endpoint) where T : class
    {
        var url = baseApiUrl + endpoint;

        using var request = UnityWebRequest.Get(url);
        // Token'ı Authorization header'ına ekliyoruz.
        request.SetRequestHeader("Authorization", "Bearer " + _authToken);
        Debug.Log($"<color=orange>TOKEN SENT:</color> Bearer {_authToken}");

        var operation = request.SendWebRequest();

        while (!operation.isDone) await Task.Yield();

        if (request.result == UnityWebRequest.Result.Success)
            return JsonConvert.DeserializeObject<T>(request.downloadHandler.text);
        Debug.LogError($"API Hatası ({request.responseCode}): {request.error} - {request.downloadHandler.text}");
        return null;
    }


    /// <summary>
    ///     API'ye POST isteği göndermek için genel bir metot.
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
        if (IsLoggedIn) request.SetRequestHeader("Authorization", "Bearer " + _authToken);

        var operation = request.SendWebRequest();

        while (!operation.isDone) await Task.Yield();

        Debug.Log($"<color=orange>TOKEN SENT:</color> Bearer {_authToken}" + request);
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

        // return JsonConvert.DeserializeObject<T>(request.downloadHandler.text);
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
        if (IsLoggedIn) Debug.Log("Geçerli bir oturum bulundu.");
    }
}