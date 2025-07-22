// Filename: NpcController.cs

using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent), typeof(ShipCombat), typeof(Targetable))]
public class NpcController : NetworkBehaviour
{
    [Header("NPC Referansları")]
    public Transform _cannonSpawnPoint;
    // --- Durumlar ---
    private enum State { Initializing, Patrolling, Chasing, Attacking }
    private State _currentState;
    
    // --- Referanslar ve Ayarlar ---
    private NavMeshAgent _navMeshAgent;
    private ShipCombat _shipCombat;
    private NpcData _npcData;
    private Transform _chaseTarget;
    private Vector3 _startPosition;
    
 
    public override void OnNetworkSpawn()
    {
        _navMeshAgent = GetComponent<NavMeshAgent>();

        if (!IsServer)
        {
            _navMeshAgent.enabled = false;
            this.enabled = false;
            return;
        }
        
        _shipCombat = GetComponent<ShipCombat>();
        
        // --- KRİTİK DEĞİŞİKLİK ---
        // Doğrudan bir duruma geçmek yerine, NavMesh'e yerleşme işlemini bir Coroutine ile başlat.
        StartCoroutine(InitializeAgentCoroutine());
    }

    public void Initialize(NpcData data)
    {
        _npcData = data;
    }
    
    /// <summary>
    /// Bu Coroutine, NavMesh sistemi hazır olana kadar bekler ve ardından ajanı yerleştirir.
    /// </summary>
    private IEnumerator InitializeAgentCoroutine()
    {
        // NavMesh'in yüklenmesi için birkaç kare bekle. Bu, zamanlama sorunlarını çözer.
        yield return new WaitForSeconds(1.0f); 

        // En yakın NavMesh noktasını bul ve oraya ışınlan (Warp).
        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 50.0f, NavMesh.AllAreas))
        {
            _navMeshAgent.Warp(hit.position);
            _startPosition = hit.position;
            _currentState = State.Patrolling;
            Debug.Log($"NPC {NetworkObjectId} başarıyla NavMesh'e yerleştirildi: {hit.position}");
        }
        else
        {
            Debug.LogError($"NPC {NetworkObjectId} için 50 birim yakında geçerli bir NavMesh yüzeyi BULUNAMADI! Sahnenin NavMesh'ini kontrol edin.");
            // İsteğe bağlı: NPC'yi yok et veya pasif bırak.
            // GetComponent<NetworkObject>().Despawn();
        }
    }


    private void Update()
    {
        // Sunucu değilse veya hala başlatılıyorsa, yapay zekayı çalıştırma.
        if (!IsServer || _currentState == State.Initializing) return;

        // State Machine (Durum Makinesi) mantığı
        switch (_currentState)
        {
            case State.Patrolling:
                LookForTargets();
                Patrol();
                break;
            case State.Chasing:
                ChaseTarget();
                break;
            case State.Attacking:
                AttackTarget();
                break;
        }
    }
    
    private void PlaceOnNavMesh()
    {
        // En yakın NavMesh noktasını bul ve oraya ışınlan (Warp).
        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 50.0f, NavMesh.AllAreas))
        {
            _navMeshAgent.Warp(hit.position);
            _startPosition = hit.position; // Başlangıç pozisyonunu güncelle.
            _currentState = State.Patrolling; // Başarılı, artık devriyeye başlayabiliriz.
            Debug.Log($"NPC {NetworkObjectId} başarıyla NavMesh'e yerleştirildi.");
        }
        else
        {
            // Eğer yakınlarda hiç NavMesh yoksa, bunu bir hata olarak bildir.
            Debug.LogError($"NPC {NetworkObjectId} için 50 birim yakında geçerli bir NavMesh yüzeyi bulunamadı!");
        }
    }

    private void LookForTargets()
    {
        // Belirli bir alandaki tüm collider'ları bul.
        Collider[] colliders = Physics.OverlapSphere(transform.position, _npcData.aggroRadius);
        foreach (var collider in colliders)
        {
            // Eğer bir oyuncu gemisi ise...
            if (collider.TryGetComponent<PlayerController>(out var player))
            {
                _chaseTarget = player.transform;
                _currentState = State.Chasing;
                return;
            }
        }
    }

    private void Patrol()
    {
        // Eğer bir hedefi yoksa veya hedefe vardıysa, yeni bir devriye noktası seç.
        if (!_navMeshAgent.hasPath || _navMeshAgent.remainingDistance < 1f)
        {
            Vector2 randomPoint = Random.insideUnitCircle * _npcData.patrolRadius;
            Vector3 destination = _startPosition + new Vector3(randomPoint.x, 0, randomPoint.y);
            _navMeshAgent.SetDestination(destination);
        }
    }

    private void ChaseTarget()
    {
        if (_chaseTarget == null)
        {
            _currentState = State.Patrolling;
            return;
        }

        float distance = Vector3.Distance(transform.position, _chaseTarget.position);
        
        // Eğer menzile girdiyse, saldırmaya başla.
        if (distance <= _npcData.range)
        {
            _currentState = State.Attacking;
            _shipCombat.ToggleAutoAttack(_chaseTarget.GetComponent<NetworkObject>().NetworkObjectId);
        }
        else // Menzilde değilse, takibe devam et.
        {
            _navMeshAgent.SetDestination(_chaseTarget.position);
        }
    }

    private void AttackTarget()
    {
        if (_chaseTarget == null)
        {
            _currentState = State.Patrolling;
            _shipCombat.ToggleAutoAttack(ulong.MaxValue); // Saldırıyı durdur
            return;
        }
        
        float distance = Vector3.Distance(transform.position, _chaseTarget.position);

        // Eğer hedef menzilden çıkarsa, tekrar takibe başla.
        if (distance > _npcData.range)
        {
            _currentState = State.Chasing;
            _shipCombat.ToggleAutoAttack(_chaseTarget.GetComponent<NetworkObject>().NetworkObjectId); // Saldırıyı durdur
        }
        else
        {
            // Hedefe doğru dönmeye devam et
            Vector3 direction = (_chaseTarget.position - transform.position).normalized;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 2f);
        }
    }
}