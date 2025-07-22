// Filename: AutoDestroyParticle.cs
using UnityEngine;

public class AutoDestroyParticle : MonoBehaviour
{
    void Start()
    {
        // Particle sisteminin süresi bittiğinde objeyi yok et.
        Destroy(gameObject, GetComponent<ParticleSystem>().main.duration);
    }
}