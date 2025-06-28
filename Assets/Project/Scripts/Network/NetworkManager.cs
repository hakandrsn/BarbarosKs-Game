using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using BarbarosKs.core.DTOs;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine.SceneManagement;

namespace Project.Scripts.Network
{
    public class NetworkManager : MonoBehaviour
    {
        public static NetworkManager Instance { get; private set; }

        [Header("Ağ Ayarları")] [SerializeField]
        private string serverIP = "127.0.0.1";

        [SerializeField] private int serverPort = 9999;

        [Header("Oyuncu Ayarları")] [SerializeField]
        private string playerName = "Barbaros";

        private TcpClient _tcpClient;
        private NetworkStream _stream;
        private Thread _clientReceiveThread;
        private readonly Queue<string> _incomingMessages = new Queue<string>();
        private readonly object _messageLock = new object();

        public string LocalPlayerId { get; private set; }
        private string _playerNameAttempt; // Oyuna girerken kullandığımız ismi saklamak için

        // --- İSİMLER DÜZELTİLDİ ---
        public bool IsConnected { get; private set; }
        public int SentPacketCount { get; private set; } // SentMessageCount -> SentPacketCount
        public int ReceivedPacketCount { get; private set; } // ReceivedMessageCount -> ReceivedPacketCount
        public float ConnectionUptime => IsConnected ? Time.time - _connectionStartTime : 0f;
        public float LastPingTime { get; private set; }
        public string ServerEndpoint => $"{serverIP}:{serverPort}";
        private float _connectionStartTime;
        private readonly Dictionary<long, float> _pingTimestamps = new Dictionary<long, float>();
        // ---------------------------------------------

        public event Action OnConnectedToServer;
        public event Action OnDisconnectedFromServer;
        public event Action<Player> OnPlayerJoined;
        public event Action<string, Vector3> OnPlayerMoved;
        public event Action<List<Player>> OnWorldStateReceived;

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
        
        private void OnEnable()
        {
            // Sahne yüklendiğinde bu metot çalışır.
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        
        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void Start()
        {
          //  ConnectToServer();
        }

        private void Update()
        {
            ProcessMessageQueue();
        }
        
        public void ConnectToServer(string playerId, string playerName)
        {
            if (IsConnected) return;

            // Bu bilgileri daha sonra sunucuya göndermek için saklayabiliriz.
            this.LocalPlayerId = playerId; 
            this.playerName = playerName; // Sınıfa private string playerName; ekleyin.

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

        private void OnApplicationQuit()
        {
            DisconnectFromServer();
        }
        
        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Eğer yüklenen sahne oyun sahnesiyse ve bir kullanıcı giriş yapmışsa, sunucuya bağlan.
            if (scene.name != "GameScene" || GameManager.Instance.CurrentUser == null) return;
            Debug.Log("Oyun sahnesi yüklendi. NetworkManager başlatılıyor...");
        
            var pId = GameManager.Instance.CurrentPlayer.Id.ToString();
            var pName = GameManager.Instance.CurrentPlayer.Username;
            ConnectToServer(pId, pName);
        }

        #region Bağlantı ve Mesajlaşma

        public void ConnectToServer()
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

                // Bağlantı başarılı olduğunda, bu olayı ana thread'de tetiklemek için kuyruğa ekle.
                // Ana thread'de hem olayı tetikleyip hem de oyuna katılma isteği göndereceğiz.
                lock (_messageLock)
                {
                    _incomingMessages.Enqueue("INTERNAL_CONNECTED");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"OnConnectCallback hatası: {e.Message}");
            }
        }

