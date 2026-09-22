using smpc_sales_app.Pages;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Serilog;
using smpc_sales_system.Config;
using Microsoft.Extensions.Configuration;
using System.Configuration;

namespace smpc_sales_system
{
    public static class Program
    {
        public static string ApiBaseUrl { get; private set; }
        public static string WssBaseUrl { get; private set; }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            LoggerConfig.Configure();
            // Read environment once at startup
            string env = System.Configuration.ConfigurationManager.AppSettings["Environment"] ?? "Development";

            // Resolve the correct API URL
            // smpc.endpoints.xml wins when present; App.config is the fallback.
            ApiBaseUrl = SmpcEndpoints.Api(
                System.Configuration.ConfigurationManager.AppSettings[$"ApiBaseUrl.{env}"])
                         ?? throw new ConfigurationErrorsException($"No API URL configured for environment: {env}");

            Log.Information("Running in {Environment} environment", env);
            Log.Information("API URL: {Url}", ApiBaseUrl);

            // The engineering assembly is hosted in-process (shared ItemSetUC +
            // engineering sub-login at sign-in) so its own Main never runs and its
            // ApiBaseUrl would stay null - every engineering call then fell back to
            // http://127.0.0.1:3000/api, and a PC with no local API (e.g. VPN-only)
            // failed sign-in with a bare "Something went wrong". Point it at this
            // process's resolved URL: production on production PCs, dev URL under
            // Development. WssBaseUrl is this app's own unresolved null, so only
            // the API URL is mirrored.
            smpc_engineering_app.Program.Configure(ApiBaseUrl);

            string reportsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports");
            smpc_sales_system.Properties.Settings.Default.REPORTPATH = reportsFolder;
            Log.Information("Report path resolved to: {ReportsFolder}", reportsFolder);

            // Set application-wide currency format to Philippine Peso
            CultureInfo culture = new CultureInfo("en-PH");
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;

            // Global crash guard (mirrors dispatching's Program.cs). Without this, an
            // unhandled exception on the UI thread - e.g. an "Index out of range" during a
            // grid/list bind - showed the raw .NET Continue/Quit dialog. CatchException
            // routes UI-thread exceptions here so the app keeps running; the full stack is
            // written to the Serilog log for diagnosis and the user sees a clean message.
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (s, e) =>
            {
                try { Serilog.Log.Error(e.Exception, "Unhandled UI-thread exception"); } catch { }
                MessageBox.Show(
                    "Something went wrong and that action could not be completed." + Environment.NewLine + Environment.NewLine
                    + e.Exception.Message + Environment.NewLine + Environment.NewLine
                    + "The app will keep running. Full details were saved to the log.",
                    "Unexpected Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            };
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                var ex = e.ExceptionObject as Exception;
                try { Serilog.Log.Error(ex, "Unhandled non-UI exception"); } catch { }
                MessageBox.Show(
                    "A serious error occurred." + Environment.NewLine + Environment.NewLine
                    + (ex?.Message ?? "Unknown error") + Environment.NewLine + Environment.NewLine
                    + "Details were saved to the log.",
                    "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Layout());
            EndServerSession(ApiBaseUrl, smpc_sales_app.Data.CacheData.SessionToken);
        }

        // Ends this session on the server as the app closes. The API revokes a token on
        // /logout; no app ever called it, so a closed app's token stayed usable for the rest
        // of its 24 hours. Best effort - a slow or unreachable API must not hold up closing.
        private static void EndServerSession(string apiBaseUrl, string token)
        {
            if (string.IsNullOrWhiteSpace(apiBaseUrl) || string.IsNullOrWhiteSpace(token)) return;
            try
            {
                using (var client = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(3) })
                {
                    client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", token);
                    // Task.Run keeps the blocking wait off the UI synchronization context.
                    System.Threading.Tasks.Task.Run(() => client.PostAsync(apiBaseUrl.TrimEnd('/') + "/logout", null))
                        .Wait(TimeSpan.FromSeconds(3));
                }
            }
            catch
            {
                // Closing the app matters more than telling the server.
            }
        }
    }
}