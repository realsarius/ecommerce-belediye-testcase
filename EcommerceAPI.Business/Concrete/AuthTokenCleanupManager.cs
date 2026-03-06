using EcommerceAPI.Application.Abstractions.ServiceContracts;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.Application.Abstractions.Persistence;
using Microsoft.Extensions.Logging;

namespace EcommerceAPI.Business.Concrete;

public class AuthTokenCleanupManager : IAuthTokenCleanupService
{
    private readonly IUserDal _userDal;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AuthTokenCleanupManager> _logger;

    public AuthTokenCleanupManager(
        IUserDal userDal,
        IUnitOfWork unitOfWork,
        ILogger<AuthTokenCleanupManager> logger)
    {
        _userDal = userDal;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task ExecuteAsync()
    {
        var utcNow = DateTime.UtcNow;
        var users = await _userDal.GetListAsync(u =>
            (u.EmailVerificationToken != null &&
             u.EmailVerificationTokenExpiry.HasValue &&
             u.EmailVerificationTokenExpiry.Value < utcNow) ||
            (u.PasswordResetToken != null &&
             u.PasswordResetTokenExpiry.HasValue &&
             u.PasswordResetTokenExpiry.Value < utcNow) ||
            (u.EmailChangeToken != null &&
             u.EmailChangeTokenExpiry.HasValue &&
             u.EmailChangeTokenExpiry.Value < utcNow));

        var affectedUsers = 0;
        foreach (var user in users)
        {
            var changed = false;

            if (user.EmailVerificationToken != null &&
                user.EmailVerificationTokenExpiry.HasValue &&
                user.EmailVerificationTokenExpiry.Value < utcNow)
            {
                user.EmailVerificationToken = null;
                user.EmailVerificationTokenExpiry = null;
                changed = true;
            }

            if (user.PasswordResetToken != null &&
                user.PasswordResetTokenExpiry.HasValue &&
                user.PasswordResetTokenExpiry.Value < utcNow)
            {
                user.PasswordResetToken = null;
                user.PasswordResetTokenExpiry = null;
                changed = true;
            }

            if (user.EmailChangeToken != null &&
                user.EmailChangeTokenExpiry.HasValue &&
                user.EmailChangeTokenExpiry.Value < utcNow)
            {
                user.PendingEmail = null;
                user.EmailChangeToken = null;
                user.EmailChangeTokenExpiry = null;
                changed = true;
            }

            if (!changed)
            {
                continue;
            }

            _userDal.Update(user);
            affectedUsers++;
        }

        if (affectedUsers > 0)
        {
            await _unitOfWork.SaveChangesAsync();
        }

        _logger.LogInformation(
            "Expired auth tokens cleanup completed. AffectedUsers={AffectedUsers}, CheckedAtUtc={CheckedAtUtc}",
            affectedUsers,
            utcNow);
    }
}
