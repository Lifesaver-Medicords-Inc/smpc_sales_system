using System;
using System.Net.Http;
using System.Threading;
using System.Windows.Forms;

namespace smpc_sales_app.Services.Helpers
{
    // Connection-failure state shared by every API call in the app.
    //
    // This deliberately lives on a NON-generic class. RequestToApi<T> is generic, and a
    // static field on a generic type gets its own copy per closed type - so
    // RequestToApi<ApiResponseModel<Items>> and RequestToApi<ApiResponseModel<BomList>>
    // would each hold a separate "is a dialog already open?" flag, and a burst spanning
    // several response types would stack exactly the dialogs the gate exists to prevent.
    // One screen load touches a dozen different T's, so that is the normal case, not an
    // edge case.
    internal static class ApiConnection
    {
        static readonly object gate = new object();
        static bool dialogOpen;
        static DateTime lastShownUtc = DateTime.MinValue;
        static string lastMessage;

        static readonly TimeSpan RepeatWindow = TimeSpan.FromSeconds(5);

        // Bumped on every request that exhausted its retries. RunWithLoadingAsync snapshots
        // this before a module load and compares afterwards, which is how a screen knows
        // its data is incomplete and can offer to reload - without every service method
        // having to thread a success flag back up through its return type.
        static int failureCount;

        public static int FailureCount { get { return Volatile.Read(ref failureCount); } }

        public static void NoteFailure()
        {
            Interlocked.Increment(ref failureCount);
        }

        // Retry policy. Transport failures (tunnel drop, DNS blip, timeout) are transient
        // and worth retrying; an HTTP status code is a real answer from the server and
        // never is. Crucially this only ever applies to GET: replaying a POST/PUT/DELETE
        // that may have already reached the server is how you get two sales orders, two
        // POs, or two credit memos from one click. A write that fails, fails.
        public const int GetAttempts = 3;

        public static bool IsRetryable(HttpMethod method, Exception ex)
        {
            if (method != HttpMethod.Get) return false;

            return ex is HttpRequestException
                || ex is OperationCanceledException   // covers TaskCanceledException (timeout)
                || ex is System.Net.WebException
                || ex is System.IO.IOException;
        }

        public static TimeSpan BackoffFor(int attempt)
        {
            // 400ms, then 1200ms. Most tunnel blips clear inside the first wait, and the
            // total worst case stays under two seconds so a genuinely dead server still
            // reports quickly instead of hanging the loading overlay.
            return TimeSpan.FromMilliseconds(attempt == 1 ? 400 : 1200);
        }

        // HttpRequestException's own Message is permanently "An error occurred while
        // sending the request." - the real cause ("Unable to connect to the remote
        // server", "No such host is known") is on the innermost exception.
        public static string Describe(Exception ex)
        {
            Exception root = ex;
            while (root.InnerException != null)
                root = root.InnerException;

            return root.Message;
        }

        public static void ShowError(string url, Exception ex)
        {
            string message = "Cannot reach the server." + Environment.NewLine + Environment.NewLine
                + Describe(ex) + Environment.NewLine + Environment.NewLine
                + "Request: " + url;

            lock (gate)
            {
                if (dialogOpen) return;

                if (message == lastMessage && DateTime.UtcNow - lastShownUtc < RepeatWindow)
                    return;

                dialogOpen = true;
                lastMessage = message;
                lastShownUtc = DateTime.UtcNow;
            }

            try
            {
                MessageBox.Show(message, "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                lock (gate)
                {
                    dialogOpen = false;
                    // Restart the window when the dialog is dismissed, not when it opened -
                    // otherwise a box left up longer than RepeatWindow lets the next queued
                    // failure open a second one the instant it closes.
                    lastShownUtc = DateTime.UtcNow;
                }
            }
        }
    }
}
