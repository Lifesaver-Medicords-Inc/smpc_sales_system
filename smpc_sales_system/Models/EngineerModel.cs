using Newtonsoft.Json;

namespace smpc_sales_system.Models
{
    internal class EngineerModel
    {
        [JsonProperty("id")]
        public int Id { get; set; }
        [JsonProperty("first_name")]
        public string FirstName { get; set; }
        [JsonProperty("last_name")]
        public string LastName { get; set; }
        [JsonProperty("full_name")]
        public string FullName { get; set; }
        [JsonProperty("department")]
        public string Department { get; set; }
    }
}
