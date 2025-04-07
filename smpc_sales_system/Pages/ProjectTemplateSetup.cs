using Newtonsoft.Json;
using smpc_sales_app.Services.Helpers;
using smpc_sales_system.Models;
using smpc_sales_system.Services.Sales;
using smpc_sales_system.Services.Setup;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace smpc_sales_system.Pages
{
    public partial class ProjectTemplateSetup : Form
    {
        DataTable itemlist = new DataTable();
        public ProjectTemplateSetup(DataTable dt)
        {
            InitializeComponent();
            treeView1.DrawMode = TreeViewDrawMode.OwnerDrawText;
            treeView1.DrawNode += treeView1_DrawNode;
            this.itemlist = dt;

        }
        TreeNode lastAddedNode = null; 

        private void button1_Click(object sender, EventArgs e)
        {
            string nodeTxt = txtNode.Text.Trim();

            if (string.IsNullOrEmpty(nodeTxt))
            {
                MessageBox.Show("Please enter a node name.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            TreeNode newNode = new TreeNode(nodeTxt);

            if (treeView1.SelectedNode == null)
            {
              
                if (lastAddedNode == null)
                {
                    treeView1.Nodes.Insert(0, newNode);
                }
                else
                {
                   
                    int index = treeView1.Nodes.IndexOf(lastAddedNode);
                    treeView1.Nodes.Insert(index + 1, newNode);
                }
            }
            else
            {
                
                if (lastAddedNode == null || lastAddedNode.Parent != treeView1.SelectedNode)
                {
                    treeView1.SelectedNode.Nodes.Insert(0, newNode);
                }
                else
                {
                    int index = treeView1.SelectedNode.Nodes.IndexOf(lastAddedNode);
                    treeView1.SelectedNode.Nodes.Insert(index + 1, newNode);
                }

                treeView1.SelectedNode.Expand(); 
            }

            lastAddedNode = newNode;
            txtNode.Clear();
        }

        private void treeView1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                TreeNode clickedNode = treeView1.GetNodeAt(e.X, e.Y);

                if (clickedNode == null) 
                {
                    treeView1.SelectedNode = null;
                }
            }
        }

        //private void ConvertToData()
        //{
        //    dataGridView1.Rows.Clear(); // Clear existing rows
        //    dataGridView1.Columns.Clear(); // Clear existing columns

        //    // Add columns
        //    dataGridView1.Columns.Add("NodeName", "Node Name");
            
        //    // Define font styles
        //    Font boldFont = new Font(dataGridView1.DefaultCellStyle.Font, FontStyle.Bold);
        //    Font normalFont = new Font(dataGridView1.DefaultCellStyle.Font, FontStyle.Regular);

        //    // Loop through parent nodes
        //    foreach (TreeNode parent in treeView1.Nodes)
        //    {
        //        // Add Parent Node (Bold + Light Coral)
        //        int parentRowIndex = dataGridView1.Rows.Add("▶ " + parent.Text, "ROOT");
        //        dataGridView1.Rows[parentRowIndex].DefaultCellStyle.BackColor = Color.LightCoral;
        //        dataGridView1.Rows[parentRowIndex].DefaultCellStyle.Font = boldFont;

        //        // Recursively process child nodes
        //        foreach (TreeNode child in parent.Nodes)
        //        {
        //            AddNodeToDataGrid(child, parent.Text, 1);
        //        }
        //    }
        //}

        //private void AddNodeToDataGrid(TreeNode node, string parentName, int level)
        //{
            
        //    string indent = new string(' ', level * 4) + "└▶ ";

           
        //    int rowIndex = dataGridView1.Rows.Add(indent + node.Text, parentName);

            
        //    if (node.Nodes.Count > 0)
        //    {
               
        //        dataGridView1.Rows[rowIndex].DefaultCellStyle.BackColor = Color.LightGreen;
        //        dataGridView1.Rows[rowIndex].DefaultCellStyle.Font = new Font(dataGridView1.DefaultCellStyle.Font, FontStyle.Bold);
        //    }
        //    else
        //    {
        //        // Leaf nodes (final children) → Light Yellow
        //        dataGridView1.Rows[rowIndex].DefaultCellStyle.BackColor = Color.LightYellow;
        //        dataGridView1.Rows[rowIndex].DefaultCellStyle.Font = new Font(dataGridView1.DefaultCellStyle.Font, FontStyle.Regular);
        //    }

        //    // Traverse and add child nodes
        //    foreach (TreeNode child in node.Nodes)
        //    {
        //        AddNodeToDataGrid(child, node.Text, level + 1);
        //        dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

        //    }
        //}

        private Dictionary<string, dynamic> ConvertToApiModel(string templateName)
        {
            Dictionary<string, dynamic> result = new Dictionary<string, dynamic>();

           
            result["SalesProjectTemplate"] = new List<ProjectTemplateModel>();
            result["sales_project_template_child"] = new List<ProjectTemplateChildModel>();

           
            ProjectTemplateModel template = new ProjectTemplateModel
            {
                template_name = templateName
            };
            ((List<ProjectTemplateModel>)result["SalesProjectTemplate"]).Add(template);

           
            int nodeIdCounter = 1;
            var childList = (List<ProjectTemplateChildModel>)result["sales_project_template_child"];

          
            foreach (TreeNode parent in treeView1.Nodes)
            {
                ProjectTemplateChildModel parentModel = new ProjectTemplateChildModel
                {
                    node_id = nodeIdCounter++,  
                    parent_node_id = 0,        
                    node_name = parent.Text,
                    node_level = 0,
                    node_order = childList.Count + 1,
                    item_id = 0,
                    node_type = parent.Nodes.Count > 0 ? "Parent" : "Leaf"
                };

                childList.Add(parentModel);

              
                foreach (TreeNode child in parent.Nodes)
                {
                    AddNodeToApiModel(child, parentModel.node_id, 1, childList, ref nodeIdCounter);
                }
            }
            return result;
        }


        private void AddNodeToApiModel(TreeNode node, int parentId, int level, List<ProjectTemplateChildModel> childNodes, ref int nodeIdCounter)
        {
            ProjectTemplateChildModel childModel = new ProjectTemplateChildModel
            {
                node_id = nodeIdCounter++,  
                parent_node_id = parentId, 
                node_name = node.Text,
                node_level = level,
                node_order = childNodes.Count + 1,
                item_id = 0,
                node_type = node.Nodes.Count > 0 ? "Parent" : "Leaf"
            };

            childNodes.Add(childModel);

            // Process all child nodes
            foreach (TreeNode child in node.Nodes)
            {
                AddNodeToApiModel(child, childModel.node_id, level + 1, childNodes, ref nodeIdCounter);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            
        }

        private void button4_Click(object sender, EventArgs e)
        {
           
            TreeNode parentNode = new TreeNode("COMMON PACKAGE");

           
            TreeNode dischargeNode = new TreeNode("DISCHARGE COMMON HEADER");
            TreeNode suctionNode = new TreeNode("SUCTION COMMON HEADER");

           
            parentNode.Nodes.Add(dischargeNode);
            parentNode.Nodes.Add(suctionNode);

            if (lastAddedNode == null)
            {
          
                treeView1.Nodes.Insert(0, parentNode);
            }
            else
            {
              
                int index = treeView1.Nodes.IndexOf(lastAddedNode);
                treeView1.Nodes.Insert(index, parentNode);
            }

         
            parentNode.Expand();

            lastAddedNode = parentNode;
        }


        private void treeView1_DrawNode(object sender, DrawTreeNodeEventArgs e)
        {
            if (e.Node == treeView1.SelectedNode)
            {
               
                e.Graphics.FillRectangle(Brushes.LightBlue, e.Bounds);
            }
            else
            {
                e.Graphics.FillRectangle(SystemBrushes.Window, e.Bounds);
            }

           
            Font nodeFont = e.Node.Parent == null ? new Font(treeView1.Font, FontStyle.Bold) : treeView1.Font;
            TextRenderer.DrawText(e.Graphics, e.Node.Text, nodeFont, e.Bounds, Color.Black, TextFormatFlags.GlyphOverhangPadding);

            e.DrawDefault = false; 
        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node != null)
            {
                e.Node.BeginEdit();
            }
        }

        private async void ProjectTemplateSetup_Load(object sender, EventArgs e)
        {
            var data = await ProjectService.GetBom();
            DataTable parent = JsonHelper.ToDataTable(data.BomParent);
            DataTable child = JsonHelper.ToDataTable(data.setup_bom_details);

            DataTable parentCopy = parent.Clone();
            DataTable childCopy = child.Clone();

            parentCopy.Columns.Add("item_name", typeof(string));
            childCopy.Columns.Add("item_name", typeof(string));

            
            foreach (DataRow parentRow in parent.Rows)
            {
                DataRow newRow = parentCopy.NewRow();
                foreach (DataColumn col in parent.Columns)
                {
                    newRow[col.ColumnName] = parentRow[col.ColumnName];
                }

                string itemId = parentRow["item_id"].ToString();
                DataRow[] itemRows = itemlist.Select($"id = '{itemId}'");

                newRow["item_name"] = itemRows.Length > 0 ? itemRows[0]["item_name"].ToString() : "Unknown Item";

                parentCopy.Rows.Add(newRow);
            }

            // Populate childCopy
            foreach (DataRow childRow in child.Rows)
            {
                DataRow newRow = childCopy.NewRow();
                foreach (DataColumn col in child.Columns)
                {
                    newRow[col.ColumnName] = childRow[col.ColumnName];
                }

                string itemId = childRow["item_id"].ToString();
                DataRow[] itemRows = itemlist.Select($"id = '{itemId}'");

                newRow["item_name"] = itemRows.Length > 0 ? itemRows[0]["item_name"].ToString() : "Unknown Item";

                childCopy.Rows.Add(newRow);
            }


            //dgv_bom.DataSource = parentCopy;


            //foreach (DataGridViewColumn column in dgv_bom.Columns)
            //{
            //    if (column.Name != "item_name" && column.Name != "id")
            //    {
            //        column.Visible = false;
            //    }
            //}


            this.childData = childCopy;
        }


        private DataTable childData;

       

        private async void button5_Click(object sender, EventArgs e)
        {
            Dictionary<string, dynamic> data = new Dictionary<string, dynamic>();


            var dataa = ConvertToApiModel("TEST");

            var send = await ProjectTemplatesService.Insert(dataa);
            if (send.Success)
            {
                MessageBox.Show("POST");
            }
            else
            {
                MessageBox.Show("FAIL");
            }
        }

        //private async void button6_Click(object sender, EventArgs e)
        //{
        //    var data = await ProjectTemplatesService.GetProjectTemplates();

        //    var dt1 = data.SalesProjectTemplate; 
        //    var dt2 = data.sales_project_template_child; 

        //    if (dt1 == null || dt2 == null || dt1.Count == 0)
        //    {
        //        MessageBox.Show("No project templates found.");
        //        return;
        //    }

        //    dataGridView1.Rows.Clear();
        //    dataGridView1.Columns.Clear();
        //    dataGridView1.Columns.Add("NodeName", "Node Name");
        //    dataGridView1.Columns.Add("ParentName", "Parent"); 

        //    Font boldFont = new Font(dataGridView1.DefaultCellStyle.Font, FontStyle.Bold);
        //    Font normalFont = new Font(dataGridView1.DefaultCellStyle.Font, FontStyle.Regular);

        //    int selectedTemplateId = 3;

           
        //    var filteredNodes = dt2.Where(n => n.based_id == selectedTemplateId).ToList();

            
        //    Dictionary<int, ProjectTemplateChildModel> nodeLookup = filteredNodes.ToDictionary(n => n.node_id);

           
        //    var rootNodes = filteredNodes.Where(n => n.parent_node_id == 0)
        //                                 .OrderBy(n => n.node_order)
        //                                 .ToList();

         
        //    foreach (var rootNode in rootNodes)
        //    {
               
        //        int parentRowIndex = dataGridView1.Rows.Add("▶ " + rootNode.node_name, "ROOT");
        //        dataGridView1.Rows[parentRowIndex].DefaultCellStyle.BackColor = Color.LightCoral;
        //        dataGridView1.Rows[parentRowIndex].DefaultCellStyle.Font = boldFont;

        //        AddChildNodesFromDb(rootNode.node_id, filteredNodes, nodeLookup, 1);
        //    }

        //    dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        //}

      
        //private void AddChildNodesFromDb(int parentId, List<ProjectTemplateChildModel> allNodes,
        //                                Dictionary<int, ProjectTemplateChildModel> nodeLookup, int level)
        //{
          
        //    var childNodes = allNodes.Where(n => n.parent_node_id == parentId)
        //                             .OrderBy(n => n.node_order)
        //                             .ToList();

          
        //    Font boldFont = new Font(dataGridView1.DefaultCellStyle.Font, FontStyle.Bold);
        //    Font normalFont = new Font(dataGridView1.DefaultCellStyle.Font, FontStyle.Regular);

          
        //    foreach (var childNode in childNodes)
        //    {
              
        //        string indent = new string(' ', level * 4) + "└▶ ";

               
        //        string parentName = nodeLookup.ContainsKey(childNode.parent_node_id)
        //                           ? nodeLookup[childNode.parent_node_id].node_name
        //                           : "Unknown";

        //        int rowIndex = dataGridView1.Rows.Add(indent + childNode.node_name, parentName);

              
        //        if (childNode.node_type == "Parent")
        //        {
        //            dataGridView1.Rows[rowIndex].DefaultCellStyle.BackColor = Color.LightGreen;
        //            dataGridView1.Rows[rowIndex].DefaultCellStyle.Font = boldFont;
        //        }
        //        else
        //        {
                   
        //            dataGridView1.Rows[rowIndex].DefaultCellStyle.BackColor = Color.LightYellow;
        //            dataGridView1.Rows[rowIndex].DefaultCellStyle.Font = normalFont;
        //        }

        //        AddChildNodesFromDb(childNode.node_id, allNodes, nodeLookup, level + 1);
        //    }
        //}


    }
}

