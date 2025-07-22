// Filename: MainMenuUIManager.cs

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

// Bu yardımcı sınıfı script'in en altına veya ayrı bir dosyaya ekleyin.
[System.Serializable]
public class ConnectionPayload
{
    public string shipId; // GUID'i string olarak göndermek JSON için daha güvenilirdir.
}

public class MainMenuUIManager : MonoBehaviour
{
    [Header("Developer Settings")] [SerializeField]
    private bool _bypassServerCheck = false;

    [Header("Panels")] [SerializeField] private GameObject _loginPanel;
    [SerializeField] private GameObject _shipSelectionPanel;
    [SerializeField] private GameObject _loadingScreenPanel;

    [Header("Login UI")] [SerializeField] private TMP_InputField _emailInput;
    [SerializeField] private TMP_InputField _passwordInput;
    [SerializeField] private Button _loginButton;
    [SerializeField] private TMP_Text _statusText;

    [Header("Ship Selection UI")] [SerializeField]
    private RectTransform _shipListContent;

    [SerializeField] private GameObject _shipButtonPrefab;
    [SerializeField] private Button _launchButton;

    [Header("Loading Screen UI")] [SerializeField]
    private TMP_Text _loadingStatusText;

    [SerializeField] private TMP_Text _loadingTipText;
    [SerializeField] private string[] _gameTips;

    private AuthApiService _authApiService;
    private PlayerApiService _playerApiService;
    private GameSession _gameSession;

    private bool _networkSubscribed = false;


    private void Start()
    {
        _authApiService = ServiceLocator.Current.Get<AuthApiService>();
        _playerApiService = ServiceLocator.Current.Get<PlayerApiService>();
        _gameSession = ServiceLocator.Current.Get<GameSession>();

        _loginButton.onClick.AddListener(OnLoginClicked);
        _launchButton.onClick.AddListener(OnLaunchClicked);

        _authApiService.OnLoginSuccess += HandleLoginSuccess;
        _playerApiService.OnPlayerDataReceived += HandlePlayerDataReceived;

        _loginPanel.SetActive(true);
        _shipSelectionPanel.SetActive(false);
        _loadingScreenPanel.SetActive(false);
        _launchButton.interactable = false;
        _emailInput.text = "egoist@egoist.com";
        _passwordInput.text = "egoist";
    }

    // private void Update()
    // {
    //     if (_networkSubscribed || NetworkManager.Singleton == null)
    //     {
    //         return;
    //     }
    //     
    //     Debug.Log("MainMenuUIManager: NetworkManager hazır, olaylara abone olunuyor.");
    //     NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
    //     NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnected;
    //     _networkSubscribed = true;
    // }
    private void OnLoginClicked()
    {
        _statusText.text = "Giriş yapılıyor...";
        _loginButton.interactable = false;
        _ = _authApiService.LoginAsync(_emailInput.text, _passwordInput.text);
    }

    private void HandleLoginSuccess()
    {
        _statusText.text = "Karakter verileri alınıyor...";
        _ = _playerApiService.GetMyCharacterDataAsync();
    }

    private void HandlePlayerDataReceived()
    {
        _loginPanel.SetActive(false);
        _shipSelectionPanel.SetActive(true);
        PopulateShipList();
    }

    private void PopulateShipList()
    {
        foreach (Transform child in _shipListContent) Destroy(child.gameObject);

        foreach (var ship in _playerApiService.PlayerData.Ships)
        {
            var buttonGo = Instantiate(_shipButtonPrefab, _shipListContent);
            buttonGo.GetComponent<ShipSelectionButton>().Setup(ship, SelectShip);
        }
    }

    private void SelectShip(System.Guid shipId)
    {
        _gameSession.SelectedShipId = shipId;
        _launchButton.interactable = true;
        Debug.Log($"Gemi seçildi: {shipId}");
    }

    private async void OnLaunchClicked()
    {
        if (_gameSession.SelectedShipId == System.Guid.Empty) return;

        // Panelleri ayarla ve yükleme ekranını göster
        _shipSelectionPanel.SetActive(false);
        _loadingScreenPanel.SetActive(true);
        _loadingTipText.text = _gameTips[Random.Range(0, _gameTips.Length)];

        // Geliştirici bypass'ı aktif mi?
        if (_bypassServerCheck)
        {
            Debug.LogWarning("Geliştirici modu: Sunucu durum kontrolü atlanıyor.");
            ConnectToGameServer();
            return;
        }

        // Sunucu durumunu kontrol et ve bağlan.
        _loadingStatusText.text = "Sunucu durumu kontrol ediliyor...";
        bool isServerOnline = await _authApiService.CheckServerStatusAsync();

        if (isServerOnline)
        {
            ConnectToGameServer();
        }
        else
        {
            HandleConnectionFailed("Oyun sunucusu şu an aktif değil veya yanıt vermiyor.");
        }
    }

    private void ConnectToGameServer()
    {
        _loadingStatusText.text = "Sunucuya bağlanılıyor...";

        var payload = new ConnectionPayload { shipId = _gameSession.SelectedShipId.ToString() };
        string payloadJson = JsonUtility.ToJson(payload);
        NetworkManager.Singleton.NetworkConfig.ConnectionData = System.Text.Encoding.UTF8.GetBytes(payloadJson);
#if UNITY_EDITOR
        NetworkManager.Singleton.StartHost();
#else
            NetworkManager.Singleton.StartClient();
#endif
    }

    // private void HandleClientConnected(ulong clientId)
    // {
    //     if (clientId == NetworkManager.Singleton.LocalClientId)
    //     {
    //         _loadingStatusText.text = "Dünya yükleniyor...";
    //         SceneManager.LoadScene("Main");
    //     }
    // }

    // Hem bağlantı kopması hem de başarısızlık durumları için ortak metot
    private void HandleConnectionFailed(string reason)
    {
        _loadingScreenPanel.SetActive(false);
        _loginPanel.SetActive(true);
        _loginButton.interactable = true;
        _statusText.text = reason; // Hata sebebini göster
    }

    // private void HandleClientDisconnected(ulong clientId)
    // {
    //     if (clientId == NetworkManager.Singleton.LocalClientId)
    //     {
    //         HandleConnectionFailed("Sunucu ile bağlantı koptu.");
    //     }
    // }


    private void OnDestroy()
    {
        // Bu script yok olduğunda olay aboneliklerini iptal et.
        if (_authApiService != null && _playerApiService != null)
        {
            _authApiService.OnLoginSuccess -= HandleLoginSuccess;
            _playerApiService.OnPlayerDataReceived -= HandlePlayerDataReceived;
        }

        // if (NetworkManager.Singleton != null)
        // {
        //     NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
        //     NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnected;
        // }
    }
}