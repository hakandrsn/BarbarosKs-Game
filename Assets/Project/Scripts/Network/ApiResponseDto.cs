using System;

/// <summary>
/// API'den gelen yanıtları standart formatta almak için kullanılan DTO
/// </summary>
[Serializable]
public class ApiResponseDto<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T Data { get; set; }
    public string Error { get; set; } = string.Empty;
} 