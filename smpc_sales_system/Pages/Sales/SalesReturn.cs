using System.Windows.Forms;

namespace smpc_sales_app.Pages.Sales
{
    // Sales Return (SRT#), spec Sec5.13. Design pass only - layout in
    // SalesReturn.Designer.cs matches the fields/read-only rules the spec
    // requires (REF. DOC. TYPE chosen before item selection, salesperson/
    // currency/sales period/unit price all sourced from the reference
    // document, approval display, GENERATE CREDIT MEMO gated on approval).
    // No logic wired yet: no data binding, no save/approve/search handlers,
    // no dgv_sales_return_details row-sum validation. That's the next pass.
    public partial class SalesReturn : UserControl
    {
        public SalesReturn()
        {
            InitializeComponent();
        }
    }
}
