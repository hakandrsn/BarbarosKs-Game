// Filename: Health.cs (Simplified Version)

using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class Health : NetworkBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private int _maxHealth = 100;
    [SerializeField] private Slider _healthBar;
    
    [Header("Destruction Settings")]
    [SerializeField] private GameObject _destructionEffectPrefab; // Yok olma efekti (patlama, batma vb.)
    [SerializeField] private float _destructionDelay = 1.5f; // Efektin oynatılacağı süre

    public NetworkVariable<int> MaxHealth = new NetworkVariable<int>(100);
    public NetworkVariable<int> CurrentHealth = new NetworkVariable<int>();
    
    // Geminin ölüm sürecinde olup olmadığını tüm client'lara bildirir.
    private NetworkVariable<bool> _isDead = new NetworkVariable<bool>(false);
    public void Initialize(int maxHealth, int currentHealth)
    {
        if (!IsServer) return;
        MaxHealth.Value = maxHealth;
        CurrentHealth.Value = currentHealth;
        _isDead.Value = false;
    }

    public override void OnNetworkSpawn()
    {
        // Olaylara abone ol.
        MaxHealth.OnValueChanged += OnMaxHealthChanged;
        CurrentHealth.OnValueChanged += OnCurrentHealthChanged;

        // Başlangıç değerlerini UI'a yansıt.
        OnMaxHealthChanged(0, MaxHealth.Value);
        OnCurrentHealthChanged(0, CurrentHealth.Value);
    }

    public override void OnNetworkDespawn()
    {
        // Abonelikten çık.
        MaxHealth.OnValueChanged -= OnMaxHealthChanged;
        CurrentHealth.OnValueChanged -= OnCurrentHealthChanged;
    }

    private void OnMaxHealthChanged(int previousValue, int newValue)
    {
        if (_healthBar) _healthBar.maxValue = newValue;
    }

    private void OnCurrentHealthChanged(int previousValue, int newValue)
    {
        if (_healthBar) _healthBar.value = newValue;
    }

    public void TakeDamage(int damage)
    {
        if (!IsServer || _isDead.Value) return; 
        
        CurrentHealth.Value = Mathf.Max(0, CurrentHealth.Value - damage);
        
        if (CurrentHealth.Value <= 0)
        {
            // --- ÖLÜM SÜRECİNİ BAŞLAT ---
            _isDead.Value = true;
            // StartDestructionSequenceClientRpc(); // Tüm client'lara animasyonu başlatmalarını söyle.
            StartCoroutine(DestroyAfterDelay());  // Sunucuda, gecikmeli yok etme işlemini başlat.

        }
    }
    
    // [ClientRpc]
    // private void StartDestructionSequenceClientRpc()
    // {
    //     // Bu kod, sunucu dahil tüm client'larda çalışır.
    //     Debug.Log($"Gemi {NetworkObjectId} için yok olma sekansı client'ta başlatıldı.");
    //
    //     // Görsel efektleri oynat
    //     if (_destructionEffectPrefab != null)
    //     {
    //         Instantiate(_destructionEffectPrefab, transform.position, transform.rotation);
    //     }
    //
    //     // Eğer bu geminin sahibi bizsek, kontrolleri devre dışı bırak.
    //     if (IsOwner)
    //     {
    //         GetComponent<PlayerController>().DisableControls();
    //     }
    //
    //     // Gemi modelini ve UI'ı gizle
    //     // (Renderer'ları ve Canvas'ı kapatarak gemiyi görünmez yapabiliriz)
    //     // Örnek: GetComponentInChildren<MeshRenderer>().enabled = false;
    //     // Örnek: transform.Find("HealthCanvas").gameObject.SetActive(false);
    // }
    
    private IEnumerator DestroyAfterDelay()
    {
        // Bu coroutine SADECE SUNUCUDA çalışır.
        yield return new WaitForSeconds(_destructionDelay);

        // Belirtilen süre sonunda, network objesini tüm client'lardan kaldır.
        GetComponent<NetworkObject>().Despawn(true);
    }
}