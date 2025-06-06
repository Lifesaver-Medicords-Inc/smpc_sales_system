using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Management;
using System.Data; 
using System.Data.SqlTypes;
using System.Windows.Forms;
using System.Drawing;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace smpc_app.Services.Helpers
{
    public static class Helpers
    {
        public static void ResetControls(Panel pnl)
        {
            foreach (Control control in pnl.Controls)
            {
                // Check if the control is a TextBox
                if (control is TextBox textBox)
                {
                    // Reset the TextBox's text
                    textBox.Text = "";
                }
            }
        }
        public static void ReadOnlyControls(Panel[] pnl_list)
        {
            foreach (Panel pnl in pnl_list)
            {
                foreach (Control ctrl in pnl.Controls)
                {
                    if (ctrl is TextBox)
                    {
                        ((TextBox)ctrl).ReadOnly = true;
                    }
                    if (ctrl is Button)
                    {
                        ((Button)ctrl).Enabled = false;
                    }
                    //if (ctrl is ComboBox)
                    //{
                    //    ((ComboBox)ctrl).DropDownStyle = ComboBoxStyle.Simple;
                    //    ((ComboBox)ctrl).Enabled = false;
                    //}
                    if (ctrl is DateTimePicker)
                    {
                        ((DateTimePicker)ctrl).Enabled = false;
                    }
                }
            }
        }

        public static void ResetReadOnlyControls(Panel[] pnl_list)
        {
            foreach (Panel pnl in pnl_list)
            {
                foreach (Control ctrl in pnl.Controls)
                {
                    if (ctrl is TextBox)
                    {
                        ((TextBox)ctrl).ReadOnly = false;
                    }
                    if (ctrl is Button)
                    {
                        ((Button)ctrl).Enabled = true;
                    }
                    if (ctrl is ComboBox)
                    {
                        ((ComboBox)ctrl).DropDownStyle = ComboBoxStyle.DropDownList;
                        ((ComboBox)ctrl).Enabled = true;
                    }
                    if (ctrl is DateTimePicker)
                    {
                        ((DateTimePicker)ctrl).Enabled = true;
                    }
                }
            }
        }



        public static Dictionary<string, dynamic> GetControlsValues(Panel pnl)
        {
            Dictionary<string, dynamic> values = new Dictionary<string, dynamic>();
            foreach (Control control in pnl.Controls)
            {
                // Check if the control is a TextBox
                if (control is TextBox textBox)
                {
                    string key = textBox.Name.Replace("txt_", "");
                    dynamic val = null; 

                    if (textBox.Tag != null && textBox.Tag.ToString() == "money_format")
                    {
                        bool isParsed = decimal.TryParse(textBox.Text.ToString().Replace(",", ""), out decimal tempVal);
                        if (isParsed)
                        {
                            val = tempVal; 
                        }
                        else
                        {
                            MessageBox.Show("Invalid money format. Please enter a valid number.");
                            val = 0; 
                        }
                    }
                    else
                    {
                        val = textBox.Text.ToString();
                    }
                    values[key] = val;
                }

            // Check if the control is a Combobox
            if (control is ComboBox comboBox)
                {
                    string key = comboBox.Name.Replace("cmb_", "");
                    string val = "";

                    if (comboBox.Tag == "DYNAMIC")
                    {
                        key = key + "_id";
                        val = comboBox.SelectedValue.ToString();
                    }
                    else if (string.IsNullOrEmpty(comboBox.Text.ToString()))
                    {
                        val = "";
                    }
                    else
                    {
                        val = comboBox.Text.ToString();
                    }

                    if (comboBox.Tag == "DYNAMIC")
                    {
                        values.Add(key, int.Parse(val));
                    }
                    else
                    {
                        values.Add(key, val);
                    }
                    
                }

                // Check if the control is a Checkbox
                if (control is CheckBox checkbox)
                {
                    string key = checkbox.Name.Replace("chk_", "");
                    string val = String.Format("{0}", checkbox.Checked ? 1 : 0);
                    values.Add(key, val);
                }

                // Check if the control is a DATETIME PICKER
                if (control is DateTimePicker dateTimePicker)
                {
                    string key = dateTimePicker.Name.Replace("dtp_", "");
                    string val = String.Format("{0:yyyy-MM-dd}", dateTimePicker.Value);
                    values.Add(key, val);
                }

                // Check if the control is a NUMERIC
                if (control is NumericUpDown numericUpDown)
                {
                    string key = numericUpDown.Name.Replace("txt_", "");
                    string val = String.Format("{0}", numericUpDown.Value);
                    values.Add(key, val);
                }
            }

            return values;
        }

        public static Dictionary<string, dynamic> GetControlsValues(Panel[] pnl1)
        { 
            Dictionary<string, dynamic> values = new Dictionary<string, dynamic>();

            foreach (Panel pnl in pnl1)
            {
                foreach (Control control in pnl.Controls)
                {
                    // Check if the control is a TextBox
                    // Check if the control is a TextBox
                    if (control is TextBox textBox)
                    {
                        string key = textBox.Name.Replace("txt_", "");
                        dynamic val = null;

                        // Handle money formatting
                        if (textBox.Tag != null && textBox.Tag.ToString() == "money_format")
                        {
                            if (decimal.TryParse(textBox.Text.Replace(",", ""), out decimal tempVal))
                            {
                                val = tempVal;
                            }
                            else
                            {
                                MessageBox.Show("Invalid money format. Please enter a valid number.");
                                val = 0m;
                            }
                        }
                        // Handle _id conversion
                        else if (key.EndsWith("_id"))
                        {
                            if (int.TryParse(textBox.Text, out int idVal))
                            {
                                val = idVal;
                            }
                            else
                            {
                                MessageBox.Show($"Invalid ID format for '{key}'. Please enter a valid number.");
                                val = 0;
                            }
                        }
                        else
                        {
                            // Default to string if no special formatting
                            val = textBox.Text.ToString();
                        }

                        values[key] = val;
                    }


                    if (control is ComboBox comboBox)
                    {
                        string key = comboBox.Name.Replace("cmb_", "");
                        string val = "";

                        if (comboBox.Tag?.ToString() == "DYNAMIC")
                        {
                            key += "_id";
                            var selectedValue = comboBox.SelectedValue;

                            // Handle null SelectedValue
                            if (selectedValue != null && !(selectedValue is DataRowView))
                            {
                                values.Add(key, int.Parse(selectedValue.ToString()));
                            }
                            else
                            {
                                Console.WriteLine($"Warning: No valid selected value for {comboBox.Name}");
                            }
                        }
                        else
                        {
                            val = comboBox.Text?.ToString() ?? string.Empty;
                            values.Add(key, val);
                        }
                    }



                    if (control is CheckBox checkbox)
                    {
                        string key = checkbox.Name.Replace("chk_", "");
                        string val = String.Format("{0}", checkbox.Checked ? 1 : 0);
                        values.Add(key, val);
                    }

                   
                    if (control is DateTimePicker dateTimePicker)
                    {
                        string key = dateTimePicker.Name.Replace("dtp_", "");
                        string val = String.Format("{0:yyyy-MM-dd HH:mm:ss}", dateTimePicker.Value);
                        values.Add(key, val);
                    }

                    
                    if (control is NumericUpDown numericUpDown)
                    {
                        string key = numericUpDown.Name.Replace("txt_", "");
                        string val = String.Format("'{0}'", numericUpDown.Value);
                        values.Add(key, val);
                    }
                }
            }
            return values;
        }
        public static Boolean ValidateControlsValues(Panel pnl)
        {
            Boolean isError = false;
            Dictionary<string, dynamic> values = new Dictionary<string, dynamic>();
            foreach (Control control in pnl.Controls)
            {
                // Check if the control is a TextBox
                if (control is TextBox textBox)
                {
                    string key = textBox.Name.Replace("txt_", "");
                    string val = "";
                    if (textBox.Tag == "REQUIRED" && textBox.Text == "")
                    {
                        control.BackColor = Color.Red;
                        control.ForeColor = Color.White;
                        isError = true; 
                    }
                    else
                    {
                        control.BackColor = Color.White;
                        control.ForeColor = Color.Black;
                    }
                } 
            } 
            return isError;
        }
        public static void BindControls(Panel[] pnl_list, DataTable dt, int selectedIndex = 0)
        {
            Dictionary<string, dynamic> values = new Dictionary<string, dynamic>();

            foreach (var col_name in dt.Columns)
            { 
                foreach (var pnl in pnl_list)
                { 
                    foreach (Control control in pnl.Controls)
                    {
                        
                        if (control.Name.Contains(col_name.ToString() ))
                        {
                            string column_name = col_name.ToString();
                            Console.WriteLine(column_name);

                            // Check if the control is a TextBox 
                            if (control is TextBox textBox && textBox.Name.Replace("txt_", "") == column_name)
                            {
                               
                                string key = textBox.Name.Replace("txt_", "");
                             
                                if (textBox.Tag == "money_format")
                                { 
                                    textBox.Text = Helpers.MoneyFormat(double.Parse(dt.Rows[selectedIndex][column_name].ToString()));
                                }
                                else
                                {
                                   
                                    textBox.Text = (string)dt.Rows[selectedIndex][column_name].ToString();
                                }
                            }

                            // Check if the control is a Combobox
                            if (control is ComboBox comboBox)
                            {
                                Console.WriteLine(comboBox.Name);
                                string key = comboBox.Name.Replace("cmb_", "") + "_id";

                                if (comboBox.Tag == "DYNAMIC")
                                {
                                   
                                    //Console.WriteLine(comboBox.Name);
                                    comboBox.SelectedValue = (string)dt.Rows[selectedIndex][key].ToString();
                                }
                                else
                                {
                                    string keys = comboBox.Name.Replace("cmb_", "");
                                    //Console.WriteLine(comboBox.Name);
                                    comboBox.Text = (string)dt.Rows[selectedIndex][column_name].ToString();
                                }
                             
                            }

                            // Check if the control is a Checkbox
                            if (control is CheckBox checkbox)
                            {
                                
                                string key = checkbox.Name.Replace("chk_", ""); 
                                checkbox.Checked = (string)dt.Rows[selectedIndex][column_name].ToString() == "1" ? true : false; 
                            }

                            // Check if the control is a DATETIME PICKER
                            if (control is DateTimePicker dateTimePicker)
                            {
                                string key = dateTimePicker.Name.Replace("dtp_", "");
                                string val = String.Format("'{0}'", dateTimePicker.Value);
                                object valueFromDataTable = dt.Rows[selectedIndex][column_name]; 

                                if (valueFromDataTable != DBNull.Value && valueFromDataTable is DateTime dateTimeValue)
                                {
                                    dateTimePicker.Value = dateTimeValue;
                                }
                                else
                                { 
                                    dateTimePicker.Value = DateTime.Now;  
                                }

                            }

                            // Check if the control is a NUMERIC
                            if (control is NumericUpDown numericUpDown)
                            {
                                string key = numericUpDown.Name.Replace("txt_", "");
                                numericUpDown.Text = (string)dt.Rows[selectedIndex][column_name].ToString();
                            }
                        } 
                    }
                }
            }
             
        }
        public static string GetLocalIPAddress()
        {
            string localIP = string.Empty;

            // Get the host name
            string hostName = Dns.GetHostName();

            // Get the list of IP addresses associated with the host
            foreach (var ip in Dns.GetHostAddresses(hostName))
            {
                // Check if it's an IPv4 address
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    localIP = ip.ToString();
                    break; // Exit the loop after Getting the first IPv4 address
                }
            }

            return localIP;
        }

        public static DataTable GetDataTableFromUnboundGrid(DataGridView dgv)
        {
            DataTable dt = new DataTable();

            // Create columns using DataPropertyName
            foreach (DataGridViewColumn col in dgv.Columns)
            {
                string columnName = string.IsNullOrWhiteSpace(col.DataPropertyName) ? col.Name : col.DataPropertyName;
                dt.Columns.Add(columnName, typeof(string)); // Adjust the type as needed
            }

            // Add rows
            foreach (DataGridViewRow row in dgv.Rows)
            {
                if (!row.IsNewRow)
                {
                    DataRow dtRow = dt.NewRow();
                    for (int i = 0; i < row.Cells.Count; i++)
                    {
                        dtRow[i] = row.Cells[i].Value ?? DBNull.Value;
                    }
                    dt.Rows.Add(dtRow);
                }
            }

            return dt;
        }










        public static string GetSerialNumber()
        {
            try
            {
                string serialNumber = string.Empty;
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_BaseBoard");

                foreach (ManagementObject mo in searcher.Get())
                {
                    serialNumber = mo["SerialNumber"].ToString();
                    break; // Assuming only one motherboard
                }
                return serialNumber;
            }
            catch (Exception ex)
            {
                
                Console.WriteLine("Error: " + ex.Message);
                return "";
            }
        }
        public static void ShowDialogMessage(string status,string message="")
        {
            switch (status)
            {
                case "success":
                    MessageBox.Show(message, "SMPC SOFTWARE", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    break;
                case "error":
                    MessageBox.Show(message, "SMPC SOFTWARE", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    break;
                default:
                    // Handle unexpected status values
                    MessageBox.Show("Unknown status: " + status, "SMPC SOFTWARE", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    break;
            }
        }
        public static void CopyFileTo(string filePath,string destinationPath)
        {
            try
            {
                File.Copy(filePath, destinationPath, true);
            }
            catch (Exception)
            { 
                throw;
            }
        }
        public static DataTable ConvertDataGridViewToDataTable(DataGridView dgv)
        {
            DataTable dataTable = new DataTable();

            // Add columns to DataTable
            foreach (DataGridViewColumn column in dgv.Columns)
            {
                dataTable.Columns.Add(column.Name);
            }

            // Add rows to DataTable
            foreach (DataGridViewRow row in dgv.Rows)
            {
                // Skip the new row placeholder if it's present
                if (!row.IsNewRow)
                {
                    DataRow dataRow = dataTable.NewRow();
                    for (int i = 0; i < dgv.Columns.Count; i++)
                    {
                        dataRow[i] = row.Cells[i].Value;
                    }
                    dataTable.Rows.Add(dataRow);
                }
            }

            return dataTable;
        }



        public static string MoneyFormat(double money)
        {
            return String.Format("{0:N2}", money);
        }

        public static string MoneyFormatDecimal(decimal money)
        {
            return String.Format("{0:N2}", money);
        }


        // format to peso
        public static string FormatAsCurrency(object value)
        {
            if (decimal.TryParse(value?.ToString().Replace("₱", "").Replace(",", "").Trim(), out decimal number))
            {
                return number.ToString("C2", System.Globalization.CultureInfo.GetCultureInfo("en-PH"));
            }
            return "₱0.00";
        }


        // trims the peso sign
        public static string GetCleanedPriceValue(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "0";
            // Remove currency symbols and thousands separators
            var cleaned = input.Replace("₱", "")
                               .Replace("$", "")
                               .Replace(",", "")
                               .Trim();

            return cleaned;
        }


        // Converts the data types to string so it can be easily editable
        public static DataTable ConvertDataTableToStringTable(DataTable originalTable)
        {
            DataTable stringTable = new DataTable();

            
            foreach (DataColumn col in originalTable.Columns)
            {
                stringTable.Columns.Add(col.ColumnName, typeof(string));
            }

            // Copy rows as strings
            foreach (DataRow row in originalTable.Rows)
            {
                var newRow = stringTable.NewRow();
                foreach (DataColumn col in originalTable.Columns)
                {
                    newRow[col.ColumnName] = row[col]?.ToString();
                }
                stringTable.Rows.Add(newRow);
            }

            return stringTable;
        }





        public static void GetModalData(TextBox textBox, DataView dataView)
        {
            int recordIndex = 0;
            textBox.Text = "";

            foreach (DataRowView rowView in dataView)
            {

                textBox.Text += recordIndex == 0 ? rowView["name"].ToString() : ", " + rowView["name"].ToString();
                recordIndex++;

            }

        }

        public static bool ConvertToIntIfString(Dictionary<string, object> data, string key)
        {
            if (data.ContainsKey(key) && data[key] is string strValue)
            {
                if (int.TryParse(strValue, out int intValue))
                {
                    data[key] = intValue;
                    return true;
                }
                else
                {
                    MessageBox.Show($"Invalid {key.Replace("_", " ")}");
                    return false;
                }
            }
            return true; // If the key is not present or not a string, no conversion needed
        }
        
        public static DataTable FilterDataTable(DataTable dataTable, string searchTerm, params string[] columnsToSearch)
        {
            if (dataTable == null || columnsToSearch == null || columnsToSearch.Length == 0)
            {
                return dataTable;
            }

            searchTerm = searchTerm?.ToLower() ?? string.Empty;

            var filteredRows = dataTable.AsEnumerable().Where(row =>
                columnsToSearch.Any(column =>
                    row[column]?.ToString().ToLower().Contains(searchTerm) == true));

            return filteredRows.Any() ? filteredRows.CopyToDataTable() : dataTable.Clone();
        }


        public static DataTable FilterExactDataTable(DataTable dataTable, string searchTerm, params string[] columnsToSearch)
        {
            if (dataTable == null || columnsToSearch == null || columnsToSearch.Length == 0)
            {
                return dataTable;
            }

            searchTerm = searchTerm?.ToLower() ?? string.Empty;

            var filteredRows = dataTable.AsEnumerable().Where(row =>
                columnsToSearch.Any(column =>
                    row[column]?.ToString().ToLower() == searchTerm));

            return filteredRows.Any() ? filteredRows.CopyToDataTable() : dataTable.Clone();
        }

        public static void GetBPIModalData(TextBox textBox, DataView dataView, int columnIndex)
        {
            if (dataView != null && dataView.Count > 0)
            {
                textBox.Text = dataView[0][columnIndex].ToString();
            }
        }
        public static void SetRowNumber(DataGridView grid, DataGridViewRowPostPaintEventArgs e, int columnIndex = 0)
        {
            if (grid != null && e.RowIndex >= 0 && columnIndex >= 0 && columnIndex < grid.ColumnCount)
            {
                grid.Rows[e.RowIndex].Cells[columnIndex].Value = (e.RowIndex + 1).ToString();
            }
        }
        public static void ClearDataGridView(DataGridView grid)
        {
            if (grid != null && grid.Rows.Count > 0)
            {
                grid.Rows.Clear();
            }
        }
        public static void LoadDirectory(string path, TreeView treeView)
        { 
            // Clear any existing nodes
            treeView.Nodes.Clear();

            // Get the top-level directory and create a root node
            DirectoryInfo dirInfo = new DirectoryInfo(path);
            TreeNode rootNode = new TreeNode(dirInfo.Name);
            treeView.Nodes.Add(rootNode);

            // Load subdirectories and files recursively
            LoadSubdirectoriesAndFiles(rootNode, dirInfo.FullName);
        } 
        private static void LoadSubdirectoriesAndFiles(TreeNode parentNode, string path)
        {
            try
            {
                // Get all subdirectories in the given path
                string[] subdirectories = Directory.GetDirectories(path);

                foreach (string subdirectory in subdirectories)
                {
                    // Create a node for the subdirectory
                    DirectoryInfo dirInfo = new DirectoryInfo(subdirectory);
                    TreeNode subDirNode = new TreeNode(dirInfo.Name);

                    // Add the subdirectory node to the parent node
                    parentNode.Nodes.Add(subDirNode);

                    // Recursively load subdirectories and files into the current subdirectory node
                    LoadSubdirectoriesAndFiles(subDirNode, subdirectory);
                }

                // Get all files in the current directory and add them as leaf nodes
                string[] files = Directory.GetFiles(path);
                foreach (string file in files)
                {
                    FileInfo fileInfo = new FileInfo(file);
                    TreeNode fileNode = new TreeNode(fileInfo.Name);
                    fileNode.Tag = file; // Store the full file path in the Tag property
                    parentNode.Nodes.Add(fileNode); // Add the file node
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Handle access permissions issues if necessary
            }
        }

        public static JObject GetChangedEntries(JObject newData, Dictionary<string, dynamic> cachedData)
        {
            var changedEntries = new Dictionary<string, dynamic>();
           
            foreach (var kvp in newData)
            {
                string key = kvp.Key;
                var newValue = kvp.Value;

                if (cachedData.TryGetValue(key, out var cachedValue))
                {
                    string newJson = JsonConvert.SerializeObject(newValue);
                    string cachedJson = JsonConvert.SerializeObject(cachedValue);

                    if (newJson == cachedJson)
                    {
                        continue; // Value is the same, skip it
                    }
                }

                // Either new key or changed value
                changedEntries[key] = newValue;
            }

            return JObject.FromObject(changedEntries);
        }

        public static Dictionary<string, dynamic> GetChangedEntries(Dictionary<string, JArray> newData, Dictionary<string, dynamic> cachedData)
        {
            var changedEntries = new Dictionary<string, dynamic>();

            foreach (var kvp in newData)
            {
                string key = kvp.Key;
                var newValue = kvp.Value;

                if (cachedData.TryGetValue(key, out var cachedValue))
                {
                    string newJson = JsonConvert.SerializeObject(newValue);
                    string cachedJson = JsonConvert.SerializeObject(cachedValue);

                    if (newJson == cachedJson)
                    {
                        continue; // Value is the same, skip it
                    }
                }

                // Either new key or changed value
                changedEntries[key] = newValue;
            }

            return changedEntries;
        }

    }
} 
