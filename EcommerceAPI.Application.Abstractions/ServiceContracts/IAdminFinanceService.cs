using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.Entities.DTOs;

namespace EcommerceAPI.Application.Abstractions.ServiceContracts;

public interface IAdminFinanceService
{
    Task<IDataResult<AdminFinanceSummaryDto>> GetSummaryAsync(DateTime? from = null, DateTime? to = null);
}
