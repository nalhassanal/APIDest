using Entities;

namespace Process.Interface
{
    public interface IHomeProcess
    {
        Task<List<WeatherForecast>> GetWeatherForecast();
    }
}
