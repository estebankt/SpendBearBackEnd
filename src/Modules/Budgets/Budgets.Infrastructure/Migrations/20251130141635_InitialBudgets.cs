using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Budgets.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialBudgets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "budgets");

            migrationBuilder.CreateTable(
                name: "Budgets",
                schema: "budgets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Period = table.Column<int>(type: "integer", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    CurrentSpent = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    WarningThreshold = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    IsExceeded = table.Column<bool>(type: "boolean", nullable: false),
                    WarningTriggered = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Budgets", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Budgets_UserId",
                schema: "budgets",
                table: "Budgets",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Budgets_UserId_CategoryId",
                schema: "budgets",
                table: "Budgets",
                columns: new[] { "UserId", "CategoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_Budgets_UserId_StartDate_EndDate",
                schema: "budgets",
                table: "Budgets",
                columns: new[] { "UserId", "StartDate", "EndDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Budgets",
                schema: "budgets");
        }
    }
}
