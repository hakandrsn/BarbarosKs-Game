using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using BarbarosKs.Player;
using BarbarosKs.Core;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.SceneManagement;
using BarbarosKs.Shared.DTOs;

namespace Project.Scripts.Network
{
    public class NetworkManager : MonoBehaviour
    {
        [Header("Ağ Ayarları")] 
        [SerializeField] private string serverIP = "127.0.0.1";
        [SerializeField] private int serverPort = 9999;
        
        [Header("Debug")]
        [SerializeField] private bool verboseLogging = true;
        
        private readonly Queue<string> _incomingMessages = new();
        private readonly object _messageLock = new();
        private readonly Dictionary<long, float> _pingTimestamps = new();
        private Thread _clientReceiveThread;

        private float _connectionStartTime;
        private NetworkStream _stream;
        private TcpClient _tcpClient;
        
        public static NetworkManager Instance { get; private set; }

        public bool IsConnected { get; private set; }
        public int SentPacketCount { get; private set; }
        public int ReceivedPacketCount { get; private set; }
        public float ConnectionUptime => IsConnected ? Time.time - _connectionStartTime : 0f;
        public float LastPingTime { get; private set; }
        public string ServerEndpoint => $"{serverIP}:{serverPort}";

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                DebugLog("✅ NetworkManager initialized");
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            ProcessMessageQueue();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnApplicationQuit()
        {
            DisconnectFromServer();
        }

        /// <summary>
        /// "FisherSea" yüklendiğinde oyun sunucusuna bağlanma sürecini başlatır.
        /// </summary>
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            DebugLog($"==== SAHNE YÜKLENDİ: {scene.name} ====");
            
            // PlayerManager durumunu kontrol et
            bool playerManagerExists = PlayerManager.Instance != null;
            bool hasPlayerData = playerManagerExists && PlayerManager.Instance.HasPlayerData;
            bool hasActiveShip = playerManagerExists && PlayerManager.Instance.HasActiveShip;
            
            DebugLog($"PlayerManager Instance: {(playerManagerExists ? "MEVCUT" : "NULL")}");
            if (playerManagerExists)
            {
                DebugLog($"Player Data: {(hasPlayerData ? $"MEVCUT - {PlayerManager.Instance.PlayerProfile.Username}" : "NULL")}");
                DebugLog($"Active Ship: {(hasActiveShip ? $"MEVCUT - {PlayerManager.Instance.ActiveShip.Name} (Lv.{PlayerManager.Instance.ActiveShip.Level})" : "NULL")}");
            }
            
            bool apiManagerExists = ApiManager.Instance != null;
            string authToken = ApiManager.Instance?.GetAuthToken();
            DebugLog($"ApiManager Instance: {(apiManagerExists ? "MEVCUT" : "NULL")}");
            DebugLog($"Auth Token: {(string.IsNullOrEmpty(authToken) ? "NULL/BOŞ" : $"MEVCUT ({authToken.Length} karakter)")}");
            
            if (scene.name == "FisherSea" && hasActiveShip)
            {
                DebugLog("✅ Tüm koşullar sağlandı. NetworkManager oyun sunucusuna bağlanıyor...");
                ConnectToGameServer();
            }
            else if (scene.name == "FisherSea")
            {
                Debug.LogError("❌ FisherSea sahnesinde ActiveShip NULL! Sunucuya bağlanılamadı.");
                Debug.LogError("➡️ Gemi seçimi yapılmamış olabilir. Gemi seçim ekranından gemi seçmeyi deneyin.");
            }
            else
            {
                DebugLog($"ℹ️ Sahne '{scene.name}' - Bağlantı gerekmiyor.");
            }
        }

        #region Public Events

        /// <summary>
        /// Gerçek zamanlı oyun sunucusuna başarıyla bağlandığında tetiklenir.
        /// </summary>
        public event Action OnConnectedToServer;

        /// <summary>
        /// Sunucuyla olan bağlantı koptuğunda tetiklenir.
        /// </summary>
        public event Action OnDisconnectedFromServer;

        /// <summary>
        /// Oyuna ilk girildiğinde, sunucudaki tüm varlıkların durumunu içeren paket geldiğinde tetiklenir.
        /// </summary>
        public event Action<S2C_WorldStateData> OnWorldStateReceived;

