using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StatementImport.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialStatementImport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "statement_import");

            migrationBuilder.CreateTable(
                name: "StatementUploads",
                schema: "statement_import",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    OriginalFileName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    StoredFilePath = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    StatementMonth = table.Column<int>(type: "integer", nullable: true),
                    StatementYear = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StatementUploads", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ParsedTransactions",
                schema: "statement_import",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StatementUploadId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Amount = table.Column<long>(type: "bigint", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    SuggestedCategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConfirmedCategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    OriginalText = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParsedTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ParsedTransactions_StatementUploads_StatementUploadId",
                        column: x => x.StatementUploadId,
                        principalSchema: "statement_import",
                        principalTable: "StatementUploads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ParsedTransactions_StatementUploadId",
                schema: "statement_import",
                table: "ParsedTransactions",
                column: "StatementUploadId");

            migrationBuilder.CreateIndex(
                name: "IX_StatementUploads_UserId",
                schema: "statement_import",
                table: "StatementUploads",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ParsedTransactions",
                schema: "statement_import");

            migrationBuilder.DropTable(
                name: "StatementUploads",
                schema: "statement_import");
        }
    }
}
