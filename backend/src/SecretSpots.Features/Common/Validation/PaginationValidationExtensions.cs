using System.Linq.Expressions;
using FluentValidation;

namespace SecretSpots.Features.Common.Validation;

public static class PaginationValidationExtensions
{
    public static void ValidatePagination<T>(
        this AbstractValidator<T> validator,
        Expression<Func<T, int>> pageSelector,
        Expression<Func<T, int>> pageSizeSelector,
        int maxPageSize,
        string pageMessage,
        string pageSizeMessage)
    {
        validator.RuleFor(pageSelector)
            .GreaterThanOrEqualTo(1).WithMessage(pageMessage);

        validator.RuleFor(pageSizeSelector)
            .InclusiveBetween(1, maxPageSize).WithMessage(pageSizeMessage);
    }
}
