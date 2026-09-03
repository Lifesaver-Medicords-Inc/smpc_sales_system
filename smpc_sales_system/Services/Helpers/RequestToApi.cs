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

        // Callers universally read response.Data straight off the result, so handing back
        // null on failure turned every unreachable-API call into a NullReferenceException
        // inside the service layer (ItemService.GetItem, ProjectService.GetBom, and 40-odd
        // others all have the same shape). An empty envelope keeps .Data null - which every
        // caller's existing "no data" path already handles - instead of crashing first.
        private static T EmptyResponse()
        {
            try
            {
                return Activator.CreateInstance<T>();
            }
            catch
            {
                return default(T);
            }
        }

        // silent: true skips the connection-error dialog this method otherwise shows once
        // a request has exhausted its retries - for a call that backs a purely
        // informational/convenience UI element (e.g. the sales quotation grid's per-item
        // stock indicator), a transient failure shouldn't interrupt the user; the caller
        // just gets an empty envelope back and falls back accordingly, same as it already
        // does for a clean "no data" response. It is still counted as a failure, so the
        // owning screen can still offer to reload.
        static private async Task<T> SendRequestAsync(string url, HttpMethod method, string body = null, bool silent = false)
        {
            // Retries only ever apply to GET - see ApiConnection.IsRetryable for why a
            // write is never replayed.
            int maxAttempts = method == HttpMethod.Get ? ApiConnection.GetAttempts : 1;
            Exception lastError = null;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                // Both the handler and the client are rebuilt per attempt: an
                // HttpRequestMessage cannot be re-sent, and a disposed client cannot be
                // reused. The CookieContainer is a shared static, so the session survives.
                HttpClientHandler handler = new HttpClientHandler
                {
                    CookieContainer = cookieContainer
                };

                using (HttpClient client = new HttpClient(handler))
                {
                    try
                    {
                        HttpContent content = null;
                        if (method != HttpMethod.Get)
                        {
                            content = new StringContent(body, Encoding.UTF8, "application/json");
                        }

                        var requestMessage = new HttpRequestMessage(method, baseUrl + url)
                        {
                            Content = content
                        };

                        if (CacheData.SessionToken != "")
                        {
                            client.DefaultRequestHeaders.Add("Authorization", CacheData.SessionToken);
                        }

                        HttpResponseMessage response = await client.SendAsync(requestMessage);

                        if (response.IsSuccessStatusCode)
                        {
                            string responseContent = await response.Content.ReadAsStringAsync();

                            // Only the login endpoint issues a Set-Cookie; every other call
                            // succeeds without one. HttpResponseHeaders.GetValues THROWS
                            // ("The given header was not found") rather than returning null
                            // when the header is absent, so any successful non-login response
                            // arriving while the token is still empty - anything the login
                            // screen itself fetches before sign-in - blew up here. TryGetValues
                            // is the non-throwing form: capture the token when it's actually
                            // present, otherwise carry on.
                            if (string.IsNullOrEmpty(CacheData.SessionToken)
                                && response.Headers.TryGetValues("Set-Cookie", out var setCookieValues))
                            {
                                List<String> tokenResponseArr = setCookieValues.ToList();
                                if (tokenResponseArr.Count > 0)
                                {
                                    string token = ExtractToken(tokenResponseArr[0]);
                                    if (!string.IsNullOrEmpty(token))
                                    {
                                        CacheData.SessionToken = token;
                                    }
                                }
                            }

                            // An empty body deserializes to null without throwing, which is
                            // the other way callers ended up dereferencing null - treat it
                            // the same as a failure and hand back an empty envelope.
                            T okResult = JsonConvert.DeserializeObject<T>(responseContent);
                            return okResult ?? EmptyResponse();
                        }
                        else
                        {
                            // A status code is a real answer from the server, not a transport
                            // failure - never retried, and not counted as a connection
                            // failure. It still carries the API's own
                            // {"Success":false,"message":...} envelope, which callers inspect.
                            string responseContent = await response.Content.ReadAsStringAsync();
                            T errResult = JsonConvert.DeserializeObject<T>(responseContent);
                            return errResult ?? EmptyResponse();
                        }
                    }
                    catch (Exception ex)
                    {
                        lastError = ex;

                        if (attempt < maxAttempts && ApiConnection.IsRetryable(method, ex))
                        {
                            await Task.Delay(ApiConnection.BackoffFor(attempt));
                            continue;
                        }

                        break;
                    }
                }
            }

            // Out of attempts. Record it either way - a screen that suppressed the dialog
            // still needs to know its data is incomplete.
            ApiConnection.NoteFailure();

            if (!silent && lastError != null)
            {
                ApiConnection.ShowError(url, lastError);
            }

            return EmptyResponse();
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
        // Returns null when the cookie carries no Authorization value, rather than throwing.
        // This computed the Substring BEFORE testing tokenEndIndex for -1, so a Set-Cookie
        // with no trailing semicolon threw ArgumentOutOfRangeException on the line above the
        // check meant to handle exactly that case; and a cookie with no "Authorization=" at
        // all left IndexOf at -1, putting the start index at 13 and slicing from the middle
        // of whatever was there. Both surfaced as a bare exception dialog at login.
        // Matches the same helper in the inventory, accounting and engineering apps.
        private static string ExtractToken(string cookieString)
        {
            if (string.IsNullOrEmpty(cookieString)) return null;

            const string marker = "Authorization=";
            int markerIndex = cookieString.IndexOf(marker);
            if (markerIndex < 0) return null;

            int tokenStartIndex = markerIndex + marker.Length;
            int tokenEndIndex = cookieString.IndexOf(";", tokenStartIndex);

            // No semicolon means there is no expiry info after it - take the rest.
            return tokenEndIndex < 0
                ? cookieString.Substring(tokenStartIndex)
                : cookieString.Substring(tokenStartIndex, tokenEndIndex - tokenStartIndex);
        }
    }
}
