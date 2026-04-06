namespace app_server.ViewModels.Storefront;

public sealed class StorefrontHomeViewModel
{
    public IReadOnlyList<ProductCardViewModel> Games { get; init; } = [];

    public IReadOnlyList<ProductCardViewModel> SpotlightGames { get; init; } = [];

    public IReadOnlyList<StorefrontCategorySectionViewModel> Categories { get; init; } = [];
}

public sealed class ProductCardViewModel
{
    public Guid GameId { get; init; }

    public string Slug { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string? PosterImageUrl { get; init; }

    public string Badge { get; init; } = "Kich hoat Steam ca nhan";

    public string Tag { get; init; } = "Steam";

    public bool IsTrending { get; init; }

    public IReadOnlyList<string> CategoryNames { get; init; } = [];

    public string CurrentPriceText { get; init; } = string.Empty;

    public string? ReferencePriceText { get; init; }

    public string? DiscountText { get; init; }
}

public sealed class StorefrontCategorySectionViewModel
{
    public string Name { get; init; } = string.Empty;

    public string Slug { get; init; } = string.Empty;

    public IReadOnlyList<ProductCardViewModel> Games { get; init; } = [];
}

public sealed class ProductDetailViewModel
{
    public Guid GameId { get; init; }

    public string Slug { get; init; } = string.Empty;

    public int SteamAppId { get; init; }

    public string Name { get; init; } = string.Empty;

    public string PageTitle { get; init; } = string.Empty;

    public string RatingScoreText { get; init; } = "-";

    public string RatingCountText { get; init; } = "Chua co du lieu";

    public string CurrentPriceText { get; init; } = string.Empty;

    public string? ReferencePriceText { get; init; }

    public string? DiscountText { get; init; }

    public string StatusText { get; init; } = "Con hang";

    public string CoverImageUrl { get; init; } = string.Empty;

    public string AgeLetter { get; init; } = "ALL";

    public string AgeCaption { get; init; } = "Steam";

    public string AgeDescription { get; init; } = "Phu hop voi hau het nguoi choi.";

    public string? ShortDescription { get; init; }

    public string? AboutTheGameHtml { get; init; }

    public string? DetailedDescriptionHtml { get; init; }

    public string? SteamReviewsHtml { get; init; }

    public string? Website { get; init; }

    public IReadOnlyList<string> SummaryTags { get; init; } = [];

    public IReadOnlyList<ProductFactViewModel> Facts { get; init; } = [];

    public IReadOnlyList<ProductGalleryItemViewModel> Gallery { get; init; } = [];

    public IReadOnlyList<ProductEditionViewModel> Editions { get; init; } = [];

    public string EditionNote { get; init; } = string.Empty;

    public IReadOnlyList<string> PackageItems { get; init; } = [];

    public IReadOnlyList<ProductRequirementCardViewModel> Requirements { get; init; } = [];

    public IReadOnlyList<ProductCustomerReviewViewModel> CustomerReviews { get; init; } = [];

    public IReadOnlyList<ProductCardViewModel> RelatedProducts { get; init; } = [];
}

public sealed class ProductGalleryItemViewModel
{
    public string ThumbnailUrl { get; init; } = string.Empty;

    public string FullUrl { get; init; } = string.Empty;

    public string Alt { get; init; } = string.Empty;

    public string MainPosition { get; init; } = "center center";
}

public sealed class ProductEditionViewModel
{
    public string Name { get; init; } = string.Empty;

    public string Note { get; init; } = string.Empty;

    public string? PriceText { get; init; }

    public bool IsActive { get; init; }
}

public sealed class ProductRequirementCardViewModel
{
    public string Platform { get; init; } = string.Empty;

    public string Html { get; init; } = string.Empty;
}

public sealed class ProductCustomerReviewViewModel
{
    public string DisplayName { get; init; } = string.Empty;

    public string AvatarText { get; init; } = string.Empty;

    public string RatingText { get; init; } = "-";

    public string CreatedAtText { get; init; } = string.Empty;

    public string? ReviewText { get; init; }
}

public sealed class ProductFactViewModel
{
    public string Label { get; init; } = string.Empty;

    public string Value { get; init; } = string.Empty;
}
