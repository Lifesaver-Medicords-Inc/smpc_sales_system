using System.Collections.Generic;
using System.Windows.Forms;

namespace smpc_sales_system.Pages.Sales
{
    // One supplier frm_canvas_modal already knows can supply the item being canvassed
    // (from ProjectService.GetSuppliers(), filtered to this item_id - see
    // frm_canvas_modal.fetchBpiSuppliers) and isn't already a row on the sheet. Carries
    // both the id GetDGVData() needs to save and the resolved display name.
    public class EligibleSupplier
    {
        public int SupplierId { get; set; }
        public string SupplierName { get; set; }

        public override string ToString() => SupplierName;
    }

    // "ADD SUPPLIER" on the Canvas Sheet opens this - a plain pick-one-from-a-list dialog
    // over whichever eligible suppliers for this item aren't on the sheet yet (see
    // frm_canvas_modal.btn_add_bpi_Click). Not a general business-partner browser -
    // there's no filtering/search here because the candidate list is already scoped to
    // "suppliers set up for this exact item" by the caller.
    public partial class SupplierPickerModal : Form
    {
        public EligibleSupplier Selected { get; private set; }

        public SupplierPickerModal(List<EligibleSupplier> candidates)
        {
            InitializeComponent();

            lst_suppliers.DisplayMember = "SupplierName";
            lst_suppliers.DataSource = candidates;
        }

        private void btn_ok_Click(object sender, System.EventArgs e)
        {
            if (lst_suppliers.SelectedItem == null)
            {
                MessageBox.Show("Select a supplier first.", "No Supplier Selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            Selected = lst_suppliers.SelectedItem as EligibleSupplier;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void btn_cancel_Click(object sender, System.EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
