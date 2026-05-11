namespace LogisticsGroup.Web.Services
{
    public interface IRouteApiService
    {
        // Повертає дистанцію в км та час у годинах
        Task<(double DistanceKm, double TimeHours)> GetRouteInfoAsync(double startLat, double startLng, double endLat, double endLng);
    }
}