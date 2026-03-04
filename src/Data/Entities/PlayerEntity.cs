using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Data.Entities;

public class PlayerEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public string SteamId { get; set; } = null!;

    public string Name { get; set; } = null!;
}
