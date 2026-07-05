using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CS2_Admin.Models;

[Table("admin_tags")]
public class AdminTag
{
    [Key]
    public int Id { get; set; }

    [Column("group_name")]
    public string GroupName { get; set; } = string.Empty;

    [Column("tag_text")]
    public string TagText { get; set; } = string.Empty;

    [Column("tag_color")]
    public string TagColor { get; set; } = "[default]";

    [Column("chat_color")]
    public string ChatColor { get; set; } = "[white]";

    [Column("name_color")]
    public string NameColor { get; set; } = "[default]";

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }
}
