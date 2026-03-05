using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EcommerceAPI.Business.Concrete;

public class PlatformSellerManager : IPlatformSellerService
{
    private const string DefaultPlatformSellerEmail = "platform-seller@system.local";
    private const string DefaultPlatformSellerFirstName = "Platform";
    private const string DefaultPlatformSellerLastName = "Seller";
    private const string DefaultPlatformSellerBrandName = "Platform Store";
    private const string DefaultPlatformSellerBrandDescription = "Platform tarafından yönetilen ürünler";

    private readonly IUserDal _userDal;
    private readonly ISellerProfileDal _sellerProfileDal;
    private readonly IRoleDal _roleDal;
    private readonly IHashingService _hashingService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PlatformSellerManager> _logger;

    public PlatformSellerManager(
        IUserDal userDal,
        ISellerProfileDal sellerProfileDal,
        IRoleDal roleDal,
        IHashingService hashingService,
        IUnitOfWork unitOfWork,
        IConfiguration configuration,
        ILogger<PlatformSellerManager> logger)
    {
        _userDal = userDal;
        _sellerProfileDal = sellerProfileDal;
        _roleDal = roleDal;
        _hashingService = hashingService;
        _unitOfWork = unitOfWork;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<IDataResult<int>> GetOrCreatePlatformSellerIdAsync()
    {
        var sellerRole = await _roleDal.GetAsync(role => role.Name == "Seller");
        if (sellerRole == null)
        {
            _logger.LogError("Platform seller oluşturulamadı çünkü Seller rolü bulunamadı");
            return new ErrorDataResult<int>("Platform satıcı hesabı hazırlanamadı");
        }

        var email = ResolveSetting("PlatformSeller:Email", DefaultPlatformSellerEmail).ToLowerInvariant();
        var emailHash = _hashingService.Hash(email);

        var user = await _userDal.GetAsync(entity => entity.EmailHash == emailHash);
        if (user == null)
        {
            user = await CreatePlatformUserAsync(email, sellerRole.Id, emailHash);
            if (user == null)
            {
                return new ErrorDataResult<int>("Platform satıcı hesabı hazırlanamadı");
            }
        }

        if (user.RoleId != sellerRole.Id)
        {
            _logger.LogError(
                "Platform seller email'i farklı role sahip bir kullanıcı ile çakıştı. UserId={UserId}, RoleId={RoleId}",
                user.Id,
                user.RoleId);
            return new ErrorDataResult<int>("Platform satıcı hesabı rol uyuşmazlığı nedeniyle hazırlanamadı");
        }

        var existingProfile = await _sellerProfileDal.GetAsync(profile => profile.UserId == user.Id);
        if (existingProfile != null)
        {
            return new SuccessDataResult<int>(existingProfile.Id);
        }

        var profile = new SellerProfile
        {
            UserId = user.Id,
            BrandName = ResolveSetting("PlatformSeller:BrandName", DefaultPlatformSellerBrandName),
            BrandDescription = ResolveSetting("PlatformSeller:BrandDescription", DefaultPlatformSellerBrandDescription),
            ContactEmail = email,
            IsVerified = true,
            ApplicationReviewedAt = DateTime.UtcNow
        };

        await _sellerProfileDal.AddAsync(profile);

        try
        {
            await _unitOfWork.SaveChangesAsync();
            return new SuccessDataResult<int>(profile.Id);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogWarning(ex, "Platform seller profile oluşturulurken yarış durumu algılandı, mevcut profil okunuyor");
            var concurrentProfile = await _sellerProfileDal.GetAsync(entity => entity.UserId == user.Id);
            if (concurrentProfile != null)
            {
                return new SuccessDataResult<int>(concurrentProfile.Id);
            }

            return new ErrorDataResult<int>("Platform satıcı profili oluşturulamadı");
        }
    }

    private async Task<User?> CreatePlatformUserAsync(string email, int sellerRoleId, string emailHash)
    {
        var user = new User
        {
            Email = email,
            EmailHash = emailHash,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString("N")),
            FirstName = ResolveSetting("PlatformSeller:FirstName", DefaultPlatformSellerFirstName),
            LastName = ResolveSetting("PlatformSeller:LastName", DefaultPlatformSellerLastName),
            RoleId = sellerRoleId,
            IsEmailVerified = true
        };

        await _userDal.AddAsync(user);

        try
        {
            await _unitOfWork.SaveChangesAsync();
            return user;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogWarning(ex, "Platform seller user oluşturulurken yarış durumu algılandı, mevcut kullanıcı okunuyor");
            return await _userDal.GetAsync(entity => entity.EmailHash == emailHash);
        }
    }

    private string ResolveSetting(string key, string fallback)
    {
        var configured = _configuration[key];
        return string.IsNullOrWhiteSpace(configured) ? fallback : configured.Trim();
    }
}
