using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Analytics.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialAnalytics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "analytics");

            migrationBuilder.CreateTable(
                name: "analytic_snapshots",
                schema: "analytics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SnapshotDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Period = table.Column<string>(type: "text", nullable: false),
                    TotalIncome = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalExpense = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    NetBalance = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    SpendingByCategory = table.Column<string>(type: "jsonb", nullable: false),
                    IncomeByCategory = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_analytic_snapshots", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_analytic_snapshots_UserId_SnapshotDate_Period",
                schema: "analytics",
                table: "analytic_snapshots",
                columns: new[] { "UserId", "SnapshotDate", "Period" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "analytic_snapshots",
                schema: "analytics");
        }
    }
}
