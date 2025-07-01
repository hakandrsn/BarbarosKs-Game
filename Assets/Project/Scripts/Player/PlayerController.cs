using System;
using System.Collections;
using BarbarosKs.Shared.DTOs;
using BarbarosKs.Combat;
using Project.Scripts.Interfaces;
using Project.Scripts.Network;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using Newtonsoft.Json;

// Sadece yeni ve doğru DTO namespace'i

namespace BarbarosKs.Player
{
    [RequireComponent(typeof(Rigidbody), typeof(PlayerInput), typeof(NavMeshAgent))]
    public class PlayerController : MonoBehaviour, IDamageable
    {
        [Header("Hareket Ayarları")] [SerializeField]
        private float moveSpeed = 12f;

        [SerializeField] private float rotationSpeed = 540f;
        [SerializeField] private float sharpTurnAngleThreshold = 120f;

        [Header("Ağ Ayarları")] [SerializeField]
        private float networkSyncInterval = 0.1f;

        [Header("Savaş Ayarları")] [SerializeField]
        private float attackCooldown = 2.0f;

        [SerializeField] private float attackRange = 25f;
        private readonly int _attackTriggerHash = Animator.StringToHash("Attack");

        [Header("Hedef Seçim")] [SerializeField]
        private LayerMask selectableTargets = -1; // Varsayılan: tüm layer'lar
        [SerializeField] private float targetSelectionTimeout = 30f; // 30 saniye sonra hedef kaybı
        private GameObject _selectedTarget; // Seçili hedef object
        private GameObject _targetHighlight; // Hedef vurgulama efekti
        private float _lastInteractionTime; // Son etkileşim zamanı

        // Animator Hashes
        private readonly int _ısMovingHash = Animator.StringToHash("IsMoving");
        private Animator _animator;
        private const bool CanAttack = true;
        private string _currentTargetId;
        
        // Combat System
        private WeaponSystem _weaponSystem;
        private PlayerHealth _health;
        private InputSystem_Actions _inputActions;

        private bool _isLocalPlayer;
        
        /// <summary>
        /// Bu PlayerController'ın local player'a ait olup olmadığını belirtir.
        /// NetworkObjectSpawner tarafından server transform update'lerini ignore etmek için kullanılır.
        /// </summary>
        public bool IsLocalPlayer => _isLocalPlayer;
        private Camera _mainCamera;
        private NavMeshAgent _navMeshAgent;
        private float _networkSyncTimer;

        // Bileşenler ve Durumlar
        private Rigidbody _rb;

        // Network optimizasyonu için önceki pozisyon/rotasyon değerleri
        private Vector3 _lastSentPosition = Vector3.zero;
        private Quaternion _lastSentRotation = Quaternion.identity;
        private Vector3 _lastSentVelocity = Vector3.zero;
        private const float POSITION_THRESHOLD = 0.1f; // 10cm hareket gerekiyor
        private const float ROTATION_THRESHOLD = 2f; // 2 derece rotasyon gerekiyor
        private const float VELOCITY_THRESHOLD = 0.1f; // Hız değişimi eşiği

        private void Awake()
        {
            Debug.Log("🎮 [PLAYER] PlayerController Awake başladı");
            _rb = GetComponent<Rigidbody>();
            _animator = GetComponent<Animator>();
            _health = GetComponent<PlayerHealth>();
            _navMeshAgent = GetComponent<NavMeshAgent>();
            _weaponSystem = GetComponent<WeaponSystem>();
            _mainCamera = Camera.main;
            
            _inputActions = new InputSystem_Actions();
            Debug.Log("🎮 [PLAYER] InputSystem_Actions oluşturuldu");
            
            // Input Actions event'lerini manuel olarak bağla
            _inputActions.Player.SetDestination.performed += OnSetDestination;
            _inputActions.Player.SetDestination.started += (ctx) => Debug.Log("🖱️ [INPUT] SetDestination STARTED");
            _inputActions.Player.SetDestination.canceled += (ctx) => Debug.Log("🖱️ [INPUT] SetDestination CANCELED");
            _inputActions.Player.Fire.performed += OnFire; // Space tuşu için ateş
            Debug.Log("🎮 [PLAYER] Input Actions event'leri bağlandı");
        }

