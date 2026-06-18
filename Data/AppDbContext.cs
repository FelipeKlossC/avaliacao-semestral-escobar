using Microsoft.EntityFrameworkCore;
using SalaReuniaoApi.Models;

namespace SalaReuniaoApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<SalaReuniao> SalasReuniao => Set<SalaReuniao>();
}
