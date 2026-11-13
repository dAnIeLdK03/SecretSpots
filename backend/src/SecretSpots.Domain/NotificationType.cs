namespace SecretSpots.Domain;

// Stored as int in the DB, so every member carries an explicit value that must never change —
// new ones get the next free number. 0 belonged to a type that no longer exists and stays unused.
public enum NotificationType
{
    NewSpotNearby = 1,
    NewCommentOnYourSpot = 2,
    NewRatingOnYourSpot = 3,
    YourContentRemoved = 4,
    ReportSubmitted = 5,
    NewVisitorOnYourSpot = 6,
    SpotSubmittedForReview = 7,
    SpotChangeRequestSubmitted = 8,
    SpotApproved = 9,
    SpotRejected = 10,
    SpotRemoved = 11,
    SpotRestored = 12,
    SpotEditApproved = 13,
    SpotEditRejected = 14,
    SpotDeletionApproved = 15,
    SpotDeletionRejected = 16,
    CheckInPhotoAddedToSpot = 17,
    RejectedSpotDeleted = 18,
}
