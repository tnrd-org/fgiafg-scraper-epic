using FGIAFG.Scraper.EpicGames.Scraping.JsonData;
using FluentResults;
using Newtonsoft.Json;

namespace FGIAFG.Scraper.EpicGames.Scraping;

internal class EpicGamesScraper
{
    private const string URL =
        "https://store-site-backend-static-ipv4.ak.epicgames.com/freeGamesPromotions?locale=en-US&country=US&allowCountries=US";

    private readonly IHttpClientFactory httpClientFactory;
    private readonly ILogger<EpicGamesScraper> logger;

    public EpicGamesScraper(IHttpClientFactory httpClientFactory, ILogger<EpicGamesScraper> logger)
    {
        this.httpClientFactory = httpClientFactory;
        this.logger = logger;
    }

    public async Task<Result<IEnumerable<FreeGame>>> Scrape(CancellationToken cancellationToken)
    {
        HttpClient httpClient = httpClientFactory.CreateClient("epicgames");

        HttpResponseMessage response = await httpClient.GetAsync(URL, cancellationToken);

        try
        {
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException e)
        {
            return Result.Fail(new ExceptionalError(e));
        }

        DataWrapper? data;

        try
        {
            string json = await response.Content.ReadAsStringAsync(cancellationToken);
            data = JsonConvert.DeserializeObject<DataWrapper>(json);
        }
        catch (JsonSerializationException e)
        {
            return Result.Fail(new ExceptionalError(e));
        }

        if (data == null)
            return Result.Fail(new Error("Data is null"));

        List<Element> elements = GetElements(data);
        elements = GetElementsWithZeroPrice(elements);
        elements = GetElementsWithActivePromotion(elements);
        return Result.Ok(CreateFreeGames(elements));
    }

    private List<Element> GetElements(DataWrapper dataWrapper)
    {
        if (dataWrapper.Data?.Catalog?.SearchStore?.Elements == null)
            return new List<Element>();

        return dataWrapper.Data.Catalog.SearchStore.Elements ?? new List<Element>();
    }

    private List<Element> GetElementsWithZeroPrice(List<Element> elements)
    {
        List<Element> filtered = new List<Element>();

        foreach (Element element in elements)
        {
            if (element.Price?.TotalPrice == null)
                continue;

            if (element.Price.TotalPrice.DiscountPrice == 0)
                filtered.Add(element);
        }

        return filtered;
    }

    private List<Element> GetElementsWithActivePromotion(List<Element> elements)
    {
        List<Element> filtered = new List<Element>();

        foreach (Element element in elements)
        {
            if (element.Promotions?.PromotionalOffers == null)
                continue;

            foreach (PromotionalOffersWrapper wrapper in element.Promotions.PromotionalOffers)
            {
                if (wrapper.PromotionalOffers == null)
                    continue;

                filtered.AddRange(wrapper.PromotionalOffers
                    .Where(IsValidPromotionalOffer)
                    .Select(x => element));
            }
        }

        return filtered;
    }

    private bool IsValidPromotionalOffer(PromotionalOffer promotionalOffer)
    {
        if (promotionalOffer.DiscountSetting.DiscountPercentage != 0)
            return false;

        if (!promotionalOffer.StartDate.HasValue && !promotionalOffer.EndDate.HasValue)
            return false;

        if (promotionalOffer.StartDate.HasValue &&
            promotionalOffer.StartDate.Value > DateTimeOffset.UtcNow)
            return false;

        if (promotionalOffer.EndDate.HasValue &&
            promotionalOffer.EndDate.Value < DateTimeOffset.UtcNow)
            return false;

        return true;
    }

    private IEnumerable<FreeGame> CreateFreeGames(IEnumerable<Element> elements)
    {
        return new List<FreeGame>(elements.Select(CreateFreeGame));
    }

    private FreeGame CreateFreeGame(Element element)
    {
        return new FreeGame(element.Title,
            GetImageUrl(element),
            GetStoreUrl(element),
            GetStartDate(element),
            GetEndDate(element));
    }

    private string GetImageUrl(Element element)
    {
        KeyImage image = element.KeyImages.FirstOrDefault(x => x.Type == KeyImageType.DieselStoreFrontWide);
        if (image != null)
            return image.Url;

        image = element.KeyImages.FirstOrDefault(x => x.Type == KeyImageType.OfferImageWide);
        if (image != null)
            return image.Url;

        return string.Empty;
    }

    private string GetStoreUrl(Element element)
    {
        if (!string.IsNullOrEmpty(element.ProductSlug))
            return $"https://www.epicgames.com/store/en-US/p/{element.ProductSlug}";

        if (element.CatalogNs is { Mappings: not null } && element.CatalogNs.Mappings.Length != 0)
        {
            Mapping? mapping = element.CatalogNs.Mappings.FirstOrDefault(x =>
                !string.IsNullOrEmpty(x.PageSlug) && x.PageType == "productHome");

            if (mapping != null)
            {
                return $"https://www.epicgames.com/store/en-US/p/{mapping.PageSlug}";
            }
        }

        return "https://store.epicgames.com/en-US/free-games";
    }

    private DateTime GetStartDate(Element element)
    {
        PromotionalOffer? promotionalOffer = element.Promotions.PromotionalOffers
            .SelectMany(x => x.PromotionalOffers
                .Where(IsValidPromotionalOffer))
            .FirstOrDefault();

        return promotionalOffer.StartDate.Value.DateTime;
    }

    private DateTime GetEndDate(Element element)
    {
        PromotionalOffer? promotionalOffer = element.Promotions.PromotionalOffers
            .SelectMany(x => x.PromotionalOffers
                .Where(IsValidPromotionalOffer))
            .FirstOrDefault();

        return promotionalOffer.EndDate.Value.DateTime;
    }
}
