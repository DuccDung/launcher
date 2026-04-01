using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using app_server.Models;
using app_server.ViewModels.Storefront;

namespace app_server.Services.Storefront;

public static partial class StorefrontViewModelFactory
{
    public static ProductCardViewModel ToProductCard(Game game)
    {
        var displayName = string.IsNullOrWhiteSpace(game.Name) ? "Steam Product" : game.Name.Trim();
        var tag = game.GameCategories
            .Select(item => item.Category?.Name)
            .FirstOrDefault(item => !string.IsNullOrWhiteSpace(item))
            ?? "Steam";
        var discount = CalculateDiscount(game.OldPrice, game.NewPrice);

        return new ProductCardViewModel
        {
            GameId = game.GameId,
            Slug = Slugify(displayName),
            Name = displayName,
            PosterImageUrl = ResolvePosterImage(game),
            Tag = tag,
            NewPriceText = FormatMoney(game.NewPrice, isFree: false),
            OldPriceText = ShouldShowOldPrice(game.OldPrice, game.NewPrice) ? FormatMoney(game.OldPrice, isFree: false) : null,
            DiscountText = discount.HasValue ? $"-{discount.Value}%" : null
        };
    }

    public static ProductDetailViewModel ToProductDetail(
        Game game,
        SteamStoreAppData steamData,
        IReadOnlyList<ProductCardViewModel> relatedProducts)
    {
        var displayName = string.IsNullOrWhiteSpace(steamData.Name) ? game.Name.Trim() : steamData.Name.Trim();
        var versions = BuildEditions(game, displayName);
        var facts = BuildFacts(steamData);
        var gallery = BuildGallery(game, steamData, displayName);
        var requirements = BuildRequirements(steamData);
        var discount = CalculateDiscount(game.OldPrice, game.NewPrice);
        var ratingText = game.Rating?.ToString("0.0", CultureInfo.InvariantCulture) ?? ResolveMetacriticAsFivePointScale(steamData);

        return new ProductDetailViewModel
        {
            GameId = game.GameId,
            Slug = Slugify(displayName),
            SteamAppId = steamData.SteamAppId,
            Name = displayName,
            PageTitle = $"{displayName} | Deluxe Gaming",
            RatingScoreText = ratingText,
            RatingCountText = BuildRatingCountText(steamData),
            NewPriceText = FormatMoney(game.NewPrice, steamData.IsFree),
            OldPriceText = ShouldShowOldPrice(game.OldPrice, game.NewPrice) ? FormatMoney(game.OldPrice, false) : null,
            DiscountText = discount.HasValue ? $"-{discount.Value}%" : null,
            StatusText = steamData.IsFree ? "Miễn phí trên Steam" : "Còn hàng",
            CoverImageUrl = ResolveCoverImage(game, steamData),
            AgeLetter = BuildAgeLetter(steamData),
            AgeCaption = BuildAgeCaption(steamData),
            AgeDescription = BuildAgeDescription(steamData),
            ShortDescription = CoalesceText(steamData.ShortDescription, StripHtml(steamData.AboutTheGame)),
            AboutTheGameHtml = CoalesceHtml(steamData.AboutTheGame, steamData.DetailedDescription),
            DetailedDescriptionHtml = NormalizeDetailedDescription(steamData),
            SteamReviewsHtml = string.IsNullOrWhiteSpace(steamData.Reviews) ? null : steamData.Reviews,
            Website = CoalesceText(steamData.Website, steamData.SupportInfo?.Url),
            SummaryTags = BuildTags(game, steamData),
            Facts = facts,
            Gallery = gallery,
            Editions = versions,
            EditionNote = versions.FirstOrDefault(item => item.IsActive)?.Note ?? string.Empty,
            PackageItems = BuildPackageItems(steamData),
            Requirements = requirements,
            CustomerReviews = BuildCustomerReviews(game),
            RelatedProducts = relatedProducts
        };
    }

    public static string Slugify(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "steam-product";
        }

