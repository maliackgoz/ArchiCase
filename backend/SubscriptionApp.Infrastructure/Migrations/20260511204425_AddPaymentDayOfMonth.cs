using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SubscriptionApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentDayOfMonth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PaymentDayOfMonth",
                table: "Subscriptions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Backfill existing rows: default the payment day to the billing day.
            migrationBuilder.Sql("UPDATE [Subscriptions] SET [PaymentDayOfMonth] = [BillingDayOfMonth];");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaymentDayOfMonth",
                table: "Subscriptions");
        }
    }
}
