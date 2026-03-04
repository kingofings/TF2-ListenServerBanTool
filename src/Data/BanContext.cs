using Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Data;

public sealed class BanContext : DbContext
{
    public BanContext(DbContextOptions<BanContext> options) : base(options)
    {
    }

    public DbSet<PlayerEntity> BannedPlayers { get; set; }
}