        private void ReceiveMessages()
        {
            byte[] buffer = new byte[8192];
            StringBuilder messageBuilder = new StringBuilder();

            while (IsConnected)
            {
                try
                {
                    int bytesRead = _stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead <= 0)
                    {
                        lock (_messageLock)
                        {
                            _incomingMessages.Enqueue("INTERNAL_DISCONNECTED");
                        }

                        break;
                    }

                    string receivedData = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    messageBuilder.Append(receivedData);

                    string allData = messageBuilder.ToString();
                    int lastBracketIndex = -1;
                    int currentIndex = 0;

                    while (currentIndex < allData.Length)
                    {
                        int firstBracketIndex = allData.IndexOf('{', currentIndex);
                        if (firstBracketIndex == -1) break;

                        int braceCount = 1;
                        int endBracketIndex = firstBracketIndex + 1;
                        while (endBracketIndex < allData.Length && braceCount > 0)
                        {
                            if (allData[endBracketIndex] == '{') braceCount++;
                            else if (allData[endBracketIndex] == '}') braceCount--;
                            endBracketIndex++;
                        }

                        if (braceCount == 0)
                        {
                            string jsonMessage =
                                allData.Substring(firstBracketIndex, endBracketIndex - firstBracketIndex);
                            lock (_messageLock)
                            {
                                _incomingMessages.Enqueue(jsonMessage);
                            }

                            lastBracketIndex = endBracketIndex;
                            currentIndex = endBracketIndex;
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (lastBracketIndex > 0)
                    {
                        messageBuilder.Remove(0, lastBracketIndex);
                    }
                }
                catch
                {
                    lock (_messageLock)
                    {
                        _incomingMessages.Enqueue("INTERNAL_DISCONNECTED");
                    }

                    break;
                }
            }
        }

        private void ProcessMessageQueue()
        {
            if (_incomingMessages.Count == 0) return;

            lock (_messageLock)
            {
                while (_incomingMessages.Count > 0)
                {
                    string rawMessage = _incomingMessages.Dequeue();

                    if (rawMessage == "INTERNAL_CONNECTED")
                    {
                        _connectionStartTime = Time.time;
                        OnConnectedToServer?.Invoke();
                        SendJoinRequest(); // <<< YENİ SATIR: Oyuna katılma isteğini burada gönderiyoruz.
                        continue;
                    }

                    if (rawMessage == "INTERNAL_DISCONNECTED")
                    {
                        DisconnectFromServer();
                        continue;
                    }

                    ReceivedPacketCount++; // İSİM DÜZELTİLDİ

                    try
                    {
                        GameMessage gameMessage = JsonConvert.DeserializeObject<GameMessage>(rawMessage);
                        if (gameMessage == null) continue;

                        switch (gameMessage.Type)
                        {
                            case MessageType.Pong:
                                ProcessPong(Convert.ToInt64(gameMessage.Data));
                                break;
                            case MessageType.WorldState:
                                // Önce 'Data'yı bir JObject olarak al
                                JObject worldStateData = JObject.Parse(gameMessage.Data.ToString());
                                // Sonra içinden 'Players' JArray'ini çek
                                JArray playersArray = (JArray)worldStateData["Players"];
                                // Şimdi bu diziyi List<Player>'a çevir
                                List<Player> players = playersArray.ToObject<List<Player>>();
                                if (LocalPlayerId == null)
                                {
                                    Player self = players.Find(p => p.Name == _playerNameAttempt);
                                    if (self != null)
                                    {
                                        LocalPlayerId = self.Id;
                                        Debug.Log($"Yerel oyuncu ID'miz WorldState üzerinden atandı: {LocalPlayerId}");
                                    }
                                }
                                // ---------------------------

                                OnWorldStateReceived?.Invoke(players);
                                break;
                            case MessageType.PlayerJoined:
                                var joinedPlayer = JsonConvert.DeserializeObject<Player>(gameMessage.Data.ToString());

                                // --- YENİ EKLENEN MANTIK ---
                                // Eğer henüz bir ID'miz yoksa ve katılan oyuncunun ismi,
                                // bizim katılma isteği gönderdiğimiz isimle aynıysa, bu biziz demektir!
                                if (LocalPlayerId == null && joinedPlayer.Name == _playerNameAttempt)
                                {
                                    LocalPlayerId = joinedPlayer.Id;
                                    Debug.Log($"Yerel oyuncu ID'miz sunucu tarafından atandı: {LocalPlayerId}");
                                }
                                // ---------------------------

                                OnPlayerJoined?.Invoke(joinedPlayer);
                                break;
                            case MessageType.PlayerMoved:
                                var moveData = JObject.Parse(gameMessage.Data.ToString());
                                string playerId = moveData["PlayerId"].ToString();
                                Vector3 newPos = moveData["Position"].ToObject<Vector3>();
                                OnPlayerMoved?.Invoke(playerId, newPos);
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

        public void SendJoinRequest()
        {
            if (!IsConnected) return;

            var joinData = new PlayerJoinData { PlayerName = this.playerName };
            var message = new GameMessage
            {
                Type = MessageType.PlayerJoin,
                Data = joinData
            };
            SendMessage(message);
            Debug.Log(this.playerName + " ismiyle oyuna katılma isteği gönderildi.");
        }

        public void SendMessage(GameMessage message)
        {
            if (!IsConnected) return;
            try
            {
                string json = JsonConvert.SerializeObject(message);
                byte[] data = Encoding.UTF8.GetBytes(json);
                _stream.WriteAsync(data, 0, data.Length);
                SentPacketCount++; // İSİM DÜZELTİLDİ
            }
            catch (Exception e)
            {
                Debug.LogError($"Mesaj gönderme hatası: {e.Message}");
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
            Debug.Log("Sunucu bağlantısı kesildi.");
        }

        public void SendPing()
        {
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            _pingTimestamps[timestamp] = Time.time;
            SendMessage(new GameMessage { Type = MessageType.Ping, Data = timestamp });
        }

        private void ProcessPong(long timestamp)
        {
            if (_pingTimestamps.TryGetValue(timestamp, out float sendTime))
            {
                LastPingTime = (Time.time - sendTime) * 1000f;
                _pingTimestamps.Remove(timestamp);
            }
        }

        #endregion
    }
}