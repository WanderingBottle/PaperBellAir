using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PaperBellStore.Migrations
{
    /// <inheritdoc />
    public partial class Add_Code_To_Owner_And_Project : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "PmProjects",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "PmOwners",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_PmProjects_Code_TenantId",
                table: "PmProjects",
                columns: new[] { "Code", "TenantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PmOwners_Code_TenantId",
                table: "PmOwners",
                columns: new[] { "Code", "TenantId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PmProjects_Code_TenantId",
                table: "PmProjects");

            migrationBuilder.DropIndex(
                name: "IX_PmOwners_Code_TenantId",
                table: "PmOwners");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "PmProjects");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "PmOwners");
        }
    }
}
