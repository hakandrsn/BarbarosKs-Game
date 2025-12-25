// Filename: PlayerController.cs (Restored Server-Authoritative Logic with DETAILED LOGGING)

using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

[RequireComponent(typeof(NavMeshAgent))]
public class PlayerController : NetworkBehaviour
{
    // --- Olaylar ---
    public static event Action<Transform> OnLocalPlayerShipReady;
    public static event Action<Targetable> OnTargetChanged;

    // --- Referanslar ve Ayarlar ---
    [Header("Görsel Ayarlar")] [SerializeField]
    private GameObject _moveFlagPrefab;

    public Transform _cannonSpawnPoint;

    [Header("Katman Maskeleri")] [SerializeField]
    private LayerMask _movementLayerMask;

    [SerializeField] private LayerMask _targetLayerMask;

    // --- Durum Değişkenleri ---
    public Targetable CurrentTarget { get; private set; }
    private NavMeshAgent _navMeshAgent;
    private Camera _mainCamera;
    private PlayerInputActions _playerInputActions;
    private GameObject _currentMoveFlag;

    #region Başlatma

    public override void OnNetworkSpawn()
    {
        Debug.Log(
            $"[PC-LOG {NetworkObjectId}] OnNetworkSpawn çağrıldı. IsServer: {IsServer}, IsClient: {IsClient}, IsOwner: {IsOwner}");

        // NavMeshAgent'a SADECE SUNUCUDA ihtiyaç duyulur.
        if (IsServer)
        {
            Debug.Log($"[PC-LOG {NetworkObjectId}] SUNUCU tarafı: NavMeshAgent bileşeni alınıyor.");
            _navMeshAgent = GetComponent<NavMeshAgent>();
            if (!_navMeshAgent) Debug.LogError($"[PC-LOG {NetworkObjectId}] SUNUCU HATA: NavMeshAgent bulunamadı!");
        }
        else // Client ise, NavMeshAgent'ı devre dışı bırak.
        {
            Debug.Log($"[PC-LOG {NetworkObjectId}] CLIENT tarafı: NavMeshAgent devre dışı bırakılıyor.");
            GetComponent<NavMeshAgent>().enabled = false;
        }

        // Eğer geminin sahibi biz isek, client'a özel kontrolleri başlat.
        if (IsOwner)
        {
            Debug.Log($"[PC-LOG {NetworkObjectId}] Bu geminin SAHİBİ benim. Client kontrolleri başlatılıyor.");
            InitializeClientControls();
        }
        else
        {
            Debug.Log($"[PC-LOG {NetworkObjectId}] Bu geminin sahibi değilim.");
        }
    }

    private void InitializeClientControls()
    {
        Debug.Log($"[PC-LOG {NetworkObjectId}] InitializeClientControls çağrıldı.");
        _mainCamera = Camera.main;
        if (!_mainCamera) Debug.LogError($"[PC-LOG {NetworkObjectId}] HATA: Camera.main bulunamadı!");

        var currentEventSystem = EventSystem.current;
        var inputModuleName = currentEventSystem != null && currentEventSystem.currentInputModule != null
            ? currentEventSystem.currentInputModule.GetType().Name
            : "NULL";
        Debug.Log(
            $"[PC-LOG {NetworkObjectId}] EventSystem mevcut mu: {(currentEventSystem != null)} | ActiveInputModule: {inputModuleName}");
        Debug.Log($"[PC-LOG {NetworkObjectId}] Cursor.lockState={Cursor.lockState} visible={Cursor.visible}");
        Debug.Log(
            $"[PC-LOG {NetworkObjectId}] LayerMasks | movement={_movementLayerMask.value} target={_targetLayerMask.value}");

        OnLocalPlayerShipReady?.Invoke(transform);
        Debug.Log($"[PC-LOG {NetworkObjectId}] OnLocalPlayerShipReady olayı tetiklendi.");

        _playerInputActions = new PlayerInputActions();
        _playerInputActions.Player.Enable();
        _playerInputActions.Player.PrimaryClick.performed += HandlePrimaryClick;
        _playerInputActions.Player.Attack.performed += HandleAttack;
        Debug.Log($"[PC-LOG {NetworkObjectId}] Input Actions kuruldu ve olaylara abone olundu.");
    }

    public override void OnNetworkDespawn()
    {
        Debug.Log($"[PC-LOG {NetworkObjectId}] OnNetworkDespawn çağrıldı.");
        if (IsOwner && _playerInputActions != null)
        {
            Debug.Log($"[PC-LOG {NetworkObjectId}] Input olaylarından abonelikler iptal ediliyor.");
            _playerInputActions.Player.PrimaryClick.performed -= HandlePrimaryClick;
            _playerInputActions.Player.Attack.performed -= HandleAttack;
            _playerInputActions.Player.Disable();
        }
    }

    #endregion

    #region Input ve Komutlar

