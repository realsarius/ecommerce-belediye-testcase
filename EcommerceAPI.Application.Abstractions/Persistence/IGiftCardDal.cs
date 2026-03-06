using EcommerceAPI.Core.DataAccess;
using EcommerceAPI.Entities.Concrete;

namespace EcommerceAPI.Application.Abstractions.Persistence;

public interface IGiftCardDal : IEntityRepository<GiftCard>
{
    Task<GiftCard?> GetByCodeAsync(string code);
    Task<GiftCard?> GetByIdWithAssignedUserAsync(int id);
    Task<IList<GiftCard>> GetAllWithAssignedUserAsync();
    Task<IList<GiftCard>> GetUserGiftCardsAsync(int userId);
}
