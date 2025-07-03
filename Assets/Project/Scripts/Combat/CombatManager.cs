using System;
using System.Collections.Generic;
using UnityEngine;
using BarbarosKs.Core;
using BarbarosKs.Shared.DTOs;
using BarbarosKs.Testing;

namespace BarbarosKs.Combat
{
    /// <summary>
    /// Tüm combat işlemlerini merkezi olarak yöneten sistem
    /// </summary>
    public class CombatManager : MonoBehaviour
    {
        public static CombatManager Instance { get; private set; }

        [Header("Combat Settings")]
        [SerializeField] private bool allowFriendlyFire = false;
        [SerializeField] private float combatRange = 15f;
        [SerializeField] private float autoTargetRange = 10f;

        [Header("Targeting")]
        [SerializeField] private LayerMask targetLayers = -1;
        [SerializeField] private bool autoTargeting = true;

        [Header("Debug")]
        [SerializeField] private bool verboseLogging = true;
        [SerializeField] private bool showGizmos = true;

        // Combat state
        private Transform currentTarget;
        private List<Transform> availableTargets = new List<Transform>();
        private float lastTargetScanTime;
        private float targetScanInterval = 1f;

        // Events
        public static event Action<Transform> OnTargetChanged;
        public static event Action<Transform> OnTargetLost;
        public static event Action<GameObject> OnProjectileFired;
        public static event Action<float> OnDamageDealt;
        public static event Action<float> OnDamageReceived;

        // Properties
        public Transform CurrentTarget => currentTarget;
        public List<Transform> AvailableTargets => availableTargets;
        public bool HasTarget => currentTarget != null;
        public float CombatRange => combatRange;
        public bool IsInCombatRange => HasTarget && Vector3.Distance(transform.position, currentTarget.position) <= combatRange;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                DebugLog("✅ CombatManager initialized");
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        private void Start()
        {
            // GameStateManager event'lerini dinle
            if (GameStateManager.Instance != null)
            {
                GameStateManager.OnGameStateChanged += OnGameStateChanged;
            }
        }

        private void Update()
        {
            // Sadece oyun içindeyken çalış
            if (GameStateManager.Instance != null && !GameStateManager.Instance.IsInGame)
                return;

            // Periyodik target tarama
            if (autoTargeting && Time.time - lastTargetScanTime >= targetScanInterval)
            {
                ScanForTargets();
                lastTargetScanTime = Time.time;
            }

            // Mevcut target kontrolü
            if (HasTarget)
            {
                ValidateCurrentTarget();
            }
        }

        private void OnDestroy()
        {
            if (GameStateManager.Instance != null)
            {
                GameStateManager.OnGameStateChanged -= OnGameStateChanged;
            }
        }

        #region Target Management

        /// <summary>
        /// Hedef taraması yapar
        /// </summary>
        private void ScanForTargets()
        {
            availableTargets.Clear();

            // Player pozisyonunu al
            Vector3 playerPosition = GetPlayerPosition();
            
            // Çevreden potansiyel hedefleri bul
            Collider[] nearbyColliders = Physics.OverlapSphere(playerPosition, autoTargetRange, targetLayers);
            
            foreach (var collider in nearbyColliders)
            {
                // Kendisi değilse ve düşman ise
                if (IsValidTarget(collider.transform))
                {
                    availableTargets.Add(collider.transform);
                }
            }

            DebugLog($"📡 Target scan - {availableTargets.Count} hedef bulundu");

            // Otomatik hedef seçimi
            if (!HasTarget && availableTargets.Count > 0)
            {
                SetTarget(GetClosestTarget());
            }
        }

        /// <summary>
        /// Geçerli hedef olup olmadığını kontrol eder
        /// </summary>
        private bool IsValidTarget(Transform target)
        {
            if (target == null) return false;

            // Kendisi mi?
            if (target == transform) return false;

            // Player ship'i mi?
            var playerController = target.GetComponent<Player.PlayerController>();
            if (playerController != null)
            {
                // Friendly fire kontrolü
                if (!allowFriendlyFire && IsAlly(playerController))
                {
                    return false;
                }
                return true;
            }

            // Enemy mi?
            var enemy = target.GetComponent<TestEnemy>();
            if (enemy != null) return true;

            // IDamageable interface'i var mı?
            var damageable = target.GetComponent<Project.Scripts.Interfaces.IDamageable>();
            if (damageable != null) return true;

            return false;
        }

