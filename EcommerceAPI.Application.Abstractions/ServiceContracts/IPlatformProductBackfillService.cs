using EcommerceAPI.Core.Utilities.Results;

namespace EcommerceAPI.Application.Abstractions.ServiceContracts;

public interface IPlatformProductBackfillService
{
    Task<IReadOnlyList<int>> GetProductIdsWithoutSellerSnapshotAsync();
    Task<IResult> BackfillMissingSellerIdsAsync();
}
