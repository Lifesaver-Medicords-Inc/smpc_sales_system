using Newtonsoft.Json;
using smpc_app.Services.Helpers;
using smpc_sales_app.Services.Helpers;
using smpc_sales_system.Services.Setup;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace smpc_sales_system.Pages.Sales
{
    public partial class ProjectTest : Form
    {


        private ClientWebSocket _websocket;
        private CancellationTokenSource _cancelTokenSource;
        public ProjectTest()
        {
             InitializeComponent();
            _websocket = new ClientWebSocket();
            _cancelTokenSource = new CancellationTokenSource();

        }



        private void fetchData(DataTable tb)
        {
            Panel[] pnls = { pnl_header };
           // var data = await ProjectService.GetAsDatatable();
            projects = tb;
            dataGridView1.DataSource = projects;

            if (SELECTEDINDEX >= 0)
            {
                Helpers.BindControls(pnls, projects, SELECTEDINDEX);
            }
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            Panel[] pnl_list = { pnl_header };

            var data = Helpers.GetControlsValues(pnl_list);
            ApiResponseModel response = new ApiResponseModel();

            string errorMessage =
                                string.IsNullOrWhiteSpace(txt_project_name.Text) ? "Name cannot be empty." : null;

            if (!string.IsNullOrEmpty(errorMessage))
            {
                Helpers.ShowDialogMessage("error", errorMessage);
                return;
            }



            response = await ProjectServicesss.Insert(data);

            if (response.Success)
            {
                Helpers.ResetControls(pnl_header);
                MessageBox.Show("Saved");
            }

            Helpers.ShowDialogMessage(response.Success ? "success" : "error");
        }
        int countdown = 5;
        private async void txt_project_name_TextChanged(object sender, EventArgs e)
        {
            Panel[] pnls = { pnl_header };

            if (IsEdit && !ISBind)
            {
                //var data = Helpers.GetControlsValues(pnls);
                timer_save.Stop();
                //await SendMessageAsync(data);
                timer_save.Start();
            }
        }

        DataTable projects = new DataTable();
        private async void ProjectTest_Load(object sender, EventArgs e)
        {


            //fetchData();
            ConnectWebSocket();
        }

        private async void ConnectWebSocket()
        {
            //Uri serverUri = new Uri("ws://localhost:3000/api/setup/witem/");
            Uri serverUri = new Uri("ws://94e9-112-201-111-220.ngrok-free.app/api/ws/purchasing/redboxlist");
            await _websocket.ConnectAsync(serverUri, _cancelTokenSource.Token);
            MessageBox.Show("Connected!!");
            lbl_status.ForeColor = Color.Green;
            lbl_status.Text = "CONNECTED";
            ReceiveDataAsync();

        }





        private async void ReceiveDataAsync()
        {
            byte[] buffer = new byte[1024 * 1024];
            List<byte> messageBuffer = new List<byte>();
            Panel[] pnl_list = { pnl_header };
            while (_websocket.State == WebSocketState.Open)
            {
                try
                {
                    WebSocketReceiveResult result = await _websocket.ReceiveAsync(new ArraySegment<byte>(buffer), _cancelTokenSource.Token);

                    if (result.MessageType == WebSocketMessageType.Text)
                    {

                        // receives the fragmented data
                        messageBuffer.AddRange(buffer.Take(result.Count));
                        Console.WriteLine(messageBuffer);

                        // once the data is complete proceed to decode the bytes to string and convert to JSON object then convert to table to use as datasource;
                        if (result.EndOfMessage)
                        {
                            string completeData = Encoding.UTF8.GetString(messageBuffer.ToArray());
                            messageBuffer.Clear();

                            var jsonObject = JsonConvert.DeserializeObject<List<Project>>(completeData);
                            DataTable table = JsonHelper.ToDataTable(jsonObject);
                            DataTable table2 = JsonHelper.ToDataTable(jsonObject);


                            if (table.Rows.Count > 0)
                            {
                                ISBind = true;

                                Invoke(new Action(() =>
                                {
                                    fetchData(table);
                                }));

                                ISBind = false;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error receiving message: {ex.Message}");
                }
            }

        }
        //private async Task SendMessageAsync(Dictionary<string, dynamic> data)
        //{
        //    if (_websocket.State == WebSocketState.Open)
        //    {
        //        try
        //        {
        //            string jsonString = JsonConvert.SerializeObject(data);

        //            byte[] messageBytes = Encoding.UTF8.GetBytes(jsonString);
        //            ArraySegment<byte> messageSegment = new ArraySegment<byte>(messageBytes);

        //            // Send the message
        //            await _websocket.SendAsync(messageSegment, WebSocketMessageType.Text, true, _cancelTokenSource.Token);

        //            MessageBox.Show("Message sent!");
        //        }
        //        catch (Exception ex)
        //        {
        //            MessageBox.Show($"Error sending message: {ex.Message}");
        //        }
        //    }
        //}






        private void BindDataTableToTextBoxes(DataTable table)
        {
            if (table.Rows.Count > 0)
            {

                DataRow row = table.Rows[0];

                txt_project_name.Text = row["project_name"].ToString();
                txt_customer_name.Text = row["customer_name"].ToString();

            }
            else
            {
                MessageBox.Show("No data found in the DataTable.");
            }
        }



        private bool IsEdit = false;
        private bool ISBind = false;
        private int SELECTEDINDEX { get; set; }
        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex > 0)
            {
                SELECTEDINDEX = e.RowIndex;
                Panel[] pnls = { pnl_header };
                Helpers.BindControls(pnls, projects, SELECTEDINDEX);
                IsEdit = true;
            }
        }

        private async void timer_save_Tick(object sender, EventArgs e)
        {
            timer_save.Stop();

            //var data = Helpers.GetControlsValues(pnl_header);

            Dictionary<string, object> data = new Dictionary<string, object>();
            data.Add("id", int.Parse(txt_id.Text));
            data.Add("project_name", txt_project_name.Text);
            data.Add("customer_name", txt_customer_name.Text);
            data.Add("web_socket_id", int.Parse(txt_web_socket_id.Text));
           
      
            var success = await ProjectServicesss.Update(data);
            if (success.Success)
            {
                MessageBox.Show("SAVED" + data);
            }
            //await SendMessageAsync(data);
        }

        private async void txt_customer_name_TextChanged(object sender, EventArgs e)
        {
            Panel[] pnls = { pnl_header };

            if (IsEdit && !ISBind)
            {
                timer_save.Stop();
                timer_save.Start();
            }
        }
    }
}
