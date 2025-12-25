namespace EcommerceAPI.Entities.Concrete;

public class RefreshToken : BaseEntity
{
    public int UserId { get; set; }
    public string Token { get; set; } = string.Empty;
    public string JwtId { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }
    public bool IsUsed { get; set; }
    public string? ReplacedByToken { get; set; }
    public string? RevokedReason { get; set; }
    
    public User User { get; set; } = null!;
}
