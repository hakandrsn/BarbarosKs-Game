// Filename: CannonballDatabase.cs
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "CannonballDatabase", menuName = "BarbarosKs/Cannonball Database")]
public class CannonballDatabase : ScriptableObject
{
    public List<CannonballData> allCannonballTemplates;

    public CannonballData GetCannonballByCode(int code)
    {
        return allCannonballTemplates.FirstOrDefault(c => c.cannonballCode == code);
    }
}