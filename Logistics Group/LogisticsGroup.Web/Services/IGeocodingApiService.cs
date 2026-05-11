namespace LogisticsGroup.Web.Services
{
    public interface IGeocodingApiService
    {
        Task<(double? Lat, double? Lng)> GetCoordinatesAsync(string address);
    }
}