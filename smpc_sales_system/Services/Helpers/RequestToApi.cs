using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace smpc_inventory_app.Services.Helpers
{
    static class RequestToApi<T> where T : class
    {

        static private async Task<T> SendRequestAsync(string url, HttpMethod method, string body = null)
        {
            string baseUrl = "http://localhost:3000/api";
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    HttpContent content = null;
                    // If no content is provided, create an empty StringContent with Content-Type set to "application/json"
                    if (content == null && method != HttpMethod.Get)
                    {
                        content = new StringContent(body, Encoding.UTF8, "application/json");
                    }

                    // Create the HttpRequestMessage with the specified method (GET, POST, PUT, DELETE)
                    var requestMessage = new HttpRequestMessage(method, baseUrl + url)
                    {
                        Content = content
                    };

                    // Perform the HTTP request asynchronously
                    HttpResponseMessage response = await client.SendAsync(requestMessage);



                    // Check if the response is successful
                    if (response.IsSuccessStatusCode)
                    {
                        string responseContent = await response.Content.ReadAsStringAsync();

                        // Optionally, you can parse the responseContent into an object of type T
                        T result = JsonConvert.DeserializeObject<T>(responseContent);

                        // Display the response content (for debugging purposes)
                        //MessageBox.Show(responseContent, "API Response");

                        return result; // Return the parsed result
                    }

                    else
                    {
                        string responseContent = await response.Content.ReadAsStringAsync();

                        // Optionally, you can parse the responseContent into an object of type T
                        T result = JsonConvert.DeserializeObject<T>(responseContent);

                        // Display the response content (for debugging purposes)
                        //MessageBox.Show(responseContent, "API Response");

                        return result; // Return the
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Exception: " + "Call Senior Lem", "Error");
                    return default(T);  // Return default value of T in case of exception
                }
            }
        }

        //// POST Method
        static internal async Task<T> Post(string url, HttpContent data)
        {
            string jsonContent = JsonConvert.SerializeObject(data);

            return await SendRequestAsync(url, HttpMethod.Post, jsonContent);
        }

        static internal async Task<T> Post(string url, Dictionary<string, dynamic> data)
        {
            string jsonContent = JsonConvert.SerializeObject(data);

            return await SendRequestAsync(url, HttpMethod.Post, jsonContent);
        }

        // PUT Method
        static internal async Task<T> Put(string url, HttpContent data)
        {
            string jsonContent = JsonConvert.SerializeObject(data);

            return await SendRequestAsync(url, HttpMethod.Put, jsonContent);
        }
        static internal async Task<T> Put(string url, Dictionary<string, object> data)
        {
            string jsonContent = JsonConvert.SerializeObject(data);

            return await SendRequestAsync(url, HttpMethod.Put, jsonContent);
        }

        // GET Method
        public static async Task<T> Get(string url)
        {
            return await SendRequestAsync(url, HttpMethod.Get);
        }

        //DELETE Method
        static internal async Task<T> Delete(string url, HttpContent data)
        {
            string jsonContent = JsonConvert.SerializeObject(data);

            return await SendRequestAsync(url, HttpMethod.Delete, jsonContent);
        }

        static internal async Task<T> Delete(string url, Dictionary<string, object> data)
        {
            string jsonContent = JsonConvert.SerializeObject(data);

            return await SendRequestAsync(url, HttpMethod.Delete, jsonContent);
        }

    }
}
