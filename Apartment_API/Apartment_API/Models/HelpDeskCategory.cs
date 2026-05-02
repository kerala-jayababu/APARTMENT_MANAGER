using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Apartment_API.Models;

[Table("HelpDeskCategories", Schema = "dbo")]
public sealed class HelpDeskCategory
{
    [Key, Column("IdHelpDeskCategory")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdHelpDeskCategory { get; set; }

    public int ApartmentId { get; set; }

    [Required, MaxLength(50)]
    public string HelpDeskCategoryName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}
