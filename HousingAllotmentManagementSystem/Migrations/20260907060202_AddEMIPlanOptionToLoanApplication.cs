using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HousingAllotmentManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddEMIPlanOptionToLoanApplication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EMIPlanOptionId",
                schema: "AITStudent",
                table: "LoanApplications",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_LoanApplications_EMIPlanOptionId",
                schema: "AITStudent",
                table: "LoanApplications",
                column: "EMIPlanOptionId");

            migrationBuilder.AddForeignKey(
                name: "FK_LoanApplications_EMIPlanOptions_EMIPlanOptionId",
                schema: "AITStudent",
                table: "LoanApplications",
                column: "EMIPlanOptionId",
                principalSchema: "AITStudent",
                principalTable: "EMIPlanOptions",
                principalColumn: "EMIPlanOptionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LoanApplications_EMIPlanOptions_EMIPlanOptionId",
                schema: "AITStudent",
                table: "LoanApplications");

            migrationBuilder.DropIndex(
                name: "IX_LoanApplications_EMIPlanOptionId",
                schema: "AITStudent",
                table: "LoanApplications");

            migrationBuilder.DropColumn(
                name: "EMIPlanOptionId",
                schema: "AITStudent",
                table: "LoanApplications");
        }
    }
}
