using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Project.Scripts.Network
{
    /// <summary>
    /// Ağ istatistiklerini görselleştiren bir bileşen.
    /// NetworkManager ile birlikte kullanılır.
    /// </summary>
    [RequireComponent(typeof(NetworkManager))]
    public class NetworkStats : MonoBehaviour
    {
        [Header("UI Referansları")]
        [SerializeField] private Text connectionStatusText;
        [SerializeField] private Text pingText;
        [SerializeField] private Text packetStatsText;
        [SerializeField] private Text uptimeText;
        [SerializeField] private Image connectionStatusImage;

        [Header("Ayarlar")]
        [SerializeField] private float updateInterval = 0.5f;
        [SerializeField] private Color connectedColor = Color.green;
        [SerializeField] private Color disconnectedColor = Color.red;
        [SerializeField] private bool sendAutomaticPings = true;
        [SerializeField] private float pingInterval = 2f;
        [SerializeField] private bool showDebugGUI = true;

        // NetworkManager referansı
        private NetworkManager networkManager;
        private float lastUpdateTime;
        private float lastPingTime;

        private void Start()
        {
            networkManager = GetComponent<NetworkManager>();

            if (networkManager == null)
            {
                Debug.LogError("NetworkStats bileşeni NetworkManager ile aynı GameObject üzerinde olmalıdır!");
                enabled = false;
                return;
            }
        }

        private void Update()
        {
            // İstatistikleri güncelle
            if (Time.time - lastUpdateTime >= updateInterval)
            {
                UpdateUIStats();
                lastUpdateTime = Time.time;
            }

            // Otomatik ping gönder
            if (sendAutomaticPings && networkManager.IsConnected && Time.time - lastPingTime >= pingInterval)
            {
                networkManager.SendPing();
                lastPingTime = Time.time;
            }
        }

        /// <summary>
        /// UI istatistiklerini günceller
        /// </summary>
        private void UpdateUIStats()
        {
            // Bağlantı durumu
            if (connectionStatusText != null)
            {
                connectionStatusText.text = networkManager.IsConnected ? "Bağlı" : "Bağlı Değil";
            }

            // Bağlantı durumu görselleştirme
            if (connectionStatusImage != null)
            {
                connectionStatusImage.color = networkManager.IsConnected ? connectedColor : disconnectedColor;
            }

            // Ping
            if (pingText != null)
            {
                pingText.text = $"Ping: {networkManager.LastPingTime:F1} ms";
            }

            // Paket istatistikleri
            if (packetStatsText != null)
            {
                packetStatsText.text = $"Gönderilen: {networkManager.SentPacketCount}\nAlınan: {networkManager.ReceivedPacketCount}";
            }

            // Çalışma süresi
            if (uptimeText != null)
            {
                if (networkManager.IsConnected)
                {
                    float uptime = networkManager.ConnectionUptime;
                    string formattedTime = FormatTime(uptime);
                    uptimeText.text = $"Çalışma Süresi: {formattedTime}";
                }
                else
                {
                    uptimeText.text = "Çalışma Süresi: --:--:--";
                }
            }
        }

        /// <summary>
        /// Saniye cinsinden süreyi formatlar (SS:DD:SS)
        /// </summary>
        private string FormatTime(float timeInSeconds)
        {
            int hours = Mathf.FloorToInt(timeInSeconds / 3600);
            int minutes = Mathf.FloorToInt((timeInSeconds % 3600) / 60);
            int seconds = Mathf.FloorToInt(timeInSeconds % 60);

            return string.Format("{0:00}:{1:00}:{2:00}", hours, minutes, seconds);
        }

        /// <summary>
        /// Temel ağ istatistiklerini göstermek için OnGUI
        /// </summary>
        private void OnGUI()
        {
            if (!showDebugGUI) return;

            GUILayout.BeginArea(new Rect(10, 10, 200, 120));
            GUILayout.BeginVertical("box");

            GUILayout.Label($"Bağlantı: {(networkManager.IsConnected ? "Aktif" : "Kapalı")}");
            GUILayout.Label($"Sunucu: {networkManager.ServerEndpoint}");
            GUILayout.Label($"Ping: {networkManager.LastPingTime:F1} ms");
            GUILayout.Label($"Paketler: +{networkManager.SentPacketCount} / -{networkManager.ReceivedPacketCount}");

            if (networkManager.IsConnected)
            {
                GUILayout.Label($"Süre: {FormatTime(networkManager.ConnectionUptime)}");
            }

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
    }
}
