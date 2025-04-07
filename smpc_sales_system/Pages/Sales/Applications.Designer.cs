
namespace smpc_sales_app.Pages.Sales
{
    partial class Applications
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Applications));
            this.pnl_name = new System.Windows.Forms.Panel();
            this.label1 = new System.Windows.Forms.Label();
            this.pnl_input = new System.Windows.Forms.Panel();
            this.lbl_id = new System.Windows.Forms.Label();
            this.lbl_name = new System.Windows.Forms.Label();
            this.lbl_code = new System.Windows.Forms.Label();
            this.txt_id = new System.Windows.Forms.TextBox();
            this.txt_name = new System.Windows.Forms.TextBox();
            this.txt_code = new System.Windows.Forms.TextBox();
            this.pnl_application_dgv = new System.Windows.Forms.Panel();
            this.dgv_application_setup = new System.Windows.Forms.DataGridView();
            this.Column1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column3 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.btn_new = new System.Windows.Forms.ToolStripButton();
            this.btn_delete = new System.Windows.Forms.ToolStripButton();
            this.btn_edit = new System.Windows.Forms.ToolStripButton();
            this.btn_save = new System.Windows.Forms.ToolStripButton();
            this.pnl_name.SuspendLayout();
            this.pnl_input.SuspendLayout();
            this.pnl_application_dgv.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgv_application_setup)).BeginInit();
            this.toolStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // pnl_name
            // 
            this.pnl_name.Controls.Add(this.label1);
            this.pnl_name.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnl_name.Location = new System.Drawing.Point(0, 0);
            this.pnl_name.Name = "pnl_name";
            this.pnl_name.Size = new System.Drawing.Size(633, 67);
            this.pnl_name.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 15.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(4, 21);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(197, 25);
            this.label1.TabIndex = 0;
            this.label1.Text = "Application Setup";
            // 
            // pnl_input
            // 
            this.pnl_input.Controls.Add(this.lbl_id);
            this.pnl_input.Controls.Add(this.lbl_name);
            this.pnl_input.Controls.Add(this.lbl_code);
            this.pnl_input.Controls.Add(this.txt_id);
            this.pnl_input.Controls.Add(this.txt_name);
            this.pnl_input.Controls.Add(this.txt_code);
            this.pnl_input.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnl_input.Location = new System.Drawing.Point(0, 67);
            this.pnl_input.Name = "pnl_input";
            this.pnl_input.Size = new System.Drawing.Size(633, 565);
            this.pnl_input.TabIndex = 1;
            // 
            // lbl_id
            // 
            this.lbl_id.AutoSize = true;
            this.lbl_id.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbl_id.Location = new System.Drawing.Point(342, 54);
            this.lbl_id.Name = "lbl_id";
            this.lbl_id.Size = new System.Drawing.Size(19, 16);
            this.lbl_id.TabIndex = 5;
            this.lbl_id.Text = "Id";
            // 
            // lbl_name
            // 
            this.lbl_name.AutoSize = true;
            this.lbl_name.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbl_name.Location = new System.Drawing.Point(29, 81);
            this.lbl_name.Name = "lbl_name";
            this.lbl_name.Size = new System.Drawing.Size(45, 16);
            this.lbl_name.TabIndex = 4;
            this.lbl_name.Text = "Name";
            // 
            // lbl_code
            // 
            this.lbl_code.AutoSize = true;
            this.lbl_code.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbl_code.Location = new System.Drawing.Point(32, 55);
            this.lbl_code.Name = "lbl_code";
            this.lbl_code.Size = new System.Drawing.Size(41, 16);
            this.lbl_code.TabIndex = 3;
            this.lbl_code.Text = "Code";
            // 
            // txt_id
            // 
            this.txt_id.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txt_id.Location = new System.Drawing.Point(364, 53);
            this.txt_id.Name = "txt_id";
            this.txt_id.Size = new System.Drawing.Size(171, 20);
            this.txt_id.TabIndex = 2;
            // 
            // txt_name
            // 
            this.txt_name.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txt_name.Location = new System.Drawing.Point(76, 79);
            this.txt_name.Name = "txt_name";
            this.txt_name.Size = new System.Drawing.Size(197, 20);
            this.txt_name.TabIndex = 1;
            // 
            // txt_code
            // 
            this.txt_code.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txt_code.Location = new System.Drawing.Point(76, 55);
            this.txt_code.Name = "txt_code";
            this.txt_code.Size = new System.Drawing.Size(197, 20);
            this.txt_code.TabIndex = 0;
            // 
            // pnl_application_dgv
            // 
            this.pnl_application_dgv.Controls.Add(this.dgv_application_setup);
            this.pnl_application_dgv.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnl_application_dgv.Location = new System.Drawing.Point(0, 205);
            this.pnl_application_dgv.Name = "pnl_application_dgv";
            this.pnl_application_dgv.Size = new System.Drawing.Size(633, 427);
            this.pnl_application_dgv.TabIndex = 2;
            // 
            // dgv_application_setup
            // 
            this.dgv_application_setup.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgv_application_setup.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Column1,
            this.Column2,
            this.Column3});
            this.dgv_application_setup.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgv_application_setup.Location = new System.Drawing.Point(0, 0);
            this.dgv_application_setup.Name = "dgv_application_setup";
            this.dgv_application_setup.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgv_application_setup.Size = new System.Drawing.Size(633, 427);
            this.dgv_application_setup.TabIndex = 0;
            this.dgv_application_setup.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgv_application_setup_CellClick);
            // 
            // Column1
            // 
            this.Column1.DataPropertyName = "id";
            this.Column1.HeaderText = "ID";
            this.Column1.Name = "Column1";
            this.Column1.Visible = false;
            // 
            // Column2
            // 
            this.Column2.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.Column2.DataPropertyName = "code";
            this.Column2.HeaderText = "CODE";
            this.Column2.Name = "Column2";
            // 
            // Column3
            // 
            this.Column3.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.Column3.DataPropertyName = "name";
            this.Column3.HeaderText = "NAME";
            this.Column3.Name = "Column3";
            // 
            // toolStrip1
            // 
            this.toolStrip1.BackColor = System.Drawing.SystemColors.ControlLight;
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.btn_new,
            this.btn_delete,
            this.btn_edit,
            this.btn_save});
            this.toolStrip1.Location = new System.Drawing.Point(0, 67);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(633, 25);
            this.toolStrip1.TabIndex = 3;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // btn_new
            // 
            this.btn_new.Image = ((System.Drawing.Image)(resources.GetObject("btn_new.Image")));
            this.btn_new.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btn_new.Name = "btn_new";
            this.btn_new.Size = new System.Drawing.Size(51, 22);
            this.btn_new.Text = "New";
            this.btn_new.Click += new System.EventHandler(this.btn_new_Click);
            // 
            // btn_delete
            // 
            this.btn_delete.Image = ((System.Drawing.Image)(resources.GetObject("btn_delete.Image")));
            this.btn_delete.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btn_delete.Name = "btn_delete";
            this.btn_delete.Size = new System.Drawing.Size(60, 22);
            this.btn_delete.Text = "Delete";
            this.btn_delete.Click += new System.EventHandler(this.btn_delete_Click);
            // 
            // btn_edit
            // 
            this.btn_edit.Image = ((System.Drawing.Image)(resources.GetObject("btn_edit.Image")));
            this.btn_edit.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btn_edit.Name = "btn_edit";
            this.btn_edit.Size = new System.Drawing.Size(47, 22);
            this.btn_edit.Text = "Edit";
            this.btn_edit.Click += new System.EventHandler(this.btn_edit_Click);
            // 
            // btn_save
            // 
            this.btn_save.Image = ((System.Drawing.Image)(resources.GetObject("btn_save.Image")));
            this.btn_save.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btn_save.Name = "btn_save";
            this.btn_save.Size = new System.Drawing.Size(51, 22);
            this.btn_save.Text = "Save";
            this.btn_save.Click += new System.EventHandler(this.btn_save_Click);
            // 
            // Applications
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.toolStrip1);
            this.Controls.Add(this.pnl_application_dgv);
            this.Controls.Add(this.pnl_input);
            this.Controls.Add(this.pnl_name);
            this.Name = "Applications";
            this.Size = new System.Drawing.Size(633, 632);
            this.Load += new System.EventHandler(this.Applications_Load);
            this.pnl_name.ResumeLayout(false);
            this.pnl_name.PerformLayout();
            this.pnl_input.ResumeLayout(false);
            this.pnl_input.PerformLayout();
            this.pnl_application_dgv.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgv_application_setup)).EndInit();
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Panel pnl_name;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Panel pnl_input;
        private System.Windows.Forms.Label lbl_id;
        private System.Windows.Forms.Label lbl_name;
        private System.Windows.Forms.Label lbl_code;
        private System.Windows.Forms.TextBox txt_id;
        private System.Windows.Forms.TextBox txt_name;
        private System.Windows.Forms.TextBox txt_code;
        private System.Windows.Forms.Panel pnl_application_dgv;
        private System.Windows.Forms.DataGridView dgv_application_setup;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton btn_new;
        private System.Windows.Forms.ToolStripButton btn_delete;
        private System.Windows.Forms.ToolStripButton btn_edit;
        private System.Windows.Forms.ToolStripButton btn_save;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column1;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column2;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column3;
    }
}
