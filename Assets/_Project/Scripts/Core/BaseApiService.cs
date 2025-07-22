// Filename: BaseApiService.cs (Final, Complete Version)
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

public abstract class BaseApiService
{
    private const string API_BASE_URL = "https://localhost:7272";
    private const string SERVER_SECRET_KEY = "BarbarosKs_SuperGizli_Sunucu_Anahtari_12345";

    protected static readonly HttpClient HttpClient;
    protected static string JwtToken;

    static BaseApiService()
    {
        HttpClient = new HttpClient { BaseAddress = new Uri(API_BASE_URL) };
        // This secret key is for server-to-server communication, ensuring that
        // requests from our Unity server are trusted by the Web API.
        HttpClient.DefaultRequestHeaders.Add("X-Server-Secret", SERVER_SECRET_KEY);
    }

    protected void SetToken(string token)
    {
        JwtToken = token;
        // This adds the player's JWT token for player-specific actions.
        HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", JwtToken);
    }

    protected static async Task<T> GetAsync<T>(string endpoint, bool requireAuth = true)
    {
        if (requireAuth && string.IsNullOrEmpty(JwtToken))
        {
            Debug.LogError($"Authentication required, but no token found for GET request: {endpoint}");
            return default;
        }

        try
        {
            var response = await HttpClient.GetAsync(endpoint);
            var responseJson = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<T>(responseJson);
            }

            Debug.LogError($"API GET Error: {response.StatusCode} - {responseJson} ({endpoint})");
            return default;
        }
        catch (Exception e)
        {
            Debug.LogError($"Exception during API GET request: {e.Message} ({endpoint})");
            return default;
        }
    }

    protected async Task<TResponse> PostAsync<TRequest, TResponse>(string endpoint, TRequest payload, bool requireAuth = true)
    {
        if (requireAuth && string.IsNullOrEmpty(JwtToken))
        {
            Debug.LogError($"Authentication required, but no token found for POST request: {endpoint}");
            return default;
        }

        try
        {
            var jsonPayload = JsonConvert.SerializeObject(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var response = await HttpClient.PostAsync(endpoint, content);
            var responseJson = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<TResponse>(responseJson);
            }

            
            // Try to deserialize regardless of status code, as the body might contain error details.
            try { return JsonConvert.DeserializeObject<TResponse>(responseJson); }
            catch { return default; } // Eğer bu da başarısız olursa, null dön.
        }
        catch (Exception e)
        {
            Debug.LogError($"Exception during API POST request: {e.Message} ({endpoint})");
            return default;
        }
    }

    /// <summary>
    /// Sends a PUT request to the specified endpoint to update a resource.
    /// </summary>
    protected async Task<TResponse> PutAsync<TRequest, TResponse>(string endpoint, TRequest payload, bool requireAuth = true)
    {
        if (requireAuth && string.IsNullOrEmpty(JwtToken))
        {
            Debug.LogError($"Authentication required, but no token found for PUT request: {endpoint}");
            return default;
        }

        try
        {
            var jsonPayload = JsonConvert.SerializeObject(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var response = await HttpClient.PutAsync(endpoint, content);
            var responseJson = await response.Content.ReadAsStringAsync();
            
            return JsonConvert.DeserializeObject<TResponse>(responseJson);
        }
        catch (Exception e)
        {
            Debug.LogError($"Exception during API PUT request: {e.Message} ({endpoint})");
            return default;
        }
    }

    /// <summary>
    /// Sends a DELETE request to the specified endpoint to delete a resource.
    /// </summary>
    /// <returns>True if the deletion was successful (e.g., 200 OK or 204 No Content), false otherwise.</returns>
    protected async Task<bool> DeleteAsync(string endpoint, bool requireAuth = true)
    {
        if (requireAuth && string.IsNullOrEmpty(JwtToken))
        {
            Debug.LogError($"Authentication required, but no token found for DELETE request: {endpoint}");
            return false;
        }

        try
        {
            var response = await HttpClient.DeleteAsync(endpoint);
            
            if (!response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync();
                Debug.LogError($"API DELETE Error: {response.StatusCode} - {responseJson} ({endpoint})");
            }

            return response.IsSuccessStatusCode;
        }
        catch (Exception e)
        {
            Debug.LogError($"Exception during API DELETE request: {e.Message} ({endpoint})");
            return false;
        }
    }
}