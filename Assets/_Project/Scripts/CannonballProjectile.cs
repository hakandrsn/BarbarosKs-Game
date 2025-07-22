// Filename: CannonballProjectile.cs

using Unity.Netcode;
using UnityEngine;

public class CannonballProjectile : NetworkBehaviour
{
    [Header("Effects")] public GameObject explosionPrefab; // Patlama efekti prefab'ı (Inspector'dan atanacak)

    private int _cannonballCode;
    private ulong _targetId;
    private int _damage;
    private float _speed;
    private Transform _targetTransform;


    public void Initialize(ulong targetId, int damage, float speed, int cannonballCode)
    {
        _targetId = targetId;
        _damage = damage;
        _speed = speed;
        _cannonballCode = cannonballCode;

        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(_targetId, out NetworkObject targetObject))
        {
            _targetTransform = targetObject.transform;
        }
    }

    private void Update()
    {
        // Sadece sunucuda hareket et. NetworkTransform bu hareketi client'lara senkronize eder.
        if (!IsServer) return;

        // Eğer hedefimiz yok olduysa veya bir sebepten bulunamıyorsa, mermiyi yok et.
        if (_targetTransform == null)
        {
            GetComponent<NetworkObject>().Despawn(); // Hedef yoksa direkt yok ol.
            return;
        }

        // Hedefe doğru hareket et.
        Vector3 direction = (_targetTransform.position - transform.position).normalized;
        transform.position += direction * _speed * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        // Çarptığımız objenin NetworkObject'ini al ve ID'sini bizim hedefimizin ID'si ile karşılaştır.
        if (other.TryGetComponent<NetworkObject>(out var networkObject) && networkObject.NetworkObjectId == _targetId)
        {
            // Eğer doğru hedefe çarptıysak, hasar ver ve kendini yok et.
            DoImpact(other.transform);
        }
    }

    private void DoImpact(Transform impactTarget)
    {
        // Hedefin Health bileşenini bul ve hasar ver.
        if (impactTarget.TryGetComponent<Health>(out Health targetHealth))
        {
            targetHealth.TakeDamage(_damage);
        }

        // DÜZELTME: Patlama efektini oynatmak için tüm client'lara komut gönder.
        PlayImpactEffectsClientRpc(transform.position);

        // Mermiyi yok et.
        GetComponent<NetworkObject>().Despawn();
    }


    // Bu metot sunucu tarafından çağrılır, ancak tüm client'larda çalışır.
    [ClientRpc]
    private void PlayImpactEffectsClientRpc(Vector3 position)
    {
        var cannonballDb = GameManager.Instance.CannonballDatabase;
        CannonballData cannonballData = cannonballDb.GetCannonballByCode(_cannonballCode);
        if (cannonballData == null || cannonballData.impactEffectPrefab == null) return;

        Instantiate(cannonballData.impactEffectPrefab, position, Quaternion.identity);
    }
}