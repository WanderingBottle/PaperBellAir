using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PaperBellStore.Migrations
{
    /// <inheritdoc />
    public partial class Add_Project_Owner_ForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_PmProjects_OwnerId",
                table: "PmProjects",
                column: "OwnerId");

            migrationBuilder.AddForeignKey(
                name: "FK_PmProjects_PmOwners_OwnerId",
                table: "PmProjects",
                column: "OwnerId",
                principalTable: "PmOwners",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PmProjects_PmOwners_OwnerId",
                table: "PmProjects");

            migrationBuilder.DropIndex(
                name: "IX_PmProjects_OwnerId",
                table: "PmProjects");
        }
    }
}
