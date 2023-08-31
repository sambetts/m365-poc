using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SPO.ColdStorage.Entities.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "sites",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    url = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sites", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "target_migration_sites",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    root_url = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_target_migration_sites", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "webs",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    site_id = table.Column<int>(type: "int", nullable: false),
                    url = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_webs", x => x.id);
                    table.ForeignKey(
                        name: "FK_webs_sites_site_id",
                        column: x => x.site_id,
                        principalTable: "sites",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "files",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    web_id = table.Column<int>(type: "int", nullable: false),
                    last_modified = table.Column<DateTime>(type: "datetime2", nullable: false),
                    last_modified_by = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    url = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_files", x => x.id);
                    table.ForeignKey(
                        name: "FK_files_webs_web_id",
                        column: x => x.web_id,
                        principalTable: "webs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "file_migration_errors",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    error = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    file_id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_file_migration_errors", x => x.id);
                    table.ForeignKey(
                        name: "FK_file_migration_errors_files_file_id",
                        column: x => x.file_id,
                        principalTable: "files",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "file_migrations_completed",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    migrated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    file_id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_file_migrations_completed", x => x.id);
                    table.ForeignKey(
                        name: "FK_file_migrations_completed_files_file_id",
                        column: x => x.file_id,
                        principalTable: "files",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_file_migration_errors_file_id",
                table: "file_migration_errors",
                column: "file_id");

            migrationBuilder.CreateIndex(
                name: "IX_file_migrations_completed_file_id",
                table: "file_migrations_completed",
                column: "file_id");

            migrationBuilder.CreateIndex(
                name: "IX_files_web_id",
                table: "files",
                column: "web_id");

            migrationBuilder.CreateIndex(
                name: "IX_webs_site_id",
                table: "webs",
                column: "site_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "file_migration_errors");

            migrationBuilder.DropTable(
                name: "file_migrations_completed");

            migrationBuilder.DropTable(
                name: "target_migration_sites");

            migrationBuilder.DropTable(
                name: "files");

            migrationBuilder.DropTable(
                name: "webs");

            migrationBuilder.DropTable(
                name: "sites");
        }
    }
}
