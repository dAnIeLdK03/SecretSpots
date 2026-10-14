using System.Linq.Expressions;
using FluentValidation;
using Microsoft.Extensions.Localization;
using SecretSpots.Features.Common.Localization;

namespace SecretSpots.Features.Rewards;

internal static class RewardValidationExtensions
{
    public static void ValidateRewardFields<T>(
        this AbstractValidator<T> validator,
        Expression<Func<T, string>> titleSelector,
        Expression<Func<T, string>> descriptionSelector,
        Expression<Func<T, int>> crystalCostSelector,
        IStringLocalizer<SharedResources> localizer)
    {
        validator.RuleFor(titleSelector)
            .NotEmpty().WithMessage(localizer[RewardsMessageKeys.TitleRequired].Value)
            .MaximumLength(100).WithMessage(localizer[RewardsMessageKeys.TitleTooLong].Value);

        validator.RuleFor(descriptionSelector)
            .NotEmpty().WithMessage(localizer[RewardsMessageKeys.DescriptionRequired].Value)
            .MaximumLength(2000).WithMessage(localizer[RewardsMessageKeys.DescriptionTooLong].Value);

        validator.RuleFor(crystalCostSelector)
            .GreaterThan(0).WithMessage(localizer[RewardsMessageKeys.CrystalCostOutOfRange].Value);
    }
}
