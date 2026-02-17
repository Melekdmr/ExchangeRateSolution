using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExchangeRate.Core.DTOs
{
    public class ApiResponseDto
    {
        public string Base { get; set; }  // USD
        public string Date { get; set; }  // 2024-02-17
        public Dictionary<string, decimal> Rates { get; set; }
    }
}
