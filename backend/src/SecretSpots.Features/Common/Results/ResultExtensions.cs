using Microsoft.AspNetCore.Http;

namespace SecretSpots.Features.Common.Results;

public static class ResultExtensions
{
    // Only maps the failure branch — the right success status (200/201/204/...) depends
    // on the endpoint, not the result, so callers handle IsSuccess themselves.
    public static IResult ToProblem<TValue>(this Result<TValue> result)
    {
        if (result.IsSuccess)
        {
            throw new InvalidOperationException(ResultMessages.CannotConvertSuccessToProblem);
        }

        return Microsoft.AspNetCore.Http.Results.Problem(
            detail: result.Error.Message,
            statusCode: result.Error.StatusCode,
            extensions: new Dictionary<string, object?> { ["code"] = result.Error.Code });
    }

    // Convenience for the common case where success really is a plain 200 OK.
    // Endpoints that need a different success status (201, 204, ...) branch on
    // IsSuccess themselves instead of using this.
    public static IResult ToOkOrProblem<TValue>(this Result<TValue> result) =>
        result.IsSuccess ? Microsoft.AspNetCore.Http.Results.Ok(result.Value) : result.ToProblem();
}
