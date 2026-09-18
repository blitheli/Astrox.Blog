using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Astrox.Blog.Models;

/// <summary>单篇文章阅读次数。</summary>
public class PostViewCount
{
    [Key]
    public int PostId { get; set; }

    public long Count { get; set; }

    [ForeignKey(nameof(PostId))]
    public Post? Post { get; set; }
}
