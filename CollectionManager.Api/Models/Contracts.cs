namespace CollectionManager.Api;

public sealed record LoginRequest(string Email, string Password);
public sealed record RegisterRequest(string Email, string DisplayName, string Password);
public sealed record AuthResponse(string Token, string Role, string DisplayName, DateTime ExpiresAt);
public sealed record ItemInput(string Name, int CategoryId, string? Description, string? Condition, string? ImageUrl, string? DeliveryAddress, string? Phone);
public sealed record ItemDto(int Id, string Name, string Category, string? Description, string? Condition, string? ImageUrl, string Status, string OwnerName, int OwnerId, DateTime CreatedAt, DateTime? AuctionEndsAt, decimal? CurrentBid, decimal? MinimumNextBid, string? ContactPhone, string? DeliveryAddress, string? OwnerAvatarUrl);
public sealed record ProfileInput(string DisplayName, string? Bio, string? AvatarUrl, string? Location, string? Website);
public sealed record ProfileDto(int Id, string DisplayName, string Role, string? Bio, string? AvatarUrl, string? Location, string? Website, DateTime MemberSince);
public sealed record ApprovalInput(bool Approve);
public sealed record BidInput(decimal Amount);
public sealed record TradeInput(int WantedItemId, int OfferedItemId, string? Note);
public sealed record TradeDto(int Id, int WantedItemId, int OfferedItemId, int SenderId, string Status, string? Note, DateTime CreatedAt);
public sealed record CategoryInput(string Name);
public sealed record MessageInput(int RecipientId, int? ItemId, string Text);
public sealed record MessageDto(int Id, int SenderId, int RecipientId, int? ItemId, string Text, DateTime SentAt);
public sealed record NotificationDto(int Id, string Type, string Title, string Body, DateTime CreatedAt, bool IsRead, int? ItemId, int? ActorId, string? ActorName);

public sealed class DomainException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}

