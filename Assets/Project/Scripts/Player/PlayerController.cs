using UnityEngine;
using UnityEngine.InputSystem;
using Project.Scripts.Interfaces;
using Project.Scripts.Network;
using Project.Scripts.Network.Models;
using System.Collections; // Coroutine için eklendi

namespace BarbarosKs.Player
{
    [RequireComponent(typeof(Rigidbody), typeof(PlayerInput))]
    public class PlayerController : MonoBehaviour, IDamageable
    {
        [Header("Hareket Ayarları")]
        [SerializeField] private float moveSpeed = 10f;
        [SerializeField] private float rotationSpeed = 540f; // Daha hızlı dönüş için artırıldı

        [Header("Ağ Ayarları")]
        [SerializeField] private float networkSyncInterval = 0.1f;

        [Header("Savaş Ayarları")]
        [SerializeField] private float attackCooldown = 1.0f;
        [SerializeField] private Transform attackPoint; // Güllelerin çıkış noktası
        [SerializeField] private float attackRange = 20f; // Gülle menzili

        // Özel değişkenler
        private Rigidbody _rb;
        private Animator _animator;
        private PlayerInput _playerInput;
        private PlayerHealth _health;
        private BarbarosKs.UI.GameUI _gameUI;

        private bool _isLocalPlayer = false;
        private bool _canAttack = true;
        private string _currentTargetId;
        private Vector3 _movementDestination;
        private bool _isMovingToDestination = false;
        private float _networkSyncTimer;

        // Animator parametreleri (Cache'lenmiş)
        private readonly int IsMovingHash = Animator.StringToHash("IsMoving");
        private readonly int AttackTriggerHash = Animator.StringToHash("Attack");

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _animator = GetComponent<Animator>();
            _playerInput = GetComponent<PlayerInput>();
            _health = GetComponent<PlayerHealth>();
            _gameUI = FindObjectOfType<BarbarosKs.UI.GameUI>(); // UI referansı
        }

        /// <summary>
        /// Karakterin yerel mi yoksa uzak bir oyuncuya mı ait olduğunu belirler.
        /// Bu metot, NetworkObjectSpawner tarafından çağrılır.
        /// </summary>
        public void Initialize(bool isLocal, string networkId)
        {
            _isLocalPlayer = isLocal;
            
            // Eğer bu karakter bize ait değilse (uzak oyuncu), girdi ve fizik motorunu devre dışı bırak.
            // Çünkü onun tüm hareketleri sunucudan gelen verilerle yönetilecek.
            if (!_isLocalPlayer)
            {
                _playerInput.enabled = false;
                if (_rb != null) _rb.isKinematic = true;
            }
            else // Eğer yerel oyuncu ise
            {
                gameObject.tag = "Player"; // Yerel oyuncuyu etiketle
                _movementDestination = transform.position; // Başlangıç hedefi mevcut pozisyon
            }
        }

        private void Update()
        {
            if (!_isLocalPlayer) return;

            HandleMouseInput();
            HandleKeyboardInput();
            HandleMovement();
            HandleNetworkSync();
        }

        #region Girdi (Input) ve Oyun Mantığı

