using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketsPerstince.Migrations
{
    /// <inheritdoc />
    public partial class AddProgramSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Program",
                table: "Tickets",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Program",
                table: "Subjects",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Program",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Program",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "Program",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "Program",
                table: "AspNetUsers");
        }
    }
}
