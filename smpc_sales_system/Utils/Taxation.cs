using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace smpc_sales_app.Utils
{
      class TaxValue {
        public double Vatable { get; set; }
        public double Vat { get; set; }
    }

      class Taxation
    {
        private double GrossAmount { get; set; }
        private double VatRate = 0.12; // 12% VAT rate
        public Taxation(double grossAmount,double vatRate)
        {
            this.GrossAmount = grossAmount;
            this.VatRate = vatRate;
        }

        public double GetVatInclusive()
        { 
            return this.GrossAmount * (1 + (this.VatRate/100));
        }

        public double GetVatExclusive()
        {
            return this.GrossAmount / (1 + (this.VatRate / 100));
        }
         
    }
}
