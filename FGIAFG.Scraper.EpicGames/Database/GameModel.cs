namespace FGIAFG.Scraper.EpicGames.Database;

internal class GameModel
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public string Url { get; set; } = null!;
    public string ImageUrl { get; set; } = null!;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Hash { get; set; } = null!;
}
