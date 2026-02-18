using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketsPerstince.Migrations
{
    /// <inheritdoc />
    public partial class AddTermToSubject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Term",
                table: "Subjects",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Term",
                table: "Subjects");
        }
    }
}
