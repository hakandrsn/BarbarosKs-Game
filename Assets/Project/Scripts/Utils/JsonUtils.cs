using UnityEngine;
using Newtonsoft.Json.Linq; // Newtonsoft.Json kütüphanesinin projenizde olması gerekir.

/// <summary>
/// JSON (özellikle JArray) ve Unity tipleri (Vector3, Quaternion) arasında
/// dönüşüm yapmak için yardımcı metotlar içeren statik bir sınıf.
/// </summary>
public static class JsonUtils
{
    /// <summary>
    /// Bir JArray'i Vector3'e dönüştürür.
    /// Örnek JArray: [1.0, 2.5, 3.0]
    /// </summary>
    /// <param name="array">Dönüştürülecek JArray.</param>
    /// <param name="defaultValue">Eğer array geçersizse döndürülecek varsayılan değer.</param>
    /// <returns>Dönüştürülmüş Vector3 veya varsayılan değer.</returns>
    public static Vector3 ParseVector3(JArray array, Vector3 defaultValue = default)
    {
        // JArray null veya eleman sayısı 3'ten az ise varsayılan değeri dön
        if (array == null || array.Count < 3)
        {
            return defaultValue;
        }

        try
        {
            return new Vector3(
                (float)array[0],
                (float)array[1],
                (float)array[2]
            );
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Vector3 parse hatası: {ex.Message}");
            return defaultValue;
        }
    }

    /// <summary>
    /// Bir JArray'i Quaternion'a dönüştürür. JArray'in Euler açılarını (x, y, z) içerdiği varsayılır.
    /// Örnek JArray: [0, 90.0, 0]
    /// </summary>
    /// <param name="array">Dönüştürülecek JArray (Euler açıları).</param>
    /// <param name="defaultValue">Eğer array geçersizse döndürülecek varsayılan değer.</param>
    /// <returns>Dönüştürülmüş Quaternion veya varsayılan değer.</returns>
    public static Quaternion ParseQuaternion(JArray array, Quaternion defaultValue = default)
    {
        // Quaternion için varsayılan değer olarak Quaternion.identity kullanmak daha mantıklıdır.
        if (defaultValue == default)
        {
            defaultValue = Quaternion.identity;
        }

        // JArray null veya eleman sayısı 3'ten az ise varsayılan değeri dön
        if (array == null || array.Count < 3)
        {
            return defaultValue;
        }

        try
        {
            return Quaternion.Euler(
                (float)array[0],
                (float)array[1],
                (float)array[2]
            );
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Quaternion parse hatası: {ex.Message}");
            return defaultValue;
        }
    }
}