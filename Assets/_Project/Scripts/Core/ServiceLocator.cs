// Filename: ServiceLocator.cs
using System;
using System.Collections.Generic;
using UnityEngine;

public class ServiceLocator
{
    // Singleton deseni ile ServiceLocator'a global erişim sağlıyoruz.
    public static ServiceLocator Current { get; private set; }

    // Servisleri tiplerine göre saklayacağımız bir sözlük (dictionary).
    private readonly Dictionary<Type, IGameService> _services = new Dictionary<Type, IGameService>();

    public ServiceLocator()
    {
        if (Current != null)
        {
            // Zaten bir ServiceLocator varsa, yeni bir tane oluşturulmasını engelle.
            Debug.LogError("Bir ServiceLocator örneği zaten mevcut.");
            return;
        }
        Current = this;
    }

    /// <summary>
    /// Bir servisi kaydeder.
    /// </summary>
    /// <typeparam name="T">Servisin tipi.</typeparam>
    /// <param name="service">Kaydedilecek servis örneği.</param>
    public void Register<T>(T service) where T : IGameService
    {
        var type = typeof(T);
        if (_services.TryAdd(type, service)) return;
        Debug.LogWarning($"Servis tipi {type.Name} zaten kayıtlı. Üzerine yazılıyor.");
        _services[type] = service;
    }

    /// <summary>
    /// Kayıtlı bir servisi getirir.
    /// </summary>
    /// <typeparam name="T">Getirilecek servisin tipi.</typeparam>
    /// <returns>İstenen servis örneği.</returns>
    public T Get<T>() where T : IGameService
    {
        var type = typeof(T);
        if (!_services.TryGetValue(type, out var service))
        {
            Debug.LogError($"Servis tipi {type.Name} bulunamadı.");
            throw new InvalidOperationException($"Servis bulunamadı: {type.Name}");
        }
        return (T)service;
    }
}