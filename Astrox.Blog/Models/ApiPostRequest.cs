using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Astrox.Blog.Models;

public class ApiPostRequest
{
    [Required, MaxLength(200)]
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [MaxLength(220)]
    [JsonPropertyName("slug")]
    public string? Slug { get; set; }

    [Required]
    [JsonPropertyName("markdown")]
    public string Markdown { get; set; } = string.Empty;

    [MaxLength(500)]
    [JsonPropertyName("summary")]
    public string? Summary { get; set; }

    /// <summary>Array of tags or a single comma-separated string.</summary>
    [JsonPropertyName("tags")]
    public object? Tags { get; set; }

    [JsonPropertyName("publish")]
    public bool Publish { get; set; }

    public string? NormalizedTags()
    {
        if (Tags is null) return null;
        if (Tags is System.Text.Json.JsonElement el)
        {
            if (el.ValueKind == System.Text.Json.JsonValueKind.String)
                return NormalizeTagString(el.GetString());
            if (el.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                var parts = el.EnumerateArray()
                    .Where(x => x.ValueKind == System.Text.Json.JsonValueKind.String)
                    .Select(x => x.GetString()?.Trim())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Cast<string>();
                return string.Join(',', parts);
            }
        }
        if (Tags is string s) return NormalizeTagString(s);
        if (Tags is IEnumerable<object> list)
            return string.Join(',', list.Select(x => x?.ToString()?.Trim()).Where(x => !string.IsNullOrWhiteSpace(x)));
        return null;
    }

    private static string? NormalizeTagString(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var parts = raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length == 0 ? null : string.Join(',', parts);
    }
}
