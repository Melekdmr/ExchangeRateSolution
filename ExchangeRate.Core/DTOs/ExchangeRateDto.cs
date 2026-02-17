using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExchangeRate.Core.DTOs
{
    public class ExchangeRateDto
    {
        public string CurrencyCode { get; set; }      // USD, EUR vb.
        public decimal RateBuy { get; set; }          // Alış kuru
        public decimal RateSell { get; set; }         // Satış kuru
        public decimal RateAverage { get; set; }      // Ortalama kuru
        public DateTime Date { get; set; }            // Kurun tarihi

       
   }
    }


