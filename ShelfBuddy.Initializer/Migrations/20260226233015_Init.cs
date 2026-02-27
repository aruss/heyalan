using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ShelfBuddy.Initializer.Migrations
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
                        name: "srbd_fk_srbd_asp_net_role_claims_asp_net_roles_role_id",
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
                        name: "srbd_fk_srbd_asp_net_user_claims_asp_net_users_user_id",
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
                        name: "srbd_fk_srbd_asp_net_user_logins_asp_net_users_user_id",
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
                        name: "srbd_fk_srbd_asp_net_user_roles_asp_net_roles_role_id",
                        column: x => x.RoleId,
                        principalTable: "srbd_asp_net_roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "srbd_fk_srbd_asp_net_user_roles_asp_net_users_user_id",
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
                        name: "srbd_fk_srbd_asp_net_user_tokens_asp_net_users_user_id",
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
                    BasePromptRaw = table.Column<string>(type: "text", nullable: true),
                    BasePromptSanitized = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TwilioPhoneNumber = table.Column<string>(type: "text", nullable: true),
                    TelegramBotToken = table.Column<string>(type: "text", nullable: true)
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

            migrationBuilder.CreateIndex(
                name: "srbd_ix_srbd_agents_subscription_id",
                table: "srbd_agents",
                column: "SubscriptionId");

            migrationBuilder.CreateIndex(
                name: "srbd_ix_srbd_agents_telegram_bot_token",
                table: "srbd_agents",
                column: "TelegramBotToken",
                filter: "\"TelegramBotToken\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "srbd_ix_srbd_agents_twilio_phone_number",
                table: "srbd_agents",
                column: "TwilioPhoneNumber",
                filter: "\"TwilioPhoneNumber\" IS NOT NULL");

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
                name: "srbd_ix_srbd_credit_transactions_stripe_event_id",
                table: "srbd_credit_transactions",
                column: "StripeEventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "srbd_ix_srbd_credit_transactions_subscription_id",
                table: "srbd_credit_transactions",
                column: "SubscriptionId");

            migrationBuilder.CreateIndex(
                name: "srbd_ix_srbd_subscription_users_user_id",
                table: "srbd_subscription_users",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "srbd_agents");

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
                name: "srbd_credit_transactions");

            migrationBuilder.DropTable(
                name: "srbd_data_protection_keys");

            migrationBuilder.DropTable(
                name: "srbd_subscription_users");

            migrationBuilder.DropTable(
                name: "srbd_asp_net_roles");

            migrationBuilder.DropTable(
                name: "srbd_asp_net_users");

            migrationBuilder.DropTable(
                name: "srbd_subscriptions");
        }
    }
}
