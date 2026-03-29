using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace app_server.Models;

[Table("roles")]
[Index(nameof(RoleName), Name = "UQ_roles_role_name", IsUnique = true)]
[Index(nameof(RoleCode), Name = "UQ_roles_role_code", IsUnique = true)]
public partial class Role
{
    [Key]
    [Column("role_id")]
    public Guid RoleId { get; set; }

    [Column("role_name")]
    [StringLength(100)]
    public string RoleName { get; set; } = null!;

    [Column("role_code")]
    [StringLength(50)]
    public string RoleCode { get; set; } = null!;

    [Column("description")]
    [StringLength(255)]
    public string? Description { get; set; }

    [Column("created_at", TypeName = "datetime2(0)")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at", TypeName = "datetime2(0)")]
    public DateTime UpdatedAt { get; set; }

    [InverseProperty(nameof(UserRole.Role))]
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
