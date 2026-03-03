using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HeyAlan.Initializer.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "srbd_asp_net_roles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("srbd_pk_srbd_asp_net_roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "srbd_asp_net_users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayName = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("srbd_pk_srbd_asp_net_users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "srbd_data_protection_keys",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FriendlyName = table.Column<string>(type: "text", nullable: true),
                    Xml = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("srbd_pk_srbd_data_protection_keys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "srbd_subscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StripeCustomerId = table.Column<string>(type: "text", nullable: true),
                    StripeSubscriptionId = table.Column<string>(type: "text", nullable: true),
                    SubscriptionCreditBalance = table.Column<int>(type: "integer", nullable: false),
                    TopUpCreditBalance = table.Column<int>(type: "integer", nullable: false),
                    StripePriceId = table.Column<string>(type: "text", nullable: true),
                    CurrentPeriodEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("srbd_pk_srbd_subscriptions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "srbd_asp_net_role_claims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("srbd_pk_srbd_asp_net_role_claims", x => x.Id);
                    table.ForeignKey(
                        name: "srbd_fk_srbd_asp_net_role_claims_srbd_asp_net_roles_role_id",
                        column: x => x.RoleId,
                        principalTable: "srbd_asp_net_roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "srbd_asp_net_user_claims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("srbd_pk_srbd_asp_net_user_claims", x => x.Id);
                    table.ForeignKey(
                        name: "srbd_fk_srbd_asp_net_user_claims_srbd_asp_net_users_user_id",
                        column: x => x.UserId,
                        principalTable: "srbd_asp_net_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "srbd_asp_net_user_logins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    ProviderKey = table.Column<string>(type: "text", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("srbd_pk_srbd_asp_net_user_logins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "srbd_fk_srbd_asp_net_user_logins_srbd_asp_net_users_user_id",
                        column: x => x.UserId,
                        principalTable: "srbd_asp_net_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "srbd_asp_net_user_roles",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("srbd_pk_srbd_asp_net_user_roles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "srbd_fk_srbd_asp_net_user_roles_srbd_asp_net_roles_role_id",
                        column: x => x.RoleId,
                        principalTable: "srbd_asp_net_roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "srbd_fk_srbd_asp_net_user_roles_srbd_asp_net_users_user_id",
                        column: x => x.UserId,
                        principalTable: "srbd_asp_net_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "srbd_asp_net_user_tokens",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("srbd_pk_srbd_asp_net_user_tokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "srbd_fk_srbd_asp_net_user_tokens_srbd_asp_net_users_user_id",
                        column: x => x.UserId,
                        principalTable: "srbd_asp_net_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "srbd_agents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubscriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Personality = table.Column<int>(type: "integer", nullable: true),
                    PersonalityPromptRaw = table.Column<string>(type: "text", nullable: true),
                    PersonalityPromptSanitized = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TwilioPhoneNumber = table.Column<string>(type: "text", nullable: true),
                    TelegramBotToken = table.Column<string>(type: "text", nullable: true),
                    WhatsappNumber = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("srbd_pk_srbd_agents", x => x.Id);
                    table.ForeignKey(
                        name: "srbd_fk_srbd_agents_subscriptions_subscription_id",
                        column: x => x.SubscriptionId,
                        principalTable: "srbd_subscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "srbd_credit_transactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubscriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<int>(type: "integer", nullable: false),
                    Source = table.Column<int>(type: "integer", nullable: false),
                    StripeEventId = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("srbd_pk_srbd_credit_transactions", x => x.Id);
                    table.ForeignKey(
                        name: "srbd_fk_srbd_credit_transactions_subscriptions_subscription_id",
                        column: x => x.SubscriptionId,
                        principalTable: "srbd_subscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "srbd_subscription_square_connections",
                columns: table => new
                {
                    SubscriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SquareMerchantId = table.Column<string>(type: "text", nullable: false),
                    EncryptedAccessToken = table.Column<string>(type: "text", nullable: false),
                    EncryptedRefreshToken = table.Column<string>(type: "text", nullable: false),
                    AccessTokenExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Scopes = table.Column<string>(type: "text", nullable: false),
                    ConnectedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DisconnectedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("srbd_pk_srbd_subscription_square_connections", x => x.SubscriptionId);
                    table.ForeignKey(
                        name: "srbd_fk_srbd_subscription_square_connections_srbd_asp_net_users_con~",
                        column: x => x.ConnectedByUserId,
                        principalTable: "srbd_asp_net_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "srbd_fk_srbd_subscription_square_connections_srbd_subscriptions_sub~",
                        column: x => x.SubscriptionId,
                        principalTable: "srbd_subscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "srbd_subscription_users",
                columns: table => new
                {
                    SubscriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("srbd_pk_srbd_subscription_users", x => new { x.SubscriptionId, x.UserId });
                    table.ForeignKey(
                        name: "srbd_fk_srbd_subscription_users_srbd_asp_net_users_user_id",
                        column: x => x.UserId,
                        principalTable: "srbd_asp_net_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "srbd_fk_srbd_subscription_users_srbd_subscriptions_subscription_id",
                        column: x => x.SubscriptionId,
                        principalTable: "srbd_subscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

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
                name: "srbd_subscription_onboarding_states",
                columns: table => new
                {
                    SubscriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CurrentStep = table.Column<string>(type: "text", nullable: false),
                    PrimaryAgentId = table.Column<Guid>(type: "uuid", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("srbd_pk_srbd_subscription_onboarding_states", x => x.SubscriptionId);
                    table.ForeignKey(
                        name: "srbd_fk_srbd_subscription_onboarding_states_srbd_agents_primary_agen~",
                        column: x => x.PrimaryAgentId,
                        principalTable: "srbd_agents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "srbd_fk_srbd_subscription_onboarding_states_srbd_subscriptions_subs~",
                        column: x => x.SubscriptionId,
                        principalTable: "srbd_subscriptions",
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
                name: "srbd_ix_srbd_agents_subscription_id",
                table: "srbd_agents",
                column: "SubscriptionId");

            migrationBuilder.CreateIndex(
                name: "srbd_ix_srbd_agents_telegram_bot_token",
                table: "srbd_agents",
                column: "TelegramBotToken",
                unique: true,
                filter: "\"TelegramBotToken\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "srbd_ix_srbd_agents_twilio_phone_number",
                table: "srbd_agents",
                column: "TwilioPhoneNumber",
                filter: "\"TwilioPhoneNumber\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "srbd_ix_srbd_agents_whatsapp_number",
                table: "srbd_agents",
                column: "WhatsappNumber",
                filter: "\"WhatsappNumber\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "srbd_ix_srbd_asp_net_role_claims_role_id",
                table: "srbd_asp_net_role_claims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "srbd_role_name_index",
                table: "srbd_asp_net_roles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "srbd_ix_srbd_asp_net_user_claims_user_id",
                table: "srbd_asp_net_user_claims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "srbd_ix_srbd_asp_net_user_logins_user_id",
                table: "srbd_asp_net_user_logins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "srbd_ix_srbd_asp_net_user_roles_role_id",
                table: "srbd_asp_net_user_roles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "srbd_email_index",
                table: "srbd_asp_net_users",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "srbd_user_name_index",
                table: "srbd_asp_net_users",
                column: "NormalizedUserName",
                unique: true);

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

            migrationBuilder.CreateIndex(
                name: "srbd_ix_srbd_credit_transactions_stripe_event_id",
                table: "srbd_credit_transactions",
                column: "StripeEventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "srbd_ix_srbd_credit_transactions_subscription_id",
                table: "srbd_credit_transactions",
                column: "SubscriptionId");

            migrationBuilder.CreateIndex(
                name: "srbd_ix_srbd_subscription_onboarding_states_primary_agent_id",
                table: "srbd_subscription_onboarding_states",
                column: "PrimaryAgentId");

            migrationBuilder.CreateIndex(
                name: "srbd_ix_srbd_subscription_onboarding_states_subscription_id",
                table: "srbd_subscription_onboarding_states",
                column: "SubscriptionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "srbd_ix_srbd_subscription_square_connections_connected_by_user_id",
                table: "srbd_subscription_square_connections",
                column: "ConnectedByUserId");

            migrationBuilder.CreateIndex(
                name: "srbd_ix_srbd_subscription_square_connections_square_merchant_id",
                table: "srbd_subscription_square_connections",
                column: "SquareMerchantId");

            migrationBuilder.CreateIndex(
                name: "srbd_ix_srbd_subscription_square_connections_subscription_id",
                table: "srbd_subscription_square_connections",
                column: "SubscriptionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "srbd_ix_srbd_subscription_users_user_id",
                table: "srbd_subscription_users",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "srbd_asp_net_role_claims");

            migrationBuilder.DropTable(
                name: "srbd_asp_net_user_claims");

            migrationBuilder.DropTable(
                name: "srbd_asp_net_user_logins");

            migrationBuilder.DropTable(
                name: "srbd_asp_net_user_roles");

            migrationBuilder.DropTable(
                name: "srbd_asp_net_user_tokens");

            migrationBuilder.DropTable(
                name: "srbd_conversation_messages");

            migrationBuilder.DropTable(
                name: "srbd_credit_transactions");

            migrationBuilder.DropTable(
                name: "srbd_data_protection_keys");

            migrationBuilder.DropTable(
                name: "srbd_subscription_onboarding_states");

            migrationBuilder.DropTable(
                name: "srbd_subscription_square_connections");

            migrationBuilder.DropTable(
                name: "srbd_subscription_users");

            migrationBuilder.DropTable(
                name: "srbd_asp_net_roles");

            migrationBuilder.DropTable(
                name: "srbd_conversations");

            migrationBuilder.DropTable(
                name: "srbd_asp_net_users");

            migrationBuilder.DropTable(
                name: "srbd_agents");

            migrationBuilder.DropTable(
                name: "srbd_subscriptions");
        }
    }
}
