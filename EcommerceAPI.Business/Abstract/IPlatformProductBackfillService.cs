using EcommerceAPI.Core.Utilities.Results;

namespace EcommerceAPI.Business.Abstract;

public interface IPlatformProductBackfillService
{
    Task<IReadOnlyList<int>> GetProductIdsWithoutSellerSnapshotAsync();
    Task<IResult> BackfillMissingSellerIdsAsync();
}
