using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReviewRefIdString : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ReferenceId",
                table: "Reviews",
                type: "text",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "ReferenceId",
                table: "Reviews",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");
        }
    }
}
