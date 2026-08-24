
namespace smpc_sales_system.Pages.Sales
{
    partial class SizeUpPickerModal
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.panel1 = new System.Windows.Forms.Panel();
            this.label1 = new System.Windows.Forms.Label();
            this.panel3 = new System.Windows.Forms.Panel();
            this.txt_search = new System.Windows.Forms.TextBox();
            this.lbl_search = new System.Windows.Forms.Label();
            this.panel2 = new System.Windows.Forms.Panel();
            this.dgv_pumps = new System.Windows.Forms.DataGridView();
            this.col_select = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.col_item_id = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.col_brand = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.col_model = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.col_list_price = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.panel_buttons = new System.Windows.Forms.Panel();
            this.btn_cancel = new System.Windows.Forms.Button();
            this.btn_save = new System.Windows.Forms.Button();
            this.panel1.SuspendLayout();
            this.panel3.SuspendLayout();
            this.panel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgv_pumps)).BeginInit();
            this.panel_buttons.SuspendLayout();
            this.SuspendLayout();
            //
            // panel1
            //
            this.panel1.Controls.Add(this.label1);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(560, 57);
            this.panel1.TabIndex = 0;
            //
            // label1
            //
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(23, 21);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(203, 20);
            this.label1.TabIndex = 0;
            this.label1.Text = "Select Pumps for Size Up";
            //
            // panel3
            //
            this.panel3.Controls.Add(this.txt_search);
            this.panel3.Controls.Add(this.lbl_search);
            this.panel3.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel3.Location = new System.Drawing.Point(0, 57);
            this.panel3.Name = "panel3";
            this.panel3.Padding = new System.Windows.Forms.Padding(20, 4, 20, 4);
            this.panel3.Size = new System.Drawing.Size(560, 34);
            this.panel3.TabIndex = 1;
            //
            // txt_search
            //
            this.txt_search.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txt_search.Location = new System.Drawing.Point(78, 4);
            this.txt_search.Name = "txt_search";
            this.txt_search.Size = new System.Drawing.Size(462, 20);
            this.txt_search.TabIndex = 1;
            this.txt_search.TextChanged += new System.EventHandler(this.txt_search_TextChanged);
            //
            // lbl_search
            //
            this.lbl_search.AutoSize = true;
            this.lbl_search.Dock = System.Windows.Forms.DockStyle.Left;
            this.lbl_search.Location = new System.Drawing.Point(20, 4);
            this.lbl_search.Name = "lbl_search";
            this.lbl_search.Padding = new System.Windows.Forms.Padding(0, 6, 6, 0);
            this.lbl_search.Size = new System.Drawing.Size(58, 19);
            this.lbl_search.TabIndex = 0;
            this.lbl_search.Text = "Search:";
            //
            // panel2
            //
            this.panel2.Controls.Add(this.dgv_pumps);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel2.Location = new System.Drawing.Point(0, 91);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(560, 309);
            this.panel2.TabIndex = 2;
            //
            // dgv_pumps
            //
            this.dgv_pumps.AllowUserToAddRows = false;
            this.dgv_pumps.AllowUserToDeleteRows = false;
            this.dgv_pumps.AutoGenerateColumns = false;
            this.dgv_pumps.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.None;
            this.dgv_pumps.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgv_pumps.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.col_select,
            this.col_item_id,
            this.col_brand,
            this.col_model,
            this.col_list_price});
            this.dgv_pumps.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgv_pumps.Location = new System.Drawing.Point(0, 0);
            this.dgv_pumps.MultiSelect = false;
            this.dgv_pumps.Name = "dgv_pumps";
            this.dgv_pumps.RowHeadersVisible = false;
            this.dgv_pumps.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.CellSelect;
            this.dgv_pumps.Size = new System.Drawing.Size(560, 309);
            this.dgv_pumps.TabIndex = 0;
            this.dgv_pumps.CurrentCellDirtyStateChanged += new System.EventHandler(this.dgv_pumps_CurrentCellDirtyStateChanged);
            //
            // col_select
            //
            this.col_select.HeaderText = "";
            this.col_select.Name = "col_select";
            this.col_select.Width = 40;
            //
            // col_item_id
            //
            this.col_item_id.HeaderText = "id";
            this.col_item_id.Name = "col_item_id";
            this.col_item_id.ReadOnly = true;
            this.col_item_id.Visible = false;
            //
            // col_brand
            //
            this.col_brand.HeaderText = "BRAND";
            this.col_brand.Name = "col_brand";
            this.col_brand.ReadOnly = true;
            this.col_brand.Width = 130;
            //
            // col_model
            //
            this.col_model.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.col_model.HeaderText = "MODEL NAME";
            this.col_model.Name = "col_model";
            this.col_model.ReadOnly = true;
            //
            // col_list_price
            //
            // Always blank - no master-data source exists for list price yet (confirmed:
            // neither the item catalog, ItemPumpsView, nor vw_PumpSpecifications carry a
            // price field). Column exists so one can be wired in later without another UI
            // change.
            this.col_list_price.HeaderText = "LIST PRICE";
            this.col_list_price.Name = "col_list_price";
            this.col_list_price.ReadOnly = true;
            this.col_list_price.Width = 100;
            //
            // panel_buttons
            //
            this.panel_buttons.Controls.Add(this.btn_save);
            this.panel_buttons.Controls.Add(this.btn_cancel);
            this.panel_buttons.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel_buttons.Location = new System.Drawing.Point(0, 400);
            this.panel_buttons.Name = "panel_buttons";
            this.panel_buttons.Padding = new System.Windows.Forms.Padding(20, 10, 20, 10);
            this.panel_buttons.Size = new System.Drawing.Size(560, 50);
            this.panel_buttons.TabIndex = 3;
            //
            // btn_cancel
            //
            this.btn_cancel.Dock = System.Windows.Forms.DockStyle.Right;
            this.btn_cancel.Location = new System.Drawing.Point(440, 10);
            this.btn_cancel.Name = "btn_cancel";
            this.btn_cancel.Size = new System.Drawing.Size(100, 30);
            this.btn_cancel.TabIndex = 1;
            this.btn_cancel.Text = "Cancel";
            this.btn_cancel.UseVisualStyleBackColor = true;
            this.btn_cancel.Click += new System.EventHandler(this.btn_cancel_Click);
            //
            // btn_save
            //
            this.btn_save.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btn_save.Location = new System.Drawing.Point(330, 10);
            this.btn_save.Name = "btn_save";
            this.btn_save.Size = new System.Drawing.Size(100, 30);
            this.btn_save.TabIndex = 0;
            this.btn_save.Text = "Save";
            this.btn_save.UseVisualStyleBackColor = true;
            this.btn_save.Click += new System.EventHandler(this.btn_save_Click);
            //
            // SizeUpPickerModal
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(560, 450);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panel_buttons);
            this.Controls.Add(this.panel3);
            this.Controls.Add(this.panel1);
            this.MinimizeBox = false;
            this.MaximizeBox = false;
            this.Name = "SizeUpPickerModal";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Select Pumps for Size Up";
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.panel3.ResumeLayout(false);
            this.panel3.PerformLayout();
            this.panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgv_pumps)).EndInit();
            this.panel_buttons.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.TextBox txt_search;
        private System.Windows.Forms.Label lbl_search;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.DataGridView dgv_pumps;
        private System.Windows.Forms.DataGridViewCheckBoxColumn col_select;
        private System.Windows.Forms.DataGridViewTextBoxColumn col_item_id;
        private System.Windows.Forms.DataGridViewTextBoxColumn col_brand;
        private System.Windows.Forms.DataGridViewTextBoxColumn col_model;
        private System.Windows.Forms.DataGridViewTextBoxColumn col_list_price;
        private System.Windows.Forms.Panel panel_buttons;
        private System.Windows.Forms.Button btn_cancel;
        private System.Windows.Forms.Button btn_save;
    }
}
