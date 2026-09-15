using smpc_app.Services.Helpers;
using smpc_sales_app.Data;
using smpc_sales_app.Services.Helpers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Forms;

namespace smpc_sales_system.Pages.Sales
{
    // The charges modal behind the Sales Order's CANCEL (spec 5.4, 8.15).
    //
    // Cancelling an approved SO is a two-stage act. Any sales executive may raise
    // one on an order they own - they hold the client relationship and know the
    // commercial terms - but PROCEED only submits it. Nothing moves until the
    // Sales Manager or the CBDO approves: stock stays reserved, every department's
    // view is unchanged, and A/R sees nothing.
    //
    // Both percentages always appear and there are no checkboxes: a fee that is
    // not being charged is entered as 0, and a 0 fee produces no invoice line
    // (8.15). The defaults come from Company Setup (4.5.6) and are overridable on
    // purpose - a rate negotiated with the client needs somewhere to live, and a
    // locked field would push the charge outside the system.
    //
    // The abandon button is BACK, never CANCEL. The header button that starts the
    // cancellation is already called CANCEL, and one starting it while the other
    // abandons it is the kind of collision that gets clicked wrong.
    public partial class CancellationChargesModal : Form
    {
        private readonly int _orderId;
        private readonly string _documentNo;
        private bool _loading;

        public double RestockingPercent { get; private set; }
        public double CancellationPercent { get; private set; }

        public CancellationChargesModal(int orderId, string documentNo)
        {
            InitializeComponent();

            _orderId = orderId;
            _documentNo = documentNo;
            Text = "Cancellation Charges - " + documentNo;

            btn_back.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            btn_proceed.Click += async (s, e) => await ProceedAsync();
            txt_restocking.TextChanged += async (s, e) => await PreviewAsync();
            txt_cancellation.TextChanged += async (s, e) => await PreviewAsync();
        }

        protected override async void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            await LoadDefaultsAsync();
        }

        private static double Number(string text)
        {
            double value;
            return double.TryParse((text ?? "").Replace(",", "").Trim(),
                NumberStyles.Any, CultureInfo.InvariantCulture, out value) ? value : 0;
        }

        private async System.Threading.Tasks.Task LoadDefaultsAsync()
        {
            Helpers.Loading.ShowLoading(this);
            try
            {
                _loading = true;
                try
                {
                    var response = await RequestToApi<ApiResponseModel<ChargeDefaults>>
                        .Get("/sales-orders/charges/defaults");

                    var defaults = response?.Data;
                    // A missing or unreachable default is shown as 0 rather than guessed
                    // at. 0 is a real, meaningful value here, and inventing a percentage
                    // is the one thing that must not happen on a form that charges money.
                    txt_restocking.Text = (defaults?.restocking_fee_percent ?? 0).ToString("0.##");
                    txt_cancellation.Text = (defaults?.cancellation_fee_percent ?? 0).ToString("0.##");
                }
                catch (Exception)
                {
                    txt_restocking.Text = "0";
                    txt_cancellation.Text = "0";
                }
                finally
                {
                    _loading = false;
                }

                await PreviewAsync();
            }
            finally
            {
                Helpers.Loading.HideLoading(this);
            }
        }

        // The figures are computed by the API, not here: the fee base depends on
        // which lines are undelivered and on the VAT rate frozen on the SO (12.1),
        // neither of which this form can see.
        private async System.Threading.Tasks.Task PreviewAsync()
        {
            if (_loading) return;

            double restocking = Number(txt_restocking.Text);
            double cancellation = Number(txt_cancellation.Text);

            try
            {
                string url = "/sales-orders/" + _orderId + "/charges/preview"
                    + "?restocking_fee_percent=" + restocking.ToString(CultureInfo.InvariantCulture)
                    + "&cancellation_fee_percent=" + cancellation.ToString(CultureInfo.InvariantCulture);

                var response = await RequestToApi<ApiResponseModel<ChargePreview>>.Get(url, true);
                var preview = response?.Data;
                if (preview == null) return;

                lbl_base.Text = preview.fee_base.ToString("N2");
                lbl_restocking_amount.Text = preview.restocking_fee.ToString("N2");
                lbl_cancellation_amount.Text = preview.cancellation_fee.ToString("N2");
                lbl_total.Text = preview.total_charge.ToString("N2");
            }
            catch (Exception)
            {
                // A failed preview leaves the last figures alone rather than blanking
                // them - the user is mid-typing, and a flicker to 0.00 reads as "no
                // charge" on a form where that is a meaningful answer.
            }
        }

        private async System.Threading.Tasks.Task ProceedAsync()
        {
            double restocking = Number(txt_restocking.Text);
            double cancellation = Number(txt_cancellation.Text);

            if (restocking < 0 || cancellation < 0)
            {
                MessageBox.Show("A fee percentage cannot be negative.", "Cancellation Charges",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (MessageBox.Show(
                    "Submit the cancellation of " + _documentNo + " for approval?",
                    "Cancellation Charges", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
                != DialogResult.Yes)
            {
                return;
            }

            var user = CacheData.CurrentUser;
            var payload = new Dictionary<string, dynamic>
            {
                { "order_id", _orderId },
                { "restocking_fee_percent", restocking },
                { "cancellation_fee_percent", cancellation },
                { "raised_by", user == null ? "" : (user.first_name + " " + user.last_name).Trim() },
                { "raised_by_id", user == null ? 0 : user.id },
                { "raised_date", DateTime.Now.ToString("yyyy-MM-dd") },
            };

            Helpers.Loading.ShowLoading(this);
            try
            {
                try
                {
                    var response = await RequestToApi<ApiResponseModel>.Post("/sales-orders/charges", payload);
                    if (response == null || !response.Success)
                    {
                        MessageBox.Show(response?.message ?? "Could not submit the cancellation.",
                            "Cancellation Charges", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Could not submit the cancellation: " + ex.Message,
                        "Cancellation Charges", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
            finally
            {
                Helpers.Loading.HideLoading(this);
            }

            RestockingPercent = restocking;
            CancellationPercent = cancellation;
            DialogResult = DialogResult.OK;
            Close();
        }

        private class ChargeDefaults
        {
            public double restocking_fee_percent { get; set; }
            public double cancellation_fee_percent { get; set; }
        }

        private class ChargePreview
        {
            public double undelivered_net { get; set; }
            public double vat_rate_percent { get; set; }
            public double fee_base { get; set; }
            public double restocking_fee { get; set; }
            public double cancellation_fee { get; set; }
            public double total_charge { get; set; }
        }
    }
}
