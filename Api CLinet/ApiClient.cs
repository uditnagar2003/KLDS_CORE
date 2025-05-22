// Add this file to keylogger core project (e.g., Core/Api/ApiClient.cs)
using System;
using System.Net.Http;
using System.Net.Http.Json; // Requires System.Net.Http.Json NuGet if not already referenced
using System.Threading.Tasks;
using keylogger_lib.Entities; // Reference keylogger lib project

namespace VisualKeyloggerDetector.Core.Api
{
    public class ApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiBaseUrl; // e.g., "https://localhost:7001/api"

        // Consider injecting HttpClientFactory for better management
        public ApiClient(string apiBaseUrl)
        {
            _apiBaseUrl = apiBaseUrl.TrimEnd('/');
            _httpClient = new HttpClient();
            // TODO: Add authentication header handling if API requires it
            // _httpClient.DefaultRequestHeaders.Authorization =
            //     new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "YOUR_JWT_TOKEN");
        }

        /// <summary>
        /// Sends a detected keylogger log entry to the backend API.
        /// </summary>
        /// <param name="log">The KeyloggerLog object to send.</param>
        /// <returns>True if successful, False otherwise.</returns>
        public async Task<bool> SendDetectionLogAsync(KeyloggerLog log)
        {
            if (log == null) return false;

            string url = $"{_apiBaseUrl}/Keylogger/add";
            try
            {
                // The API expects a KeyloggerLog object in the body
                HttpResponseMessage response = await _httpClient.PostAsJsonAsync(url, log);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Successfully sent log for PID {log.Process_Id} to API.");
                    return true;
                }
                else
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Failed to send log for PID {log.Process_Id}. Status: {response.StatusCode}. Error: {errorContent}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception sending log for PID {log.Process_Id}: {ex.Message}");
                return false;
            }
        }
    }
}
