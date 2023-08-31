using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SPO.ColdStorage.Entities.Migrations
{
    public partial class SiteFilterConfig : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "filter_config_json",
                table: "target_migration_sites",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "filter_config_json",
                table: "target_migration_sites");
        }
    }
}