        /// <summary>
        /// En yakın hedefi döndürür
        /// </summary>
        private Transform GetClosestTarget()
        {
            if (availableTargets.Count == 0) return null;

            Vector3 playerPosition = GetPlayerPosition();
            Transform closestTarget = null;
            float closestDistance = float.MaxValue;

            foreach (var target in availableTargets)
            {
                if (target == null) continue;

                float distance = Vector3.Distance(playerPosition, target.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestTarget = target;
                }
            }

            return closestTarget;
        }

        /// <summary>
        /// Hedef ayarlar
        /// </summary>
        public void SetTarget(Transform target)
        {
            if (currentTarget == target) return;

            var previousTarget = currentTarget;
            currentTarget = target;

            if (currentTarget != null)
            {
                DebugLog($"🎯 Hedef seçildi: {currentTarget.name}");
                OnTargetChanged?.Invoke(currentTarget);
            }
            else
            {
                DebugLog("❌ Hedef kaldırıldı");
                OnTargetLost?.Invoke(previousTarget);
            }
        }

        /// <summary>
        /// Mevcut hedefin geçerliliğini kontrol eder
        /// </summary>
        private void ValidateCurrentTarget()
        {
            if (currentTarget == null)
            {
                SetTarget(null);
                return;
            }

            // Hedef hala aktif mi?
            if (!currentTarget.gameObject.activeInHierarchy)
            {
                DebugLog("⚠️ Hedef pasif oldu");
                SetTarget(null);
                return;
            }

            // Hedef menzil içinde mi?
            Vector3 playerPosition = GetPlayerPosition();
            float distance = Vector3.Distance(playerPosition, currentTarget.position);
            
            if (distance > autoTargetRange * 1.5f) // %50 buffer
            {
                DebugLog("⚠️ Hedef menzil dışında");
                SetTarget(null);
                return;
            }
        }

        /// <summary>
        /// Hedefi temizler
        /// </summary>
        public void ClearTarget()
        {
            SetTarget(null);
        }

        #endregion

        #region Combat Actions

        /// <summary>
        /// Gülle fırlatır (Ana metod)
        /// </summary>
        public bool FireProjectile(CannonballTypeDto cannonballData = null)
        {
            if (!HasTarget)
            {
                DebugLog("❌ Hedef yok - gülle fırlatılamıyor");
                return false;
            }

            if (!IsInCombatRange)
            {
                DebugLog("❌ Hedef menzil dışında - gülle fırlatılamıyor");
                return false;
            }

            Vector3 firePosition = GetFirePosition();
            
            GameObject projectile = null;

            // CannonballTypeDto ile fırlatma
            if (cannonballData != null)
            {
                projectile = ProjectileManager.Instance?.SpawnProjectile(cannonballData, firePosition, currentTarget);
            }
            else
            {
                // Varsayılan gülle ile fırlatma
                projectile = ProjectileManager.Instance?.SpawnProjectile("CB1", firePosition, currentTarget, 10);
            }

            if (projectile != null)
            {
                DebugLog($"🚀 Gülle fırlatıldı: {cannonballData?.Name ?? "CB1"} → {currentTarget.name}");
                OnProjectileFired?.Invoke(projectile);
                return true;
            }
            else
            {
                Debug.LogError("❌ Gülle fırlatma başarısız!");
                return false;
            }
        }

