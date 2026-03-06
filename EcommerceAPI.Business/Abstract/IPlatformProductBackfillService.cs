using EcommerceAPI.Core.Utilities.Results;

namespace EcommerceAPI.Business.Abstract;

public interface IPlatformProductBackfillService
{
    Task<IResult> BackfillMissingSellerIdsAsync();
}
