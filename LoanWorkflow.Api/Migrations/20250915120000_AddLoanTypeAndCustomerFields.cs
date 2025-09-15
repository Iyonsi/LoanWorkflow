using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LoanWorkflow.Api.Migrations
{
    public partial class AddLoanTypeAndCustomerFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add new columns
            migrationBuilder.AddColumn<string>(
                name: "LoanType",
                table: "LoanRequest",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FullName",
                table: "LoanRequest",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsEligible",
                table: "LoanRequest",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // Data migration from old FlowType if it exists
            // Map 1->standard, 2->multi_stage, 3->flex_review
            migrationBuilder.Sql(@"UPDATE LoanRequest SET LoanType = CASE FlowType WHEN 1 THEN 'standard' WHEN 2 THEN 'multi_stage' WHEN 3 THEN 'flex_review' ELSE 'standard' END WHERE (LoanType = '' OR LoanType IS NULL)");

            // Drop old FlowType column if present
            try
            {
                migrationBuilder.DropColumn(
                    name: "FlowType",
                    table: "LoanRequest");
            }
            catch { /* ignore if already removed */ }
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Recreate FlowType (int) with default 1 if rollback needed
            migrationBuilder.AddColumn<int>(
                name: "FlowType",
                table: "LoanRequest",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.Sql(@"UPDATE LoanRequest SET FlowType = CASE LoanType WHEN 'standard' THEN 1 WHEN 'multi_stage' THEN 2 WHEN 'flex_review' THEN 3 ELSE 1 END");

            migrationBuilder.DropColumn(
                name: "LoanType",
                table: "LoanRequest");
            migrationBuilder.DropColumn(
                name: "FullName",
                table: "LoanRequest");
            migrationBuilder.DropColumn(
                name: "IsEligible",
                table: "LoanRequest");
        }
    }
}
