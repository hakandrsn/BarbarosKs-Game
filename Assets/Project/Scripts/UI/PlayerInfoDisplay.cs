using System;
using UnityEngine;
using TMPro;
using BarbarosKs.Shared.DTOs;
using BarbarosKs.Core;

namespace BarbarosKs.UI
{
    /// <summary>
    /// PlayerManager'dan veri çekerek oyuncu bilgilerini gösteren UI component'i
    /// </summary>
    public class PlayerInfoDisplay : MonoBehaviour
    {
        [Header("UI Referansları")]
        [SerializeField] private TextMeshProUGUI playerNameText;
        [SerializeField] private TextMeshProUGUI activeShipNameText;
        [SerializeField] private TextMeshProUGUI shipLevelText;
        [SerializeField] private TextMeshProUGUI shipHealthText;
        [SerializeField] private TextMeshProUGUI playerIdText;
        [SerializeField] private TextMeshProUGUI shipCountText;
        
        [Header("Debug")]
        [SerializeField] private bool autoUpdate = true;
        [SerializeField] private float updateInterval = 1f;
        [SerializeField] private bool verboseLogging = false;

        private void Start()
        {
            // Event'leri dinle
            PlayerManager.OnPlayerDataLoaded += OnPlayerDataLoaded;
            PlayerManager.OnActiveShipChanged += OnActiveShipChanged;
            
            // Eğer veri zaten yüklüyse, hemen güncelle
            UpdateUI();
            
            // Otomatik güncelleme
            if (autoUpdate)
            {
                InvokeRepeating(nameof(UpdateUI), updateInterval, updateInterval);
            }
            
            DebugLog("PlayerInfoDisplay başlatıldı");
        }

        private void OnDestroy()
        {
            // Event'leri temizle
            PlayerManager.OnPlayerDataLoaded -= OnPlayerDataLoaded;
            PlayerManager.OnActiveShipChanged -= OnActiveShipChanged;
            
            // Otomatik güncellemeyi durdur
            if (autoUpdate)
            {
                CancelInvoke(nameof(UpdateUI));
            }
        }

        private void OnPlayerDataLoaded(PlayerProfileDto playerProfile)
        {
            DebugLog("Player data yüklendi, UI güncelleniyor");
            UpdateUI();
        }

        private void OnActiveShipChanged(ShipSummaryDto activeShip)
        {
            DebugLog($"Active ship değişti: {activeShip?.Name ?? "NULL"}, UI güncelleniyor");
            UpdateUI();
        }

        [ContextMenu("Update UI")]
        private void UpdateUI()
        {
            if (PlayerManager.Instance == null) 
            {
                DebugLog("PlayerManager bulunamadı - UI temizleniyor");
                ClearUI();
                return;
            }

            // Player bilgileri
            if (playerNameText != null)
            {
                string playerName = PlayerManager.Instance.HasPlayerData ? 
                    PlayerManager.Instance.PlayerProfile.Username : "No Player";
                playerNameText.text = playerName;
            }

            // Player ID bilgisi
            if (playerIdText != null)
            {
                string playerId = PlayerManager.Instance.GetPlayerId()?.ToString() ?? "No ID";
                playerIdText.text = $"ID: {playerId.Substring(0, Math.Min(8, playerId.Length))}...";
            }

            // Ship count
            if (shipCountText != null)
            {
                shipCountText.text = $"Gemiler: {PlayerManager.Instance.ShipCount}";
            }

            // Active Ship bilgileri
            if (activeShipNameText != null)
            {
                string shipName = PlayerManager.Instance.HasActiveShip ? 
                    PlayerManager.Instance.ActiveShip.Name : "No Ship Selected";
                activeShipNameText.text = shipName;
            }

            if (shipLevelText != null)
            {
                if (PlayerManager.Instance.HasActiveShip)
                {
                    shipLevelText.text = $"Level {PlayerManager.Instance.ActiveShip.Level}";
                }
                else
                {
                    shipLevelText.text = "Level --";
                }
            }

            // Ship health bilgileri
            if (shipHealthText != null)
            {
                if (PlayerManager.Instance.HasActiveShip)
                {
                    var ship = PlayerManager.Instance.ActiveShip;
                    float percentage = ship.MaxHull > 0 ? (float)ship.CurrentHull / ship.MaxHull * 100f : 0f;
                    shipHealthText.text = $"HP: {ship.CurrentHull}/{ship.MaxHull} ({percentage:F1}%)";
                }
                else
                {
                    shipHealthText.text = "HP: --/--";
                }
            }

            DebugLog("UI güncellendi");
        }

