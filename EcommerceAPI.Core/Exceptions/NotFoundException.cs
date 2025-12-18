namespace EcommerceAPI.Core.Exceptions;

public class NotFoundException : DomainException
{
    public string ResourceType { get; }
    public object? ResourceId { get; }

    public NotFoundException(string resourceType, object? resourceId = null)
        : base($"{resourceType} bulunamadı", "RESOURCE_NOT_FOUND")
    {
        ResourceType = resourceType;
        ResourceId = resourceId;
    }

    public NotFoundException(string resourceType, object resourceId, string message)
        : base(message, "RESOURCE_NOT_FOUND")
    {
        ResourceType = resourceType;
        ResourceId = resourceId;
    }
}
