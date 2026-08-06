namespace SecretSpots.Features.Ratings;

public record RatingResponse(Guid SpotId, int Value, double AverageRating, int RatingsCount);
