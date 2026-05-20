using System.Net;

namespace BankApp.Services
{
    public static class GeoLocationService
    {
        public static string GetLocation()
        {
            try
            {
                using (WebClient client = new WebClient())
                {
                    string country =
                        client.DownloadString("https://ipapi.co/country_name/");

                    string city =
                        client.DownloadString("https://ipapi.co/city/");

                    return $"{city.Trim()}, {country.Trim()}";
                }
            }
            catch
            {
                return "Неизвестное местоположение";
            }
        }
    }
}