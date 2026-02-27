using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SquareBuddy.Initializer.Migrations
{
    /// <inheritdoc />
    public partial class Conversations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "srbd_conversations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AgentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParticipantExternalId = table.Column<string>(type: "text", nullable: false),
                    Channel = table.Column<int>(type: "integer", nullable: false),
                    LastMessagePreview = table.Column<string>(type: "text", nullable: true),
                    LastMessageAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastMessageRole = table.Column<int>(type: "integer", nullable: true),
                    UnreadCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("srbd_pk_srbd_conversations", x => x.Id);
                    table.ForeignKey(
                        name: "srbd_fk_srbd_conversations_srbd_agents_agent_id",
                        column: x => x.AgentId,
                        principalTable: "srbd_agents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "srbd_conversation_messages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uuid", nullable: false),
                    AgentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    From = table.Column<string>(type: "text", nullable: false),
                    To = table.Column<string>(type: "text", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    ReadAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("srbd_pk_srbd_conversation_messages", x => x.Id);
                    table.ForeignKey(
                        name: "srbd_fk_srbd_conversation_messages_srbd_agents_agent_id",
                        column: x => x.AgentId,
                        principalTable: "srbd_agents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "srbd_fk_srbd_conversation_messages_srbd_conversations_conversation_id",
                        column: x => x.ConversationId,
                        principalTable: "srbd_conversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "srbd_ix_srbd_conversation_messages_agent_id",
                table: "srbd_conversation_messages",
                column: "AgentId");

            migrationBuilder.CreateIndex(
                name: "srbd_ix_srbd_conversation_messages_conversation_id_is_read_role",
                table: "srbd_conversation_messages",
                columns: new[] { "ConversationId", "IsRead", "Role" });

            migrationBuilder.CreateIndex(
                name: "srbd_ix_srbd_conversation_messages_conversation_id_occurred_at_id",
                table: "srbd_conversation_messages",
                columns: new[] { "ConversationId", "OccurredAt", "Id" },
                descending: new[] { false, true, true });

            migrationBuilder.CreateIndex(
                name: "srbd_ix_srbd_conversations_agent_id_last_message_at_id",
                table: "srbd_conversations",
                columns: new[] { "AgentId", "LastMessageAt", "Id" },
                descending: new[] { false, true, true });

            migrationBuilder.CreateIndex(
                name: "srbd_ix_srbd_conversations_agent_id_participant_external_id_channel",
                table: "srbd_conversations",
                columns: new[] { "AgentId", "ParticipantExternalId", "Channel" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "srbd_ix_srbd_conversations_agent_id_unread_count",
                table: "srbd_conversations",
                columns: new[] { "AgentId", "UnreadCount" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "srbd_conversation_messages");

            migrationBuilder.DropTable(
                name: "srbd_conversations");
        }
    }
}
