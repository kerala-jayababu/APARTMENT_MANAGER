using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Apartment_API.Models;

[Table("NotificationSettings", Schema = "dbo")]
public sealed class NotificationSetting
{
    [Key, Column("IdNotificationSetting")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdNotificationSetting { get; set; }

    public int ApartmentId { get; set; }

    [Required, MaxLength(50)]
    public string EventCode { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string EventName { get; set; } = string.Empty;

    public bool IsSmsEnabled { get; set; }
    public bool IsEmailEnabled { get; set; }
    public bool IsAppPushEnabled { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
}
