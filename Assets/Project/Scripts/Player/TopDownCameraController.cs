using UnityEngine;
using UnityEngine.InputSystem;
using BarbarosKs.Player;

namespace Project.Scripts.Utils
{
    public class TopDownCameraController : MonoBehaviour
    {
        [Header("Camera Settings")]
        [SerializeField] private Vector3 offset = new Vector3(0, 15, -3);
        [SerializeField] private float cameraAngleX = 85f; // Top-down için 85 derece
        
        [Header("RTS Camera Settings")]
        [SerializeField] private bool isLocked = true; // Başlangıçta kilitli
        [SerializeField] private float edgeScrollSpeed = 10f; // Mouse edge scroll hızı
        [SerializeField] private float edgeThreshold = 50f; // Kenar hassasiyeti (pixel)
        [SerializeField] private float returnToShipSpeed = 15f; // X tuşu ile gemiye dönüş hızı
        
        [Header("Zoom Settings")]
        [SerializeField] private float minZoom = 5f;
        [SerializeField] private float maxZoom = 20f;
        [SerializeField] private float zoomSpeed = 2f;
        
        [Header("Input Actions")]
        [SerializeField] private InputActionReference toggleLockAction;
        [SerializeField] private InputActionReference returnToShipAction;
        
        private Transform _target; // Local player'ın transform'u
        private Camera _camera;
        private float _currentZoom;
        private bool _isReturningToShip = false; // X tuşu ile geri dönüş durumu
        
        // Input Actions
        private InputAction _toggleLock;
        private InputAction _returnToShip;
        
        private void Awake()
        {
            _camera = GetComponent<Camera>();
            _currentZoom = offset.y;
            
            // Input Actions'ları setup et
            SetupInputActions();
            
            // Kamerayı top-down açıya ayarla
            SetTopDownAngle();
        }
        
        private void Start()
        {
            // UI güncelle
            UpdateLockUI();
        }
        
        private void OnEnable()
        {
            // Local player spawn olduğunda event'i dinle
            PlayerController.OnLocalPlayerSpawned += OnLocalPlayerSpawned;
            
            // Input Actions'ları enable et
            _toggleLock?.Enable();
            _returnToShip?.Enable();
        }
        
        private void OnDisable()
        {
            // Event subscription'ını temizle
            PlayerController.OnLocalPlayerSpawned -= OnLocalPlayerSpawned;
            
            // Input Actions'ları disable et
            _toggleLock?.Disable();
            _returnToShip?.Disable();
        }
        
        private void OnDestroy()
        {
            // Input Actions'ları dispose et
            _toggleLock?.Dispose();
            _returnToShip?.Dispose();
        }
        
        private void SetupInputActions()
        {
            // L tuşu - Lock/Unlock toggle
            _toggleLock = new InputAction("ToggleLock", InputActionType.Button);
            _toggleLock.AddBinding("<Keyboard>/l");
            _toggleLock.performed += OnToggleLock;
            
            // X tuşu - Return to ship
            _returnToShip = new InputAction("ReturnToShip", InputActionType.Button);
            _returnToShip.AddBinding("<Keyboard>/x");
            _returnToShip.performed += OnReturnToShip;
            
            Debug.Log("🎥 [CAMERA] Input Actions setup tamamlandı");
        }
        
        private void OnLocalPlayerSpawned(PlayerController localPlayer)
        {
            Debug.Log($"🎥 [CAMERA] Local player spawn oldu, hedef ayarlandı: {localPlayer.name}");
            _target = localPlayer.transform;
            
            // Başlangıçta kamera geminin konumuna git (sabit, smooth değil)
            if (_target != null)
            {
                SnapToTarget();
            }
        }
        
        private void LateUpdate()
        {
            if (_target == null) return;
            
            HandleCameraMovement();
            HandleZoom();
        }
        
        private void SetTopDownAngle()
        {
            // Top-down için kamera açısını ayarla (sabit)
            transform.rotation = Quaternion.Euler(cameraAngleX, 0, 0);
        }
        
        private void HandleCameraMovement()
        {
            if (_isReturningToShip)
            {
                // X tuşu ile geri dönüş - smooth movement
                Vector3 targetPosition = _target.position + offset;
                targetPosition.y = _currentZoom;
                
                transform.position = Vector3.MoveTowards(transform.position, targetPosition, returnToShipSpeed * Time.deltaTime);
                
                // Hedefe ulaştık mı?
                if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
                {
                    _isReturningToShip = false;
                    Debug.Log("🎥 [CAMERA] Gemiye geri dönüş tamamlandı");
                }
                return;
            }
            
            if (isLocked)
            {
                // KİLİTLİ MOD: Gemiyi sabit takip et (smooth değil!)
                SnapToTarget();
            }
            else
            {
                // SERBEST MOD: Mouse edge scrolling
                HandleEdgeScrolling();
            }
        }
        
