using Microsoft.EntityFrameworkCore;

namespace ForgeDotNet.SqlContent;

public static class EfOrdersMigrationsStarter
{
    public static Task<bool> ApplyAsync(MiniErpContext context, CancellationToken cancellationToken = default) =>
        context.Database.EnsureCreatedAsync(cancellationToken);
}
