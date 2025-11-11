using System.ComponentModel.DataAnnotations;

namespace durrableShop.models.RequestDto
{
    public class CreateCookieRequestDto
    {
        [Required]
        public int CustomerId { get; set; }

        [Required]
        public ICollection<CreateCookieRequestItemDto> OrderItems {  get; set; }

        [Required]
        public string ShippingAdress { get; set; }
    }
    
    public class CreateCookieRequestItemDto
    {
        [Required]
        public int CookieId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1.")]
        public int Quantity { get; set; }
    }
}