        /// <summary>
        /// String ile gülle fırlatır
        /// </summary>
        public bool FireProjectile(string projectileType, int damage = 10)
        {
            if (!HasTarget) return false;
            if (!IsInCombatRange) return false;

            Vector3 firePosition = GetFirePosition();
            var projectile = ProjectileManager.Instance?.SpawnProjectile(projectileType, firePosition, currentTarget, damage);

            if (projectile != null)
            {
                DebugLog($"🚀 Gülle fırlatıldı: {projectileType} → {currentTarget.name}");
                OnProjectileFired?.Invoke(projectile);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Aktif cannonball ile fırlatır
        /// </summary>
        public bool FireActiveCannonball()
        {
            // GameDataManager'dan aktif cannonball'ı al
            if (GameDataManager.Instance?.ActiveCannonballs != null && GameDataManager.Instance.ActiveCannonballs.Count > 0)
            {
                var activeCannonball = GameDataManager.Instance.ActiveCannonballs[0]; // İlk aktif cannonball
                return FireProjectile(activeCannonball);
            }
            else
            {
                // Fallback - varsayılan gülle
                return FireProjectile("CB1", 10);
            }
        }

        /// <summary>
        /// Hasar verir
        /// </summary>
        public void DealDamage(float damage, Transform target = null)
        {
            Transform damageTarget = target ?? currentTarget;
            
            if (damageTarget == null)
            {
                Debug.LogWarning("⚠️ Hasar verme hedefi bulunamadı");
                return;
            }

            // IDamageable interface'i varsa hasar ver
            var damageable = damageTarget.GetComponent<Project.Scripts.Interfaces.IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage((int)damage);
                DebugLog($"💥 {damage} hasar verildi: {damageTarget.name}");
                OnDamageDealt?.Invoke(damage);
            }
        }

        /// <summary>
        /// Hasar alır
        /// </summary>
        public void TakeDamage(float damage)
        {
            DebugLog($"💔 {damage} hasar alındı");
            OnDamageReceived?.Invoke(damage);

            // Player health'i güncelle
            if (PlayerManager.Instance != null)
            {
                float newHealth = PlayerManager.Instance.LastKnownHealth - damage;
                PlayerManager.Instance.UpdateHealth(Mathf.Max(0, newHealth));
            }
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Player pozisyonunu döndürür
        /// </summary>
        private Vector3 GetPlayerPosition()
        {
            // PlayerController'ı bul
            var playerController = FindObjectOfType<Player.PlayerController>();
            if (playerController != null)
            {
                return playerController.transform.position;
            }

            // PlayerManager'dan son bilinen pozisyon
            if (PlayerManager.Instance != null)
            {
                return PlayerManager.Instance.LastKnownPosition;
            }

            // Fallback
            return transform.position;
        }

        /// <summary>
        /// Gülle fırlatma pozisyonunu döndürür
        /// </summary>
        private Vector3 GetFirePosition()
        {
            Vector3 playerPos = GetPlayerPosition();
            return playerPos + Vector3.up * 1f; // Biraz yukarıdan fırlat
        }

        /// <summary>
        /// Müttefik mi kontrol eder
        /// </summary>
        private bool IsAlly(Player.PlayerController playerController)
        {
            // Şu an için tüm player'lar müttefik
            // İleride team sistemi eklenebilir
            return true;
        }

        #endregion

        #region Event Handlers

        private void OnGameStateChanged(GameState previousState, GameState newState)
        {
            switch (newState)
            {
                case GameState.InGame:
                    DebugLog("🎮 Combat sistem aktif");
                    enabled = true;
                    break;
                case GameState.Paused:
                    DebugLog("⏸️ Combat sistem duraklatıldı");
                    break;
                default:
                    DebugLog("🚪 Combat sistem pasif");
                    enabled = false;
                    ClearTarget();
                    break;
            }
        }

        #endregion

        #region Debug Methods

        private void DebugLog(string message)
        {
            if (verboseLogging)
            {
                Debug.Log($"[CombatManager] {message}");
            }
        }

        [ContextMenu("Debug: Show Combat Info")]
        private void DebugShowCombatInfo()
        {
            Debug.Log("=== COMBAT INFO ===");
            Debug.Log($"Current Target: {(currentTarget != null ? currentTarget.name : "None")}");
            Debug.Log($"Available Targets: {availableTargets.Count}");
            Debug.Log($"Is In Combat Range: {IsInCombatRange}");
            Debug.Log($"Combat Range: {combatRange}");
            Debug.Log($"Auto Target Range: {autoTargetRange}");
            Debug.Log($"Allow Friendly Fire: {allowFriendlyFire}");
        }

        [ContextMenu("Debug: Fire Test Projectile")]
        private void DebugFireTestProjectile()
        {
            FireProjectile("CB1", 15);
        }

        [ContextMenu("Debug: Scan For Targets")]
        private void DebugScanForTargets()
        {
            ScanForTargets();
            DebugShowCombatInfo();
        }

        private void OnDrawGizmos()
        {
            if (!showGizmos) return;

            Vector3 playerPos = GetPlayerPosition();

            // Combat range
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(playerPos, combatRange);

            // Auto target range
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(playerPos, autoTargetRange);

            // Current target
            if (HasTarget)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(playerPos, currentTarget.position);
                Gizmos.DrawWireSphere(currentTarget.position, 1f);
            }

            // Available targets
            Gizmos.color = Color.cyan;
            foreach (var target in availableTargets)
            {
                if (target != null && target != currentTarget)
                {
                    Gizmos.DrawWireSphere(target.position, 0.5f);
                }
            }
        }

        #endregion
    }
} 