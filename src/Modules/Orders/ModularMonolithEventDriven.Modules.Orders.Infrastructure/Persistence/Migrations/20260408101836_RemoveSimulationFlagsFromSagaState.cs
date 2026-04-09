using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ModularMonolithEventDriven.Modules.Orders.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSimulationFlagsFromSagaState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SimulatePaymentFailure",
                schema: "orders",
                table: "OrderSagaState");

            migrationBuilder.DropColumn(
                name: "SimulateStockFailure",
                schema: "orders",
                table: "OrderSagaState");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "SimulatePaymentFailure",
                schema: "orders",
                table: "OrderSagaState",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SimulateStockFailure",
                schema: "orders",
                table: "OrderSagaState",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
