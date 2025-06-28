using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.AI;
using Project.Scripts.Interfaces;
using Project.Scripts.Network;
using System.Collections;
using BarbarosKs.core.DTOs;

namespace BarbarosKs.Player
{
    [RequireComponent(typeof(Rigidbody), typeof(PlayerInput), typeof(NavMeshAgent))]
    public class PlayerController : MonoBehaviour, IDamageable, InputSystem_Actions.IPlayerActions
    {
        [Header("Hareket Ayarları")]
        [SerializeField] private float moveSpeed = 12f;
        [SerializeField] [Tooltip("Geminin 180 derecelik bir dönüşü tamamlaması için gereken maksimum süre (saniye).")]
        private float maxRotationTime = 1.0f; 
        [SerializeField] [Tooltip("Geminin 180 derecelik bir dönüşü tamamlaması için gereken minimum süre (saniye).")]
        private float minRotationTime = 0.3f;
        [SerializeField] [Tooltip("Bu açıdan daha büyük dönüşlerde geminin yavaşlaması gereken açı.")]
        private float sharpTurnAngleThreshold = 120f;

        public static event System.Action<PlayerController> OnLocalPlayerSpawned;

        [Header("Ağ Ayarları")]
        [SerializeField] private float networkSyncInterval = 0.1f;

        [Header("Savaş Ayarları")]
        [SerializeField] private float attackCooldown = 1.0f;
        [SerializeField] private float attackRange = 25f;

        // Bileşen Referansları
        private Rigidbody _rb;
        private Animator _animator;
        private PlayerHealth _health;
        private NavMeshAgent _navMeshAgent;
        private Camera _mainCamera;
        private BarbarosKs.UI.GameUI _gameUI;
        private InputSystem_Actions _inputActions;

        // Durum Değişkenleri
        private bool _isLocalPlayer = false;
        private bool _canAttack = true;
        private string _currentTargetId;
        private float _networkSyncTimer;

        // Animator Parametreleri (Cache'lenmiş)
        private readonly int IsMovingHash = Animator.StringToHash("IsMoving");
        private readonly int AttackTriggerHash = Animator.StringToHash("Attack");

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _animator = GetComponent<Animator>();
            _health = GetComponent<PlayerHealth>();
            _navMeshAgent = GetComponent<NavMeshAgent>();
            _mainCamera = Camera.main;
            _gameUI = FindObjectOfType<BarbarosKs.UI.GameUI>();
            
            _inputActions = new InputSystem_Actions();
        }
        
        private void OnEnable()
        {
            // Bu script aktif olduğunda input eylemlerini dinlemeye başla
            _inputActions.Player.Enable();
        }

        private void OnDisable()
        {
            // Bu script devre dışı kaldığında dinlemeyi bırak
            _inputActions.Player.Disable();
        }

        public void Initialize(bool isLocal, string networkId)
        {
            _isLocalPlayer = isLocal;
            
            if (_isLocalPlayer)
            {
                gameObject.tag = "Player";
                _navMeshAgent.speed = moveSpeed;
                _navMeshAgent.angularSpeed = 0;
                _navMeshAgent.acceleration = 999;
                _navMeshAgent.updateRotation = false;

                // Bu script'i, input olaylarını dinleyecek şekilde ayarlıyoruz.
                GetComponent<PlayerInput>().enabled = false; 
                _inputActions.Player.SetCallbacks(this);
                // YENİ SATIR: Yerel oyuncunun oluşturulduğunu tüm sisteme bildiriyoruz.
                OnLocalPlayerSpawned?.Invoke(this);
            }
            else
            {
                _navMeshAgent.enabled = false;
                GetComponent<PlayerInput>().enabled = false; 
                if (_rb != null) _rb.isKinematic = true;
            }
        }

        private void Update()
        {
            if (!_isLocalPlayer) return;

            HandleRotation();
            UpdateAnimator();
            HandleNetworkSync();
        }

        #region Input (Girdi) Arayüz Metotları

