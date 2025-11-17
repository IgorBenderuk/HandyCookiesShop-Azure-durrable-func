using durrableShop.models;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;

namespace DurrableShop.models.RequestDto
{
    public class ConfirmPurchaseRequestDto
    {
        public string InstanceId { get; set; }

        public Order Order { get; set; }
    }
}
