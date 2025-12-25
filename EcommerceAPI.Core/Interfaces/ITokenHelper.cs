namespace EcommerceAPI.Core.Interfaces;

public interface ITokenHelper
{
    string GenerateAccessToken(int userId, string email, string role, string firstName, string lastName);
    
    string GenerateRefreshToken();
}
