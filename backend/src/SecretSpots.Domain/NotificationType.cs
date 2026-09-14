namespace SecretSpots.Domain;

// Stored as int in the DB — existing members must keep their position/value, so new ones only
// get appended.
public enum NotificationType
{
    CrystalsEarned,
    NewSpotNearby,
    NewCommentOnYourSpot,
    NewRatingOnYourSpot,
    YourContentRemoved,
}