        /// <summary>
        /// Oyun dünyasına yeni bir varlık (oyuncu, NPC vb.) eklendiğinde tetiklenir.
        /// </summary>
        public event Action<S2C_EntitySpawnData> OnEntitySpawned;

        /// <summary>
        /// Oyun dünyasından bir varlık kaldırıldığında tetiklenir.
        /// </summary>
        public event Action<S2C_EntityDespawnData> OnEntityDespawned;

        /// <summary>
        /// Dünyadaki varlıkların pozisyon/rotasyon güncellemeleri geldiğinde tetiklenir.
        /// </summary>
        public event Action<S2C_TransformUpdateData> OnTransformUpdate;

        /// <summary>
        /// Bir varlığın canı değiştiğinde tetiklenir.
        /// </summary>
        public event Action<S2C_HealthUpdateData> OnHealthUpdate;

        /// <summary>
        /// Oyuncunun gönderdiği aksiyon başarılı olduğunda tetiklenir.
        /// </summary>
        public event Action<object> OnActionSuccess; // object: sunucudan gelen action data'sı

        /// <summary>
        /// Oyuncunun gönderdiği aksiyon başarısız olduğunda tetiklenir.
        /// </summary>
        public event Action<S2C_ActionFailedData> OnActionFailed;

        /// <summary>
        /// Sunucudan gülle spawn mesajı geldiğinde tetiklenir.
        /// </summary>
        public event Action<S2C_ProjectileSpawnData> OnProjectileSpawn;

        #endregion

        #region Bağlantı ve Temel İletişim

        public void ConnectToGameServer()
        {
            if (IsConnected) return;
            try
            {
                _clientReceiveThread = new Thread(ReceiveMessages) { IsBackground = true };
                _tcpClient = new TcpClient();
                _tcpClient.BeginConnect(serverIP, serverPort, OnConnectCallback, null);
                DebugLog($"🔌 Sunucuya bağlanma başlatıldı: {ServerEndpoint}");
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ Bağlantı hatası: {e.Message}");
            }
        }

        private void OnConnectCallback(IAsyncResult ar)
        {
            try
            {
                _tcpClient.EndConnect(ar);
                if (!_tcpClient.Connected) 
                {
                    Debug.LogError("❌ TCP bağlantısı başarısız!");
                    return;
                }

                DebugLog("✅ TCP bağlantısı başarılı! Mesaj alma thread'i başlatılıyor...");
                _stream = _tcpClient.GetStream();
                _clientReceiveThread.Start();
                IsConnected = true;
                _connectionStartTime = Time.time;
                
                lock (_messageLock)
                {
                    _incomingMessages.Enqueue(JsonConvert.SerializeObject(new GameMessage
                        { Type = (MessageType)(-1) }));
                } // Özel içsel mesaj
                DebugLog("🎉 NetworkManager sunucuya başarıyla bağlandı!");
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ OnConnectCallback hatası: {e.Message}");
            }
        }

