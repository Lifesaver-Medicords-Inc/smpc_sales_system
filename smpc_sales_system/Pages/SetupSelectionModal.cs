using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace smpc_inventory_app.Pages
{
    public partial class SetupSelectionModal : Form
    {
        private string Title { get; }
        private string EndPoint { get;}
        private string Data { get; }

        private DataView result { get; set; }
        private DataTable Dt;
        public SetupSelectionModal(string title, string api, DataTable dt, string currentValue)
        {
            InitializeComponent();
       
            lbl_title.Text = title;
           
            this.EndPoint = api;
            this.Data = currentValue;
            this.Dt = dt;
            if (dt != null )
            {
                if (dt.Columns["select"] != null)    // Check if select column already exist
                    return;
                dt.Columns.Add("select");            // Add select column if not 
            }
        





        }

        private void SelectionModal_Load(object sender, EventArgs e)
        {
            dg_general.DataSource = this.Dt;
           
        }

       private DataView GetEntityData()
        {

            DataView dataView = new DataView(dg_general.DataSource as DataTable);
            dataView.RowFilter = $"select = true";

            return dataView;
         
        }
        public DataView GetResult()
        {
           return this.result ;
        }
        
   

        private void btn_ok_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.result = GetEntityData();
            this.Close();
          
        }

        private void btn_cancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
