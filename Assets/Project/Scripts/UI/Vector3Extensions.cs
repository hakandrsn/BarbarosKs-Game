using UnityEngine;

// Bu script, System.Numerics.Vector3 ve UnityEngine.Vector3 arasında
// kolayca dönüşüm yapmak için "extension" metotları sağlar.
public static class Vector3Extensions
{
    /// <summary>
    /// UnityEngine.Vector3 tipini System.Numerics.Vector3 tipine dönüştürür.
    /// </summary>
    public static System.Numerics.Vector3 ToNumeric(this UnityEngine.Vector3 unityVector)
    {
        return new System.Numerics.Vector3(unityVector.x, unityVector.y, unityVector.z);
    }

    /// <summary>
    /// System.Numerics.Vector3 tipini UnityEngine.Vector3 tipine dönüştürür.
    /// DİKKAT: System.Numerics.Vector3'ün property'leri büyük harfle başlar (X, Y, Z).
    /// </summary>
    public static UnityEngine.Vector3 ToUnity(this System.Numerics.Vector3 numericVector)
    {
        return new UnityEngine.Vector3(numericVector.X, numericVector.Y, numericVector.Z);
    }
}