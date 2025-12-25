using System.Collections.Generic;
using System.Threading.Tasks;
using BarbarosKs.Shared.DTOs;
using UnityEngine;

namespace _Project.Scripts.Api
{
    public class GameDataApiService : BaseApiService, IGameService
    {
        // Backend'deki "Items" tablosundan sadece Gülleleri (Type=1) veya hepsini çekeceğiz.
        // Backend endpoint'inin "/api/cannonballs/all" olduğunu varsayıyorum.
        public async Task<List<CannonballDto>> GetAllCannonballTypesAsync()
        {
            var result = await GetAsync<List<CannonballDto>>("/api/cannonballs/all");
            if (result == null)
            {
                Debug.LogError("[GameDataApi] Gülle verileri çekilemedi! Backend çalışıyor mu?");
                return new List<CannonballDto>();
            }

            return result;
        }
    }
}