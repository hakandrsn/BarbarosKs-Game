// Filename: CameraManager.cs (ULTRA-SAFE FINAL VERSION)
using Unity.Cinemachine;
using UnityEngine;

[RequireComponent(typeof(CinemachineCamera))]
public class CameraManager : MonoBehaviour
{
    private CinemachineCamera _cinemachineCamera;

    private void Awake()
    {
        _cinemachineCamera = GetComponent<CinemachineCamera>();
        PlayerController.OnLocalPlayerShipReady += InitializeCamera;
    }

    private void OnDestroy()
    {
        PlayerController.OnLocalPlayerShipReady -= InitializeCamera;
    }

    /// <summary>
    /// Bu metot artık SADECE kameranın ana hedeflerini atar.
    /// </summary>
    private void InitializeCamera(Transform playerShipTransform)
    {
        if (playerShipTransform == null) return;
        
        Debug.Log($"Kamera Follow ve LookAt hedefleri atanıyor: {playerShipTransform.name}");

        // KODUN TEK GÖREVİ BUDUR:
        _cinemachineCamera.Follow = playerShipTransform;
        _cinemachineCamera.LookAt = playerShipTransform;

        Debug.LogWarning("Kamera hedefleri atandı. Damping ve Offset gibi diğer tüm ayarları Inspector üzerinden manuel olarak yapınız.");
    }
}