        private void SnapToTarget()
        {
            // Gemiyi sabit takip et (smooth değil, anında)
            Vector3 targetPosition = _target.position + offset;
            targetPosition.y = _currentZoom;
            transform.position = targetPosition;
        }
        
        private void HandleEdgeScrolling()
        {
            Vector2 mousePosition = Mouse.current.position.ReadValue();
            Vector3 moveDirection = Vector3.zero;
            
            // Ekran kenarlarını kontrol et
            if (mousePosition.x <= edgeThreshold) // Sol kenar
            {
                moveDirection += Vector3.left;
            }
            else if (mousePosition.x >= Screen.width - edgeThreshold) // Sağ kenar
            {
                moveDirection += Vector3.right;
            }
            
            if (mousePosition.y <= edgeThreshold) // Alt kenar
            {
                moveDirection += Vector3.back; // Kamera açısına göre geri
            }
            else if (mousePosition.y >= Screen.height - edgeThreshold) // Üst kenar
            {
                moveDirection += Vector3.forward; // Kamera açısına göre ileri
            }
            
            // Kamera hareket ettir
            if (moveDirection != Vector3.zero)
            {
                moveDirection.Normalize();
                transform.Translate(moveDirection * edgeScrollSpeed * Time.deltaTime, Space.World);
            }
        }
        
        private void HandleZoom()
        {
            // Mouse scroll wheel ile zoom
            var mouse = Mouse.current;
            if (mouse != null)
            {
                Vector2 scrollDelta = mouse.scroll.ReadValue();
                float scroll = scrollDelta.y / 120f; // Mouse wheel değerini normalize et
                
                if (Mathf.Abs(scroll) > 0.01f)
                {
                    _currentZoom -= scroll * zoomSpeed;
                    _currentZoom = Mathf.Clamp(_currentZoom, minZoom, maxZoom);
                    
                    // Y pozisyonunu güncelle
                    Vector3 pos = transform.position;
                    pos.y = _currentZoom;
                    transform.position = pos;
                }
            }
        }
        
        #region Input Handlers
        
        private void OnToggleLock(InputAction.CallbackContext context)
        {
            isLocked = !isLocked;
            _isReturningToShip = false; // Geri dönüşü iptal et
            
            Debug.Log($"🎥 [CAMERA] Kamera kilidi {(isLocked ? "AÇILDI" : "KAPANDI")} - L tuşu");
            UpdateLockUI();
            
            if (isLocked && _target != null)
            {
                // Kilitlendiyse hemen gemiye snap yap
                SnapToTarget();
            }
        }
        
        private void OnReturnToShip(InputAction.CallbackContext context)
        {
            if (isLocked || _target == null) return; // Kilitliyken X tuşu çalışmaz
            
            Debug.Log("🎥 [CAMERA] Gemiye geri dönüş başlatıldı - X tuşu");
            _isReturningToShip = true;
        }
        
        #endregion
        
        #region UI ve Debug
        
        private void UpdateLockUI()
        {
            // UI güncellemesi - daha sonra GameUI'a bağlanabilir
            string lockStatus = isLocked ? "🔒 KİLİTLİ" : "🔓 SERBEST";
            Debug.Log($"🎥 [CAMERA] Kamera modu: {lockStatus}");
        }
        
        private void OnGUI()
        {
            // Debug UI - sol üst köşede kamera durumunu göster
            GUIStyle style = new GUIStyle();
            style.fontSize = 16;
            style.normal.textColor = isLocked ? Color.red : Color.green;
            
            string status = isLocked ? "🔒 KAMERA KİLİTLİ" : "🔓 KAMERA SERBEST";
            string controls = isLocked ? "L: Kilidi Aç" : "L: Kilitle | X: Gemiye Dön | Mouse: Hareket";
            
            GUI.Label(new Rect(10, 10, 300, 25), status, style);
            
            style.fontSize = 12;
            style.normal.textColor = Color.white;
            GUI.Label(new Rect(10, 35, 400, 20), controls, style);
            
            if (_isReturningToShip)
            {
                style.normal.textColor = Color.yellow;
                GUI.Label(new Rect(10, 55, 300, 20), "⚡ Gemiye geri dönülüyor...", style);
            }
        }
        
        // Inspector'da test için manuel hedef atama
        [ContextMenu("Find Local Player")]
        private void FindLocalPlayer()
        {
            var player = FindObjectOfType<PlayerController>();
            if (player != null)
            {
                _target = player.transform;
                Debug.Log($"🎥 [CAMERA] Hedef bulundu: {player.name}");
                SnapToTarget();
            }
            else
            {
                Debug.LogWarning("🎥 [CAMERA] PlayerController bulunamadı!");
            }
        }
        
        #endregion
    }
}