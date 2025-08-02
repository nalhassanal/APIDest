using Entities;
using Process.Interface;
using RestSharp;
using System.Collections.Generic;
using System.ServiceModel;

namespace Process
{
    public class HomeProcess : IHomeProcess
    {
        // Call API here
        // For example, we Get /WeatherForecast
        public async Task<List<WeatherForecast>> GetWeatherForecast()
        {
            List<WeatherForecast> result = new List<WeatherForecast>();
            try
            {
                RestClient client = new RestClient("https://localhost:7056/");
                RestRequest request = new RestRequest(
                    "api/Home/GetWeatherForecast", Method.Get);
                request.RequestFormat = DataFormat.Json;
                request.Timeout = RestClientHelper.GetRestClientTimeOutTimeSpan();

                //var requestBody = new
                //{

                //};
                //request.AddJsonBody(requestBody);

                RestResponse<List<WeatherForecast>> response = await client.ExecuteAsync <List<WeatherForecast>>(request);
                if (response.IsSuccessful)
                {
                    result = response.Data;
                }
            }
            catch (FaultException fex)
            {
                throw new ApplicationException(fex.Message);
            }
            return result;
        }

        // Now, imagine if we build 5 callers here
    }
}
