// Filename: WorldSpaceUIFollower.cs
using UnityEngine;

public class WorldSpaceUIFollower : MonoBehaviour
{
    [Header("Takip Ayarları")]
    [Tooltip("Bu UI elementinin takip edeceği ana obje (Gemi).")]
    [SerializeField] private Transform _targetToFollow;
    
    [Tooltip("Takip edilecek objenin merkezine göre uygulanacak mesafe.")]
    [SerializeField] private Vector3 _positionOffset = new Vector3(0, -5f, 0); // Varsayılan olarak 5 birim aşağıda

    private Camera _mainCamera;

    void Start()
    {
        _mainCamera = Camera.main;

        // Eğer hedef Inspector'dan atanmamışsa, kendi parent'ını takip etmeyi dene.
        // Bu, script'i doğrudan HealthCanvas'a koyduğumuzda işe yarar.
        if (_targetToFollow == null)
        {
            _targetToFollow = transform.parent;
        }
    }

    void LateUpdate()
    {
        if (_mainCamera == null || _targetToFollow == null) return;

        // 1. Pozisyonu Ayarla:
        // Her karede, pozisyonumuzu hedefin pozisyonu + belirlediğimiz mesafe olarak ayarla.
        transform.position = _targetToFollow.position + _positionOffset;

        // 2. Rotasyonu Ayarla (Billboard Efekti):
        // Her karede, rotasyonumuzu kameranın rotasyonuyla aynı yap.
        transform.rotation = _mainCamera.transform.rotation;
    }
}