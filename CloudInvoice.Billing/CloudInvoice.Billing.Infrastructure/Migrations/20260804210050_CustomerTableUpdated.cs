using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloudInvoice.Billing.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CustomerTableUpdated : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TaxNumber",
                table: "Customers",
                newName: "TaxId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TaxId",
                table: "Customers",
                newName: "TaxNumber");
        }
    }
}
