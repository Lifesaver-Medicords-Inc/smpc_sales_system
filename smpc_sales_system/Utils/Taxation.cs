using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace smpc_sales_app.Utils
{
    class TaxValue
    {
        public double Vatable { get; set; }
        public double Vat { get; set; }
        public double Tax { get; set; }  // Added tax property
    }

    class Taxation
    {
        private double GrossAmount { get; set; }
        private const double VAT_RATE = 0.12;  // Fixed 12% VAT rate
        private const double TAX_RATE = 0.12;  // Fixed 12% Tax rate

        public Taxation(double grossAmount)
        {
            this.GrossAmount = grossAmount;
        }

        // Calculate amount including VAT
        public double GetVatInclusive()
        {
            return this.GrossAmount * (1 + VAT_RATE);
        }

        // Calculate amount excluding VAT
        public double GetVatExclusive()
        {
            return this.GrossAmount / (1 + VAT_RATE);
        }

        // Calculate VAT amount
        public double GetVatAmount()
        {
            return this.GrossAmount * VAT_RATE;
        }

        // Calculate Tax amount
        public double GetTaxAmount()
        {
            return this.GrossAmount * TAX_RATE;
        }

        // Get all tax calculations in one object
        public TaxValue GetTaxBreakdown()
        {
            return new TaxValue
            {
                Vatable = this.GrossAmount,
                Vat = this.GetVatAmount(),
                Tax = this.GetTaxAmount()
            };
        }
    }
}
