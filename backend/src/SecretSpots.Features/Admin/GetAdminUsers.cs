using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using SecretSpots.Features.Common.Configuration;
using SecretSpots.Features.Common.Localization;
using SecretSpots.Features.Common.Mediator;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Common.Results;
using SecretSpots.Features.Common.Validation;

namespace SecretSpots.Features.Admin;

public static class GetAdminUsers
{
    public record Query(string? SearchTerm, int Page, int PageSize) : IRequest<PagedResponse<AdminUserResponse>>;

    public class Validator : AbstractValidator<Query>
    {
        public Validator(IStringLocalizer<SharedResources> localizer, IOptions<AdminUsersOptions> adminUsersOptions)
        {
            this.ValidatePagination(
                q => q.Page,
                q => q.PageSize,
                adminUsersOptions.Value.MaxPageSize,
                localizer[AdminMessageKeys.PageOutOfRange].Value,
                localizer[AdminMessageKeys.PageSizeOutOfRange].Value);
        }
    }

    public class Handler(IAppDbContext db) : IRequestHandler<Query, PagedResponse<AdminUserResponse>>
    {
        public async Task<PagedResponse<AdminUserResponse>> Handle(Query query, CancellationToken cancellationToken)
        {
            var baseQuery = db.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.SearchTerm))
            {
                var term = query.SearchTerm.Trim();
                baseQuery = baseQuery.Where(u =>
                    EF.Functions.ILike(u.Email, $"%{term}%") ||
                    EF.Functions.ILike(u.DisplayName, $"%{term}%"));
            }

            return await baseQuery
                .OrderBy(u => u.Email)
                .Select(u => new AdminUserResponse(u.Id, u.Email, u.DisplayName, u.Role))
                .ToPagedResponseAsync(query.Page, query.PageSize, cancellationToken);
        }
    }
}
