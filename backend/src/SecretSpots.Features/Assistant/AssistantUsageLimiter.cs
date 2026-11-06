using Microsoft.EntityFrameworkCore;
using SecretSpots.Domain;
using SecretSpots.Features.Common.Persistence;

namespace SecretSpots.Features.Assistant;

// The per-user and app-wide daily question limits, counted in AssistantUsages.
public static class AssistantUsageLimiter
{
    public enum Outcome
    {
        Allowed,
        UserLimitReached,
        TotalLimitReached,
    }

    // Takes one question from today's allowance, or reports which limit stops it. The per-user count
    // is raised with a conditional UPDATE, so two parallel questions can't both slip past the limit.
    // The app-wide total is a plain sum read first; a burst of parallel questions may overshoot it by
    // a few, which is fine for a cost ceiling.
    public static async Task<Outcome> TryConsumeAsync(
        IAppDbContext db, Guid userId, DateOnly date, int perUserLimit, int totalLimit, CancellationToken cancellationToken)
    {
        var usedToday = await db.AssistantUsages.Where(u => u.Date == date).SumAsync(u => u.QuestionCount, cancellationToken);
        if (usedToday >= totalLimit)
        {
            return Outcome.TotalLimitReached;
        }

        for (var attempt = 0; attempt < 2; attempt++)
        {
            var incremented = await db.AssistantUsages
                .Where(u => u.UserId == userId && u.Date == date && u.QuestionCount < perUserLimit)
                .ExecuteUpdateAsync(s => s.SetProperty(u => u.QuestionCount, u => u.QuestionCount + 1), cancellationToken);
            if (incremented == 1)
            {
                return Outcome.Allowed;
            }

            if (await db.AssistantUsages.AnyAsync(u => u.UserId == userId && u.Date == date, cancellationToken))
            {
                return Outcome.UserLimitReached;
            }

            // First question today. A parallel first question may insert the same row in between —
            // then the key clashes and the next attempt increments that row instead.
            var usage = new AssistantUsage { UserId = userId, Date = date, QuestionCount = 1 };
            db.AssistantUsages.Add(usage);
            try
            {
                await db.SaveChangesAsync(cancellationToken);
                return Outcome.Allowed;
            }
            catch (DbUpdateException)
            {
                db.AssistantUsages.Remove(usage);
            }
        }

        return Outcome.UserLimitReached;
    }

    // Gives the question back when the model failed to answer — the user shouldn't pay for that.
    public static Task RefundAsync(IAppDbContext db, Guid userId, DateOnly date, CancellationToken cancellationToken) =>
        db.AssistantUsages
            .Where(u => u.UserId == userId && u.Date == date && u.QuestionCount > 0)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.QuestionCount, u => u.QuestionCount - 1), cancellationToken);
}
