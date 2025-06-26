using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json.Linq;

namespace Project.Scripts.Network
{
    /// <summary>
    /// MessageRouter, ağ üzerinden gelen mesajları mesaj tipine göre işler ve ilgili işleyicilere yönlendirir.
    /// </summary>
    [Serializable]
    public class MessageRouter
    {
        // Mesaj işleyici delegeleri
        public delegate void MessageHandler(JObject messageData);

        // Mesaj işleyicileri sözlüğü
        private Dictionary<string, List<MessageHandler>> messageHandlers = new Dictionary<string, List<MessageHandler>>();

        // Varsayılan işleyici (tanımlanmamış mesaj tipleri için)
        private MessageHandler defaultHandler;

        /// <summary>
        /// Belirli bir mesaj tipi için bir işleyici ekler
        /// </summary>
        /// <param name="messageType">Mesaj tipi</param>
        /// <param name="handler">İşleyici fonksiyon</param>
        public void RegisterHandler(string messageType, MessageHandler handler)
        {
            if (string.IsNullOrEmpty(messageType) || handler == null) return;

            if (!messageHandlers.ContainsKey(messageType))
            {
                messageHandlers[messageType] = new List<MessageHandler>();
            }

            messageHandlers[messageType].Add(handler);
        }

        /// <summary>
        /// Belirli bir mesaj tipi için bir işleyiciyi kaldırır
        /// </summary>
        /// <param name="messageType">Mesaj tipi</param>
        /// <param name="handler">İşleyici fonksiyon</param>
        public void UnregisterHandler(string messageType, MessageHandler handler)
        {
            if (string.IsNullOrEmpty(messageType) || handler == null) return;

            if (messageHandlers.ContainsKey(messageType))
            {
                messageHandlers[messageType].Remove(handler);

                // Liste boşsa, mesaj tipini tamamen kaldır
                if (messageHandlers[messageType].Count == 0)
                {
                    messageHandlers.Remove(messageType);
                }
            }
        }

        /// <summary>
        /// Bilinmeyen mesaj tipleri için varsayılan bir işleyici ayarlar
        /// </summary>
        public void SetDefaultHandler(MessageHandler handler)
        {
            defaultHandler = handler;
        }

        /// <summary>
        /// Bir mesajı alır ve uygun işleyicilere yönlendirir
        /// </summary>
        /// <param name="jsonMessage">JSON formatında mesaj</param>
        public void RouteMessage(string jsonMessage)
        {
            if (string.IsNullOrEmpty(jsonMessage)) return;

            try
            {
                // JSON'ı parse et
                JObject messageObj = JObject.Parse(jsonMessage);

                // Mesaj tipini al
                string messageType = messageObj["type"]?.ToString();

                if (string.IsNullOrEmpty(messageType))
                {
                    Debug.LogWarning("Mesajda 'type' alanı bulunamadı!");
                    defaultHandler?.Invoke(messageObj);
                    return;
                }

                // İlgili işleyicileri çağır
                if (messageHandlers.TryGetValue(messageType, out var handlers))
                {
                    foreach (var handler in handlers)
                    {
                        try
                        {
                            handler.Invoke(messageObj);
                        }
                        catch (Exception e)
                        {
                            Debug.LogError($"Mesaj işleme hatası (tip: {messageType}): {e.Message}");
                        }
                    }
                }
                else if (messageHandlers.TryGetValue("*", out var genericHandlers))
                {
                    // Özel işleyici yoksa ve "*" için işleyici varsa (tüm mesajlar için)
                    foreach (var handler in genericHandlers)
                    {
                        try
                        {
                            handler.Invoke(messageObj);
                        }
                        catch (Exception e)
                        {
                            Debug.LogError($"Generic mesaj işleyici hatası: {e.Message}");
                        }
                    }
                }
                else
                {
                    // Eğer bu mesaj tipi için işleyici yoksa, varsayılan işleyiciyi kullan
                    if (defaultHandler != null)
                    {
                        defaultHandler.Invoke(messageObj);
                    }
                    else
                    {
                        Debug.Log($"'{messageType}' mesaj tipi için işleyici bulunamadı");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Mesaj yönlendirme hatası: {e.Message}\nMesaj: {jsonMessage}");
            }
        }

        /// <summary>
        /// Tüm işleyicileri temizler
        /// </summary>
        public void ClearAllHandlers()
        {
            messageHandlers.Clear();
            defaultHandler = null;
        }

        /// <summary>
        /// Belirli bir mesaj tipi için tüm işleyicileri temizler
        /// </summary>
        /// <param name="messageType">Mesaj tipi</param>
        public void ClearHandlersForType(string messageType)
        {
            if (messageHandlers.ContainsKey(messageType))
            {
                messageHandlers.Remove(messageType);
            }
        }
    }
}