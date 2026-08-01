using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GM.Messaging.Sample.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMessageUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                schema: "outbox_messages",
                table: "outbox_messages",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_outbox_messages_user_id",
                schema: "outbox_messages",
                table: "outbox_messages",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_outbox_messages_user_id",
                schema: "outbox_messages",
                table: "outbox_messages");

            migrationBuilder.DropColumn(
                name: "UserId",
                schema: "outbox_messages",
                table: "outbox_messages");
        }
    }
}
