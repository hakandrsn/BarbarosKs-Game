using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;
using BarbarosKs.Shared.DTOs.Game; // DİKKAT: Yeni ve doğru DTO namespace'i
using Newtonsoft.Json;

namespace Project.Scripts.Network
{
    public class NetworkManager : MonoBehaviour
    {
        public static NetworkManager Instance { get; private set; }

        [Header("Ağ Ayarları")] [SerializeField]
        private string serverIP = "127.0.0.1";

        [SerializeField] private int serverPort = 9999;

        private TcpClient _tcpClient;
        private NetworkStream _stream;
        private Thread _clientReceiveThread;
        private readonly Queue<string> _incomingMessages = new Queue<string>();
        private readonly object _messageLock = new object();

        public bool IsConnected { get; private set; }
        public int SentPacketCount { get; private set; }
        public int ReceivedPacketCount { get; private set; }
        public float ConnectionUptime => IsConnected ? Time.time - _connectionStartTime : 0f;
        public float LastPingTime { get; private set; }
        public string ServerEndpoint => $"{serverIP}:{serverPort}";

        private float _connectionStartTime;
        private readonly Dictionary<long, float> _pingTimestamps = new Dictionary<long, float>();


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

        #endregion

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
        private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

        private void Update()
        {
            ProcessMessageQueue();
        }

        private void OnApplicationQuit()
        {
            DisconnectFromServer();
        }

        /// <summary>
        /// "GameScene" yüklendiğinde oyun sunucusuna bağlanma sürecini başlatır.
        /// </summary>
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == "GameScene" && GameManager.Instance?.ActiveShip != null)
            {
                Debug.Log("Oyun sahnesi yüklendi. NetworkManager oyun sunucusuna bağlanıyor...");
                ConnectToGameServer();
            }
        }

        #region Bağlantı ve Temel İletişim

        private void ConnectToGameServer()
        {
            if (IsConnected) return;
            try
            {
                _clientReceiveThread = new Thread(ReceiveMessages) { IsBackground = true };
                _tcpClient = new TcpClient();
                _tcpClient.BeginConnect(serverIP, serverPort, OnConnectCallback, null);
            }
            catch (Exception e)
            {
                Debug.LogError($"Bağlantı hatası: {e.Message}");
            }
        }

        private void OnConnectCallback(IAsyncResult ar)
        {
            try
            {
                _tcpClient.EndConnect(ar);
                if (!_tcpClient.Connected) return;

                _stream = _tcpClient.GetStream();
                _clientReceiveThread.Start();
                IsConnected = true;
                lock (_messageLock)
                {
                    _incomingMessages.Enqueue(JsonConvert.SerializeObject(new GameMessage
                        { Type = (MessageType)(-1) }));
                } // Özel içsel mesaj
            }
            catch (Exception e)
            {
                Debug.LogError($"OnConnectCallback hatası: {e.Message}");
            }
        }

        private void ReceiveMessages()
        {
            // ... Bu metodun içeriği aynı kalabilir (byte okuma ve kuyruğa ekleme) ...
        }

        public void DisconnectFromServer()
        {
            if (!IsConnected) return;
            IsConnected = false;
            _stream?.Close();
            _tcpClient?.Close();
            _clientReceiveThread?.Abort();
            OnDisconnectedFromServer?.Invoke();
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
                        if (gameMessage == null) continue;

                        // İçsel "Bağlantı Başarılı" mesajı
                        if ((int)gameMessage.Type == -1)
                        {
                            OnConnectedToServer?.Invoke();
                            SendJoinRequest();
                            continue;
                        }

                        // Gelen mesajın tipine göre doğru DTO'ya çevir ve ilgili olayı fırlat.
                        switch (gameMessage.Type)
                        {
                            case MessageType.S2C_WorldState:
                                var worldState =
                                    JsonConvert.DeserializeObject<S2C_WorldStateData>(gameMessage.DataJson);
                                if (worldState != null) OnWorldStateReceived?.Invoke(worldState);
                                break;
                            case MessageType.S2C_EntitySpawn:
                                var spawnData =
                                    JsonConvert.DeserializeObject<S2C_EntitySpawnData>(gameMessage.DataJson);
                                if (spawnData != null) OnEntitySpawned?.Invoke(spawnData);
                                break;
                            case MessageType.S2C_EntityDespawn:
                                var despawnData =
                                    JsonConvert.DeserializeObject<S2C_EntityDespawnData>(gameMessage.DataJson);
                                if (despawnData != null) OnEntityDespawned?.Invoke(despawnData);
                                break;
                            case MessageType.S2C_TransformUpdate:
                                var transformData =
                                    JsonConvert.DeserializeObject<S2C_TransformUpdateData>(gameMessage.DataJson);
                                if (transformData != null) OnTransformUpdate?.Invoke(transformData);
                                break;
                            case MessageType.S2C_HealthUpdate:
                                var healthData =
                                    JsonConvert.DeserializeObject<S2C_HealthUpdateData>(gameMessage.DataJson);
                                if (healthData != null) OnHealthUpdate?.Invoke(healthData);
                                break;
                            case MessageType.S2C_Pong:
                                var timestamp = JsonConvert.DeserializeObject<long>(gameMessage.DataJson);
                                ProcessPong(timestamp);
                                break;
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Mesaj işleme hatası: {e.Message} | Gelen Veri: {rawMessage}");
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
                string json = JsonConvert.SerializeObject(message);
                byte[] data = Encoding.UTF8.GetBytes(json);
                _stream.WriteAsync(data, 0, data.Length);
            }
            catch (Exception e)
            {
                Debug.LogError($"Mesaj gönderme hatası: {e.Message}");
            }
        }

        #endregion

        #region Public Metotlar (İstemcinin Diğer Kısımlarından Çağrılacak)

        /// <summary>
        /// Oyuna katılma isteğini, GameManager'dan aldığı güncel bilgilerle gönderir.
        /// </summary>
        public void SendJoinRequest()
        {
            if (GameManager.Instance?.ActiveShip == null) return;

            var joinData = new C2S_JoinGameData
            {
                SelectedShipId = GameManager.Instance.ActiveShip.Id
            };

            var message = new GameMessage
            {
                Type = MessageType.C2S_JoinGame,
                DataJson = JsonConvert.SerializeObject(joinData)
            };

            SendMessage(message);
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
        /// YENİ EKLENEN METOT: Oyuncunun bir aksiyon gerçekleştirdiğini sunucuya bildirir.
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
    }
}