        /// <summary>
        /// Fare tıklamalarını yönetir (Hareket & Hedef Seçimi).
        /// </summary>
        private void HandleMouseInput()
        {
            if (Input.GetMouseButtonDown(0)) // Sol tık
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    // Düşman etiketli bir nesneye mi tıklandı?
                    // NOT: Düşmanın Network kimliğini tutan bir component olmalı. (Örn: NetworkIdentity.cs)
                    if (hit.collider.CompareTag("Enemy"))
                    {
                        // TODO: Düşmanın Network ID'sini alıp hedef olarak belirle
                        // var enemyIdentity = hit.collider.GetComponent<NetworkIdentity>();
                        // if(enemyIdentity != null) SetTarget(enemyIdentity.Id);

                        Debug.Log(hit.collider.name + " hedef alındı.");
                    }
                    else // Boş bir alana tıklandı
                    {
                        _currentTargetId = null; // Hedefi bırak
                        _movementDestination = hit.point;
                        _isMovingToDestination = true;
                        Debug.Log("Yeni hareket hedefi: " + _movementDestination);
                        
                        // NOT: Sunucu otoriter bir yapıda, aslında burda sunucuya bir "MoveRequest" gönderilir.
                        // Şimdilik daha akıcı bir his için istemci tarafında hareketi başlatıyoruz.
                    }
                }
            }
        }

        /// <summary>
        /// Klavye girdilerini yönetir (Saldırı).
        /// </summary>
        private void HandleKeyboardInput()
        {
            // GDD'ye göre Boşluk tuşu ile saldırı
            if (Input.GetKeyDown(KeyCode.Space))
            {
                OnAttack();
            }
        }
        
        /// <summary>
        /// Belirlenen hedefe doğru gemiyi hareket ettirir ve döndürür.
        /// </summary>
        private void HandleMovement()
        {
            if (!_isMovingToDestination)
            {
                _animator.SetBool(IsMovingHash, false);
                return;
            }

            Vector3 direction = (_movementDestination - transform.position);
            direction.y = 0; // Y ekseninde hareket olmasın

            if (direction.magnitude > 0.5f) // Hedefe yeterince yakın değilsek
            {
                _animator.SetBool(IsMovingHash, true);
                
                // Gemiyi hedefe doğru döndür
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

                // Gemiyi ileri doğru hareket ettir
                _rb.MovePosition(transform.position + transform.forward * moveSpeed * Time.deltaTime);
            }
            else // Hedefe ulaştık
            {
                _isMovingToDestination = false;
                _animator.SetBool(IsMovingHash, false);
            }
        }

        #endregion

        #region Savaş ve Hasar

        public void OnAttack()
        {
            if (!_isLocalPlayer || !_canAttack) return;
            
            // GDD Kuralı: Saldırı için bir hedef seçilmiş olmalı.
            if (string.IsNullOrEmpty(_currentTargetId))
            {
                _gameUI?.ShowNotification("Saldırmak için bir hedef seçmelisin!");
                return;
            }

            GameObject targetObject = NetworkObjectSpawner.Instance.GetPlayerObjectById(_currentTargetId);
            if (targetObject == null)
            {
                _gameUI?.ShowNotification("Hedef bulunamadı!");
                _currentTargetId = null; // Geçersiz hedefi temizle
                return;
            }
            
            // GDD Kuralı: Menzil kontrolü
            float distance = Vector3.Distance(transform.position, targetObject.transform.position);
            if (distance > attackRange)
            {
                _gameUI?.ShowNotification("Hedef menzil dışında!");
                
                // GDD Kuralı: Menzil dışındaysa hedefe doğru otomatik hareket et
                _movementDestination = targetObject.transform.position;
                _isMovingToDestination = true;
                return;
            }
            
            // Tüm kontroller başarılı, saldırı isteğini sunucuya gönder.
            RequestAttack(_currentTargetId);
            _animator.SetTrigger(AttackTriggerHash); // Animasyonu anında başlat
            
            // Cooldown başlat
            StartCoroutine(AttackCooldownRoutine());
        }

        private IEnumerator AttackCooldownRoutine()
        {
            _canAttack = false;
            yield return new WaitForSeconds(attackCooldown);
            _canAttack = true;
        }

        // Bu metot sunucudan gelen "hasar aldın" mesajı üzerine çağrılmalı.
        public void TakeDamage(int damage)
        {
            if (_health != null)
            {
                _health.TakeDamage(damage);
            }
        }
        
        #endregion

        #region Network Gönderim İşlemleri

        /// <summary>
        /// Pozisyonu periyodik olarak sunucuya gönderir.
        /// </summary>
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

            var moveData = new PlayerMoveData { NewPosition = transform.position };
            var message = new GameMessage
            {
                Type = MessageType.PlayerMove,
                Data = moveData
            };
            NetworkManager.Instance.SendMessage(message);
        }

        /// <summary>
        /// Sunucuya saldırı isteği gönderir.
        /// </summary>
        private void RequestAttack(string targetId)
        {
            if (!NetworkManager.Instance.IsConnected) return;
            
            var actionData = new PlayerActionData
            {
                ActionType = "attack",
                TargetId = targetId,
                Position = transform.position,
                // TODO: Oyuncunun seçili olan gülle tipi de buraya eklenebilir.
                // Parameters = new Dictionary<string, object> { { "cannonball_type", "default" } }
            };
            
            var message = new GameMessage
            {
                Type = MessageType.PlayerAction,
                Data = actionData
            };
            
            NetworkManager.Instance.SendMessage(message);
            Debug.Log(targetId + " ID'li hedefe saldırı isteği gönderildi.");
        }
        
        #endregion
    }
}