using Newtonsoft.Json;
using smpc_sales_app.Data;
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
    class RequestToApi<T> where T : class
    {
        //DEV ENV
        static string baseUrl = "http://127.0.0.1:3000/api";

        //PROD ENV
        //static string baseUrl = "http://52.76.70.203:8000/api";

        //static string baseUrl = "https://b088-2001-4451-83a9-cd00-d35-514e-7116-c76a.ngrok-free.app/api";

        static Uri baseUri = new Uri(baseUrl); 

        // Create a CookieContainer to store cookies
        static CookieContainer cookieContainer = new CookieContainer();
         
        static private async Task<T> SendRequestAsync(string url, HttpMethod method, string body = null)
        {

            // Create an HttpClientHandler and assign the CookieContainer to it
            HttpClientHandler handler = new HttpClientHandler
            {
                CookieContainer = cookieContainer
            };

            using (HttpClient client = new HttpClient(handler))
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

                    if (CacheData.SessionToken != "")
                    {
                        client.DefaultRequestHeaders.Add("Authorization",   CacheData.SessionToken);
                    } 

                    // Perform the HTTP request asynchronously
                    HttpResponseMessage response = await client.SendAsync(requestMessage);


                    // Check if the response is successful
                    if (response.IsSuccessStatusCode)
                    {
                        string responseContent = await response.Content.ReadAsStringAsync();

                        if (string.IsNullOrEmpty(CacheData.SessionToken))
                        { 

                            List<String> tokenResponseArr = response.Headers.GetValues("Set-Cookie").ToList(); 
                            string token = ExtractToken(tokenResponseArr[0]); 
                            CacheData.SessionToken = token;
                        } 
                         
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
                    MessageBox.Show("Exception: " + "Call Senior Lem" + ex.Message, "Error ");
                    return default(T);  // Return default value of T in case of exception
                }
            }
        }

        //// POST Method
        static async Task<T> Post(string url, HttpContent data)
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

        private static string ExtractToken(string cookieString)
        {
            // Find the starting index of the token (after 'Authorization=')
            int tokenStartIndex = cookieString.IndexOf("Authorization=") + "Authorization=".Length;

            // Find the ending index of the token (before the first semicolon)
            int tokenEndIndex = cookieString.IndexOf(";", tokenStartIndex);

            // Extract the token
            string token = cookieString.Substring(tokenStartIndex, tokenEndIndex - tokenStartIndex);

            // If the semicolon is not found (for example, if there is no expiry info), extract until the end of the string
            if (tokenEndIndex == -1)
            {
                token = cookieString.Substring(tokenStartIndex);
            }

            return token;

        }

    }
}
