using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HousingAllotmentManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddEMIPlanOptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EMIPlanOptions",
                schema: "AITStudent",
                columns: table => new
                {
                    EMIPlanOptionId = table.Column<int>(
                        type: "int",
                        nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),

                    SchemeId = table.Column<int>(
                        type: "int",
                        nullable: false),

                    PlanName = table.Column<string>(
                        type: "nvarchar(100)",
                        maxLength: 100,
                        nullable: false),

                    TenureMonths = table.Column<int>(
                        type: "int",
                        nullable: false),

                    InterestRate = table.Column<decimal>(
                        type: "decimal(5,2)",
                        nullable: false),

                    Description = table.Column<string>(
                        type: "nvarchar(500)",
                        maxLength: 500,
                        nullable: true),

                    Status = table.Column<string>(
                        type: "nvarchar(20)",
                        maxLength: 20,
                        nullable: false),

                    CreatedDate = table.Column<DateTime>(
                        type: "datetime",
                        nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_EMIPlanOptions",
                        x => x.EMIPlanOptionId);

                    table.ForeignKey(
                        name: "FK_EMIPlanOptions_HousingSchemes_SchemeId",
                        column: x => x.SchemeId,
                        principalSchema: "AITStudent",
                        principalTable: "HousingSchemes",
                        principalColumn: "SchemeId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EMIPlanOptions_SchemeId",
                schema: "AITStudent",
                table: "EMIPlanOptions",
                column: "SchemeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EMIPlanOptions",
                schema: "AITStudent");
        }
    }
}