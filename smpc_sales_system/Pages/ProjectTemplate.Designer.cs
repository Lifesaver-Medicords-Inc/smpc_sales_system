namespace smpc_sales_system.Pages
{
    partial class ProjectTemplate
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ProjectTemplate));
            this.pnl_name = new System.Windows.Forms.Panel();
            this.label1 = new System.Windows.Forms.Label();
            this.pnl_input = new System.Windows.Forms.Panel();
            this.toolstrip_quotation = new System.Windows.Forms.ToolStrip();
            this.back = new System.Windows.Forms.ToolStripLabel();
            this.btn_search = new System.Windows.Forms.ToolStripButton();
            this.btn_prev = new System.Windows.Forms.ToolStripButton();
            this.btn_next = new System.Windows.Forms.ToolStripButton();
            this.btn_new = new System.Windows.Forms.ToolStripButton();
            this.btn_edit = new System.Windows.Forms.ToolStripButton();
            this.btn_save = new System.Windows.Forms.ToolStripButton();
            this.btn_close = new System.Windows.Forms.ToolStripButton();
            this.btn_duplicate = new System.Windows.Forms.ToolStripButton();
            this.dgv_template = new System.Windows.Forms.DataGridView();
            this.ID = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ItemId = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.component = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.addChildToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.Level = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ParentId = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.lbl_code = new System.Windows.Forms.Label();
            this.txt_template_name = new System.Windows.Forms.TextBox();
            this.pnl_name.SuspendLayout();
            this.pnl_input.SuspendLayout();
            this.toolstrip_quotation.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgv_template)).BeginInit();
            this.contextMenuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // pnl_name
            // 
            this.pnl_name.Controls.Add(this.label1);
            this.pnl_name.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnl_name.Location = new System.Drawing.Point(0, 0);
            this.pnl_name.Name = "pnl_name";
            this.pnl_name.Size = new System.Drawing.Size(877, 67);
            this.pnl_name.TabIndex = 1;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 15.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(19, 20);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(190, 25);
            this.label1.TabIndex = 0;
            this.label1.Text = "Project Template";
            // 
            // pnl_input
            // 
            this.pnl_input.Controls.Add(this.toolstrip_quotation);
            this.pnl_input.Controls.Add(this.dgv_template);
            this.pnl_input.Controls.Add(this.lbl_code);
            this.pnl_input.Controls.Add(this.txt_template_name);
            this.pnl_input.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnl_input.Location = new System.Drawing.Point(0, 67);
            this.pnl_input.Name = "pnl_input";
            this.pnl_input.Size = new System.Drawing.Size(877, 575);
            this.pnl_input.TabIndex = 2;
            // 
            // toolstrip_quotation
            // 
            this.toolstrip_quotation.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.toolstrip_quotation.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.back,
            this.btn_search,
            this.btn_prev,
            this.btn_next,
            this.btn_new,
            this.btn_edit,
            this.btn_save,
            this.btn_close,
            this.btn_duplicate});
            this.toolstrip_quotation.Location = new System.Drawing.Point(0, 0);
            this.toolstrip_quotation.Name = "toolstrip_quotation";
            this.toolstrip_quotation.RenderMode = System.Windows.Forms.ToolStripRenderMode.Professional;
            this.toolstrip_quotation.Size = new System.Drawing.Size(877, 25);
            this.toolstrip_quotation.TabIndex = 5;
            this.toolstrip_quotation.Text = "toolStrip1";
            // 
            // back
            // 
            this.back.Name = "back";
            this.back.Size = new System.Drawing.Size(32, 22);
            this.back.Text = "Back";
            this.back.Visible = false;
            // 
            // btn_search
            // 
            this.btn_search.Image = ((System.Drawing.Image)(resources.GetObject("btn_search.Image")));
            this.btn_search.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btn_search.Name = "btn_search";
            this.btn_search.Size = new System.Drawing.Size(62, 22);
            this.btn_search.Text = "Search";
            this.btn_search.Click += new System.EventHandler(this.btn_search_Click);
            // 
            // btn_prev
            // 
            this.btn_prev.Image = ((System.Drawing.Image)(resources.GetObject("btn_prev.Image")));
            this.btn_prev.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btn_prev.Name = "btn_prev";
            this.btn_prev.Size = new System.Drawing.Size(72, 22);
            this.btn_prev.Text = "Previous";
            this.btn_prev.Click += new System.EventHandler(this.btn_prev_Click);
            // 
            // btn_next
            // 
            this.btn_next.Image = ((System.Drawing.Image)(resources.GetObject("btn_next.Image")));
            this.btn_next.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btn_next.Name = "btn_next";
            this.btn_next.Size = new System.Drawing.Size(51, 22);
            this.btn_next.Text = "Next";
            this.btn_next.Click += new System.EventHandler(this.btn_next_Click);
            // 
            // btn_new
            // 
            this.btn_new.Image = ((System.Drawing.Image)(resources.GetObject("btn_new.Image")));
            this.btn_new.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btn_new.Name = "btn_new";
            this.btn_new.Size = new System.Drawing.Size(143, 22);
            this.btn_new.Text = "New Project Template";
            this.btn_new.Click += new System.EventHandler(this.btn_new_Click);
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
            this.btn_save.Visible = false;
            this.btn_save.Click += new System.EventHandler(this.btn_save_Click);
            // 
            // btn_close
            // 
            this.btn_close.Image = ((System.Drawing.Image)(resources.GetObject("btn_close.Image")));
            this.btn_close.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btn_close.Name = "btn_close";
            this.btn_close.Size = new System.Drawing.Size(56, 22);
            this.btn_close.Text = "Close";
            this.btn_close.Visible = false;
            this.btn_close.Click += new System.EventHandler(this.btn_close_Click);
            // 
            // btn_duplicate
            // 
            this.btn_duplicate.Image = ((System.Drawing.Image)(resources.GetObject("btn_duplicate.Image")));
            this.btn_duplicate.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btn_duplicate.Name = "btn_duplicate";
            this.btn_duplicate.Size = new System.Drawing.Size(77, 22);
            this.btn_duplicate.Text = "Duplicate";
            // 
            // dgv_template
            // 
            this.dgv_template.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgv_template.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgv_template.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.ID,
            this.ItemId,
            this.component,
            this.Level,
            this.ParentId});
            this.dgv_template.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.dgv_template.Location = new System.Drawing.Point(0, 113);
            this.dgv_template.Name = "dgv_template";
            this.dgv_template.Size = new System.Drawing.Size(877, 462);
            this.dgv_template.TabIndex = 1;
            this.dgv_template.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgv_template_CellClick);
            this.dgv_template.CellMouseDown += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.dgv_template_CellMouseDown);
            // 
            // ID
            // 
            this.ID.DataPropertyName = "Id";
            this.ID.HeaderText = "ID";
            this.ID.Name = "ID";
            this.ID.Visible = false;
            // 
            // ItemId
            // 
            this.ItemId.DataPropertyName = "ItemId";
            this.ItemId.HeaderText = "ITEMID";
            this.ItemId.Name = "ItemId";
            this.ItemId.Visible = false;
            // 
            // component
            // 
            this.component.ContextMenuStrip = this.contextMenuStrip1;
            this.component.DataPropertyName = "Components";
            this.component.HeaderText = "COMPONENTS";
            this.component.Name = "component";
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.addChildToolStripMenuItem});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(128, 26);
            // 
            // addChildToolStripMenuItem
            // 
            this.addChildToolStripMenuItem.Name = "addChildToolStripMenuItem";
            this.addChildToolStripMenuItem.Size = new System.Drawing.Size(127, 22);
            this.addChildToolStripMenuItem.Text = "Add Child";
            this.addChildToolStripMenuItem.Click += new System.EventHandler(this.addChildToolStripMenuItem_Click);
            // 
            // Level
            // 
            this.Level.DataPropertyName = "Level";
            this.Level.HeaderText = "LEVEL";
            this.Level.Name = "Level";
            this.Level.Visible = false;
            // 
            // ParentId
            // 
            this.ParentId.DataPropertyName = "ParentId";
            this.ParentId.HeaderText = "PARENTID";
            this.ParentId.Name = "ParentId";
            this.ParentId.Visible = false;
            // 
            // lbl_code
            // 
            this.lbl_code.AutoSize = true;
            this.lbl_code.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbl_code.Location = new System.Drawing.Point(21, 58);
            this.lbl_code.Name = "lbl_code";
            this.lbl_code.Size = new System.Drawing.Size(108, 16);
            this.lbl_code.TabIndex = 3;
            this.lbl_code.Text = "Template Name:";
            // 
            // txt_template_name
            // 
            this.txt_template_name.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txt_template_name.Location = new System.Drawing.Point(135, 58);
            this.txt_template_name.Name = "txt_template_name";
            this.txt_template_name.Size = new System.Drawing.Size(197, 20);
            this.txt_template_name.TabIndex = 0;
            // 
            // ProjectTemplate
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.pnl_input);
            this.Controls.Add(this.pnl_name);
            this.Name = "ProjectTemplate";
            this.Size = new System.Drawing.Size(877, 642);
            this.Load += new System.EventHandler(this.ProjectTemplate_Load);
            this.pnl_name.ResumeLayout(false);
            this.pnl_name.PerformLayout();
            this.pnl_input.ResumeLayout(false);
            this.pnl_input.PerformLayout();
            this.toolstrip_quotation.ResumeLayout(false);
            this.toolstrip_quotation.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgv_template)).EndInit();
            this.contextMenuStrip1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel pnl_name;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Panel pnl_input;
        private System.Windows.Forms.Label lbl_code;
        private System.Windows.Forms.TextBox txt_template_name;
        private System.Windows.Forms.DataGridView dgv_template;
        private System.Windows.Forms.ToolStrip toolstrip_quotation;
        private System.Windows.Forms.ToolStripLabel back;
        private System.Windows.Forms.ToolStripButton btn_search;
        private System.Windows.Forms.ToolStripButton btn_prev;
        private System.Windows.Forms.ToolStripButton btn_next;
        private System.Windows.Forms.ToolStripButton btn_new;
        private System.Windows.Forms.ToolStripButton btn_edit;
        private System.Windows.Forms.ToolStripButton btn_save;
        private System.Windows.Forms.ToolStripButton btn_close;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem addChildToolStripMenuItem;
        private System.Windows.Forms.DataGridViewTextBoxColumn ID;
        private System.Windows.Forms.DataGridViewTextBoxColumn ItemId;
        private System.Windows.Forms.DataGridViewTextBoxColumn component;
        private System.Windows.Forms.DataGridViewTextBoxColumn Level;
        private System.Windows.Forms.DataGridViewTextBoxColumn ParentId;
        private System.Windows.Forms.ToolStripButton btn_duplicate;
    }
}
