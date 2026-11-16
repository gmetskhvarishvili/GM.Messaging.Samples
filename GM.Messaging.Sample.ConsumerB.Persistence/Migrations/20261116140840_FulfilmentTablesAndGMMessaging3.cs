using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GM.Messaging.Sample.ConsumerB.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FulfilmentTablesAndGMMessaging3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "application");

            migrationBuilder.AddColumn<int>(
                name: "event_version",
                schema: "messaging",
                table: "inbox_messages",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "order_timeline",
                schema: "application",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    raised_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    recorded_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_order_timeline", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "stock_levels",
                schema: "application",
                columns: table => new
                {
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    warehouse_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stock_levels", x => new { x.product_id, x.warehouse_code });
                });

            migrationBuilder.CreateTable(
                name: "welcome_emails",
                schema: "application",
                columns: table => new
                {
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    sent_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_welcome_emails", x => x.event_id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_order_timeline_event_id",
                schema: "application",
                table: "order_timeline",
                column: "event_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_order_timeline_order_id_recorded_at_utc",
                schema: "application",
                table: "order_timeline",
                columns: new[] { "order_id", "recorded_at_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "order_timeline",
                schema: "application");

            migrationBuilder.DropTable(
                name: "stock_levels",
                schema: "application");

            migrationBuilder.DropTable(
                name: "welcome_emails",
                schema: "application");

            migrationBuilder.DropColumn(
                name: "event_version",
                schema: "messaging",
                table: "inbox_messages");
        }
    }
}
