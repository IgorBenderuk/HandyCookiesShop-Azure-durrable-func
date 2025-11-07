using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvoiceGenerator.Models
{
    internal class SMTPOptions
    {
        public string SmtpHost { get; set; }
        public int SmtpPort { get; set; }
        public string FromName { get; set; }
        public string FromAddress { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
