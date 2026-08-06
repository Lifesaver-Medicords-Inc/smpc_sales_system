using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data; 
using System.Data.SqlTypes;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace smpc_app.Services.Helpers
{
    internal static class Helpers
    {
        // Simple semi-transparent overlay with a message, dropped on top of whatever control
        // is passed in (e.g. a DataGridView while it's fetching data). Ported from the same
        // pattern already used in smpc_inventory_app's Helpers.Loading for consistency.
        public static class Loading
        {
            // Keyed per parent control instead of a single static field so more than one
            // overlay can be shown at the same time (e.g. pnl_header and pnl_footer both
            // loading together on the Sales Quotation screen). Each parent tracks its own
            // overlay independently - showing one doesn't block or clobber another.
            private static readonly Dictionary<Control, UserControl> overlays = new Dictionary<Control, UserControl>();

            public static void ShowLoading(Control parentControl, string message = "Loading, please wait...")
            {
                if (parentControl == null || overlays.ContainsKey(parentControl)) return; // already showing on this control

                UserControl overlayPanel = new UserControl
                {
                    BackColor = Color.FromArgb(180, Color.Gray), // semi-transparent overlay
                    Dock = DockStyle.Fill
                };

                Label lblMessage = new Label
                {
                    AutoSize = false,
                    Dock = DockStyle.Fill,
                    Text = message,
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 12, FontStyle.Bold),
                    TextAlign = ContentAlignment.MiddleCenter
                };

                overlayPanel.Controls.Add(lblMessage);

                parentControl.Controls.Add(overlayPanel);
                overlayPanel.BringToFront();

                overlays[parentControl] = overlayPanel;
            }

            public static void HideLoading(Control parentControl)
            {
                if (parentControl != null && overlays.TryGetValue(parentControl, out UserControl overlayPanel))
                {
                    parentControl.Controls.Remove(overlayPanel);
                    overlayPanel.Dispose();
                    overlays.Remove(parentControl);
                }
            }

            // Convenience overload for showing/hiding the same message across several
            // parents at once (e.g. pnl_header + pnl_footer together).
            public static void ShowLoading(Control[] parentControls, string message = "Loading, please wait...")
            {
                if (parentControls == null) return;
                foreach (Control parentControl in parentControls)
                {
                    ShowLoading(parentControl, message);
                }
            }

            public static void HideLoading(Control[] parentControls)
            {
                if (parentControls == null) return;
                foreach (Control parentControl in parentControls)
                {
                    HideLoading(parentControl);
                }
            }
        }

        // Recursively walks every control under `root` (Panels, GroupBoxes, TabPages,
        // ToolStrips, etc.) and enables/disables every clickable Button and ToolStripButton
        // it finds. Used to lock the whole form's buttons while data is still being fetched
        // from the server, so a slow response can't be raced by a user click (e.g. hitting
        // Save before the record has finished loading).
        public static void SetButtonsEnabled(Control root, bool enabled)
        {
            if (root == null) return;

            foreach (Control ctrl in root.Controls)
            {
                if (ctrl is Button button)
                {
                    button.Enabled = enabled;
                }
                else if (ctrl is ToolStrip toolStrip)
                {
                    foreach (ToolStripItem item in toolStrip.Items)
                    {
                        if (item is ToolStripButton || item is ToolStripSplitButton || item is ToolStripDropDownButton)
                        {
                            item.Enabled = enabled;
                        }
                    }
                }

                if (ctrl.Controls.Count > 0)
                {
                    SetButtonsEnabled(ctrl, enabled);
                }
            }
        }

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

        public static void ResetControls(Panel[] parents)
        {
            foreach (Panel pnl in parents)
            {
                foreach (Control control in pnl.Controls)
                {
                    if (control is TextBox textBox)
                    {
                        // Check for money_format tag
                        if (textBox.Tag?.ToString() == "money_format")
                        {
                            string input = textBox.Text?.Trim() ?? "";

                            if (!string.IsNullOrEmpty(input))
                            {
                                // Remove everything except digits, dot, comma, and minus
                                string cleaned = Regex.Replace(input, @"[^0-9\.,\-]", "");

                                // Replace comma with dot if comma used as decimal
                                if (cleaned.Count(c => c == ',') == 1 && cleaned.Count(c => c == '.') == 0)
                                    cleaned = cleaned.Replace(',', '.');

                                // Attempt parsing
                                bool isValid =
                                    decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out _) ||
                                    decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.CurrentCulture, out _);

                                if (!isValid)
                                {
                                    MessageBox.Show(
                                        $"Invalid money format in \"{textBox.Name}\".\n" +
                                        $"Value: \"{textBox.Text}\" could not be parsed.\nPlease enter a valid number.",
                                        "Invalid Input",
                                        MessageBoxButtons.OK,
                                        MessageBoxIcon.Warning
                                    );
                                    textBox.Focus();
                                    return; // stop processing
                                }
                            }
                        }

                        // Clear textbox
                        textBox.Clear();
                    }
                    else if (control is ComboBox comboBox)
                    {
                        comboBox.SelectedIndex = -1;
                    }
                    else if (control is CheckBox checkBox)
                    {
                        checkBox.Checked = false;
                    }
                    else if (control is RadioButton radioButton)
                    {
                        radioButton.Checked = false;
                    }
                    else if (control is DateTimePicker dateTimePicker)
                    {
                        dateTimePicker.Value = DateTime.Now;
                    }
                    else if (control is NumericUpDown numericUpDown)
                    {
                        numericUpDown.Value = numericUpDown.Minimum;
                    }
                    else if (control is PictureBox pictureBox)
                    {
                        pictureBox.Image = null;
                    }
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
                    //if (ctrl is Button)
                    //{
                    //    ((Button)ctrl).Enabled = false;
                    //}
                    if (ctrl is ComboBox)
                    {
                        ((ComboBox)ctrl).DropDownStyle = ComboBoxStyle.Simple;
                        ((ComboBox)ctrl).Enabled = false;
                    }
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
                    //if (ctrl is Button)
                    //{
                    //    ((Button)ctrl).Enabled = true;
                    //}
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

        // ReadOnlyControls/ResetReadOnlyControls only walk one level of a panel's direct
        // children and don't touch CheckBox or DataGridView at all - fine for the flat quick
        // quote header panels, but Project Quotation's controls (ItemSetUC) are nested inside
        // several layers of panels and include checkboxes (chk_wiring) and grids
        // (dgv_project_items, dgv_wiring, dgv_final) that were never being locked, so
        // everything stayed editable even before clicking Edit. This walks the whole control
        // tree under `root` and locks/unlocks every field type it finds. DataGridViews are
        // set ReadOnly rather than Disabled so they can still be scrolled/viewed while locked.
        public static void SetControlsEditable(Control root, bool editable)
        {
            foreach (Control ctrl in root.Controls)
            {
                switch (ctrl)
                {
                    case TextBox textBox:
                        textBox.ReadOnly = !editable;
                        break;
                    case ComboBox comboBox:
                        comboBox.Enabled = editable;
                        break;
                    case CheckBox checkBox:
                        checkBox.Enabled = editable;
                        break;
                    case DateTimePicker dateTimePicker:
                        dateTimePicker.Enabled = editable;
                        break;
                    case NumericUpDown numericUpDown:
                        numericUpDown.ReadOnly = !editable;
                        break;
                    case DataGridView dataGridView:
                        dataGridView.ReadOnly = !editable;
                        break;
                }

                // Recurse into anything that can itself contain fields (panels, group boxes,
                // split container panels, nested user controls, etc.) so nothing buried a
                // couple of levels deep gets skipped.
                if (ctrl.Controls.Count > 0)
                    SetControlsEditable(ctrl, editable);
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
                        // Only strips commas, not the "₱" MoneyFormat/BindControls puts on
                        // these fields - so any field that had already been displayed with
                        // its currency symbol (i.e. any money field that was simply left
                        // untouched since the record was loaded) failed to parse here and
                        // showed "Invalid money format" even though the value was fine.
                        bool isParsed = decimal.TryParse(GetCleanedPriceValue(textBox.Text), out decimal tempVal);
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
                            // Only stripped commas, not the "₱" that MoneyFormat/BindControls
                            // puts on these fields - so a money field that had simply been
                            // displayed with its currency symbol (i.e. left untouched since
                            // the record loaded) failed to parse here and showed "Invalid
                            // money format" on save even though nothing was actually wrong
                            // with the value.
                            if (decimal.TryParse(GetCleanedPriceValue(textBox.Text), out decimal tempVal))
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
                            // A blank id field (e.g. content_id on a tab whose content row
                            // hasn't been created/saved yet) is a normal "doesn't exist yet"
                            // state, not a user input mistake - it used to trip the same
                            // "Invalid ID format" warning as actually-malformed text, and it
                            // fired on tab switch/content-changed events, not just Save, since
                            // this runs any time this panel's values get read. Only warn when
                            // there's text that genuinely isn't a number.
                            if (string.IsNullOrWhiteSpace(textBox.Text))
                            {
                                val = 0;
                            }
                            else if (int.TryParse(textBox.Text, out int idVal))
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

        public static Dictionary<string, dynamic> GetControlsValuesV2(Panel[] pnl1)
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
                        if (textBox.Tag != null && (textBox.Tag.ToString() == "money_format" || textBox.Tag.ToString() == "percent_format"))
                        {
                            if (decimal.TryParse(GetCleanedPriceValue(textBox.Text), out decimal tempVal))
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
                            // A blank id field (e.g. content_id on a tab whose content row
                            // hasn't been created/saved yet) is a normal "doesn't exist yet"
                            // state, not a user input mistake - it used to trip the same
                            // "Invalid ID format" warning as actually-malformed text, and it
                            // fired on tab switch/content-changed events, not just Save, since
                            // this runs any time this panel's values get read. Only warn when
                            // there's text that genuinely isn't a number.
                            if (string.IsNullOrWhiteSpace(textBox.Text))
                            {
                                val = 0;
                            }
                            else if (int.TryParse(textBox.Text, out int idVal))
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
            // dt can legitimately be empty (e.g. a fresh/no-record state) or selectedIndex can
            // be stale from a previous, larger dataset - either used to throw
            // IndexOutOfRangeException: "There is no row at position 0." Nothing to bind to, so
            // just leave the controls as they are instead of crashing.
            if (dt == null || dt.Rows.Count == 0 || selectedIndex < 0 || selectedIndex >= dt.Rows.Count)
                return;

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

                                if (valueFromDataTable != DBNull.Value)
                                {
                                    if (valueFromDataTable is DateTime dateTimeValue)
                                    {
                                        dateTimePicker.Value = dateTimeValue;
                                    }
                                    else if (DateTime.TryParse(valueFromDataTable.ToString(), out DateTime parsedDate))
                                    {
                                        dateTimePicker.Value = parsedDate;
                                    }
                                    else
                                    {
                                        dateTimePicker.Value = DateTime.Now;
                                    }
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
                Type columnType = col.ValueType ?? typeof(string);
                dt.Columns.Add(columnName, columnType); 
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

            foreach (DataGridViewColumn column in dgv.Columns)
            {
                if (!dataTable.Columns.Contains(column.Name))
                {
                    dataTable.Columns.Add(column.Name);
                }
            }

            foreach (DataGridViewRow row in dgv.Rows)
            {

                if (!row.IsNewRow)
                {
                    DataRow dataRow = dataTable.NewRow();
                    for (int i = 0; i < dgv.Columns.Count; i++)
                    {
                        string columnName = dgv.Columns[i].Name;

                        if (dataRow[columnName] == DBNull.Value || dataRow[columnName] == null)
                        {
                            dataRow[columnName] = row.Cells[i].Value ?? DBNull.Value;
                        }
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
        public static class SalesItemRowStyler
        {
            public static void ApplyStyle(DataGridView dgv, int rowIndex, string type)
            {
                if (rowIndex < 0 || rowIndex >= dgv.Rows.Count) return;

                DataGridViewRow row = dgv.Rows[rowIndex];

                switch (type.ToLower())
                {
                    case "parent":
                        row.DefaultCellStyle.BackColor = Color.LightBlue;
                        row.DefaultCellStyle.Font = new Font(dgv.Font, FontStyle.Bold);
                        if (row.Cells["quick_item_code"].Value != null)
                            row.Cells["quick_item_code"].Value = "▶ " + row.Cells["quick_item_code"].Value.ToString();
                        break;

                    case "child":
                        row.DefaultCellStyle.BackColor = Color.LightGray;
                        row.DefaultCellStyle.Font = new Font(dgv.Font, FontStyle.Italic);
                        if (row.Cells["quick_item_code"].Value != null)
                            row.Cells["quick_item_code"].Value = "   ↳ " + row.Cells["quick_item_code"].Value.ToString();
                        break;

                    case "single":
                        row.DefaultCellStyle.BackColor = Color.White;
                        row.DefaultCellStyle.Font = new Font(dgv.Font, FontStyle.Regular);
                        // No arrow for single items
                        break;
                }
            }
        }

        public static void EnableGroupHeaders(DataGridView dgv, Dictionary<string, string[]> columnGroups)
        {
            if (dgv == null || columnGroups == null || columnGroups.Count == 0)
                return;

            // Double buffer to reduce flickering
            typeof(DataGridView).InvokeMember("DoubleBuffered",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.SetProperty,
                null, dgv, new object[] { true });

            // Redraw on scroll/resize
            dgv.Scroll += (s, e) => dgv.Invalidate();
            dgv.ColumnWidthChanged += (s, e) => dgv.Invalidate();

            // Paint group headers
            dgv.Paint += (s, e) => DrawGroupHeaders(dgv, e, columnGroups);

            // Override column header painting
            dgv.CellPainting += (s, e) => DrawGroupedHeaderCells(dgv, e);
        }

        private static void DrawGroupHeaders(DataGridView dgv, PaintEventArgs e, Dictionary<string, string[]> groups)
        {
            foreach (var group in groups)
            {
                string groupName = group.Key;
                string[] cols = group.Value;

                if (!cols.All(c => dgv.Columns.Contains(c)))
                    continue;

                DataGridViewColumn firstCol = dgv.Columns[cols.First()];
                DataGridViewColumn lastCol = dgv.Columns[cols.Last()];

                Rectangle r1 = dgv.GetCellDisplayRectangle(firstCol.Index, -1, true);
                Rectangle r2 = dgv.GetCellDisplayRectangle(lastCol.Index, -1, true);

                if (r1.IsEmpty || r2.IsEmpty) continue;

                Rectangle headerRect = new Rectangle(r1.X, r1.Y, r2.Right - r1.X, r1.Height / 2);

                using (Brush b = new SolidBrush(SystemColors.Control))
                    e.Graphics.FillRectangle(b, headerRect);

                e.Graphics.DrawRectangle(Pens.Gray, headerRect);

                TextRenderer.DrawText(e.Graphics, groupName,
                    dgv.ColumnHeadersDefaultCellStyle.Font,
                    headerRect, Color.Black,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        }

        private static void DrawGroupedHeaderCells(DataGridView dgv, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex == -1 && e.ColumnIndex >= 0)
            {
                e.PaintBackground(e.CellBounds, true);

                Rectangle fullRect = e.CellBounds;

                // Bottom half for column text
                Rectangle textRect = fullRect;
                textRect.Y += textRect.Height / 2;
                textRect.Height /= 2;

                TextRenderer.DrawText(e.Graphics,
                    e.FormattedValue?.ToString() ?? "",
                    e.CellStyle.Font, textRect,
                    e.CellStyle.ForeColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

                e.Handled = true;
            }
        }

    }
} 
