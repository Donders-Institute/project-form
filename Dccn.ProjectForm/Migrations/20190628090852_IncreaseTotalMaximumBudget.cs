using Microsoft.EntityFrameworkCore.Migrations;

namespace Dccn.ProjectForm.Migrations
{
    public partial class IncreaseTotalMaximumBudget : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "PaymentMaxTotalCost",
                table: "Proposals",
                type: "DECIMAL(9, 2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "DECIMAL(6, 2)",
                oldNullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "PaymentMaxTotalCost",
                table: "Proposals",
                type: "DECIMAL(6, 2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "DECIMAL(9, 2)",
                oldNullable: true);
        }
    }
}