        var normalized = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var chars = normalized
            .Where(character => CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            .Select(character => character switch
            {
                'đ' => 'd',
                _ => character
            })
            .ToArray();

        var stripped = new string(chars);
        stripped = SlugNonAlphaNumericRegex().Replace(stripped, "-").Trim('-');
        return string.IsNullOrWhiteSpace(stripped) ? "steam-product" : stripped;
    }

    public static string ResolvePosterImage(Game game)
    {
        if (game.SteamAppId is > 0)
        {
            return $"https://shared.fastly.steamstatic.com/store_item_assets/steam/apps/{game.SteamAppId}/library_600x900_2x.jpg";
        }

        var mediaImage = game.MediaItems
            .Select(item => item.Url)
            .FirstOrDefault(IsImageUrl);

        return string.IsNullOrWhiteSpace(mediaImage)
            ? "https://shared.fastly.steamstatic.com/public/shared/images/responsive/share_steam_logo.png"
            : mediaImage;
    }

    private static IReadOnlyList<string> BuildTags(Game game, SteamStoreAppData steamData)
    {
        var tags = new List<string>();

        if (steamData.Genres is not null)
        {
            tags.AddRange(steamData.Genres
                .Select(item => item.Description)
                .Where(static item => !string.IsNullOrWhiteSpace(item))!
                .Cast<string>());
        }

        if (steamData.Categories is not null)
        {
            tags.AddRange(steamData.Categories
                .Select(item => item.Description)
                .Where(static item => !string.IsNullOrWhiteSpace(item))!
                .Cast<string>());
        }

        tags.AddRange(game.GameCategories
            .Select(item => item.Category?.Name)
            .Where(static item => !string.IsNullOrWhiteSpace(item))!
            .Cast<string>());

        return tags
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(8)
            .ToArray();
    }

    private static IReadOnlyList<ProductFactViewModel> BuildFacts(SteamStoreAppData steamData)
    {
        var facts = new List<ProductFactViewModel>();
        var developers = JoinList(steamData.Developers);
        var publishers = JoinList(steamData.Publishers);
        var platforms = BuildPlatformSummary(steamData.Platforms);
        var languages = BuildLanguageSummary(steamData.SupportedLanguages);

        if (!string.IsNullOrWhiteSpace(steamData.ReleaseDate?.Date))
        {
            facts.Add(new ProductFactViewModel { Label = "Phát hành", Value = steamData.ReleaseDate.Date! });
        }

        if (!string.IsNullOrWhiteSpace(developers))
        {
            facts.Add(new ProductFactViewModel { Label = "Phát triển", Value = developers });
        }

        if (!string.IsNullOrWhiteSpace(publishers))
        {
            facts.Add(new ProductFactViewModel { Label = "Phát hành bởi", Value = publishers });
        }

        if (!string.IsNullOrWhiteSpace(platforms))
        {
            facts.Add(new ProductFactViewModel { Label = "Nền tảng", Value = platforms });
        }

        if (!string.IsNullOrWhiteSpace(languages))
        {
            facts.Add(new ProductFactViewModel { Label = "Ngôn ngữ", Value = languages });
        }

        if (steamData.Metacritic?.Score is { } score)
        {
            facts.Add(new ProductFactViewModel { Label = "Metacritic", Value = $"{score}/100" });
        }

        return facts;
    }

