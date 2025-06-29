using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.AI;
using Project.Scripts.Interfaces;
using Project.Scripts.Network;
using System.Collections;
using BarbarosKs.Shared.DTOs.Game; // Sadece yeni ve doğru DTO namespace'i

namespace BarbarosKs.Player
{
    [RequireComponent(typeof(Rigidbody), typeof(PlayerInput), typeof(NavMeshAgent))]
    public class PlayerController : MonoBehaviour, IDamageable, InputSystem_Actions.IPlayerActions
    {
        [Header("Hareket Ayarları")]
        [SerializeField] private float moveSpeed = 12f;
        [SerializeField] private float rotationSpeed = 540f;
        [SerializeField] private float sharpTurnAngleThreshold = 120f;

        [Header("Ağ Ayarları")]
        [SerializeField] private float networkSyncInterval = 0.1f;

        [Header("Savaş Ayarları")]
        [SerializeField] private float attackCooldown = 1.0f;
        [SerializeField] private float attackRange = 25f;

        // Bileşenler ve Durumlar
        private Rigidbody _rb;
        private Animator _animator;
        private PlayerHealth _health;
        private NavMeshAgent _navMeshAgent;
        private Camera _mainCamera;
        private InputSystem_Actions _inputActions;

        private bool _isLocalPlayer = false;
        private bool _canAttack = true;
        private string _currentTargetId;
        private float _networkSyncTimer;

        // Animator Hashes
        private readonly int IsMovingHash = Animator.StringToHash("IsMoving");
        private readonly int AttackTriggerHash = Animator.StringToHash("Attack");

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _animator = GetComponent<Animator>();
            _health = GetComponent<PlayerHealth>();
            _navMeshAgent = GetComponent<NavMeshAgent>();
            _mainCamera = Camera.main;
            _inputActions = new InputSystem_Actions();
        }
        
        private void OnEnable() => _inputActions.Player.Enable();
        private void OnDisable() => _inputActions.Player.Disable();

        public void Initialize(bool isLocal, string networkId)
        {
            _isLocalPlayer = isLocal;
            
            if (_isLocalPlayer)
            {
                gameObject.tag = "Player";
                _navMeshAgent.speed = moveSpeed;
                _navMeshAgent.updateRotation = false;
                GetComponent<PlayerInput>().enabled = false; 
                _inputActions.Player.SetCallbacks(this);
                PlayerController.OnLocalPlayerSpawned?.Invoke(this);
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

        #region Input (Girdi) ve Hedefleme

        public void OnPrevious(InputAction.CallbackContext context)
        {
            throw new System.NotImplementedException();
        }

        public void OnNext(InputAction.CallbackContext context)
        {
            throw new System.NotImplementedException();
        }

        public void OnSprint(InputAction.CallbackContext context)
        {
            throw new System.NotImplementedException();
        }

        public void OnSetDestination(InputAction.CallbackContext context)
        {
            if (!_isLocalPlayer || !context.performed) return;

            Ray ray = _mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                // Tıklanan nesnenin bir NetworkIdentity'si var mı diye kontrol et (yani bir düşman/hedef mi?)
                if (hit.collider.TryGetComponent<NetworkIdentity>(out var targetIdentity))
                {
                    _currentTargetId = targetIdentity.EntityId;
                    _navMeshAgent.ResetPath(); // Hedef seçildiğinde mevcut hareketi durdur.
                    // TODO: GameUI'a hedef seçildiğini bildirip marker göstermesini sağla.
                    Debug.Log($"Yeni hedef seçildi: ID = {_currentTargetId}");
                }
                else // Boş bir alana tıklandı
                {
                    _currentTargetId = string.Empty; // Hedefi bırak.
                    if (NavMesh.SamplePosition(hit.point, out NavMeshHit navHit, 1.0f, NavMesh.AllAreas))
                    {
                        _navMeshAgent.SetDestination(navHit.position);
                    }
                }
            }
        }

        public void OnFire(InputAction.CallbackContext context)
        {
            if (!_isLocalPlayer || !_canAttack || !context.performed) return;
            
            if (string.IsNullOrEmpty(_currentTargetId))
            {
                // GameUI?.ShowNotification("Saldırmak için bir hedef seçmelisin!");
                return;
            }

            // Spawner'dan hedef nesnesini alıp menzil kontrolü yap
            GameObject targetObject = NetworkObjectSpawner.Instance.GetEntityById(_currentTargetId);
            if (targetObject == null)
            {
                // GameUI?.ShowNotification("Hedef bulunamadı!");
                _currentTargetId = null;
                return;
            }
            
            float distance = Vector3.Distance(transform.position, targetObject.transform.position);
            if (distance > attackRange)
            {
                // GameUI?.ShowNotification("Hedef menzil dışında!");
                _navMeshAgent.SetDestination(targetObject.transform.position); // Otomatik olarak hedefe git
                return;
            }
            
            RequestAttack(_currentTargetId);
            _animator.SetTrigger(AttackTriggerHash);
            StartCoroutine(AttackCooldownRoutine());
        }
        
        // Arayüzün gerektirdiği diğer metotlar (şimdilik boş)
        public void OnMove(InputAction.CallbackContext context) {} // Klavye ile hareket için ileride kullanılabilir.
        // ... diğer boş metotlar ...

        #endregion

        #region Hareket ve Animasyon
        // Bu bölümlerde değişiklik yok, zaten doğru çalışıyorlar.
        private void HandleRotation() { /* ... */ }
        private void UpdateAnimator() { /* ... */ }
        #endregion

        #region Savaş ve Hasar

        private IEnumerator AttackCooldownRoutine()
        {
            yield return new WaitForSeconds(0.3f);
        }
        public void TakeDamage(int damage) { /* ... */ }
        #endregion

        #region Network Gönderim İşlemleri (Güncellendi)

        private void HandleNetworkSync()
        {
            _networkSyncTimer += Time.deltaTime;
            if (_networkSyncTimer >= networkSyncInterval)
            {
                _networkSyncTimer = 0f;
                // Pozisyon gönderme sorumluluğunu yeni metoda devret
                SendTransformUpdate();
            }
        }

        /// <summary>
        /// Yerel oyuncunun güncel transform'unu sunucuya gönderir.
        /// </summary>
        private void SendTransformUpdate()
        {
            if (!NetworkManager.Instance.IsConnected) return;

            // NetworkManager'daki yeni, temiz metodu çağırıyoruz.
            NetworkManager.Instance.SendTransformUpdate(
                transform.position,
                transform.rotation,
                _rb.linearVelocity
            );
        } 

        /// <summary>
        /// Sunucuya saldırı isteği gönderir.
        /// </summary>
        private void RequestAttack(string targetId)
        {
            if (!NetworkManager.Instance.IsConnected) return;
            
            var actionData = new C2S_PlayerActionData
            {
                ActionType = "PrimaryAttack", // Daha spesifik bir isim
                TargetEntityId = targetId,
                // Parametreler ileride eklenebilir, örn: hangi gülleyle ateş edildiği
            };
            
            // NetworkManager'daki yeni, temiz metodu çağırıyoruz.
            NetworkManager.Instance.SendPlayerAction(actionData);
            Debug.Log(targetId + " ID'li hedefe saldırı isteği gönderildi.");
        }
        
        #endregion
        
        // Statik Olay
        public static event System.Action<PlayerController> OnLocalPlayerSpawned;
    }
}