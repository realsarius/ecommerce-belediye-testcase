namespace EcommerceAPI.Entities.DTOs;

public sealed class TrackCampaignInteractionRequest
{
    public string InteractionType { get; set; } = string.Empty;
    public int? ProductId { get; set; }
}