        private void ReceiveMessages()
        {
            DebugLog("🔍 ReceiveMessages thread başlatıldı!");
            
            try
            {
                var buffer = new byte[4096];
                var messageBuffer = new List<byte>();
                
                while (IsConnected && _stream != null)
                {
                    try
                    {
                        var bytesRead = _stream.Read(buffer, 0, buffer.Length);
                        if (bytesRead == 0)
                        {
                            Debug.LogWarning("❌ Sunucu bağlantısı kapandı!");
                            break;
                        }
                        
                        if (verboseLogging)
                            DebugLog($"📥 {bytesRead} bytes alındı sunucudan");
                        
                        // Okunan byte'ları mesaj buffer'ına ekle
                        for (int i = 0; i < bytesRead; i++)
                        {
                            messageBuffer.Add(buffer[i]);
                        }
                        
                        // Mesaj sınırlayıcılarını kontrol et (\n ile ayrılmış JSON mesajları)
                        var messageData = System.Text.Encoding.UTF8.GetString(messageBuffer.ToArray());
                        
                        // Hem newline hem de '}{ pattern'lerini kontrol et (birbirine yapışık JSON'lar için)
                        var processedMessages = new List<string>();
                        var tempData = messageData;
                        
                        // Önce newline ile ayrılmış mesajları al
                        var newlineMessages = tempData.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        
                        foreach (var msg in newlineMessages)
                        {
                            var trimmedMsg = msg.Trim();
                            if (string.IsNullOrEmpty(trimmedMsg)) continue;
                            
                            // Birbirine yapışık JSON'ları ayır (}{pattern)
                            if (trimmedMsg.Contains("}{"))
                            {
                                // '}' pozisyonlarını bul ve her birinden sonra split yap
                                var currentPos = 0;
                                var braceCount = 0;
                                var jsonStart = 0;
                                
                                for (int i = 0; i < trimmedMsg.Length; i++)
                                {
                                    if (trimmedMsg[i] == '{') braceCount++;
                                    else if (trimmedMsg[i] == '}')
                                    {
                                        braceCount--;
                                        if (braceCount == 0) // Tam bir JSON tamamlandı
                                        {
                                            var jsonPart = trimmedMsg.Substring(jsonStart, i - jsonStart + 1);
                                            if (!string.IsNullOrEmpty(jsonPart))
                                            {
                                                processedMessages.Add(jsonPart);
                                            }
                                            jsonStart = i + 1;
                                        }
                                    }
                                }
                            }
                            else if (trimmedMsg.StartsWith("{") && trimmedMsg.EndsWith("}"))
                            {
                                processedMessages.Add(trimmedMsg);
                            }
                        }
                        
                        if (processedMessages.Count > 0)
                        {
                            if (verboseLogging)
                                DebugLog($"📨 {processedMessages.Count} tam mesaj bulundu");
                            
                            // İşlenen mesajları queue'ye ekle
                            foreach (var message in processedMessages)
                            {
                                lock (_messageLock)
                                {
                                    _incomingMessages.Enqueue(message);
                                    ReceivedPacketCount++;
                                }
                            }
                            
                            messageBuffer.Clear(); // Buffer'ı temizle
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"❌ ReceiveMessages hata: {ex.Message}");
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ ReceiveMessages thread hata: {ex.Message}");
            }
            finally
            {
                DebugLog("🔍 ReceiveMessages thread sonlandı");
                IsConnected = false;
            }
        }

        public void DisconnectFromServer()
        {
            if (!IsConnected) return;
            IsConnected = false;
            _stream?.Close();
            _tcpClient?.Close();
            _clientReceiveThread?.Abort();
            OnDisconnectedFromServer?.Invoke();
            DebugLog("🔌 Sunucudan bağlantı kesildi");
        }

        #endregion

        #region Mesaj İşleme ve Gönderme

        /// <summary>
        /// Arka plandaki thread'den gelen mesajları ana thread'de işler ve ilgili olayları tetikler.
        /// </summary>
        private void ProcessMessageQueue()
        {
            lock (_messageLock)
            {
                if (_incomingMessages.Count == 0) return;
            }

            lock (_messageLock)
            {
                while (_incomingMessages.Count > 0)
                {
                    var rawMessage = _incomingMessages.Dequeue();
                    try
                    {
                        var gameMessage = JsonConvert.DeserializeObject<GameMessage>(rawMessage);
                        if (gameMessage == null) 
                        {
                            Debug.LogError("❌ GameMessage deserialize edilemedi!");
                            continue;
                        }

                        // İçsel "Bağlantı Başarılı" mesajı
                        if ((int)gameMessage.Type == -1)
                        {
                            DebugLog("✅ Bağlantı başarılı mesajı alındı - OnConnectedToServer tetikleniyor");
                            OnConnectedToServer?.Invoke();
                            SendJoinRequest();
                            continue;
                        }

                        // Sadece önemli mesajları logla (Transform hariç)
                        if (gameMessage.Type != MessageType.S2C_TransformUpdate && verboseLogging)
                        {
                            DebugLog($"📨 Sunucudan mesaj alındı: {gameMessage.Type}");
                        }
                        
                        switch (gameMessage.Type)
                        {
                            case MessageType.S2C_WorldState:
                                var worldState = JsonConvert.DeserializeObject<S2C_WorldStateData>(gameMessage.DataJson);
                                DebugLog($"🌍 WorldState alındı: {worldState?.Entities?.Count ?? 0} entity");
                                if (worldState != null) OnWorldStateReceived?.Invoke(worldState);
                                break;
                            case MessageType.S2C_EntitySpawn:
                                var spawnData = JsonConvert.DeserializeObject<S2C_EntitySpawnData>(gameMessage.DataJson);
                                DebugLog($"➕ Entity spawn alındı: {spawnData?.Entity?.PrefabType} ID: {spawnData?.Entity?.EntityId}");
                                if (spawnData != null) OnEntitySpawned?.Invoke(spawnData);
                                break;
                            case MessageType.S2C_EntityDespawn:
                                var despawnData = JsonConvert.DeserializeObject<S2C_EntityDespawnData>(gameMessage.DataJson);
                                DebugLog($"➖ Entity despawn alındı: {despawnData?.EntityId}");
                                if (despawnData != null) OnEntityDespawned?.Invoke(despawnData);
                                break;
                            case MessageType.S2C_TransformUpdate:
                                var transformData = JsonConvert.DeserializeObject<S2C_TransformUpdateData>(gameMessage.DataJson);
                                if (transformData != null) OnTransformUpdate?.Invoke(transformData);
                                break;
                            case MessageType.S2C_HealthUpdate:
                                var healthData = JsonConvert.DeserializeObject<S2C_HealthUpdateData>(gameMessage.DataJson);
                                if (healthData != null) OnHealthUpdate?.Invoke(healthData);
                                break;
                            case MessageType.S2C_Pong:
                                var timestamp = JsonConvert.DeserializeObject<long>(gameMessage.DataJson);
                                ProcessPong(timestamp);
                                break;
                            case MessageType.S2C_ActionAcknowledged:
                                var actionSuccessData = JsonConvert.DeserializeObject<object>(gameMessage.DataJson);
                                DebugLog($"✅ Aksiyon başarılı: {gameMessage.DataJson}");
                                if (actionSuccessData != null) OnActionSuccess?.Invoke(actionSuccessData);
                                break;
                            case MessageType.S2C_ActionFailed:
                                var actionFailedData = JsonConvert.DeserializeObject<S2C_ActionFailedData>(gameMessage.DataJson);
                                DebugLog($"❌ Aksiyon başarısız: {actionFailedData?.Reason}");
                                if (actionFailedData != null) OnActionFailed?.Invoke(actionFailedData);
                                break;
                            case MessageType.S2C_ProjectileSpawn:
                                var projectileSpawnData = JsonConvert.DeserializeObject<S2C_ProjectileSpawnData>(gameMessage.DataJson);
                                DebugLog($"🚀 Gülle spawn alındı: {projectileSpawnData?.ProjectileType} ID: {projectileSpawnData?.ProjectileId}");
                                if (projectileSpawnData != null) OnProjectileSpawn?.Invoke(projectileSpawnData);
                                break;
                            default:
                                Debug.LogWarning($"❌ Bilinmeyen mesaj tipi: {gameMessage.Type}");
                                break;
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"❌ Mesaj işleme hatası: {e.Message} | Gelen Veri: {rawMessage}");
                    }
                }
            }
        }

        /// <summary>
        /// Sunucuya mesaj göndermek için genel bir metot.
        /// </summary>
        private void SendMessage(GameMessage message)
        {
            if (!IsConnected) return;
            try
            {
                var json = JsonConvert.SerializeObject(message);
                var messageWithDelimiter = json + "\n"; // Server newline delimiter bekliyor
                var data = Encoding.UTF8.GetBytes(messageWithDelimiter);
                
                // Sadece önemli mesajları logla (Transform hariç)
                if (message.Type != MessageType.C2S_TransformUpdate && verboseLogging)
                {
                    DebugLog($"📤 {message.Type} gönderiliyor...");
                }
                
                _stream.Write(data, 0, data.Length); // Synchronous write kullan
                _stream.Flush(); // Mesajın hemen gönderilmesini sağla
                SentPacketCount++;
                
                if (message.Type != MessageType.C2S_TransformUpdate && verboseLogging)
                {
                    DebugLog($"✅ {message.Type} başarıyla gönderildi");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ Mesaj gönderme hatası: {e.Message}");
            }
        }

        #endregion

        #region Public Metotlar (İstemcinin Diğer Kısımlarından Çağrılacak)

        /// <summary>
        /// Oyuna katılma isteğini, PlayerManager'dan aldığı güncel bilgilerle gönderir.
        /// </summary>
        public void SendJoinRequest()
        {
            DebugLog("==== SEND JOIN REQUEST ÇAĞRILDI ====");
            
            if (PlayerManager.Instance?.ActiveShip == null)
            {
                Debug.LogError("❌ HATA: PlayerManager.Instance.ActiveShip NULL! Join request gönderilemedi.");
                Debug.LogError("➡️ Çözüm: Gemi seçim ekranından bir gemi seçin.");
                return;
            }
            
            var activeShip = PlayerManager.Instance.ActiveShip;
            DebugLog($"✅ ActiveShip bulundu: {activeShip.Name} (ID: {activeShip.Id})");
            
            // JWT token'ı ApiManager'dan al
            string authToken = ApiManager.Instance?.GetAuthToken();
            if (string.IsNullOrEmpty(authToken))
            {
                Debug.LogError("❌ HATA: JWT token bulunamadı! Oyun sunucusuna katılma isteği gönderilemedi.");
                Debug.LogError("➡️ Çözüm: Login ekranından tekrar giriş yapın.");
                return;
            }
            
            DebugLog($"✅ JWT Token bulundu: {authToken.Length} karakter");

            var joinData = new C2S_JoinGameData
            {
                SelectedShipId = activeShip.Id,
                AuthToken = authToken  // JWT token'ı ekle
            };

            var message = new GameMessage
            {
                Type = MessageType.C2S_JoinGame,
                DataJson = JsonConvert.SerializeObject(joinData)
            };

            DebugLog($"🚀 Oyun sunucusuna katılma isteği gönderiliyor. Ship ID: {activeShip.Id}");
            SendMessage(message);
            DebugLog("✅ Join request gönderildi. Sunucu cevabı bekleniyor...");
        }

        /// <summary>
        /// Yerel oyuncunun gemisinin transform'unu sunucuya gönderir.
        /// </summary>
        public void SendTransformUpdate(Vector3 position, Quaternion rotation, Vector3 velocity)
        {
            var transformData = new C2S_TransformUpdateData
            {
                Position = position.ToNumeric(),
                Rotation = rotation.ToNumeric(),
                Velocity = velocity.ToNumeric()
            };

            var message = new GameMessage
            {
                Type = MessageType.C2S_TransformUpdate,
                DataJson = JsonConvert.SerializeObject(transformData)
            };

            SendMessage(message);
        }

        /// <summary>
        /// Oyuncunun bir aksiyon gerçekleştirdiğini sunucuya bildirir.
        /// PlayerController tarafından çağrılır.
        /// </summary>
        /// <param name="actionData">Gerçekleştirilen aksiyonun detaylarını içeren DTO.</param>
        public void SendPlayerAction(C2S_PlayerActionData actionData)
        {
            if (!IsConnected) return;

            var message = new GameMessage
            {
                Type = MessageType.C2S_PlayerAction,
                DataJson = JsonConvert.SerializeObject(actionData)
            };

            SendMessage(message);
        }

        public void SendPing()
        {
            if (!IsConnected) return;

            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            _pingTimestamps[timestamp] = Time.time; // Gönderme zamanını kaydet

            var message = new GameMessage
            {
                Type = MessageType.C2S_Ping,
                DataJson = JsonConvert.SerializeObject(timestamp)
            };
            SendMessage(message);
        }

        /// <summary>
        /// Sunucudan gelen pong yanıtını işler ve gecikme süresini hesaplar.
        /// </summary>
        private void ProcessPong(long timestamp)
        {
            if (!_pingTimestamps.TryGetValue(timestamp, out var sendTime)) return;
            // Round Trip Time (RTT) hesapla (milisaniye cinsinden)
            LastPingTime = (Time.time - sendTime) * 1000f;
            _pingTimestamps.Remove(timestamp);
        }

        #endregion

        #region Debug Methods

        private void DebugLog(string message)
        {
            if (verboseLogging)
            {
                Debug.Log($"[NetworkManager] {message}");
            }
        }

        [ContextMenu("Debug: Connection Status")]
        private void DebugConnectionStatus()
        {
            Debug.Log("=== NETWORK CONNECTION STATUS ===");
            Debug.Log($"Is Connected: {IsConnected}");
            Debug.Log($"Server Endpoint: {ServerEndpoint}");
            Debug.Log($"Connection Uptime: {ConnectionUptime:F1}s");
            Debug.Log($"Sent Packets: {SentPacketCount}");
            Debug.Log($"Received Packets: {ReceivedPacketCount}");
            Debug.Log($"Last Ping: {LastPingTime:F1}ms");
        }

        [ContextMenu("Debug: Send Test Ping")]
        private void DebugSendTestPing()
        {
            SendPing();
        }

        [ContextMenu("Debug: Force Disconnect")]
        private void DebugForceDisconnect()
        {
            DisconnectFromServer();
        }

        #endregion
    }
}