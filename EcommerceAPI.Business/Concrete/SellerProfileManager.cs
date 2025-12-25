using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Core.Interfaces;

using Microsoft.Extensions.Logging;

namespace EcommerceAPI.Business.Concrete;

public class SellerProfileManager : ISellerProfileService
{
    private readonly ISellerProfileDal _sellerProfileDal;
    private readonly IUserDal _userDal;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SellerProfileManager> _logger;

    public SellerProfileManager(
        ISellerProfileDal sellerProfileDal,
        IUserDal userDal,
        IUnitOfWork unitOfWork,
        ILogger<SellerProfileManager> logger)
    {
        _sellerProfileDal = sellerProfileDal;
        _userDal = userDal;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<IDataResult<SellerProfileDto>> GetByUserIdAsync(int userId)
    {
        var profile = await _sellerProfileDal.GetByUserIdWithDetailsAsync(userId);

        if (profile == null)
            return new ErrorDataResult<SellerProfileDto>("Satıcı profili bulunamadı");

        return new SuccessDataResult<SellerProfileDto>(MapToDto(profile));
    }

    public async Task<IDataResult<SellerProfileDto>> GetByIdAsync(int profileId)
    {
        var profile = await _sellerProfileDal.GetByIdWithDetailsAsync(profileId);

        if (profile == null)
            return new ErrorDataResult<SellerProfileDto>("Satıcı profili bulunamadı");

        return new SuccessDataResult<SellerProfileDto>(MapToDto(profile));
    }

    public async Task<IDataResult<SellerProfileDto>> CreateAsync(int userId, CreateSellerProfileRequest request)
    {

        var user = await _userDal.GetByIdWithRoleAsync(userId);
            
        if (user == null)
            return new ErrorDataResult<SellerProfileDto>("Kullanıcı bulunamadı");

        if (user.Role?.Name != "Seller")
            return new ErrorDataResult<SellerProfileDto>("Sadece Seller rolündeki kullanıcılar profil oluşturabilir");


        var existingProfile = await _sellerProfileDal.GetAsync(sp => sp.UserId == userId);
        if (existingProfile != null)
            return new ErrorDataResult<SellerProfileDto>("Bu kullanıcının zaten bir satıcı profili mevcut");

        var profile = new SellerProfile
        {
            UserId = userId,
            BrandName = request.BrandName,
            BrandDescription = request.BrandDescription,
            LogoUrl = request.LogoUrl,
            IsVerified = false
        };

        await _sellerProfileDal.AddAsync(profile);
        await _unitOfWork.SaveChangesAsync();

        profile.User = user;
        _logger.LogInformation("Seller profile created for user {UserId} with brand {BrandName}", userId, request.BrandName);

        return new SuccessDataResult<SellerProfileDto>(MapToDto(profile), "Satıcı profili oluşturuldu");
    }

    public async Task<IDataResult<SellerProfileDto>> UpdateAsync(int userId, UpdateSellerProfileRequest request)
    {
        var profile = await _sellerProfileDal.GetByUserIdWithDetailsAsync(userId);

        if (profile == null)
            return new ErrorDataResult<SellerProfileDto>("Satıcı profili bulunamadı");

        if (!string.IsNullOrEmpty(request.BrandName))
            profile.BrandName = request.BrandName;

        if (request.BrandDescription != null)
            profile.BrandDescription = request.BrandDescription;

        if (request.LogoUrl != null)
            profile.LogoUrl = request.LogoUrl;

        profile.UpdatedAt = DateTime.UtcNow;

        _sellerProfileDal.Update(profile);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Seller profile updated for user {UserId}", userId);

        return new SuccessDataResult<SellerProfileDto>(MapToDto(profile), "Satıcı profili güncellendi");
    }

    public async Task<IResult> DeleteAsync(int userId)
    {
        var profile = await _sellerProfileDal.GetAsync(sp => sp.UserId == userId);

        if (profile == null)
            return new ErrorResult("Satıcı profili bulunamadı");

        _sellerProfileDal.Delete(profile);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Seller profile deleted for user {UserId}", userId);

        return new SuccessResult("Satıcı profili silindi");
    }

    public async Task<bool> HasProfileAsync(int userId)
    {
        return await _sellerProfileDal.ExistsAsync(sp => sp.UserId == userId);
    }

    private static SellerProfileDto MapToDto(SellerProfile profile)
    {
        return new SellerProfileDto
        {
            Id = profile.Id,
            UserId = profile.UserId,
            BrandName = profile.BrandName,
            BrandDescription = profile.BrandDescription,
            LogoUrl = profile.LogoUrl,
            IsVerified = profile.IsVerified,
            CreatedAt = profile.CreatedAt,
            SellerFirstName = profile.User?.FirstName ?? string.Empty,
            SellerLastName = profile.User?.LastName ?? string.Empty
        };
    }
}
