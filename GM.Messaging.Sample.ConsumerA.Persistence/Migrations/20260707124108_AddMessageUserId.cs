using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GM.Messaging.Sample.ConsumerA.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMessageUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                schema: "inbox_messages",
                table: "inbox_messages",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_inbox_messages_user_id",
                schema: "inbox_messages",
                table: "inbox_messages",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_inbox_messages_user_id",
                schema: "inbox_messages",
                table: "inbox_messages");

            migrationBuilder.DropColumn(
                name: "UserId",
                schema: "inbox_messages",
                table: "inbox_messages");
        }
    }
}