        // PlayerController.cs içindeki OnSetDestination metodu
        public void OnSetDestination(InputAction.CallbackContext context)
        {
            if (!_isLocalPlayer || !context.performed) return;

            Debug.Log("OnSetDestination metodu fare tıklamasıyla çağrıldı!"); // 1. KONTROL NOKTASI

            Ray ray = _mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
    
            // Işını sahnede kırmızı bir çizgi olarak 2 saniyeliğine çizelim.
            Debug.DrawRay(ray.origin, ray.direction * 100f, Color.red, 2.0f);

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Debug.Log("Raycast bir nesneye çarptı: " + hit.collider.name); // 2. KONTROL NOKTASI

                // ŞİMDİLİK SORUNU BASİTLEŞTİRELİM: "Water" alanını kontrol etmeden, herhangi bir NavMesh alanını kabul edelim.
                if (NavMesh.SamplePosition(hit.point, out NavMeshHit navHit, 1.0f, NavMesh.AllAreas))
                {
                    Debug.Log("Geçerli NavMesh pozisyonu bulundu: " + navHit.position); // 3. KONTROL NOKTASI
                    _navMeshAgent.SetDestination(navHit.position);
                }
                else
                {
                    Debug.LogWarning("Tıklanan nokta NavMesh üzerinde bulunamadı!"); // HATA NOKTASI
                }
            }
            else
            {
                Debug.LogWarning("Raycast hiçbir şeye çarpmadı!"); // HATA NOKTASI
            }
        }

        public void OnFire(InputAction.CallbackContext context)
        {
            if (!_isLocalPlayer || !_canAttack || !context.performed) return;
            
            if (string.IsNullOrEmpty(_currentTargetId))
            {
                _gameUI?.ShowNotification("Saldırmak için bir hedef seçmelisin!");
                return;
            }

            GameObject targetObject = NetworkObjectSpawner.Instance.GetPlayerObjectById(_currentTargetId);
            if (targetObject == null)
            {
                _gameUI?.ShowNotification("Hedef bulunamadı!");
                _currentTargetId = null;
                return;
            }
            
            float distance = Vector3.Distance(transform.position, targetObject.transform.position);
            if (distance > attackRange)
            {
                _gameUI?.ShowNotification("Hedef menzil dışında!");
                _navMeshAgent.SetDestination(targetObject.transform.position);
                return;
            }
            
            RequestAttack(_currentTargetId);
            _animator.SetTrigger(AttackTriggerHash);
            StartCoroutine(AttackCooldownRoutine());
        }
        
        // Arayüzün gerektirdiği diğer metotlar (şimdilik boş)
        public void OnMove(InputAction.CallbackContext context) { }
        public void OnLook(InputAction.CallbackContext context) { }
        public void OnInteract(InputAction.CallbackContext context) { }
        public void OnCrouch(InputAction.CallbackContext context) { }
        public void OnJump(InputAction.CallbackContext context) { }
        public void OnPrevious(InputAction.CallbackContext context) { }
        public void OnNext(InputAction.CallbackContext context) { }
        public void OnSprint(InputAction.CallbackContext context) { }

        #endregion

        #region Hareket ve Animasyon

        private void HandleRotation()
        {
            if (_navMeshAgent.velocity.sqrMagnitude < 0.1f * 0.1f) return;

            Vector3 direction = _navMeshAgent.velocity.normalized;
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            
            float angleDifference = Quaternion.Angle(transform.rotation, targetRotation);
            float rotationProgress = Mathf.Clamp01(angleDifference / 180f);
            float currentRotationTime = Mathf.Lerp(maxRotationTime, minRotationTime, 1 - rotationProgress);
            float dynamicRotationSpeed = 180f / Mathf.Max(currentRotationTime, 0.01f);

            if (angleDifference > sharpTurnAngleThreshold)
            {
                _navMeshAgent.speed = moveSpeed * 0.5f;
            }
            else
            {
                _navMeshAgent.speed = moveSpeed;
            }

            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, dynamicRotationSpeed * Time.deltaTime);
        }

        private void UpdateAnimator()
        {
            bool isMoving = _navMeshAgent.velocity.magnitude > 0.2f;
            _animator.SetBool(IsMovingHash, isMoving);
        }

        #endregion

        #region Savaş ve Hasar

        private IEnumerator AttackCooldownRoutine()
        {
            _canAttack = false;
            yield return new WaitForSeconds(attackCooldown);
            _canAttack = true;
        }

        public void TakeDamage(int damage)
        {
            if (_health != null)
            {
                _health.TakeDamage(damage);
            }
        }

        #endregion

        #region Network Gönderim İşlemleri

        private void HandleNetworkSync()
        {
            _networkSyncTimer += Time.deltaTime;
            if (_networkSyncTimer >= networkSyncInterval)
            {
                _networkSyncTimer = 0f;
                SendPositionUpdate();
            }
        }

        private void SendPositionUpdate()
        {
            if (!NetworkManager.Instance.IsConnected) return;

            var moveData = new PlayerMoveData() { NewPosition = transform.position.ToNumeric() };
            var message = new GameMessage { Type = MessageType.PlayerMove, Data = moveData };
            NetworkManager.Instance.SendMessage(message);
        } 

        private void RequestAttack(string targetId)
        {
            if (!NetworkManager.Instance.IsConnected) return;
            
            var actionData = new PlayerActionData
            {
                ActionType = "attack",
                TargetId = targetId,
                Position = transform.position.ToNumeric(),
            };
            
            var message = new GameMessage { Type = MessageType.PlayerAction, Data = actionData };
            NetworkManager.Instance.SendMessage(message);
            Debug.Log(targetId + " ID'li hedefe saldırı isteği gönderildi.");
        }
        
        #endregion
    }
}