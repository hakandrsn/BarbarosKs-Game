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
    [Header("Görsel Ayarlar")]
    [SerializeField] private GameObject _moveFlagPrefab;
    public Transform _cannonSpawnPoint;

    [Header("Katman Maskeleri")]
    [SerializeField] private LayerMask _movementLayerMask;
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
        Debug.Log($"[PC-LOG {NetworkObjectId}] OnNetworkSpawn çağrıldı. IsServer: {IsServer}, IsClient: {IsClient}, IsOwner: {IsOwner}");

        // NavMeshAgent'a SADECE SUNUCUDA ihtiyaç duyulur.
        if (IsServer)
        {
            Debug.Log($"[PC-LOG {NetworkObjectId}] SUNUCU tarafı: NavMeshAgent bileşeni alınıyor.");
            _navMeshAgent = GetComponent<NavMeshAgent>();
            if (_navMeshAgent == null) Debug.LogError($"[PC-LOG {NetworkObjectId}] SUNUCU HATA: NavMeshAgent bulunamadı!");
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
        if (_mainCamera == null) Debug.LogError($"[PC-LOG {NetworkObjectId}] HATA: Camera.main bulunamadı!");

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
        if (!IsOwner || EventSystem.current.IsPointerOverGameObject())
        {
             Debug.LogWarning($"[PC-LOG {NetworkObjectId}] HandlePrimaryClick durduruldu. IsOwner: {IsOwner}, IsPointerOverGameObject: {EventSystem.current.IsPointerOverGameObject()}");
             return;
        }

        Vector2 screenPosition = _playerInputActions.Player.MousePosition.ReadValue<Vector2>();
        Ray ray = _mainCamera.ScreenPointToRay(screenPosition);
        Debug.Log($"[PC-LOG {NetworkObjectId}] Raycast için ışın oluşturuldu. Hedef: {screenPosition}");
        
        // Öncelik 1: Hedeflenebilir bir şeye mi tıklandı?
        if (Physics.Raycast(ray, out RaycastHit hitTarget, 1000f, _targetLayerMask))
        {
            Debug.Log($"[PC-LOG {NetworkObjectId}] Raycast bir hedefe çarptı: {hitTarget.collider.name}");
            if (hitTarget.collider.TryGetComponent<Targetable>(out Targetable newTarget) && newTarget.gameObject != this.gameObject)
            {
                SetTarget(newTarget);
                return;
            }
        }
        
        // Öncelik 2: Hareket edilebilir bir yere mi tıklandı?
        if (Physics.Raycast(ray, out RaycastHit hitMovement, 1000f, _movementLayerMask))
        {
            Debug.Log($"[PC-LOG {NetworkObjectId}] Raycast bir zemine çarptı: {hitMovement.point}. Sunucuya hareket komutu gönderiliyor.");
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
            Debug.LogWarning($"[PC-LOG {NetworkObjectId}] HandleAttack durduruldu. IsOwner: {IsOwner}, CurrentTarget: {(CurrentTarget == null ? "NULL" : CurrentTarget.name)}");
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

        Debug.Log($"[PC-LOG {NetworkObjectId}] SUNUCU: NavMeshAgent'ın isOnNavMesh durumu: {_navMeshAgent.isOnNavMesh}");
        if (_navMeshAgent.isOnNavMesh)
        {
            Debug.Log($"[PC-LOG {NetworkObjectId}] SUNUCU: SetDestination çağrılıyor.");
            _navMeshAgent.SetDestination(destination);
        }
        else
        {
            Debug.LogWarning($"[PC-LOG {NetworkObjectId}] SUNUCU UYARI: Gemi bir NavMesh üzerinde değil, hareket komutu yoksayılıyor.");
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