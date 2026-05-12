using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SubscriptionApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProviderDeadlineAndAutoPay : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsAutoPay",
                table: "Subscriptions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "LastPaymentDayOfMonth",
                table: "Subscriptions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Backfill: last payment day = billing + 7, capped at 28. Clamp existing payment day into the new window.
            migrationBuilder.Sql(@"
                UPDATE [Subscriptions]
                SET [LastPaymentDayOfMonth] =
                    CASE WHEN [BillingDayOfMonth] + 7 > 28 THEN 28 ELSE [BillingDayOfMonth] + 7 END;

                UPDATE [Subscriptions]
                SET [PaymentDayOfMonth] = [LastPaymentDayOfMonth]
                WHERE [PaymentDayOfMonth] > [LastPaymentDayOfMonth];

                UPDATE [Subscriptions]
                SET [PaymentDayOfMonth] = [BillingDayOfMonth]
                WHERE [PaymentDayOfMonth] < [BillingDayOfMonth];
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsAutoPay",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "LastPaymentDayOfMonth",
                table: "Subscriptions");
        }
    }
}
