using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using smpc_dispatching.Core.Interfaces;
using smpc_dispatching.Core.Services;
using smpc_dispatching.UI.Views.Sales;
using smpc_sales_app.Data;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace smpc_sales_system.Pages.Sales
{
    // The Sales Calendar, shown inside the sales app.
    //
    // The screen itself is the dispatching app's - one implementation, two entry points, the
    // same arrangement BPI has between inventory and sales (CLAUDE.md) and the one this app
    // already uses for engineering's Item Request page. The access-level workbook lists SALES
    // CALENDAR under sales, so a sales user should not have to open another app for it.
    //
    // Dispatching resolves every view from a service container, so one is built here from its
    // own registrations (Program.BuildServiceProviderForHost) and pointed at the API this app
    // is signed in to, carrying this app's session token. The container is built once and
    // kept: rebuilding it per tab would open a second HttpClient and re-read the config each
    // time the page is opened.
    public class SalesCalendarPage : UserControl
    {
        private static IServiceProvider _dispatchingServices;
        private bool _loaded;

        public SalesCalendarPage()
        {
            Dock = DockStyle.Fill;
            BackColor = Color.White;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            // Designer-hosted previews and a second Load (the page is kept in RoutesServices'
            // table, so the same instance can be shown again) must not rebuild it.
            if (_loaded || DesignMode)
                return;

            _loaded = true;

            try
            {
                ShowCalendar();
            }
            catch (Exception ex)
            {
                // Contained on purpose: a calendar that cannot start says so in its own tab
                // rather than taking the quotation screens down with it.
                ShowMessage("The Sales Calendar could not be opened." + Environment.NewLine
                            + Environment.NewLine + ex.Message);
            }
        }

        private void ShowCalendar()
        {
            if (_dispatchingServices == null)
                _dispatchingServices = BuildDispatchingServices();

            // Every request the calendar makes carries this app's session; dispatching keeps
            // the token in a static of its own, which is set per use rather than once, since
            // signing out and back in issues a new one.
            HttpService.SessionToken = CacheData.SessionToken ?? "";

            // The calendar takes its department, title and detail panel from dispatching's
            // own "selected route" (CalendarScheduleControllerUC.GetDepartmentFromRoute).
            // Nothing sets that when the view is hosted here; the fallback happens to be
            // SALES today, which is right by luck rather than by intent, so say it outright.
            _dispatchingServices.GetRequiredService<IRouteService>().SelectRoute("SALES_CALENDAR");

            var calendar = _dispatchingServices.GetRequiredService<SalesViewUserControl>();
            calendar.Dock = DockStyle.Fill;

            Controls.Clear();
            Controls.Add(calendar);
        }

        private static IServiceProvider BuildDispatchingServices()
        {
            // Dispatching's HttpService reads its base address from AppSettings:ApiBaseUrl.
            // Supplied in memory from whatever this app resolved at startup (smpc.endpoints.xml,
            // then App.config - see Program.ApiBaseUrl) rather than from a json file beside the
            // exe, so the calendar can never end up pointed at a different API from the rest of
            // this app.
            string apiBaseUrl = smpc_sales_system.Program.ApiBaseUrl;

            if (string.IsNullOrWhiteSpace(apiBaseUrl))
                throw new InvalidOperationException(
                    "No API URL is configured for this app, so the calendar has nothing to read from.");

            // BaseAddress only combines with a relative path when it ends in a slash.
            if (!apiBaseUrl.EndsWith("/"))
                apiBaseUrl += "/";

            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    { "AppSettings:ApiBaseUrl", apiBaseUrl }
                })
                .Build();

            return smpc_dispatching.Program.BuildServiceProviderForHost(configuration);
        }

        private void ShowMessage(string text)
        {
            Controls.Clear();

            Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text = text,
                TextAlign = ContentAlignment.MiddleCenter,
                Padding = new Padding(24)
            });
        }
    }
}
