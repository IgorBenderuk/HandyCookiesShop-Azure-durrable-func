using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.QuickPulse;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace handyCookiesShop.models.RequestDto
{
    public class CreateCookieRequestDto
    {
        [Required]
        public int CustomerId { get; set; }

        [Required]
        public List<CreateCookieRequestItemDto> OrderItems {  get; set; } 
    }
    public class CreateCookieRequestItemDto
    {
        [Required]
        public int CookieId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1.")]
        public int Quantity { get; set; }
    }
}
