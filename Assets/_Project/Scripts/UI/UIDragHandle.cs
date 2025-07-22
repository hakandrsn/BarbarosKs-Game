// Filename: UIDragHandle.cs (Final, Robust Version)
using UnityEngine;
using UnityEngine.EventSystems;

public class UIDragHandle : MonoBehaviour, IDragHandler
{
    [Tooltip("Bu tutamaç, hangi pencereyi sürükleyecek? Genellikle parent'ıdır.")]
    [SerializeField] private RectTransform _targetRectTransform;

    private Canvas _canvas;

    private void Awake()
    {
        // Hedef atanmamışsa, kendi parent'ını sürüklemeye çalış.
        if (_targetRectTransform == null)
        {
            _targetRectTransform = transform.parent.GetComponent<RectTransform>();
        }
        
        // Bu script'in bulunduğu en üst seviye Canvas'ı bul.
        _canvas = GetComponentInParent<Canvas>();
    }

    // Fare basılı tutulup sürüklendiği her kare çalışır.
    public void OnDrag(PointerEventData eventData)
    {
        if (_targetRectTransform == null) return;

        // Farenin hareket miktarını (delta), Canvas'ın ölçeğine bölerek
        // doğru hareket miktarını buluyoruz. Bu, "Scale With Screen Size"
        // modunda doğru çalışmasını sağlar.
        _targetRectTransform.anchoredPosition += eventData.delta / _canvas.scaleFactor;
    }
}