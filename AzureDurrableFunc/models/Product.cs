using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace durrableShop.models
{
    public enum PaymentMethods
    {
        Visa,
        MasterCard,
        BankOfAmerica,
        UniversalBank
    } 
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }          
        public string Description { get; set; }
        public decimal Price { get; set; }        
        public int StockQuantity { get; set; }    
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? DeletedAt { get; set; }
        public PaymentMethods PaymentMethod { get; set; }
        public float Weight { get; set; }
    }
}
