// Filename: ShipStats.cs (Final, Correct Initialization Logic)
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class ShipStats : NetworkBehaviour
{
    private NavMeshAgent _navMeshAgent;

    public NetworkVariable<float> Speed = new();
    public NetworkVariable<float> AngularSpeed = new();
    public NetworkVariable<float> HitRate = new();
    public NetworkVariable<float> Range = new();
    public NetworkVariable<float> Armor = new();
    public NetworkVariable<float> Cooldown = new();
    public int CurrentVigor { get; private set; }

    private void Awake()
    {
        _navMeshAgent = GetComponent<NavMeshAgent>();
    }

    public override void OnNetworkSpawn()
    {
        // OnValueChanged olayları, artık sadece oyun sırasında bir stat değişirse
        // (örn: bir buff veya debuff ile) çalışacak.
        if (IsServer)
        {
            Speed.OnValueChanged += OnSpeedChanged;
            AngularSpeed.OnValueChanged += OnAngularSpeedChanged;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            Speed.OnValueChanged -= OnSpeedChanged;
            AngularSpeed.OnValueChanged -= OnAngularSpeedChanged;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void InitializeServerRpc(ShipStatsData statsData)
    {
        // 1. NetworkVariable'lara API'den gelen ilk değerleri ata.
        Speed.Value = statsData.Speed;
        AngularSpeed.Value = statsData.Maneuverability;
        HitRate.Value = statsData.HitRate;
        Range.Value = statsData.Range;
        Armor.Value = statsData.Armor;
        Cooldown.Value = statsData.Cooldown;
        
        // --- KRİTİK DÜZELTME ---
        // 2. NavMeshAgent'ın başlangıç değerlerini, NetworkVariable'ların
        // OnValueChanged olayını beklemeden, DOĞRUDAN burada ata.
        if (IsServer && _navMeshAgent != null)
        {
            _navMeshAgent.speed = statsData.Speed;
            _navMeshAgent.angularSpeed = statsData.Maneuverability;
            Debug.Log($"[ShipStats] SUNUCU: NavMeshAgent doğrudan initialize edildi. Hız: {_navMeshAgent.speed}");
        }

        // 3. Sadece sahibinin bilmesi gereken Vigor değeri için RPC gönder.
        ClientRpcParams ownerParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { OwnerClientId } }
        };
        UpdateVigorClientRpc(statsData.CurrentVigor, ownerParams);
    }
    
    [ClientRpc]
    private void UpdateVigorClientRpc(int newVigor, ClientRpcParams clientRpcParams = default)
    {
        this.CurrentVigor = newVigor;
    }

    // Bu metotlar artık sadece oyun sırasında statlar değişirse çalışacak.
    private void OnSpeedChanged(float previousValue, float newValue)
    {
        if (IsServer && _navMeshAgent != null)
        {
            _navMeshAgent.speed = newValue;
        }
    }

    private void OnAngularSpeedChanged(float previousValue, float newValue)
    {
        if (IsServer && _navMeshAgent != null)
        {
            _navMeshAgent.angularSpeed = newValue;
        }
    }
}