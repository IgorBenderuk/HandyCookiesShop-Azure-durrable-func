using durrableShop.models;

namespace DurrableShop.models.RequestDto
{
    public class ValidateBalanceRequestDto
    {
        public Order OrderEntry {  get; set; }
        public decimal ShippingCost { get; set; }
    }
}
