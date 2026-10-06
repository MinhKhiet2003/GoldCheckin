using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Base.Migrations
{
    /// <inheritdoc />
    public partial class NormalizePendingRegularCancellations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                WITH "PendingCancellation" AS (
                    SELECT cancellation."Id" AS "CancellationId", request."Id" AS "RequestId"
                    FROM "GoldRequest" AS cancellation
                    INNER JOIN "GoldRequest" AS request
                        ON request."Id" = cancellation."ReplacesRequestId"
                    WHERE cancellation."IsCancellation" = TRUE
                        AND cancellation."Status" IN ('manager', 'hr')
                        AND request."Status" IN ('manager', 'hr')
                )
                UPDATE "GoldRequest"
                SET "Status" = 'cancelled', "Version" = "Version" + 1
                WHERE "Id" IN (SELECT "CancellationId" FROM "PendingCancellation")
                    OR "Id" IN (SELECT "RequestId" FROM "PendingCancellation");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
