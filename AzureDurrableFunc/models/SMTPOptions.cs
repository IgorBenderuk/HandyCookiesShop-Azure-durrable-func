using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvoiceGenerator.Models
{
    public record OperationResult<T>(
    bool IsSuccess,
    string Message,
    T? Value = default
    );

    public class SMTPOptions
    {
        public string SmtpHost { get; set; }
        public int SmtpPort { get; set; }
        public string FromName { get; set; }
        public string FromAddress { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
