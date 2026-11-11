using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookStore.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Books",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Author = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Isbn = table.Column<string>(type: "TEXT", maxLength: 13, nullable: true),
                    PublishedYear = table.Column<int>(type: "INTEGER", nullable: true),
                    CoverUrl = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    OpenLibraryKey = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    Genre = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    PriceCents = table.Column<long>(type: "INTEGER", nullable: false),
                    Stock = table.Column<int>(type: "INTEGER", nullable: false),
                    AddedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Books", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Books_Genre",
                table: "Books",
                column: "Genre");

            migrationBuilder.CreateIndex(
                name: "IX_Books_Isbn",
                table: "Books",
                column: "Isbn",
                unique: true,
                filter: "\"Isbn\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Books_OpenLibraryKey",
                table: "Books",
                column: "OpenLibraryKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Books");
        }
    }
}