    private static IReadOnlyList<ProductGalleryItemViewModel> BuildGallery(Game game, SteamStoreAppData steamData, string displayName)
    {
        var items = new List<ProductGalleryItemViewModel>();

        void AddImage(string? url, string alt)
        {
            if (!IsImageUrl(url))
            {
                return;
            }

            if (items.Any(item => string.Equals(item.FullUrl, url, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            items.Add(new ProductGalleryItemViewModel
            {
                ThumbnailUrl = url!,
                FullUrl = url!,
                Alt = alt
            });
        }

        AddImage(steamData.HeaderImage, displayName);
        AddImage(ResolvePosterImage(game), $"{displayName} cover");
        AddImage(steamData.CapsuleImage, $"{displayName} capsule");
        AddImage(steamData.CapsuleImageV5, $"{displayName} capsule");

        foreach (var screenshot in steamData.Screenshots ?? [])
        {
            AddImage(screenshot.Full ?? screenshot.Thumbnail, $"{displayName} screenshot");
        }

        foreach (var media in game.MediaItems)
        {
            AddImage(media.Url, $"{displayName} media");
        }

        return items.Take(10).ToArray();
    }

    private static IReadOnlyList<ProductEditionViewModel> BuildEditions(Game game, string displayName)
    {
        var editions = game.GameVersions
            .Where(item => !item.IsRemoved)
            .OrderBy(item => item.CreatedAt)
            .Select((item, index) => new ProductEditionViewModel
            {
                Name = string.IsNullOrWhiteSpace(item.VersionName) ? $"STEAM-{index + 1:D2}" : item.VersionName!.Trim().ToUpperInvariant(),
                Note = $"Phiên bản {item.VersionName?.Trim() ?? $"#{index + 1}"} dành cho {displayName}, giữ nguyên dữ liệu mô tả và hình ảnh từ Steam.",
                IsActive = index == 0
            })
            .ToList();

        if (editions.Count > 0)
        {
            return editions;
        }

        return
        [
            new ProductEditionViewModel
            {
                Name = "STEAM-EDITION",
                Note = $"Phiên bản mặc định cho {displayName}, dùng toàn bộ dữ liệu hiển thị đồng bộ từ Steam.",
                IsActive = true
            }
        ];
    }

    private static IReadOnlyList<string> BuildPackageItems(SteamStoreAppData steamData)
    {
        var items = new List<string>();
        var developers = JoinList(steamData.Developers);
        var publishers = JoinList(steamData.Publishers);
        var platforms = BuildPlatformSummary(steamData.Platforms);
        var languages = BuildLanguageSummary(steamData.SupportedLanguages);

        if (!string.IsNullOrWhiteSpace(developers))
        {
            items.Add($"Phát triển bởi {developers}.");
        }

        if (!string.IsNullOrWhiteSpace(publishers))
        {
            items.Add($"Phát hành bởi {publishers}.");
        }

        if (!string.IsNullOrWhiteSpace(platforms))
        {
            items.Add($"Hỗ trợ {platforms}.");
        }

        if (!string.IsNullOrWhiteSpace(steamData.ReleaseDate?.Date))
        {
            items.Add($"Ngày phát hành trên Steam: {steamData.ReleaseDate.Date}.");
        }

        if (!string.IsNullOrWhiteSpace(languages))
        {
            items.Add($"Ngôn ngữ nổi bật: {languages}.");
        }

        if (steamData.Dlc?.Count > 0)
        {
            items.Add($"Steam ghi nhận {steamData.Dlc.Count} nội dung DLC liên quan.");
        }

        if (!string.IsNullOrWhiteSpace(steamData.ContentDescriptors?.Notes))
        {
            items.Add(StripHtml(steamData.ContentDescriptors.Notes));
        }

        if (items.Count == 0)
        {
            items.Add($"Dữ liệu gói sản phẩm được đồng bộ trực tiếp từ Steam App ID {steamData.SteamAppId}.");
        }

        return items.Take(6).ToArray();
    }

    private static IReadOnlyList<ProductRequirementCardViewModel> BuildRequirements(SteamStoreAppData steamData)
    {
        var items = new List<ProductRequirementCardViewModel>();

        void AddRequirement(string platform, SteamStoreRequirements? requirements)
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(requirements?.Minimum))
            {
                parts.Add(requirements.Minimum!);
            }

            if (!string.IsNullOrWhiteSpace(requirements?.Recommended) &&
                !string.Equals(requirements.Recommended, requirements.Minimum, StringComparison.Ordinal))
            {
                parts.Add(requirements.Recommended!);
            }

            if (parts.Count == 0)
            {
                return;
            }

            items.Add(new ProductRequirementCardViewModel
            {
                Platform = platform,
                Html = string.Join("<hr class=\"product-requirement-separator\" />", parts)
            });
        }

        AddRequirement("Windows", steamData.PcRequirements);
        AddRequirement("macOS", steamData.MacRequirements);
        AddRequirement("Linux", steamData.LinuxRequirements);
        return items;
    }

    private static IReadOnlyList<ProductCustomerReviewViewModel> BuildCustomerReviews(Game game)
    {
        return game.Reviews
            .OrderByDescending(item => item.CreatedAt)
            .Take(6)
            .Select(item =>
            {
                var displayName = item.User.Profile?.DisplayName;
                if (string.IsNullOrWhiteSpace(displayName))
                {
                    displayName = item.User.Email.Split('@')[0];
                }

                displayName = string.IsNullOrWhiteSpace(displayName) ? "Người dùng" : displayName.Trim();

                return new ProductCustomerReviewViewModel
                {
                    DisplayName = displayName,
                    AvatarText = displayName[0].ToString().ToUpperInvariant(),
                    RatingText = item.Rating?.ToString("0.0", CultureInfo.InvariantCulture) ?? "—",
                    CreatedAtText = item.CreatedAt.ToLocalTime().ToString("dd/MM/yyyy"),
                    ReviewText = string.IsNullOrWhiteSpace(item.ReviewText) ? "Người dùng chưa để lại nội dung chi tiết." : item.ReviewText.Trim()
                };
            })
            .ToArray();
    }

    private static string ResolveCoverImage(Game game, SteamStoreAppData steamData)
    {
        if (IsImageUrl(steamData.HeaderImage))
        {
            return steamData.HeaderImage!;
        }

        if (IsImageUrl(steamData.CapsuleImage))
        {
            return steamData.CapsuleImage!;
        }

        return ResolvePosterImage(game);
    }

    private static string FormatMoney(decimal? value, bool isFree)
    {
        if (value is > 0)
        {
            return $"{value.Value:N0} đ";
        }

        if (isFree)
        {
            return "Miễn phí";
        }

        if (value == 0)
        {
            return "0 đ";
        }

        return "Liên hệ";
    }

    private static bool ShouldShowOldPrice(decimal? oldPrice, decimal? newPrice)
    {
        return oldPrice is > 0 && newPrice is >= 0 && oldPrice > newPrice;
    }

    private static int? CalculateDiscount(decimal? oldPrice, decimal? newPrice)
    {
        if (oldPrice is null || newPrice is null || oldPrice <= 0 || newPrice < 0 || newPrice >= oldPrice)
        {
            return null;
        }

        return (int)Math.Round((oldPrice.Value - newPrice.Value) / oldPrice.Value * 100M, MidpointRounding.AwayFromZero);
    }

    private static string BuildRatingCountText(SteamStoreAppData steamData)
    {
        if (steamData.Recommendations?.Total > 0)
        {
            return $"{steamData.Recommendations.Total:N0} lượt đề xuất";
        }

        if (steamData.Metacritic?.Score is { } score)
        {
            return $"Metacritic {score}/100";
        }

        return "Chưa có dữ liệu đánh giá";
    }

    private static string ResolveMetacriticAsFivePointScale(SteamStoreAppData steamData)
    {
        if (steamData.Metacritic?.Score is not { } score)
        {
            return "—";
        }

        return (score / 20M).ToString("0.0", CultureInfo.InvariantCulture);
    }

    private static string BuildAgeLetter(SteamStoreAppData steamData)
    {
        if (steamData.RequiredAge > 0)
        {
            return steamData.RequiredAge.ToString(CultureInfo.InvariantCulture);
        }

        var firstRating = steamData.Ratings?.Values.FirstOrDefault(item => !string.IsNullOrWhiteSpace(item.Rating));
        return string.IsNullOrWhiteSpace(firstRating?.Rating) ? "E" : firstRating.Rating!.Trim().ToUpperInvariant();
    }

    private static string BuildAgeCaption(SteamStoreAppData steamData)
    {
        if (steamData.Ratings is null)
        {
            return "Steam";
        }

        foreach (var rating in steamData.Ratings)
        {
            if (!string.IsNullOrWhiteSpace(rating.Value?.Rating))
            {
                return rating.Key.ToUpperInvariant();
            }
        }

        return "Steam";
    }

    private static string BuildAgeDescription(SteamStoreAppData steamData)
    {
        if (!string.IsNullOrWhiteSpace(steamData.ContentDescriptors?.Notes))
        {
            return StripHtml(steamData.ContentDescriptors.Notes);
        }

        if (steamData.RequiredAge > 0)
        {
            return $"Khuyến nghị cho người chơi từ {steamData.RequiredAge} tuổi trở lên.";
        }

        return "Nội dung phù hợp với đa số người chơi theo thông tin Steam.";
    }

    private static string? NormalizeDetailedDescription(SteamStoreAppData steamData)
    {
        if (string.IsNullOrWhiteSpace(steamData.DetailedDescription))
        {
            return null;
        }

        if (string.Equals(
                NormalizeHtmlForCompare(steamData.DetailedDescription),
                NormalizeHtmlForCompare(steamData.AboutTheGame),
                StringComparison.Ordinal))
        {
            return null;
        }

        return steamData.DetailedDescription;
    }

    private static string? CoalesceHtml(params string?[] values)
    {
        return values.FirstOrDefault(item => !string.IsNullOrWhiteSpace(item));
    }

    private static string? CoalesceText(params string?[] values)
    {
        return values
            .Select(item => string.IsNullOrWhiteSpace(item) ? null : item.Trim())
            .FirstOrDefault(item => !string.IsNullOrWhiteSpace(item));
    }

    private static string JoinList(IEnumerable<string>? values)
    {
        return string.Join(", ", values?.Where(item => !string.IsNullOrWhiteSpace(item)).Select(item => item.Trim()) ?? []);
    }

    private static string BuildPlatformSummary(SteamStorePlatforms? platforms)
    {
        if (platforms is null)
        {
            return string.Empty;
        }

        var values = new List<string>();
        if (platforms.Windows)
        {
            values.Add("Windows");
        }

        if (platforms.Mac)
        {
            values.Add("macOS");
        }

        if (platforms.Linux)
        {
            values.Add("Linux");
        }

        return string.Join(", ", values);
    }

    private static string BuildLanguageSummary(string? supportedLanguages)
    {
        var text = StripHtml(supportedLanguages);
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var values = text
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Take(6)
            .ToList();

        return string.Join(", ", values);
    }

    private static string StripHtml(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var withoutBreaks = HtmlBreakRegex().Replace(value, ", ");
        var withoutTags = HtmlTagRegex().Replace(withoutBreaks, " ");
        return WebUtility.HtmlDecode(WhitespaceRegex().Replace(withoutTags, " ")).Trim().Trim(',');
    }

    private static string NormalizeHtmlForCompare(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : WhitespaceRegex().Replace(value, string.Empty);
    }

    private static bool IsImageUrl(string? value)
    {
        return !string.IsNullOrWhiteSpace(value) &&
               ImageUrlRegex().IsMatch(value);
    }

    [GeneratedRegex("<br\\s*/?>", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex HtmlBreakRegex();

    [GeneratedRegex("<.*?>", RegexOptions.Compiled)]
    private static partial Regex HtmlTagRegex();

    [GeneratedRegex("\\s+", RegexOptions.Compiled)]
    private static partial Regex WhitespaceRegex();

    [GeneratedRegex("[^a-z0-9]+", RegexOptions.Compiled)]
    private static partial Regex SlugNonAlphaNumericRegex();

    [GeneratedRegex("\\.(png|jpe?g|webp|gif|bmp|avif)(\\?|#|$)", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex ImageUrlRegex();
}
