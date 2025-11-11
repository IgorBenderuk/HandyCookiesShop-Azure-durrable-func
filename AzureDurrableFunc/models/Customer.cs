using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace durrableShop.models
{
    public class Customer
    {
        public int Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; } 
        public string LastName { get; set; }
        public string BankAccount {  get; set; }
        public ICollection<PaymentMethods> PaymentMethods { get; set; }
    }
}
