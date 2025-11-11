using durrableShop.models;
using durrableShop.models.RequestDto;
using Mapster;

namespace durrableShop
{
    public static class MappingConfig
    {
        public static void Configure()
        {
            TypeAdapterConfig<CreateCookieRequestDto, Order>
                .NewConfig()
                .Map(dest => dest.DeliveryAddress, src => src.ShippingAdress)
                .Map(dest => dest.Items, src => src.OrderItems)
                .Map(dest => dest.IsConfirmed, src => false)
                .Map(dest => dest.IsPaied, src => false)
                .Map(dest => dest.IsCompleted, src => false);

            TypeAdapterConfig<CreateCookieRequestItemDto, OrderItem>
                .NewConfig()
                .Map(dest => dest.ProductId, src => src.CookieId)
                .Map(dest => dest.Quantity, src => src.Quantity);
        }
    }
}
