using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TroyWC_RentalManagement.Data.Migrations
{
    /// <inheritdoc />
    public partial class RentalApplicationFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApplicationApplicant_RentalApplications_ApplicationId",
                table: "ApplicationApplicant");

            migrationBuilder.DropForeignKey(
                name: "FK_Residence_ApplicationApplicant_ApplicationApplicantId",
                table: "Residence");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Residence",
                table: "Residence");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ApplicationApplicant",
                table: "ApplicationApplicant");

            migrationBuilder.RenameTable(
                name: "Residence",
                newName: "Residences");

            migrationBuilder.RenameTable(
                name: "ApplicationApplicant",
                newName: "ApplicationApplicants");

            migrationBuilder.RenameIndex(
                name: "IX_Residence_ApplicationApplicantId",
                table: "Residences",
                newName: "IX_Residences_ApplicationApplicantId");

            migrationBuilder.RenameIndex(
                name: "IX_ApplicationApplicant_ApplicationId",
                table: "ApplicationApplicants",
                newName: "IX_ApplicationApplicants_ApplicationId");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedByUserId",
                table: "RentalApplications",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "AssignedManagerId",
                table: "RentalApplications",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ApplicantInfoCompleted",
                table: "RentalApplications",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ResidenceHistoryCompleted",
                table: "RentalApplications",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "ActorUserId",
                table: "ApplicationHistory",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "AuthorUserId",
                table: "ApplicationComment",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "ApplicationApplicants",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Residences",
                table: "Residences",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ApplicationApplicants",
                table: "ApplicationApplicants",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_RentalApplications_CreatedByUserId",
                table: "RentalApplications",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationApplicants_UserId",
                table: "ApplicationApplicants",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ApplicationApplicants_AspNetUsers_UserId",
                table: "ApplicationApplicants",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ApplicationApplicants_RentalApplications_ApplicationId",
                table: "ApplicationApplicants",
                column: "ApplicationId",
                principalTable: "RentalApplications",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RentalApplications_AspNetUsers_CreatedByUserId",
                table: "RentalApplications",
                column: "CreatedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Residences_ApplicationApplicants_ApplicationApplicantId",
                table: "Residences",
                column: "ApplicationApplicantId",
                principalTable: "ApplicationApplicants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApplicationApplicants_AspNetUsers_UserId",
                table: "ApplicationApplicants");

            migrationBuilder.DropForeignKey(
                name: "FK_ApplicationApplicants_RentalApplications_ApplicationId",
                table: "ApplicationApplicants");

            migrationBuilder.DropForeignKey(
                name: "FK_RentalApplications_AspNetUsers_CreatedByUserId",
                table: "RentalApplications");

            migrationBuilder.DropForeignKey(
                name: "FK_Residences_ApplicationApplicants_ApplicationApplicantId",
                table: "Residences");

            migrationBuilder.DropIndex(
                name: "IX_RentalApplications_CreatedByUserId",
                table: "RentalApplications");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Residences",
                table: "Residences");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ApplicationApplicants",
                table: "ApplicationApplicants");

            migrationBuilder.DropIndex(
                name: "IX_ApplicationApplicants_UserId",
                table: "ApplicationApplicants");

            migrationBuilder.DropColumn(
                name: "ApplicantInfoCompleted",
                table: "RentalApplications");

            migrationBuilder.DropColumn(
                name: "ResidenceHistoryCompleted",
                table: "RentalApplications");

            migrationBuilder.RenameTable(
                name: "Residences",
                newName: "Residence");

            migrationBuilder.RenameTable(
                name: "ApplicationApplicants",
                newName: "ApplicationApplicant");

            migrationBuilder.RenameIndex(
                name: "IX_Residences_ApplicationApplicantId",
                table: "Residence",
                newName: "IX_Residence_ApplicationApplicantId");

            migrationBuilder.RenameIndex(
                name: "IX_ApplicationApplicants_ApplicationId",
                table: "ApplicationApplicant",
                newName: "IX_ApplicationApplicant_ApplicationId");

            migrationBuilder.AlterColumn<int>(
                name: "CreatedByUserId",
                table: "RentalApplications",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450);

            migrationBuilder.AlterColumn<int>(
                name: "AssignedManagerId",
                table: "RentalApplications",
                type: "int",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ActorUserId",
                table: "ApplicationHistory",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450);

            migrationBuilder.AlterColumn<int>(
                name: "AuthorUserId",
                table: "ApplicationComment",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450);

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "ApplicationApplicant",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Residence",
                table: "Residence",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ApplicationApplicant",
                table: "ApplicationApplicant",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ApplicationApplicant_RentalApplications_ApplicationId",
                table: "ApplicationApplicant",
                column: "ApplicationId",
                principalTable: "RentalApplications",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Residence_ApplicationApplicant_ApplicationApplicantId",
                table: "Residence",
                column: "ApplicationApplicantId",
                principalTable: "ApplicationApplicant",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
