using Newtonsoft.Json;
using smpc_sales_app.Data;
using smpc_sales_app.Pages.Sales;
using smpc_sales_system;
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
using System.Configuration;

namespace smpc_sales_app.Services.Helpers
{
    internal class RequestToApi<T> where T : class
    {
        static string baseUrl
        {
            get
            {
                string env =
                    ConfigurationManager.AppSettings["Environment"]
                    ?? "Development";

                // No hardcoded fallback URL - App.config's ApiBaseUrl.{env} is the one place
                // this is supposed to live, since it changes (localhost in dev, the real host
                // in production). Silently falling back to a hardcoded address just masks a
                // missing/misspelled App.config entry instead of surfacing it.
                string url = ConfigurationManager.AppSettings[$"ApiBaseUrl.{env}"];
                if (string.IsNullOrWhiteSpace(url))
                    throw new ConfigurationErrorsException($"App.config is missing \"ApiBaseUrl.{env}\" - add it under <appSettings> instead of relying on a hardcoded default.");

                return url;
            }
        }


        static CookieContainer cookieContainer = new CookieContainer();

        // silent: true skips the MessageBox this method otherwise always pops on any
        // exception (network error, timeout, etc.) - for a call that backs a purely
        // informational/convenience UI element (e.g. the sales quotation grid's per-item
        // stock indicator), a transient failure shouldn't interrupt the user with a raw
        // exception dialog; the caller just gets default(T) back and falls back
        // accordingly, same as it already does for a clean "no data" response.
        static private async Task<T> SendRequestAsync(string url, HttpMethod method, string body = null, bool silent = false)
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
                        client.DefaultRequestHeaders.Add("Authorization", CacheData.SessionToken);
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
                    if (!silent)
                    {
                        MessageBox.Show("Exception: " + ex.Message, "Error ");
                    }
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
        // Nested-body overload: every document built after Orders/CRM (Sales
        // Return, and the same shape used by Purchase Return/Credit Memo/
        // Debit Memo on the Go side) expects one POST carrying a strongly
        // typed header+details object in a single request, not the flat
        // Dictionary<string, dynamic> + separate child-row call Orders.cs
        // uses. Serializing the real object directly (instead of round-
        // tripping it through a Dictionary first) keeps nested arrays/objects
        // intact.
        static internal async Task<T> Post(string url, object data)
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
        static internal async Task<T> Put(string url, List<Dictionary<string, object>> data)
        {
            string jsonContent = JsonConvert.SerializeObject(data);
            return await SendRequestAsync(url, HttpMethod.Put, jsonContent);
        }
        // GET Method
        public static async Task<T> Get(string url, bool silent = false)
        {
            return await SendRequestAsync(url, HttpMethod.Get, silent: silent);
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
