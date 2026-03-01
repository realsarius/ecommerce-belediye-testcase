using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.Entities.DTOs;

namespace EcommerceAPI.Business.Abstract;

public interface ICampaignService
{
    Task<IDataResult<List<CampaignDto>>> GetAllAsync();
    Task<IDataResult<CampaignDto>> GetByIdAsync(int id);
    Task<IDataResult<List<CampaignDto>>> GetActiveAsync();
    Task<IDataResult<CampaignDto>> CreateAsync(CreateCampaignRequest request);
    Task<IDataResult<CampaignDto>> UpdateAsync(int id, UpdateCampaignRequest request);
    Task<IResult> DeleteAsync(int id);
    Task<IResult> ProcessCampaignLifecycleAsync();
}
