using UnityEngine;
using Project.Scripts.Network;

public class DebugHelper : MonoBehaviour
{
    [Header("Debug Kontrolleri")]
    [SerializeField] private bool autoCheckOnStart = true;
    [SerializeField] private float autoCheckInterval = 5f; // 5 saniyede bir kontrol et
    
    private void Start()
    {
        if (autoCheckOnStart)
        {
            Invoke(nameof(CheckGameState), 1f); // 1 saniye sonra kontrol et
            InvokeRepeating(nameof(CheckNetworkStatus), 2f, autoCheckInterval); // Periyodik kontrol
        }
    }
    
    [ContextMenu("Oyun Durumunu Kontrol Et")]
    public void CheckGameState()
    {
        Debug.Log("=== OYUN DURUMU KONTROLÜ ===");
        
        // GameManager Kontrolü
        if (GameManager.Instance == null)
        {
            Debug.LogError("❌ GameManager.Instance NULL!");
        }
        else
        {
            Debug.Log("✅ GameManager mevcut");
            
            if (GameManager.Instance.CharacterData == null)
            {
                Debug.LogError("❌ CharacterData NULL - Login yapılmamış olabilir");
            }
            else
            {
                Debug.Log($"✅ Player: {GameManager.Instance.CharacterData.PlayerProfile.Username}");
                Debug.Log($"✅ Gemiler: {GameManager.Instance.CharacterData.Ships.Count} adet");
            }
            
            if (GameManager.Instance.ActiveShip == null)
            {
                Debug.LogError("❌ ActiveShip NULL - Gemi seçimi yapılmamış!");
            }
            else
            {
                Debug.Log($"✅ ActiveShip: {GameManager.Instance.ActiveShip.Name} (ID: {GameManager.Instance.ActiveShip.Id})");
            }
        }
        
        // ApiManager Kontrolü
        if (ApiManager.Instance == null)
        {
            Debug.LogError("❌ ApiManager.Instance NULL!");
        }
        else
        {
            string token = ApiManager.Instance.GetAuthToken();
            if (string.IsNullOrEmpty(token))
            {
                Debug.LogError("❌ JWT Token NULL/Boş - Login yapılmamış!");
            }
            else
            {
                Debug.Log($"✅ JWT Token mevcut ({token.Length} karakter)");
            }
        }
        
        // NetworkManager Kontrolü
        if (NetworkManager.Instance == null)
        {
            Debug.LogError("❌ NetworkManager.Instance NULL!");
        }
        else
        {
            Debug.Log($"✅ NetworkManager mevcut - Bağlantı: {(NetworkManager.Instance.IsConnected ? "BAĞLI" : "BAĞLI DEĞİL")}");
            Debug.Log($"🔍 Gönderilen paket: {NetworkManager.Instance.SentPacketCount}");
            Debug.Log($"🔍 Alınan paket: {NetworkManager.Instance.ReceivedPacketCount}");
            Debug.Log($"🔍 Bağlantı süresi: {NetworkManager.Instance.ConnectionUptime:F1}s");
            Debug.Log($"🔍 Son ping: {NetworkManager.Instance.LastPingTime:F1}ms");
        }
        
        // NetworkObjectSpawner Kontrolü
        if (NetworkObjectSpawner.Instance == null)
        {
            Debug.LogError("❌ NetworkObjectSpawner.Instance NULL!");
        }
        else
        {
            Debug.Log("✅ NetworkObjectSpawner mevcut");
        }
        
        // Sahne Kontrolü
        string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        Debug.Log($"📍 Mevcut Sahne: {currentScene}");
        
        Debug.Log("=== KONTROL TAMAMLANDI ===");
    }
    
    [ContextMenu("Network Durumunu Kontrol Et")]
    public void CheckNetworkStatus()
    {
        if (NetworkManager.Instance == null) return;
        
        Debug.Log("=== NETWORK DURUMU ===");
        Debug.Log($"Bağlantı: {(NetworkManager.Instance.IsConnected ? "✅ BAĞLI" : "❌ BAĞLI DEĞİL")}");
        Debug.Log($"Endpoint: {NetworkManager.Instance.ServerEndpoint}");
        Debug.Log($"Gönderilen: {NetworkManager.Instance.SentPacketCount} paket");
        Debug.Log($"Alınan: {NetworkManager.Instance.ReceivedPacketCount} paket");
        
        if (NetworkManager.Instance.IsConnected)
        {
            Debug.Log($"⏰ Bağlantı süresi: {NetworkManager.Instance.ConnectionUptime:F1}s");
            if (NetworkManager.Instance.LastPingTime > 0)
            {
                Debug.Log($"📶 Ping: {NetworkManager.Instance.LastPingTime:F1}ms");
            }
        }
    }
    
    [ContextMenu("Manuel Join Request Gönder")]
    public void ManualSendJoinRequest()
    {
        Debug.Log("🔍 Manuel join request gönderiliyor...");
        if (NetworkManager.Instance != null && NetworkManager.Instance.IsConnected)
        {
            NetworkManager.Instance.SendJoinRequest();
        }
        else
        {
            Debug.LogError("❌ NetworkManager bağlı değil!");
        }
    }
    
    [ContextMenu("Ping Gönder")]
    public void SendPing()
    {
        Debug.Log("🔍 Ping gönderiliyor...");
        if (NetworkManager.Instance != null && NetworkManager.Instance.IsConnected)
        {
            NetworkManager.Instance.SendPing();
        }
        else
        {
            Debug.LogError("❌ NetworkManager bağlı değil!");
        }
    }
    
    [ContextMenu("Sahneyi Yeniden Yükle")]
    public void ReloadScene()
    {
        Debug.Log("🔄 Sahne yeniden yükleniyor...");
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }
    
    [ContextMenu("Gemi Spawn Et (Test)")]
    public void TestSpawnShip()
    {
        if (GameManager.Instance?.ActiveShip != null)
        {
            Debug.Log($"Test için gemi spawn ediliyor: {GameManager.Instance.ActiveShip.Name}");
            // Burada test spawn logic'i eklenebilir
        }
        else
        {
            Debug.LogError("ActiveShip NULL - Test spawn edilemedi!");
        }
    }
    
    [ContextMenu("Sunucuya Bağlan (Test)")]
    public void TestConnect()
    {
        if (NetworkManager.Instance != null)
        {
            Debug.Log("Test bağlantı deneniyor...");
            // NetworkManager'ın ConnectToGameServer metodunu çağırabiliriz
            var connectMethod = typeof(NetworkManager).GetMethod("ConnectToGameServer", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            connectMethod?.Invoke(NetworkManager.Instance, null);
        }
    }
} 