using System.ComponentModel.DataAnnotations;

namespace Astrox.Blog.Models;

/// <summary>站点级键值统计（如总 PV）。</summary>
public class SiteStat
{
    [Key, MaxLength(64)]
    public string Key { get; set; } = string.Empty;

    public long Value { get; set; }
}
