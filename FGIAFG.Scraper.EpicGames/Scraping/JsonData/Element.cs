namespace FGIAFG.Scraper.EpicGames.Scraping.JsonData
{
    internal class Element
    {
        public string Title { get; set; }
        public string Id { get; set; }
        public string Namespace { get; set; }
        public string Description { get; set; }
        public List<KeyImage> KeyImages { get; set; }
        public string ProductSlug { get; set; }
        public string UrlSlug { get; set; }
        public string Url { get; set; }
        public CatalogNs CatalogNs { get; set; }
        public Price Price { get; set; }
        public Promotions Promotions { get; set; }
    }
}
