using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GM.Messaging.Sample.ConsumerA.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CustomerActivitiesAndGMMessaging3 : Migration
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
                name: "customer_activities",
                schema: "application",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    kind = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    occurred_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_customer_activities", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_customer_activities_customer_id",
                schema: "application",
                table: "customer_activities",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_customer_activities_event_id",
                schema: "application",
                table: "customer_activities",
                column: "event_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "customer_activities",
                schema: "application");

            migrationBuilder.DropColumn(
                name: "event_version",
                schema: "messaging",
                table: "inbox_messages");
        }
    }
}
