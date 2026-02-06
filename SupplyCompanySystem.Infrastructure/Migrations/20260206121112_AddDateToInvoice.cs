using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SupplyCompanySystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDateToInvoice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "Invoices",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETDATE()");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "Invoices");
        }
    }
}