        private void ClearUI()
        {
            if (playerNameText != null) playerNameText.text = "No Player";
            if (playerIdText != null) playerIdText.text = "ID: --";
            if (shipCountText != null) shipCountText.text = "Gemiler: 0";
            if (activeShipNameText != null) activeShipNameText.text = "No Ship Selected";
            if (shipLevelText != null) shipLevelText.text = "Level --";
            if (shipHealthText != null) shipHealthText.text = "HP: --/--";
            
            DebugLog("UI temizlendi");
        }

        /// <summary>
        /// Health'i manuel güncelleme için public method
        /// </summary>
        public void UpdateHealthDisplay(int currentHealth, int maxHealth)
        {
            if (shipHealthText == null) return;
            
            float percentage = maxHealth > 0 ? (float)currentHealth / maxHealth * 100f : 0f;
            shipHealthText.text = $"HP: {currentHealth}/{maxHealth} ({percentage:F1}%)";
            
            DebugLog($"Health display manuel güncellendi: {currentHealth}/{maxHealth}");
        }

        /// <summary>
        /// Oyun durumunu göstermek için
        /// </summary>
        public void UpdateGameStatus(string status)
        {
            // Bu metod gelecekte bir status text için kullanılabilir
            DebugLog($"Game status: {status}");
        }

        private void DebugLog(string message)
        {
            if (verboseLogging)
            {
                Debug.Log($"[PlayerInfoDisplay] {message}");
            }
        }

        #region Debug Methods

        [ContextMenu("Debug: Force Update")]
        private void DebugForceUpdate()
        {
            DebugLog("Manuel UI güncellemesi tetiklendi");
            UpdateUI();
        }

        [ContextMenu("Debug: Clear UI")]
        private void DebugClearUI()
        {
            DebugLog("Manuel UI temizleme tetiklendi");
            ClearUI();
        }

        [ContextMenu("Debug: Show Player Manager Status")]
        private void DebugShowPlayerManagerStatus()
        {
            if (PlayerManager.Instance == null)
            {
                Debug.Log("❌ PlayerManager yok");
                return;
            }

            Debug.Log("=== PLAYER MANAGER STATUS ===");
            Debug.Log($"Has Player Data: {PlayerManager.Instance.HasPlayerData}");
            Debug.Log($"Has Active Ship: {PlayerManager.Instance.HasActiveShip}");
            Debug.Log($"Ship Count: {PlayerManager.Instance.ShipCount}");
            Debug.Log($"Is In Game: {PlayerManager.Instance.IsInGame}");
            
            if (PlayerManager.Instance.HasPlayerData)
            {
                Debug.Log($"Player: {PlayerManager.Instance.PlayerProfile.Username}");
                Debug.Log($"Player ID: {PlayerManager.Instance.GetPlayerId()}");
            }
            
            if (PlayerManager.Instance.HasActiveShip)
            {
                var ship = PlayerManager.Instance.ActiveShip;
                Debug.Log($"Active Ship: {ship.Name} (Level {ship.Level})");
                Debug.Log($"Ship Health: {ship.CurrentHull}/{ship.MaxHull}");
            }
        }

        [ContextMenu("Debug: Toggle Auto Update")]
        private void DebugToggleAutoUpdate()
        {
            autoUpdate = !autoUpdate;
            
            if (autoUpdate)
            {
                InvokeRepeating(nameof(UpdateUI), 0f, updateInterval);
                DebugLog("Auto update enabled");
            }
            else
            {
                CancelInvoke(nameof(UpdateUI));
                DebugLog("Auto update disabled");
            }
        }

        #endregion
    }
} 