namespace EcommerceAPI.Entities.DTOs;

public sealed class TrackRecommendationClickRequest
{
    public int TargetProductId { get; set; }
    public string Source { get; set; } = string.Empty;
}
