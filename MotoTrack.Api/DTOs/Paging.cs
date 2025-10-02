namespace MotoTrack.Api.DTOs;

public record PageParams(int Page = 1, int Size = 10);
public class PagedResult<T>
{
    public required IEnumerable<T> Items { get; init; }
    public int Page { get; init; }
    public int Size { get; init; }
    public int TotalItems { get; init; }
    public int TotalPages => (int)Math.Ceiling((double)TotalItems / Math.Max(1, Size));
    public List<Link> Links { get; set; } = new();
}

public record Link(string Rel, string Href, string Method);