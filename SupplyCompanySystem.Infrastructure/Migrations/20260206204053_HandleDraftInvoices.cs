using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SupplyCompanySystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class HandleDraftInvoices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedDate",
                table: "Invoices",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompletedDate",
                table: "Invoices");
        }
    }
}
