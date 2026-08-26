using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace smpc_sales_system.Models
{
    internal class CompanyModel
    {
        [JsonProperty("id")]
        public int Id { get; set; }
        [JsonProperty("company_code")]
        public string CompanyCode { get; set; }
        [JsonProperty("company_name")]
        public string CompanyName { get; set; }
        [JsonProperty("legal_name")]
        public string LegalName { get; set; }
        [JsonProperty("trade_name")]
        public string TradeName { get; set; }
        [JsonProperty("business_type")]
        public string BusinessType { get; set; }
        [JsonProperty("sec_registration_no")]
        public string SecRegistrationNo { get; set; }
        [JsonProperty("dti_registration_no")]
        public string DtiRegistrationNo { get; set; }
        [JsonProperty("tin")]
        public string tin { get; set; }
        [JsonProperty("bir_branch_code")]
        public string BirBranchCode { get; set; }
        [JsonProperty("rdo_code")]
        public string RdoCode { get; set; }
        [JsonProperty("industry")]
        public string Industry { get; set; }
        [JsonProperty("status")]
        public string Status { get; set; }
        [JsonProperty("is_head_office")]
        public bool IsHeadOffice { get; set; }
        [JsonProperty("beg_bal")]
        public float BegBal { get; set; }
        [JsonProperty("monthly_rate")]
        public float MonthlyRate { get; set; }
        [JsonProperty("markup_multiplier_price")]
        public float MarkUpMultiplierPrice { get; set; }
        // Sales_Quotation_Bug_Report_2026-08-03.md #18 - whole-number percentage
        // (12 means 12%), matching how VAT is written throughout the spec.
        [JsonProperty("vat_rate_percent")]
        public float VatRatePercent { get; set; }
        [JsonProperty("currency_code")]
        public string CurrencyCode { get; set; }
        [JsonProperty("inclusions_quotation_terms")]
        public string InclusionsQuotationTerms { get; set; }
        [JsonProperty("exclusions_quotation_terms")]
        public string ExclusionsQuotationTerms { get; set; }
        [JsonProperty("term_and_conditions")]
        public string TermAndConditions { get; set; }
        [JsonProperty("start_fiscal_date")]
        public string StartFiscalYearDate { get; set; }
        [JsonProperty("end_fiscal_date")]
        public string EndFiscalYearDate { get; set; }
        [JsonProperty("address")]
        public CompanyAddressModel Address { get; set; }
        [JsonProperty("contacts")]
        public CompanyContactModel[] Contacts { get; set; }
    }

    internal class  CompanyAddressModel
    {
        [JsonProperty("Id")]
        public int Id { get; set; }
        [JsonProperty("company_id")]
        public int CompanyId { get; set; }
        [JsonProperty("address_type")]
        public string AddressType { get; set; }
        [JsonProperty("unit_no")]
        public string UnitNo { get; set; }
        [JsonProperty("building_name")]
        public string BuildingName { get; set; }
        [JsonProperty("street_name")]
        public string StreetName { get; set; }
        [JsonProperty("subdivision")]
        public string Subdivision { get; set; }
        [JsonProperty("barangay")]
        public string barangay { get; set; }
        [JsonProperty("city")]
        public string City { get; set; }
        [JsonProperty("province")]
        public string Province { get; set; }
        [JsonProperty("region")]
        public string Region { get; set; }
        [JsonProperty("country")]
        public string Country { get; set; }
        [JsonProperty("postal_code")]
        public int PostalCode { get; set; }
    }

    internal class CompanyContactModel
    {
        [JsonProperty("id")]
        public int Id { get; set; }
        [JsonProperty("company_id")]
        public int Company { get; set; }
        [JsonProperty("full_name")]
        public string Fullname { get; set; }
        [JsonProperty("designation")]
        public string Designation { get; set; }
        [JsonProperty("email")]
        public string Email { get; set; }
        [JsonProperty("phone_no")]
        public string PhoneNumber { get; set; }
        [JsonProperty("mobile_no")]
        public string MobileNumber { get; set; }
        [JsonProperty("is_primary_contact")]
        public bool IsPrimaryContact { get; set; }
    }
}
