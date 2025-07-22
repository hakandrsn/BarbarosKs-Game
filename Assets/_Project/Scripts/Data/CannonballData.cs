// Filename: CannonballData.cs (Final, Clean Template)
using UnityEngine;

[CreateAssetMenu(fileName = "Cannonball_120001", menuName = "BarbarosKs/Cannonball Template")]
public class CannonballData : ScriptableObject
{
    [Header("Kimlik (API ile Eşleştirme)")]
    public int cannonballCode; // API'deki 'Code' alanı ile eşleşecek

    [Header("Görsel Varlıklar (Sadece Unity'de)")]
    public Sprite icon;
    public GameObject projectilePrefab;
    public GameObject muzzleFlashPrefab;
    public GameObject impactEffectPrefab;
}