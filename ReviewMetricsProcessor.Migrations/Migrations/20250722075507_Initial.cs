using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReviewMetricsProcessor.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "authors",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    total_lines_of_code_reviewed = table.Column<double>(type: "double precision", nullable: false),
                    lines_of_code_reviewed_per_hour = table.Column<double>(type: "double precision", nullable: false),
                    average_review_duration = table.Column<double>(type: "double precision", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_authors", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "reviews",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    started_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    completed_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    lines_of_code = table.Column<int>(type: "integer", nullable: true),
                    author_id = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reviews", x => x.id);
                    table.ForeignKey(
                        name: "fk_reviews_authors_author_id",
                        column: x => x.author_id,
                        principalTable: "authors",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "ix_reviews_author_id",
                table: "reviews",
                column: "author_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "reviews");

            migrationBuilder.DropTable(
                name: "authors");
        }
    }
}
