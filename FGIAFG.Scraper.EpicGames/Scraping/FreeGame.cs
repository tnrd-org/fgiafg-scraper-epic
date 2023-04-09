using System.Security.Cryptography;
using System.Text;

namespace FGIAFG.Scraper.EpicGames.Scraping;

public class FreeGame
{
    public FreeGame(string title, string imageUrl, string url, DateTime startDate, DateTime endDate)
    {
        Title = title;
        ImageUrl = imageUrl;
        Url = url;
        StartDate = startDate;
        EndDate = endDate;
    }

    public string Title { get; }
    public string ImageUrl { get; }
    public string Url { get; }
    public DateTime StartDate { get; }
    public DateTime EndDate { get; }

    public string CalculatePersistentHash()
    {
        string input = $"{Title}{ImageUrl}{Url}{StartDate}{EndDate}";
        byte[] hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));

        StringBuilder sb = new StringBuilder();
        foreach (byte b in hashBytes)
        {
            sb.Append(b.ToString("x2"));
        }

        return sb.ToString();
    }
}
