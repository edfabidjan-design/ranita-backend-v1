public record ReviewCreateDto(byte Rating, string? Title, string? Comment);
public record ReviewUpdateDto(byte Rating, string? Title, string? Comment);

public record ReviewItemDto(
    int Id,
    int CustomerId,
    string CustomerName,
    byte Rating,
    string? Title,
    string? Comment,
    bool VerifiedPurchase,
    DateTime CreatedAtUtc
);

public record ReviewSummaryDto(
    decimal Avg,
    int Count,
    int Star1,
    int Star2,
    int Star3,
    int Star4,
    int Star5
);