        private void Update()
        {
            if (!_isLocalPlayer) return;
            HandleRotation();
            UpdateAnimator();
            HandleNetworkSync();
            CheckTargetTimeout(); // Hedef timeout kontrolü
            
            // Test tuşları (geliştirme için)
            HandleTestKeys();
        }
        
        private void CheckTargetTimeout()
        {
            // Hedef seçili ve 30 saniye etkileşim olmazsa hedef temizle
            if (_selectedTarget != null && Time.time - _lastInteractionTime > targetSelectionTimeout)
            {
                Debug.Log($"⏰ [TARGET] 30 saniye etkileşim olmadı, hedef temizleniyor: {_selectedTarget.name}");
                ClearTarget();
            }
        }
        
        private void HandleTestKeys()
        {
            if (_weaponSystem == null) return;
            
            // Input System kullanarak keyboard kontrolü
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard == null) return;
            
            // 1 tuşu: Yavaş ateş (0.5 saniyede 1)
            if (keyboard.digit1Key.wasPressedThisFrame)
            {
                _weaponSystem.ChangeAttackSpeed(0.5f);
                Debug.Log("🔫 [TEST] Yavaş ateş moduna geçildi!");
            }
            
            // 2 tuşu: Normal ateş (1 saniyede 1)
            if (keyboard.digit2Key.wasPressedThisFrame)
            {
                _weaponSystem.ChangeAttackSpeed(1.0f);
                Debug.Log("🔫 [TEST] Normal ateş moduna geçildi!");
            }
            
            // 3 tuşu: Hızlı ateş (1 saniyede 2)
            if (keyboard.digit3Key.wasPressedThisFrame)
            {
                _weaponSystem.ChangeAttackSpeed(2.0f);
                Debug.Log("🔫 [TEST] Hızlı ateş moduna geçildi!");
            }
            
            // 4 tuşu: Çok hızlı ateş (1 saniyede 3)
            if (keyboard.digit4Key.wasPressedThisFrame)
            {
                _weaponSystem.ChangeAttackSpeed(3.0f);
                Debug.Log("🔫 [TEST] Çok hızlı ateş moduna geçildi!");
            }
            
            // ESC tuşu: Manuel hedef temizleme
            if (keyboard.escapeKey.wasPressedThisFrame && _selectedTarget != null)
            {
                Debug.Log("🚫 [TEST] ESC tuşu ile hedef manuel olarak temizlendi!");
                ClearTarget();
            }
        }

