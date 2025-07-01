using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BarbarosKs.Player;
using BarbarosKs.Core;

namespace BarbarosKs.UI
{
    /// <summary>
    /// Saldırı butonunu ve otomatik ateş sistemini yönetir
    /// </summary>
    public class AttackButtonController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] public Button attackButton;
        [SerializeField] public TextMeshProUGUI buttonText;
        [SerializeField] public Image buttonIcon;
        
        [Header("Button Sprites")]
        [SerializeField] private Sprite canAttackSprite;
        [SerializeField] private Sprite attackingSprite;
        [SerializeField] private Sprite disabledSprite;
        
        [Header("Button Colors")]
        [SerializeField] private Color canAttackColor = Color.green;
        [SerializeField] private Color attackingColor = Color.red;
        [SerializeField] private Color disabledColor = Color.gray;
        
        [Header("Auto Attack Settings")]
        [SerializeField] private bool enableAutoAttack = true;
        
        // State Management
        public enum AttackButtonState
        {
            Disabled,    // Hedef yok veya menzil dışı
            CanAttack,   // Saldırabilir durumda
            Attacking    // Şu anda saldırıyor (cooldown)
        }
        
        private AttackButtonState currentState = AttackButtonState.Disabled;
        private PlayerController localPlayer;
        
        // Auto Attack System
        private bool isAutoAttacking = false;
        private float attackCooldown = 2f; // Varsayılan, sunucudan gelecek
        
        private void Awake()
        {
            // Buton click event'ini bağla
            if (attackButton != null)
            {
                attackButton.onClick.AddListener(OnAttackButtonClick);
            }
        }
        
        private void Start()
        {
            // Local player'ı bul
            PlayerController.OnLocalPlayerSpawned += OnLocalPlayerSpawned;
            
            // Başlangıçta disabled state
            SetButtonState(AttackButtonState.Disabled);
        }
        
        private void Update()
        {
            if (localPlayer == null) return;
            
            // ✅ PlayerController'dan güncel cooldown bilgisini al (artık sadece görsel için)
            attackCooldown = localPlayer.GetAttackCooldown();
            
            // Buton durumunu güncelle
            UpdateButtonState();
            
            // Otomatik ateş sistemi
            if (enableAutoAttack && isAutoAttacking)
            {
                ProcessAutoAttack();
            }
        }
        
        private void OnDestroy()
        {
            PlayerController.OnLocalPlayerSpawned -= OnLocalPlayerSpawned;
            if (attackButton != null)
            {
                attackButton.onClick.RemoveListener(OnAttackButtonClick);
            }
        }
        
        #region Event Handlers
        
        private void OnLocalPlayerSpawned(PlayerController player)
        {
            localPlayer = player;
            Debug.Log("🎮 [ATTACK BUTTON] Local player bulundu, buton aktif edildi");
        }
        
        private void OnAttackButtonClick()
        {
            if (localPlayer == null) return;
            
            switch (currentState)
            {
                case AttackButtonState.CanAttack:
                    StartAttacking();
                    break;
                    
                case AttackButtonState.Attacking:
                    StopAttacking();
                    break;
                    
                case AttackButtonState.Disabled:
                    Debug.Log("❌ [ATTACK BUTTON] Saldırı şu anda mümkün değil");
                    break;
            }
        }
        
        #endregion
        
        #region Attack System
        
        private void StartAttacking()
        {
            if (localPlayer == null || localPlayer.GetSelectedTarget() == null) return;
            
            Debug.Log("🔫 [ATTACK BUTTON] Otomatik saldırı başlatıldı!");
            isAutoAttacking = true;
            
            // İlk saldırıyı cooldown'a bakarak yap
            if (localPlayer.CanAttackNow())
            {
                PerformAttack();
            }
            else
            {
                float remaining = localPlayer.GetRemainingCooldown();
                Debug.Log($"⏳ [ATTACK BUTTON] Cooldown'da, {remaining:F1}s sonra ateş edilecek");
            }
        }
        
        private void StopAttacking()
        {
            Debug.Log("🛑 [ATTACK BUTTON] Otomatik saldırı durduruldu!");
            isAutoAttacking = false;
        }
        
        private void PerformAttack()
        {
            if (localPlayer == null) return;
            
            // PlayerController'ın ateş methodunu çağır
            var selectedTarget = localPlayer.GetSelectedTarget();
            if (selectedTarget != null)
            {
                // AttackButtonController için özel method'u çağır
                localPlayer.FireAtTarget();
                
                Debug.Log($"🚀 [ATTACK BUTTON] Ateş edildi! Hedef: {selectedTarget.name}");
            }
        }
        
        private void ProcessAutoAttack()
        {
            if (localPlayer == null) return;
            
            var selectedTarget = localPlayer.GetSelectedTarget();
            
            // Hedef kontrolü
            if (selectedTarget == null)
            {
                Debug.Log("🛑 [AUTO ATTACK] Hedef kayboldu, otomatik ateş durduruluyor");
                StopAttacking();
                return;
            }
            
            // Menzil kontrolü
            float distance = Vector3.Distance(localPlayer.transform.position, selectedTarget.transform.position);
            if (!GameSettings.Instance.IsWithinRange(distance))
            {
                Debug.Log("🛑 [AUTO ATTACK] Hedef menzil dışı, otomatik ateş durduruluyor");
                StopAttacking();
                return;
            }
            
            // ✅ PlayerController'ın cooldown kontrolünü kullan
            if (localPlayer.CanAttackNow())
            {
                PerformAttack();
            }
            else
            {
                float remaining = localPlayer.GetRemainingCooldown();
                Debug.Log($"⏳ [AUTO ATTACK] Cooldown'da, kalan: {remaining:F1}s");
            }
        }
        
        #endregion
        
        #region Button State Management
        
        private void UpdateButtonState()
        {
            if (localPlayer == null)
            {
                SetButtonState(AttackButtonState.Disabled);
                return;
            }
            
            var selectedTarget = localPlayer.GetSelectedTarget();
            
            // Hedef yok
            if (selectedTarget == null)
            {
                SetButtonState(AttackButtonState.Disabled);
                return;
            }
            
            // Menzil kontrolü
            float distance = Vector3.Distance(localPlayer.transform.position, selectedTarget.transform.position);
            if (!GameSettings.Instance.IsWithinRange(distance))
            {
                SetButtonState(AttackButtonState.Disabled);
                return;
            }
            
            // Otomatik ateş aktif mi?
            if (isAutoAttacking)
            {
                SetButtonState(AttackButtonState.Attacking);
                return;
            }
            
            // Saldırabilir durumda
            SetButtonState(AttackButtonState.CanAttack);
        }
        
        private void SetButtonState(AttackButtonState newState)
        {
            if (currentState == newState) return;
            
            currentState = newState;
            UpdateButtonVisuals();
        }
        
        private void UpdateButtonVisuals()
        {
            if (attackButton == null) return;
            
            switch (currentState)
            {
                case AttackButtonState.Disabled:
                    attackButton.interactable = false;
                    if (buttonIcon != null)
                    {
                        buttonIcon.sprite = disabledSprite;
                        buttonIcon.color = disabledColor;
                    }
                    if (buttonText != null)
                        buttonText.text = "Saldırı\nPasif";
                    break;
                    
                case AttackButtonState.CanAttack:
                    attackButton.interactable = true;
                    if (buttonIcon != null)
                    {
                        buttonIcon.sprite = canAttackSprite;
                        buttonIcon.color = canAttackColor;
                    }
                    if (buttonText != null)
                        buttonText.text = "Saldırı\nBaşlat";
                    break;
                    
                case AttackButtonState.Attacking:
                    attackButton.interactable = true;
                    if (buttonIcon != null)
                    {
                        buttonIcon.sprite = attackingSprite;
                        buttonIcon.color = attackingColor;
                    }
                    if (buttonText != null)
                    {
                        // ✅ PlayerController'dan kalan cooldown süresini al
                        float remainingTime = localPlayer != null ? localPlayer.GetRemainingCooldown() : 0f;
                        buttonText.text = $"Saldırıyor\n{remainingTime:F1}s";
                    }
                    break;
            }
            
            Debug.Log($"🎨 [ATTACK BUTTON] Durum değişti: {currentState}");
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Otomatik ateş sistemini açar/kapatır
        /// </summary>
        public void SetAutoAttackEnabled(bool enabled)
        {
            enableAutoAttack = enabled;
            if (!enabled && isAutoAttacking)
            {
                StopAttacking();
            }
            Debug.Log($"🔄 [ATTACK BUTTON] Otomatik ateş: {(enabled ? "Açık" : "Kapalı")}");
        }
        
        /// <summary>
        /// Otomatik ateş başlatır (Space tuşu için)
        /// </summary>
        public void StartAutoAttack()
        {
            if (!enableAutoAttack)
            {
                Debug.Log("⚠️ [ATTACK BUTTON] Otomatik ateş sistemi pasif!");
                return;
            }
            
            StartAttacking();
        }
        
        /// <summary>
        /// Otomatik ateş durdurur (Space tuşu için)
        /// </summary>
        public void StopAutoAttack()
        {
            StopAttacking();
        }
        
        /// <summary>
        /// Mevcut buton durumunu döndürür
        /// </summary>
        public AttackButtonState GetCurrentState()
        {
            return currentState;
        }
        
        /// <summary>
        /// Otomatik ateş aktif mi?
        /// </summary>
        public bool IsAutoAttacking()
        {
            return isAutoAttacking;
        }
        
        #endregion
    }
} 