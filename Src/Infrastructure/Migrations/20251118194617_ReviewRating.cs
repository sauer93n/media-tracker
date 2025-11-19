using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReviewRating : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Rating",
                table: "Reviews",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "ReferenceType",
                table: "Reviews",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "Dislikes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReviewId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dislikes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Likes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReviewId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Likes", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Dislikes");

            migrationBuilder.DropTable(
                name: "Likes");

            migrationBuilder.DropColumn(
                name: "Rating",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "ReferenceType",
                table: "Reviews");
        }
    }
}
