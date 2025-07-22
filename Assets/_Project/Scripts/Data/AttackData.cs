// Filename: AttackData.cs
// Bu dosya, API ile konuşurken kullanacağımız veri yapılarını içerir.

[System.Serializable]
public class AttackRequestPayload
{
    public string AttackerShipId;
    public string TargetShipId;
}

[System.Serializable]
public class AttackResult
{
    public int damage;
    public bool isCritical;
}