using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace app_server.Models;

[Table("user_roles")]
[Index(nameof(UserId), Name = "IX_user_roles_user_id")]
[Index(nameof(RoleId), Name = "IX_user_roles_role_id")]
[Index(nameof(UserId), nameof(RoleId), Name = "UQ_user_roles_user_id_role_id", IsUnique = true)]
public partial class UserRole
{
    [Key]
    [Column("user_role_id")]
    public Guid UserRoleId { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("role_id")]
    public Guid RoleId { get; set; }

    [Column("created_at", TypeName = "datetime2(0)")]
    public DateTime CreatedAt { get; set; }

    [ForeignKey(nameof(UserId))]
    [InverseProperty(nameof(User.UserRoles))]
    public virtual User User { get; set; } = null!;

    [ForeignKey(nameof(RoleId))]
    [InverseProperty(nameof(Role.UserRoles))]
    public virtual Role Role { get; set; } = null!;
}
