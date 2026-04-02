namespace app_server.ViewModels.Storefront;

public sealed class StorefrontHomeViewModel
{
    public IReadOnlyList<ProductCardViewModel> Games { get; init; } = [];
}

public sealed class ProductCardViewModel
{
    public Guid GameId { get; init; }

    public string Slug { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string? PosterImageUrl { get; init; }

    public string Badge { get; init; } = "Kích hoạt Steam cá nhân";

    public string Tag { get; init; } = "Steam";

    public string NewPriceText { get; init; } = string.Empty;

    public string? OldPriceText { get; init; }

    public string? DiscountText { get; init; }
}

public sealed class ProductDetailViewModel
{
    public Guid GameId { get; init; }

    public string Slug { get; init; } = string.Empty;

    public int SteamAppId { get; init; }

    public string Name { get; init; } = string.Empty;

    public string PageTitle { get; init; } = string.Empty;

    public string RatingScoreText { get; init; } = "—";

    public string RatingCountText { get; init; } = "Chưa có dữ liệu";

    public string NewPriceText { get; init; } = string.Empty;

    public string? OldPriceText { get; init; }

    public string? DiscountText { get; init; }

    public string StatusText { get; init; } = "Còn hàng";

    public string CoverImageUrl { get; init; } = string.Empty;

    public string AgeLetter { get; init; } = "ALL";

    public string AgeCaption { get; init; } = "Steam";

    public string AgeDescription { get; init; } = "Phù hợp với hầu hết người chơi.";

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

    public ProductArticleViewModel? Article { get; init; }

    public IReadOnlyList<ProductCustomerReviewViewModel> CustomerReviews { get; init; } = [];

    public IReadOnlyList<ProductCardViewModel> RelatedProducts { get; init; } = [];
}

public sealed class ProductArticleViewModel
{
    public string? Eyebrow { get; init; }

    public string Title { get; init; } = string.Empty;

    public string? Summary { get; init; }

    public IReadOnlyList<ProductArticleBlockViewModel> Blocks { get; init; } = [];
}

public sealed class ProductArticleBlockViewModel
{
    public string Type { get; init; } = "paragraph";

    public string? Text { get; init; }

    public string? Title { get; init; }

    public string? Intro { get; init; }

    public IReadOnlyList<string> Items { get; init; } = [];

    public string? Url { get; init; }

    public string? Alt { get; init; }

    public bool IsYoutube { get; init; }

    public string? VideoEmbedUrl { get; init; }
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

    public string RatingText { get; init; } = "—";

    public string CreatedAtText { get; init; } = string.Empty;

    public string? ReviewText { get; init; }
}

public sealed class ProductFactViewModel
{
    public string Label { get; init; } = string.Empty;

    public string Value { get; init; } = string.Empty;
}
