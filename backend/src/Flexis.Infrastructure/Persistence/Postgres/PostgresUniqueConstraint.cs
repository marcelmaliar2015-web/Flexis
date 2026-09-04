using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Flexis.Infrastructure.Persistence.Postgres;

internal static class PostgresUniqueConstraint
{
    public static bool IsViolation(DbUpdateException exception)
    {
        for (Exception? inner = exception.InnerException; inner is not null; inner = inner.InnerException)
        {
            if (inner is PostgresException postgres
                && postgres.SqlState == PostgresErrorCodes.UniqueViolation)
            {
                return true;
            }
        }

        return false;
    }
}
