// Filename: NpcData.cs
using UnityEngine;

[CreateAssetMenu(fileName = "NewNpcData", menuName = "BarbarosKs/NPC Data")]
public class NpcData : ScriptableObject
{
    [Header("Kimlik")]
    public string npcName = "Korsan";
    public GameObject shipPrefab; // Bu NPC'nin kullanacağı gemi prefab'ı

    [Header("Temel İstatistikler")]
    public int maxHull = 150;
    public float speed = 2f;
    public float maneuverability = 300f;
    public float attackRate = 5f;
    public float range = 25f;
    public int equippedCannonballCode = 120002; // Hangi gülleyle ateş edeceği (Demir Gülle)

    [Header("Yapay Zeka Davranışları")]
    public float aggroRadius = 30f; // Oyuncuyu ne kadar mesafeden fark edeceği
    public float patrolRadius = 50f; // Doğduğu noktanın ne kadar uzağında devriye gezeceği
}