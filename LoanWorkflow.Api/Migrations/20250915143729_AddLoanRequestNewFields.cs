using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LoanWorkflow.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddLoanRequestNewFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop obsolete FlowType column if it exists and add new columns
            // Guard against cases where the column might have been manually removed
            migrationBuilder.Sql(@"IF EXISTS(SELECT 1 FROM sys.columns c JOIN sys.tables t ON c.object_id = t.object_id WHERE t.name = 'LoanRequest' AND c.name = 'FlowType')
BEGIN
    ALTER TABLE [LoanRequest] DROP COLUMN [FlowType];
END");

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LoanType",
                table: "LoanRequest");

            migrationBuilder.DropColumn(
                name: "FullName",
                table: "LoanRequest");

            migrationBuilder.DropColumn(
                name: "IsEligible",
                table: "LoanRequest");

            migrationBuilder.AddColumn<int>(
                name: "FlowType",
                table: "LoanRequest",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
