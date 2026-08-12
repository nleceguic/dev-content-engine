using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DevContentEngine.Infrastructure.Persistence;

public sealed class DevContentEngineDbContextFactory : IDesignTimeDbContextFactory<DevContentEngineDbContext>
{
    public DevContentEngineDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Default")
            ?? "Host=localhost;Database=devcontentengine;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<DevContentEngineDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new DevContentEngineDbContext(optionsBuilder.Options);
    }
}
