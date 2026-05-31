using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WarehouseOps.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIncidentContextFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RelatedEntityId",
                table: "Incidents",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "RelatedEntityType",
                table: "Incidents",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "Severity",
                table: "Incidents",
                type: "int",
                nullable: false,
                defaultValue: 2);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RelatedEntityId",
                table: "Incidents");

            migrationBuilder.DropColumn(
                name: "RelatedEntityType",
                table: "Incidents");

            migrationBuilder.DropColumn(
                name: "Severity",
                table: "Incidents");
        }
    }
}
