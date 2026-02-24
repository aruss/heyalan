using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SquareBuddy.Initializer.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "sb_asp_net_roles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("sb_pk_sb_asp_net_roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "sb_asp_net_users",
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
                    table.PrimaryKey("sb_pk_sb_asp_net_users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "sb_data_protection_keys",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FriendlyName = table.Column<string>(type: "text", nullable: true),
                    Xml = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("sb_pk_sb_data_protection_keys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "sb_subscriptions",
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
                    table.PrimaryKey("sb_pk_sb_subscriptions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "sb_asp_net_role_claims",
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
                    table.PrimaryKey("sb_pk_sb_asp_net_role_claims", x => x.Id);
                    table.ForeignKey(
                        name: "sb_fk_sb_asp_net_role_claims_asp_net_roles_role_id",
                        column: x => x.RoleId,
                        principalTable: "sb_asp_net_roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sb_asp_net_user_claims",
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
                    table.PrimaryKey("sb_pk_sb_asp_net_user_claims", x => x.Id);
                    table.ForeignKey(
                        name: "sb_fk_sb_asp_net_user_claims_asp_net_users_user_id",
                        column: x => x.UserId,
                        principalTable: "sb_asp_net_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sb_asp_net_user_logins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    ProviderKey = table.Column<string>(type: "text", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("sb_pk_sb_asp_net_user_logins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "sb_fk_sb_asp_net_user_logins_asp_net_users_user_id",
                        column: x => x.UserId,
                        principalTable: "sb_asp_net_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sb_asp_net_user_roles",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("sb_pk_sb_asp_net_user_roles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "sb_fk_sb_asp_net_user_roles_asp_net_roles_role_id",
                        column: x => x.RoleId,
                        principalTable: "sb_asp_net_roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "sb_fk_sb_asp_net_user_roles_asp_net_users_user_id",
                        column: x => x.UserId,
                        principalTable: "sb_asp_net_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sb_asp_net_user_tokens",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("sb_pk_sb_asp_net_user_tokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "sb_fk_sb_asp_net_user_tokens_asp_net_users_user_id",
                        column: x => x.UserId,
                        principalTable: "sb_asp_net_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sb_boards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    SubscriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("sb_pk_sb_boards", x => x.Id);
                    table.ForeignKey(
                        name: "sb_fk_sb_boards_subscriptions_subscription_id",
                        column: x => x.SubscriptionId,
                        principalTable: "sb_subscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sb_credit_transactions",
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
                    table.PrimaryKey("sb_pk_sb_credit_transactions", x => x.Id);
                    table.ForeignKey(
                        name: "sb_fk_sb_credit_transactions_subscriptions_subscription_id",
                        column: x => x.SubscriptionId,
                        principalTable: "sb_subscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sb_subscription_users",
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
                    table.PrimaryKey("sb_pk_sb_subscription_users", x => new { x.SubscriptionId, x.UserId });
                    table.ForeignKey(
                        name: "sb_fk_sb_subscription_users_sb_asp_net_users_user_id",
                        column: x => x.UserId,
                        principalTable: "sb_asp_net_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "sb_fk_sb_subscription_users_sb_subscriptions_subscription_id",
                        column: x => x.SubscriptionId,
                        principalTable: "sb_subscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sb_board_configs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BoardId = table.Column<Guid>(type: "uuid", nullable: false),
                    AgeGroup = table.Column<int>(type: "integer", nullable: false),
                    Language = table.Column<string>(type: "text", nullable: false),
                    Voice = table.Column<string>(type: "text", nullable: true),
                    ProducerUserPrompt = table.Column<string>(type: "text", nullable: true),
                    EvaluatorUserPrompt = table.Column<string>(type: "text", nullable: true),
                    ProducerUserPromptCompiled = table.Column<string>(type: "text", nullable: true),
                    EvaluatorUserPromptCompiled = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("sb_pk_sb_board_configs", x => x.Id);
                    table.ForeignKey(
                        name: "sb_fk_sb_board_configs_sb_boards_board_id",
                        column: x => x.BoardId,
                        principalTable: "sb_boards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sb_story_requests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConfigId = table.Column<Guid>(type: "uuid", nullable: false),
                    BoardId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Input = table.Column<string>(type: "text", nullable: false),
                    SceneGraph = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    CreatedWith = table.Column<string>(type: "text", nullable: false),
                    Duration = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("sb_pk_sb_story_requests", x => x.Id);
                    table.ForeignKey(
                        name: "sb_fk_sb_story_requests_sb_board_configs_config_id",
                        column: x => x.ConfigId,
                        principalTable: "sb_board_configs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "sb_fk_sb_story_requests_sb_boards_board_id",
                        column: x => x.BoardId,
                        principalTable: "sb_boards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sb_story_request_chunks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StoryRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false),
                    AudioObjectKey = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("sb_pk_sb_story_request_chunks", x => x.Id);
                    table.ForeignKey(
                        name: "sb_fk_sb_story_request_chunks_sb_story_requests_story_request_id",
                        column: x => x.StoryRequestId,
                        principalTable: "sb_story_requests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "sb_ix_sb_asp_net_role_claims_role_id",
                table: "sb_asp_net_role_claims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "sb_role_name_index",
                table: "sb_asp_net_roles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "sb_ix_sb_asp_net_user_claims_user_id",
                table: "sb_asp_net_user_claims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "sb_ix_sb_asp_net_user_logins_user_id",
                table: "sb_asp_net_user_logins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "sb_ix_sb_asp_net_user_roles_role_id",
                table: "sb_asp_net_user_roles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "sb_email_index",
                table: "sb_asp_net_users",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "sb_user_name_index",
                table: "sb_asp_net_users",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "sb_ix_sb_board_configs_board_id_created_at",
                table: "sb_board_configs",
                columns: new[] { "BoardId", "CreatedAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "sb_ix_sb_boards_subscription_id",
                table: "sb_boards",
                column: "SubscriptionId");

            migrationBuilder.CreateIndex(
                name: "sb_ix_sb_credit_transactions_stripe_event_id",
                table: "sb_credit_transactions",
                column: "StripeEventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "sb_ix_sb_credit_transactions_subscription_id",
                table: "sb_credit_transactions",
                column: "SubscriptionId");

            migrationBuilder.CreateIndex(
                name: "sb_ix_sb_story_request_chunks_story_request_id",
                table: "sb_story_request_chunks",
                column: "StoryRequestId");

            migrationBuilder.CreateIndex(
                name: "sb_ix_sb_story_request_chunks_story_request_id_sequence",
                table: "sb_story_request_chunks",
                columns: new[] { "StoryRequestId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "sb_ix_sb_story_requests_board_id_created_at",
                table: "sb_story_requests",
                columns: new[] { "BoardId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "sb_ix_sb_story_requests_config_id",
                table: "sb_story_requests",
                column: "ConfigId");

            migrationBuilder.CreateIndex(
                name: "sb_ix_sb_story_requests_status",
                table: "sb_story_requests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "sb_ix_sb_subscription_users_user_id",
                table: "sb_subscription_users",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sb_asp_net_role_claims");

            migrationBuilder.DropTable(
                name: "sb_asp_net_user_claims");

            migrationBuilder.DropTable(
                name: "sb_asp_net_user_logins");

            migrationBuilder.DropTable(
                name: "sb_asp_net_user_roles");

            migrationBuilder.DropTable(
                name: "sb_asp_net_user_tokens");

            migrationBuilder.DropTable(
                name: "sb_credit_transactions");

            migrationBuilder.DropTable(
                name: "sb_data_protection_keys");

            migrationBuilder.DropTable(
                name: "sb_story_request_chunks");

            migrationBuilder.DropTable(
                name: "sb_subscription_users");

            migrationBuilder.DropTable(
                name: "sb_asp_net_roles");

            migrationBuilder.DropTable(
                name: "sb_story_requests");

            migrationBuilder.DropTable(
                name: "sb_asp_net_users");

            migrationBuilder.DropTable(
                name: "sb_board_configs");

            migrationBuilder.DropTable(
                name: "sb_boards");

            migrationBuilder.DropTable(
                name: "sb_subscriptions");
        }
    }
}
