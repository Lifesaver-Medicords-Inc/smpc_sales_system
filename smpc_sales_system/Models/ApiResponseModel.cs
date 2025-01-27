using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace smpc_inventory_app.Services.Setup
{
    public class ApiResponseModel<T>
    {
        public bool Success { get; set; }
        public T Data { get; set; }
       

    }

    public  class ApiResponseModel
    {
        public  bool Success { get; set; }
        public  string message { get; set; }


    }






}
