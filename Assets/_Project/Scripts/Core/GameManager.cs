// Filename: GameManager.cs

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BarbarosKs.Shared.DTOs;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;


public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public NpcData TestNpcData;
    [Header("Network Prefabs")]
    public GameObject ShipPrefab;
    [Header("Game Databases")]
    public CannonballDatabase CannonballDatabase;

    private readonly Dictionary<ulong, Guid> _clientShipSelections = new();
    private PlayerManager _playerManager;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        InitializeServices();
    }

    private async void Start()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.ConnectionApprovalCallback = ConnectionApprovalCheck;
            NetworkManager.Singleton.OnServerStarted += OnServerReady;
        }

        await InitializeGameDataAsync();
    }


    private void OnServerReady()
    {
        Debug.Log("[GameManager] Sunucu başarıyla başlatıldı.");

        // Sahne olaylarını dinlemeye başla.
        NetworkManager.Singleton.SceneManager.OnLoadComplete += OnClientLoadComplete;

        // --- YENİ VE KRİTİK ADIM ---
        // Sunucu, hazır olur olmaz tüm mevcut ve gelecek client'ları "Main" sahnesine yönlendirir.
        Debug.Log("[GameManager] 'Main' sahnesi yükleniyor...");
        NetworkManager.Singleton.SceneManager.LoadScene("Main", LoadSceneMode.Single);
        
        var npcManager = ServiceLocator.Current.Get<NpcManager>();
        npcManager.Initialize(TestNpcData);
        npcManager.SpawnTestNpcs();
    }

    private void InitializeServices()
    {
        var serviceLocator = new ServiceLocator();

        serviceLocator.Register(new AuthApiService());
        serviceLocator.Register(new PlayerApiService());
        serviceLocator.Register(new GameDataService());
        serviceLocator.Register(new CannonballApiService());
        serviceLocator.Register(new GameSession());
        serviceLocator.Register(new PlayerInventory());
        serviceLocator.Register(new NpcManager());
        // PlayerManager'ı oluşturuyoruz ama artık IGameService olarak kaydetmiyoruz.
        _playerManager = new PlayerManager();
        serviceLocator.Register(_playerManager);
    }

    private void ConnectionApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        var clientId = request.ClientNetworkId;
        var payloadBytes = request.Payload;
        Guid selectedShipId;

        // --- YENİ VE DAHA BASİT MANTIK ---
        // Eğer bağlanan kişi Host/Editor ise, gemi ID'sini lokal GameSession'dan al.
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            selectedShipId = ServiceLocator.Current.Get<GameSession>().SelectedShipId;
        }
        // Eğer uzak bir Client ise, payload'dan al.
        else
        {
            var payloadJson = System.Text.Encoding.UTF8.GetString(payloadBytes);
            var payload = JsonUtility.FromJson<ConnectionPayload>(payloadJson);
            selectedShipId = new Guid(payload.shipId);
        }

        if (selectedShipId != Guid.Empty)
        {
            Debug.Log($"[GameManager] Onay Başarılı! Client {clientId} için gemi {selectedShipId} sıraya eklendi.");
            // Onaylanan gemi ID'sini, sahne yüklendiğinde kullanmak üzere sakla.
            _clientShipSelections[clientId] = selectedShipId;
            response.Approved = true;
            response.CreatePlayerObject = false;
        }
        else
        {
            Debug.LogError($"[GameManager] Onay BAŞARISIZ! Client {clientId} için geçerli bir gemi ID'si bulunamadı.");
            response.Approved = false;
        }
    }

    private void OnClientLoadComplete(ulong clientId, string sceneName, LoadSceneMode loadSceneMode)
    {
        if (sceneName != "Main") return;
        
        // --- YENİ VE DAHA BASİT MANTIK ---
        // Dedicated Server'ın kendisi için (clientId=0) oyuncu spawn etmesini engellemeye GEREK YOK,
        // çünkü ConnectionApproval'da onun için bir gemi ID'si hiç saklanmadı.
        
        if (_clientShipSelections.TryGetValue(clientId, out Guid shipId))
        {
            Debug.Log($"[GameManager] Main sahnesi yüklendi. Client {clientId} için spawn işlemi başlatılıyor.");
            _playerManager.SpawnPlayer(clientId, shipId);
            _clientShipSelections.Remove(clientId); // Spawn ettikten sonra listeden kaldır.
        }
        else
        {
            // Bu log, sadece Dedicated Server (clientId=0) için görünmeli, bu normaldir.
            Debug.LogWarning($"[GameManager] Client {clientId} için spawn edilecek gemi bulunamadı (Bu bir Dedicated Server olabilir).");
        }
    }



    private async Task InitializeGameDataAsync()
    {
        var cannonballApi = ServiceLocator.Current.Get<CannonballApiService>();
        List<CannonballDto> typesFromApi = await cannonballApi.GetAllCannonballTypesAsync();
    
        if (typesFromApi != null)
        {
            var gameDataService = ServiceLocator.Current.Get<GameDataService>();
            gameDataService.Initialize(typesFromApi);
            Debug.Log($"API'den {typesFromApi.Count} gülle tipi verisi yüklendi.");
        }
    }

    // Obje yok edildiğinde abonelikten çıkmayı unutmuyoruz.
    private void OnDestroy()
    {
        if (NetworkManager.Singleton == null) return;
        NetworkManager.Singleton.ConnectionApprovalCallback -= ConnectionApprovalCheck;
        NetworkManager.Singleton.OnServerStarted -= OnServerReady;
        if (NetworkManager.Singleton.SceneManager != null)
        {
            NetworkManager.Singleton.SceneManager.OnLoadComplete -= OnClientLoadComplete;
        }
    }
}