    private void HandlePrimaryClick(InputAction.CallbackContext context)
    {
        Debug.Log($"[PC-LOG {NetworkObjectId}] HandlePrimaryClick tetiklendi.");
        var hasEventSystem = EventSystem.current != null;
        bool pointerOver = hasEventSystem && EventSystem.current.IsPointerOverGameObject();

        if (!IsOwner || pointerOver)
        {
            Debug.LogWarning(
                $"[PC-LOG {NetworkObjectId}] HandlePrimaryClick durduruldu. IsOwner: {IsOwner}, HasEventSystem: {hasEventSystem}, IsPointerOverGameObject: {pointerOver}");

            if (pointerOver)
            {
                try
                {
                    var screenPositionDbg = _playerInputActions.Player.MousePosition.ReadValue<Vector2>();
                    var ped = new PointerEventData(EventSystem.current) { position = screenPositionDbg };
                    var raycastResults = new System.Collections.Generic.List<RaycastResult>();
                    EventSystem.current.RaycastAll(ped, raycastResults);
                    if (raycastResults.Count > 0)
                    {
                        var top = raycastResults[0];
                        Debug.LogWarning(
                            $"[PC-LOG {NetworkObjectId}] UI raycast tıklamayı blokluyor. Top={top.gameObject.name} | Module={top.module} | SortingOrder={top.sortingOrder}");
                    }
                    else
                    {
                        Debug.LogWarning(
                            $"[PC-LOG {NetworkObjectId}] IsPointerOverGameObject=true fakat RaycastAll sonuç yok.");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[PC-LOG {NetworkObjectId}] UI raycast debug sırasında hata: {ex.Message}");
                }
            }

            return;
        }

        var screenPosition = _playerInputActions.Player.MousePosition.ReadValue<Vector2>();
        var ray = _mainCamera.ScreenPointToRay(screenPosition);

        // ÖNCELİK 1: HEDEF SEÇME VE SALDIRI (MOBA Tarzı)
        if (Physics.Raycast(ray, out var hitTarget, 1000f, _targetLayerMask))
        {
            if (hitTarget.collider.TryGetComponent<Targetable>(out var newTarget) &&
                newTarget.gameObject != this.gameObject)
            {
                // 1. Hedefi Seç
                SetTarget(newTarget);

                // 2. Eğer bu bir düşmansa SALDIRI EMRİ VER (Eskiden '1' tuşuyla yapıyordun)
                // Not: İstersen bunu sağ tıka alabilirsin ama şimdilik "Tıkla ve Saldır" yapıyoruz.
                ulong targetId = newTarget.GetComponent<NetworkObject>().NetworkObjectId;
                RequestAttackToggleServerRpc(targetId);

                return; // Hareket etme, sadece saldırıya odaklan.
            }
        }

// ÖNCELİK 2: BOŞA TIKLANDI (HAREKET ET)
        if (Physics.Raycast(ray, out RaycastHit hitMovement, 1000f, _movementLayerMask))
        {
            // Boşa tıklayınca saldırıyı kes
            RequestAttackToggleServerRpc(ulong.MaxValue);

            if (_currentMoveFlag != null) Destroy(_currentMoveFlag);
            _currentMoveFlag = Instantiate(_moveFlagPrefab, hitMovement.point, Quaternion.identity);
            MoveToPositionServerRpc(hitMovement.point);
        }
    }

    private void HandleAttack(InputAction.CallbackContext context)
    {
        Debug.Log($"[PC-LOG {NetworkObjectId}] HandleAttack tetiklendi.");
        if (!IsOwner || CurrentTarget == null)
        {
            Debug.LogWarning(
                $"[PC-LOG {NetworkObjectId}] HandleAttack durduruldu. IsOwner: {IsOwner}, CurrentTarget: {(CurrentTarget == null ? "NULL" : CurrentTarget.name)}");
            return;
        }

        ulong targetId = CurrentTarget.GetComponent<NetworkObject>().NetworkObjectId;
        Debug.Log($"[PC-LOG {NetworkObjectId}] Sunucuya saldırı komutu gönderiliyor. Hedef ID: {targetId}");
        RequestAttackToggleServerRpc(targetId);
    }

    private void SetTarget(Targetable newTarget)
    {
        if (newTarget == CurrentTarget) return;
        Debug.Log($"[PC-LOG {NetworkObjectId}] Yeni hedef ayarlandı: {(newTarget == null ? "NULL" : newTarget.name)}");
        CurrentTarget = newTarget;
        OnTargetChanged?.Invoke(CurrentTarget);
    }

    [ServerRpc]
    private void MoveToPositionServerRpc(Vector3 destination)
    {
        Debug.Log($"[PC-LOG {NetworkObjectId}] SUNUCU: MoveToPositionServerRpc alındı. Hedef: {destination}");
        if (_navMeshAgent == null)
        {
            Debug.LogError($"[PC-LOG {NetworkObjectId}] SUNUCU HATA: _navMeshAgent DEĞİŞKENİ BOŞ (NULL)!");
            return;
        }

        Debug.Log(
            $"[PC-LOG {NetworkObjectId}] SUNUCU: NavMeshAgent'ın isOnNavMesh durumu: {_navMeshAgent.isOnNavMesh}");
        if (_navMeshAgent.isOnNavMesh)
        {
            Debug.Log($"[PC-LOG {NetworkObjectId}] SUNUCU: SetDestination çağrılıyor.");
            _navMeshAgent.SetDestination(destination);
        }
        else
        {
            Debug.LogWarning(
                $"[PC-LOG {NetworkObjectId}] SUNUCU UYARI: Gemi bir NavMesh üzerinde değil, hareket komutu yoksayılıyor.");
        }
    }

    [ServerRpc]
    private void RequestAttackToggleServerRpc(ulong targetId)
    {
        Debug.Log($"[PC-LOG {NetworkObjectId}] SUNUCU: RequestAttackToggleServerRpc alındı. Hedef ID: {targetId}");
        if (TryGetComponent<ShipCombat>(out var combat))
        {
            combat.ToggleAutoAttack(targetId);
        }
    }

    #endregion
}