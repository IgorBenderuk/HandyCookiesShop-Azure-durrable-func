using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace handyCookiesShop.models
{
    public class Customer
    {
        public int Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; } 
        public string LastName { get; set; }
        public string Address { get; set; }
    }
}
