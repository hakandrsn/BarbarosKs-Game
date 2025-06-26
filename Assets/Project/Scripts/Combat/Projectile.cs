using System.Collections;
using System.Collections.Generic;
using Project.Scripts.Interfaces;
using UnityEngine;

namespace BarbarosKs.Combat
{
    [RequireComponent(typeof(Rigidbody))]
    public class Projectile : MonoBehaviour
    {
        [Header("Projektil Ayarları")]
        [SerializeField] private float lifetime = 5f;
        [SerializeField] private int damage = 10;
        [SerializeField] private GameObject hitEffectPrefab;
        [SerializeField] private AudioClip hitSound;

        // Özel değişkenler
        private Rigidbody rb;
        private LayerMask targetLayers;
        private bool hasHit = false;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
        }

        private void Start()
        {
            // Belirli bir süre sonra kendiliğinden yok olsun
            Destroy(gameObject, lifetime);
        }

        public void Initialize(int newDamage, LayerMask newTargetLayers)
        {
            damage = newDamage;
            targetLayers = newTargetLayers;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (hasHit) return; // Zaten bir hedefe çarptıysa işlem yapma

            hasHit = true;

            // Hedefe hasar ver
            if (((1 << collision.gameObject.layer) & targetLayers) != 0)
            {
                if (collision.gameObject.TryGetComponent<IDamageable>(out var damageable))
                {
                    damageable.TakeDamage(damage);
                }
            }

            // Çarpma efekti
            if (hitEffectPrefab != null)
            {
                GameObject hitEffect = Instantiate(hitEffectPrefab, 
                    collision.contacts[0].point, 
                    Quaternion.LookRotation(collision.contacts[0].normal));

                // Efekti bir süre sonra yok et
                Destroy(hitEffect, 2f);
            }

            // Çarpma sesi
            if (hitSound != null)
            {
                AudioSource.PlayClipAtPoint(hitSound, transform.position);
            }

            // Kendini yok et
            Destroy(gameObject);
        }
    }
}
