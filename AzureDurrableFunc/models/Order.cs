using Castle.DynamicProxy;
using System.ComponentModel.DataAnnotations.Schema;

namespace durrableShop.models
{
    public class Order
    {
        public int Id { get; set; }

        public int CustomerId { get; set; }
        public Customer Customer { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsConfirmed { get; set; }
        public bool IsPaied { get; set; }
        public bool IsCompleted { get; set; }
        public string DeliveryAddress { get; set; }

        public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();

        [NotMapped]
        public decimal TotalAmount => Items.Sum(i => i.Quantity * i.Price);
    }
}