        private void OnEnable()
        {
            if (_inputActions != null)
            {
                _inputActions.Player.Enable();
                Debug.Log("🎮 [PLAYER] Input Actions enable edildi");
            }
            
            // NetworkManager action event'lerini dinle
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.OnActionSuccess += HandleActionSuccess;
                NetworkManager.Instance.OnActionFailed += HandleActionFailed;
                Debug.Log("📡 [PLAYER] NetworkManager action event'leri dinlemeye başlandı");
            }
        }

        private void OnDisable()
        {
            if (_inputActions != null)
            {
                _inputActions.Player.Disable();
                Debug.Log("🎮 [PLAYER] Input Actions disable edildi");
            }
            
            // NetworkManager action event'lerini bırak
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.OnActionSuccess -= HandleActionSuccess;
                NetworkManager.Instance.OnActionFailed -= HandleActionFailed;
                Debug.Log("📡 [PLAYER] NetworkManager action event'leri dinleme bırakıldı");
            }
        }
        
        private void OnDestroy()
        {
            if (_inputActions != null)
            {
                _inputActions.Player.SetDestination.performed -= OnSetDestination;
                _inputActions.Player.Fire.performed -= OnFire;
                _inputActions.Dispose();
                Debug.Log("🎮 [PLAYER] Input Actions event'leri temizlendi ve dispose edildi");
            }
        }

        public void Initialize(bool isLocal, string networkId)
        {
            Debug.Log($"🎮 [PLAYER] Initialize çağrıldı - isLocal: {isLocal}");
            _isLocalPlayer = isLocal;

            if (_isLocalPlayer)
            {
                Debug.Log("🎮 [PLAYER] Local player olarak initialize ediliyor");
                gameObject.tag = "Player";
                
                // NavMeshAgent ayarlarını optimize et 
                _navMeshAgent.speed = moveSpeed;
                _navMeshAgent.acceleration = moveSpeed * 4f; // Daha hızlı hızlanma
                _navMeshAgent.angularSpeed = 360f; // Normal dönüş hızı
                _navMeshAgent.stoppingDistance = 0.1f; // Daha yakın dur
                _navMeshAgent.autoBraking = true; // Otomatik frenleme
                _navMeshAgent.updateRotation = false; // Manuel rotation kullanacağız
                _navMeshAgent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance; // Hafif obstacle avoidance
                
                Debug.Log($"🚢 [NAVMESH] NavMeshAgent ayarları optimize edildi - Speed: {_navMeshAgent.speed}, Acceleration: {_navMeshAgent.acceleration}");
                
                // PlayerInput component'ini disable et (Input System Actions kullanacağız)
                var playerInput = GetComponent<PlayerInput>();
                if (playerInput != null)
                {
                    playerInput.enabled = false;
                    Debug.Log("🎮 [PLAYER] PlayerInput component disable edildi");
                }
                
                // Input Actions'ı enable et
                _inputActions.Player.Enable();
                Debug.Log("🎮 [PLAYER] Input Actions enable edildi");
                
                OnLocalPlayerSpawned?.Invoke(this);
            }
            else
            {
                Debug.Log("🎮 [PLAYER] Remote player olarak initialize ediliyor");
                _navMeshAgent.enabled = false;
                var playerInput = GetComponent<PlayerInput>();
                if (playerInput != null) playerInput.enabled = false;
                if (_rb != null) _rb.isKinematic = true;
            }
        }

        // Statik Olay
        public static event Action<PlayerController> OnLocalPlayerSpawned;

        #region Input (Girdi) ve Hedefleme

        public void OnSetDestination(InputAction.CallbackContext context)
        {
            Debug.Log($"🖱️ [INPUT] OnSetDestination çağrıldı - performed: {context.performed}, phase: {context.phase}");
            
            // Sadece mouse click performed ve local player kontrolü
            if (!_isLocalPlayer || !context.performed) return;
            
            Debug.Log($"🖱️ [INPUT] Local player kontrolü geçti, mouse kontrol ediliyor...");
            
            // Ekstra güvenlik: Sol mouse tuşunun basıldığından emin ol
            if (!Mouse.current.leftButton.isPressed) 
            {
                Debug.Log($"❌ [INPUT] Sol mouse tuşu basılı değil!");
                return;
            }
            
            Debug.Log($"✅ [INPUT] Sol mouse tuşu basılı, raycast yapılıyor...");

            var ray = _mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
            Debug.Log($"🔍 [RAYCAST] Ray origin: {ray.origin}, direction: {ray.direction}");
            
            // Tüm layer'lara raycast gönder (layer restriction kaldır)
            if (Physics.Raycast(ray, out var hit, Mathf.Infinity))
            {
                Debug.Log($"🎯 [RAYCAST] Hit object: {hit.collider.name} at {hit.point}");
                
                // 🎯 HEDEFLENEBİLİR NESNE KONTROLÜ (Enemy, NPC, Player tag'leri)
                if (IsTargetSelectable(hit.collider.gameObject))
                {
                    SelectTarget(hit.collider.gameObject);
                    _navMeshAgent.ResetPath(); // Hedef seçildiğinde mevcut hareketi durdur.
                    Debug.Log($"🎯 [TARGET] Hedef seçildi: {hit.collider.gameObject.name} - Hareket durduruldu!");
                }
                else // Boş bir alana tıklandı
                {
                    // Hedefi temizleme - KALDIRILDI! Artık sadece hareket eder
                    
                    // NavMesh ile hareket et
                    Vector3 targetPos = hit.point;
                    targetPos.y = transform.position.y; // Y pozisyonunu koru
                    
                    if (_navMeshAgent.enabled && _navMeshAgent.isOnNavMesh)
                    {
                        _navMeshAgent.SetDestination(targetPos);
                        Debug.Log($"✅ [MOVEMENT] Hareket hedefi: {targetPos} - Hedef seçimi korunuyor!");
                        
                        // Hareket etme de etkileşim sayılır
                        RefreshTargetInteraction();
                    }
                    else
                    {
                        Debug.Log($"❌ [NAVMESH] NavMeshAgent not ready!");
                    }
                }
            }
            else
            {
                Debug.Log($"❌ [INPUT] Raycast hiçbir şeye çarpmadı!");
            }
        }

        public void OnFire(InputAction.CallbackContext context)
        {
            if (!_isLocalPlayer || !context.performed) return;

            Debug.Log("🔫 [FIRE] Space tuşuna basıldı!");

            // Hedef seçili mi?
            if (_selectedTarget == null)
            {
                Debug.Log("❌ [FIRE] Ateş etmek için bir hedef seçmelisin!");
                return;
            }

            // WeaponSystem var mı?
            if (_weaponSystem == null)
            {
                Debug.LogError("❌ [FIRE] WeaponSystem bulunamadı!");
                return;
            }

            // ✅ YENİ: Saldırı kuralları kontrolü
            if (!CanAttackTarget(_selectedTarget))
            {
                return; // Hata mesajları CanAttackTarget method'unda gösterilir
            }

            // Mesafe kontrolü (bilgilendirme amaçlı - zaten CanAttackTarget'te kontrol edildi)
            float distance = Vector3.Distance(transform.position, _selectedTarget.transform.position);
            Debug.Log($"🎯 [FIRE] Hedefe mesafe: {distance:F1}m - Sunucuya ateş isteği gönderiliyor...");

            // ✅ YENİ: Sunucuya ateş etme isteği gönder (lokal ateş etme YOK!)
            string targetId = _selectedTarget.name; // Veya daha iyi bir ID sistemi kullanabilirsiniz
            RequestAttack(targetId);
            
            Debug.Log($"📡 [FIRE] Sunucuya ateş isteği gönderildi! Hedef: {_selectedTarget.name}");
            Debug.Log("⏳ [FIRE] Sunucu onayı bekleniyor... Gerçek ateş efekti sunucu onayında çalışacak.");

            // Hedef etkileşimi hemen yenile (kullanıcı feedback için)
            RefreshTargetInteraction();
        }

        #endregion

        #region Hareket ve Animasyon

        private void HandleRotation()
        {
            if (!_navMeshAgent.enabled || !_navMeshAgent.hasPath) return;

            // NavMeshAgent'ın velocity'sine göre rotasyon (daha smooth)
            Vector3 direction = _navMeshAgent.velocity.normalized;
            direction.y = 0; // Y eksenini sıfırla (sadece XZ düzleminde dönüş)

            if (direction.sqrMagnitude > 0.01f) // Çok küçük hareketleri göz ardı et
            {
                // Hedef rotasyonu hesapla
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                
                // Daha yumuşak rotasyon - Slerp kullan
                float rotationSmoothing = rotationSpeed * Time.deltaTime;
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSmoothing / 100f);
            }
        }

        private void UpdateAnimator()
        {
            if (_animator == null) return;

            // NavMeshAgent'ın hızına göre animasyon
            bool isMoving = _navMeshAgent.enabled && _navMeshAgent.velocity.sqrMagnitude > 0.1f;
            _animator.SetBool(_ısMovingHash, isMoving);
            
            // Debug log'u sadece state değiştiğinde göster (spam önlemek için)
            // if (isMoving) Debug.Log($"🏃 [ANIMATION] Moving - Velocity: {_navMeshAgent.velocity.magnitude:F2}");
        }

        #endregion

        #region Savaş ve Hasar

        private IEnumerator AttackCooldownRoutine()
        {
            yield return new WaitForSeconds(0.3f);
        }

        public void TakeDamage(int damage)
        {
            /* ... */
        }

        #endregion

        #region Network Gönderim İşlemleri (Güncellendi)

        private void HandleNetworkSync()
        {
            // Transform sync'i tekrar aktif et - çok fazla azaltmıştık
            _networkSyncTimer += Time.deltaTime;
            if (_networkSyncTimer >= networkSyncInterval)
            {
                _networkSyncTimer = 0f;
                // Pozisyon sync'i tekrar aç
                SendTransformUpdate();
                // Debug.Log($"📡 [SYNC] Transform sync normal seviyede");
            }
        }

        /// <summary>
        ///     Yerel oyuncunun güncel transform'unu sunucuya gönderir.
        ///     OPTIMIZASYON: Sadece önemli değişiklikler olduğunda gönderir.
        /// </summary>
        private void SendTransformUpdate()
        {
            if (!NetworkManager.Instance.IsConnected) return;

            var currentPosition = transform.position;
            var currentRotation = transform.rotation;
            var currentVelocity = _rb.linearVelocity;

            // Önemli değişiklik var mı kontrol et
            bool positionChanged = Vector3.Distance(currentPosition, _lastSentPosition) >= POSITION_THRESHOLD;
            bool rotationChanged = Quaternion.Angle(currentRotation, _lastSentRotation) >= ROTATION_THRESHOLD;
            bool velocityChanged = Vector3.Distance(currentVelocity, _lastSentVelocity) >= VELOCITY_THRESHOLD;

            // Eğer hiçbir önemli değişiklik yoksa gönderme
            if (!positionChanged && !rotationChanged && !velocityChanged)
            {
                return; // Gereksiz network trafiği önlendi
            }

            // Değişiklik var, gönder ve son değerleri güncelle
            NetworkManager.Instance.SendTransformUpdate(currentPosition, currentRotation, currentVelocity);
            
            _lastSentPosition = currentPosition;
            _lastSentRotation = currentRotation;
            _lastSentVelocity = currentVelocity;

            Debug.Log($"📡 [NETWORK] Transform güncellendi - Pos: {positionChanged}, Rot: {rotationChanged}, Vel: {velocityChanged}");
        }

        /// <summary>
        ///     Sunucuya sadece hedef pozisyonu gönderir (smooth movement için).
        /// </summary>
        /*
        private void SendDestinationToServer(Vector3 destination)
        {
            if (!NetworkManager.Instance.IsConnected) return;

            // NetworkManager'a destination gönder (transform değil!)
            NetworkManager.Instance.SendSetDestination(destination);
            Debug.Log($"📡 [NETWORK] Destination sent to server: {destination}");
        }
        */

        /// <summary>
        ///     Sunucuya saldırı isteği gönderir.
        /// </summary>
        private void RequestAttack(string targetId)
        {
            if (!NetworkManager.Instance.IsConnected) return;

            var actionData = new C2S_PlayerActionData()
            {
                ActionType = "PrimaryAttack", // Daha spesifik bir isim
                TargetEntityId = targetId
                // Parametreler ileride eklenebilir, örn: hangi gülleyle ateş edildiği
            };

            // NetworkManager'daki yeni, temiz metodu çağırıyoruz.
            NetworkManager.Instance.SendPlayerAction(actionData);
            Debug.Log(targetId + " ID'li hedefe saldırı isteği gönderildi.");
        }

        #endregion

        #region Network Action Handlers

        /// <summary>
        /// Sunucudan gelen action success response'unu handle eder
        /// </summary>
        private void HandleActionSuccess(object actionData)
        {
            if (!_isLocalPlayer) return; // Sadece local player için
            
            Debug.Log($"✅ [ACTION SUCCESS] Sunucudan action success alındı: {actionData}");
            
            // JSON data'yı JObject olarak parse et (dynamic yerine)
            try
            {
                var jsonString = actionData.ToString();
                var actionResponse = JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(jsonString);
                
                string actionType = actionResponse?["ActionType"]?.ToString();
                Debug.Log($"🔍 [ACTION SUCCESS] Action Type: {actionType}");
                
                // Action tipine göre işle
                if (actionType == "PrimaryAttack" || actionType == "attack")
                {
                    HandleFireSuccess(actionResponse);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ [ACTION SUCCESS] Action data parse hatası: {e.Message}");
            }
        }

        /// <summary>
        /// Sunucudan gelen action failed response'unu handle eder
        /// </summary>
        private void HandleActionFailed(S2C_ActionFailedData failedData)
        {
            if (!_isLocalPlayer) return; // Sadece local player için
            
            Debug.Log($"❌ [ACTION FAILED] Sunucudan action failed alındı: {failedData.Reason}");
            
            // Action tipine göre hata mesajları
            if (failedData.ActionType == "PrimaryAttack" || failedData.ActionType == "attack")
            {
                Debug.Log($"🔫 [FIRE FAILED] Ateş etme başarısız: {failedData.Reason}");
                // Burada UI'da hata mesajı gösterebiliriz
            }
        }

        /// <summary>
        /// Sunucu onayladığında gerçek ateş efektini çalıştırır
        /// </summary>
        private void HandleFireSuccess(Newtonsoft.Json.Linq.JObject actionResponse)
        {
            Debug.Log("🔫 [FIRE SUCCESS] Sunucu ateş etmeyi onayladı! Efekt çalıştırılıyor...");
            
            // Hedef varsa ateş et
            if (_selectedTarget != null && _weaponSystem != null)
            {
                // WeaponSystem ile gerçek ateş efektini çalıştır
                _weaponSystem.Attack(_selectedTarget.transform);
                Debug.Log($"🚀 [FIRE SUCCESS] Ateş efekti çalıştırıldı! Hedef: {_selectedTarget.name}");
                
                // Animasyonu tetikle
                if (_animator != null)
                {
                    _animator.SetTrigger(_attackTriggerHash);
                }
                
                // Ateş etme de etkileşim sayılır
                RefreshTargetInteraction();
                
                // Sunucudan gelen damage bilgisini logla
                try
                {
                    int damage = actionResponse?["Damage"]?.ToObject<int>() ?? 0;
                    float cooldown = actionResponse?["Cooldown"]?.ToObject<float>() ?? 0f;
                    Debug.Log($"💥 [FIRE SUCCESS] Damage: {damage}, Cooldown: {cooldown}s");
                }
                catch
                {
                    Debug.Log("💥 [FIRE SUCCESS] Damage bilgisi parse edilemedi");
                }
            }
            else
            {
                Debug.LogWarning("❌ [FIRE SUCCESS] Hedef yok veya WeaponSystem yok!");
            }
        }

        #endregion

        #region Hedef Seçim Sistemi

        /// <summary>
        /// Tıklanan nesnenin hedeflenebilir olup olmadığını kontrol eder.
        /// Enemy, NPC, Player tag'li nesneleri hedefleyebilir.
        /// </summary>
        private bool IsTargetSelectable(GameObject target)
        {
            if (target == null || target == gameObject) return false; // Kendini hedefleyemez

            // Tag kontrolü: Enemy, NPC, Player
            if (target.CompareTag("Enemy") || target.CompareTag("NPC") || target.CompareTag("Player"))
            {
                // IDamageable interface'i var mı?
                if (target.TryGetComponent<IDamageable>(out _))
                {
                    Debug.Log($"✅ [TARGET] Hedeflenebilir nesne: {target.name} (Tag: {target.tag})");
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Etkileşim süresini yeniler (30 saniye timer sıfırlanır).
        /// </summary>
        private void RefreshTargetInteraction()
        {
            if (_selectedTarget != null)
            {
                _lastInteractionTime = Time.time;
                // Debug.Log($"🔄 [TARGET] Etkileşim yenilendi: {_selectedTarget.name}");
            }
        }
        
        /// <summary>
        /// Yeni hedef seçer ve eski hedefi temizler.
        /// </summary>
        private void SelectTarget(GameObject newTarget)
        {
            // Eski hedefi temizle
            ClearTarget();

            // Yeni hedefi seç
            _selectedTarget = newTarget;

            // 30 saniyelik timer başlat
            _lastInteractionTime = Time.time;

            // Hedef vurgulama efekti ekle (isteğe bağlı)
            CreateTargetHighlight();

            Debug.Log($"🎯 [TARGET] Yeni hedef seçildi: {_selectedTarget.name} - 30 saniye timer başlatıldı!");

            // NetworkIdentity varsa ID'yi de kaydet (eski sistem uyumluluğu için)
            if (_selectedTarget.TryGetComponent<NetworkIdentity>(out var networkId))
            {
                _currentTargetId = networkId.EntityId;
            }
        }

        /// <summary>
        /// Mevcut hedefi temizler.
        /// </summary>
        private void ClearTarget()
        {
            if (_selectedTarget != null)
            {
                Debug.Log($"🚫 [TARGET] Hedef temizlendi: {_selectedTarget.name}");
            }

            _selectedTarget = null;
            _currentTargetId = string.Empty;

            // Vurgulama efektini kaldır
            RemoveTargetHighlight();
        }

        /// <summary>
        /// Seçili hedefe vurgulama efekti ekler.
        /// </summary>
        private void CreateTargetHighlight()
        {
            if (_selectedTarget == null) return;

            // Basit kırmızı outline efekti (geliştirilebilir)
            var renderer = _selectedTarget.GetComponent<Renderer>();
            if (renderer != null)
            {
                // Outline shader veya farklı material kullanılabilir
                // Şimdilik sadece debug için log
                Debug.Log($"🔴 [HIGHLIGHT] {_selectedTarget.name} vurgulandı");
            }
        }

        /// <summary>
        /// Hedef vurgulama efektini kaldırır.
        /// </summary>
        private void RemoveTargetHighlight()
        {
            if (_targetHighlight != null)
            {
                Destroy(_targetHighlight);
                _targetHighlight = null;
            }
        }

        /// <summary>
        /// Debug için: Mevcut hedefi döndürür.
        /// </summary>
        public GameObject GetSelectedTarget()
        {
            return _selectedTarget;
        }
        
        /// <summary>
        /// Hedefin kalan timeout süresini döndürür (saniye).
        /// </summary>
        public float GetTargetTimeRemaining()
        {
            if (_selectedTarget == null) return 0f;
            
            float timeElapsed = Time.time - _lastInteractionTime;
            float timeRemaining = targetSelectionTimeout - timeElapsed;
            return Mathf.Max(0f, timeRemaining);
        }

        #endregion

        #region Saldırı Kuralları

        /// <summary>
        /// Hedefe saldırı yapılabilir mi kontrol eder
        /// </summary>
        private bool CanAttackTarget(GameObject target)
        {
            if (target == null)
            {
                Debug.Log("❌ [ATTACK RULE] Hedef null!");
                return false;
            }

            // 1. Menzil kontrolü
            float distance = Vector3.Distance(transform.position, target.transform.position);
            var gameSettings = BarbarosKs.Core.GameSettings.Instance;
            
            if (!gameSettings.IsWithinRange(distance))
            {
                Debug.Log($"❌ [ATTACK RULE] Hedef menzil dışında! Mesafe: {distance:F1}m, Max: {gameSettings.maxProjectileRange}m");
                return false;
            }

            // 2. Hedef canlı mı kontrolü (IDamageable interface ile)
            if (target.TryGetComponent<IDamageable>(out var damageable))
            {
                // Eğer hedef PlayerHealth component'ına sahipse HP kontrolü yap
                if (target.TryGetComponent<PlayerHealth>(out var playerHealth))
                {
                    if (playerHealth.currentHealth <= 0)
                    {
                        Debug.Log($"❌ [ATTACK RULE] Hedef zaten ölü! HP: {playerHealth.currentHealth}");
                        return false;
                    }
                }
                // TestEnemy veya diğer IDamageable nesneler için genel kontrol
                else
                {
                    // TestEnemy'de can kontrolü (public property ile)
                    var testEnemy = target.GetComponent<BarbarosKs.Testing.TestEnemy>();
                    if (testEnemy != null)
                    {
                        if (testEnemy.IsDead)
                        {
                            Debug.Log($"❌ [ATTACK RULE] Test düşman zaten ölü! HP: {testEnemy.CurrentHealth}");
                            return false;
                        }
                    }
                }
            }
            else
            {
                Debug.Log($"❌ [ATTACK RULE] Hedef IDamageable interface'ine sahip değil: {target.name}");
                return false;
            }

            // 3. Line of Sight kontrolü (opsiyonel - gelecekte eklenebilir)
            // if (!HasLineOfSight(target)) return false;

            // 4. Hedef hala aktif mi kontrolü
            if (!target.activeInHierarchy)
            {
                Debug.Log($"❌ [ATTACK RULE] Hedef artık aktif değil: {target.name}");
                return false;
            }

            Debug.Log($"✅ [ATTACK RULE] Tüm kurallar geçildi! Hedefe saldırı mümkün: {target.name}");
            return true;
        }

        /// <summary>
        /// İleride eklenebilir: Line of sight kontrolü
        /// </summary>
        private bool HasLineOfSight(GameObject target)
        {
            // Raycast ile aralarında engel var mı kontrol et
            Vector3 direction = (target.transform.position - transform.position).normalized;
            float distance = Vector3.Distance(transform.position, target.transform.position);
            
            if (Physics.Raycast(transform.position, direction, out RaycastHit hit, distance))
            {
                // Eğer ray hedefe değil de başka bir şeye çarpıyorsa
                if (hit.collider.gameObject != target)
                {
                    Debug.Log($"❌ [LINE OF SIGHT] Hedef ile arada engel var: {hit.collider.name}");
                    return false;
                }
            }
            
            return true;
        }

        #endregion

        #region Unity Editor Gizmos

        /// <summary>
        /// Manuel olarak çember çizer (Gizmos.DrawWireCircle Unity'de yok)
        /// </summary>
        private void DrawCircle(Vector3 center, float radius, int segments = 36)
        {
            float angleStep = 360f / segments;
            Vector3 prevPoint = center + new Vector3(radius, 0, 0);
            
            for (int i = 1; i <= segments; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                Vector3 newPoint = center + new Vector3(
                    Mathf.Cos(angle) * radius, 
                    0, 
                    Mathf.Sin(angle) * radius
                );
                
                Gizmos.DrawLine(prevPoint, newPoint);
                prevPoint = newPoint;
            }
        }

        /// <summary>
        /// Unity Editor'da saldırı menzilini görsel olarak gösterir
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            if (!_isLocalPlayer) return; // Sadece local player için çiz

            // GameSettings'den menzil bilgisini al
            var gameSettings = BarbarosKs.Core.GameSettings.Instance;
            if (gameSettings == null) return;

            // Saldırı menzili dairesi (yeşil)
            Gizmos.color = Color.green;
            DrawCircle(transform.position, gameSettings.maxProjectileRange);
            
            // Menzil dairesini hafif şeffaf doldur
            Gizmos.color = new Color(0f, 1f, 0f, 0.1f);
            Gizmos.DrawSphere(transform.position, gameSettings.maxProjectileRange);

            // Seçili hedef varsa hedef ile bağlantı çiz
            if (_selectedTarget != null)
            {
                float distance = Vector3.Distance(transform.position, _selectedTarget.transform.position);
                
                // Menzil içindeyse yeşil, dışındaysa kırmızı çizgi
                Gizmos.color = gameSettings.IsWithinRange(distance) ? Color.green : Color.red;
                Gizmos.DrawLine(transform.position, _selectedTarget.transform.position);
                
                // Hedef pozisyonunda küçük küre
                Gizmos.color = gameSettings.IsWithinRange(distance) ? Color.green : Color.red;
                Gizmos.DrawWireSphere(_selectedTarget.transform.position, 2f);
            }
        }

        /// <summary>
        /// Her zaman görünür gizmos (oyun oynarken de görünür)
        /// </summary>
        private void OnDrawGizmos()
        {
            if (!_isLocalPlayer) return;
            if (!Application.isPlaying) return; // Sadece oyun oynarken göster

            // GameSettings'den menzil bilgisini al
            var gameSettings = BarbarosKs.Core.GameSettings.Instance;
            if (gameSettings == null) return;

            // Sadece seçili hedef varsa ve debug modda ise menzil çemberini göster
            if (_selectedTarget != null)
            {
                float distance = Vector3.Distance(transform.position, _selectedTarget.transform.position);
                
                // Menzil çemberi (hafif görünür)
                Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
                DrawCircle(transform.position, gameSettings.maxProjectileRange);
                
                // Mesafe bilgisi (sadece Scene view'da görünür)
                #if UNITY_EDITOR
                UnityEditor.Handles.color = gameSettings.IsWithinRange(distance) ? Color.green : Color.red;
                UnityEditor.Handles.Label(
                    _selectedTarget.transform.position + Vector3.up * 3f,
                    $"Mesafe: {distance:F1}m\nMenzil: {gameSettings.maxProjectileRange}m\n{(gameSettings.IsWithinRange(distance) ? "✅ Menzilde" : "❌ Menzil Dışı")}"
                );
                #endif
            }
        }

        #endregion

    }
}