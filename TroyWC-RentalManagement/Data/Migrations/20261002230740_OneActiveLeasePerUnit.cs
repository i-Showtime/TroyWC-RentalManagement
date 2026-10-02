using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TroyWC_RentalManagement.Data.Migrations
{
    /// <inheritdoc />
    public partial class OneActiveLeasePerUnit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Leases_UnitId_Active",
                table: "Leases",
                column: "UnitId",
                unique: true,
                filter: "[Status] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Leases_UnitId_Active",
                table: "Leases");
        }
    }
}
