using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HeyAlan.Initializer.Migrations
{
    /// <inheritdoc />
    public partial class Products : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "srbd_ix_srbd_agents_subscription_id",
                table: "srbd_agents");

            migrationBuilder.AddUniqueConstraint(
                name: "srbd_ak_srbd_agents_subscription_id_id",
                table: "srbd_agents",
                columns: new[] { "SubscriptionId", "Id" });

            migrationBuilder.CreateTable(
                name: "srbd_agent_sales_zip_codes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AgentId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubscriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ZipCodeNormalized = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("srbd_pk_srbd_agent_sales_zip_codes", x => x.Id);
                    table.ForeignKey(
                        name: "srbd_fk_agent_sales_zip_agent",
                        columns: x => new { x.SubscriptionId, x.AgentId },
                        principalTable: "srbd_agents",
                        principalColumns: new[] { "SubscriptionId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "srbd_square_webhook_receipts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubscriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<string>(type: "text", nullable: false),
                    EventType = table.Column<string>(type: "text", nullable: false),
                    MerchantId = table.Column<string>(type: "text", nullable: false),
                    ReceivedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsProcessed = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("srbd_pk_srbd_square_webhook_receipts", x => x.Id);
                    table.ForeignKey(
                        name: "srbd_fk_square_webhook_receipt_subscription",
                        column: x => x.SubscriptionId,
                        principalTable: "srbd_subscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "srbd_subscription_catalog_products",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubscriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SquareItemId = table.Column<string>(type: "text", nullable: false),
                    SquareVariationId = table.Column<string>(type: "text", nullable: false),
                    ItemName = table.Column<string>(type: "text", nullable: false),
                    VariationName = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Sku = table.Column<string>(type: "text", nullable: true),
                    BasePriceAmount = table.Column<long>(type: "bigint", nullable: true),
                    BasePriceCurrency = table.Column<string>(type: "text", nullable: true),
                    IsSellable = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    SquareUpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SquareVersion = table.Column<long>(type: "bigint", nullable: true),
                    SearchText = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("srbd_pk_srbd_subscription_catalog_products", x => x.Id);
                    table.UniqueConstraint("srbd_ak_srbd_subscription_catalog_products_subscription_id_id", x => new { x.SubscriptionId, x.Id });
                    table.ForeignKey(
                        name: "srbd_fk_catalog_product_subscription",
                        column: x => x.SubscriptionId,
                        principalTable: "srbd_subscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "srbd_subscription_catalog_sync_states",
                columns: table => new
                {
                    SubscriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    LastSyncedBeginTimeUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NextScheduledSyncAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastSyncStartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastSyncCompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastTriggerSource = table.Column<int>(type: "integer", nullable: true),
                    SyncInProgress = table.Column<bool>(type: "boolean", nullable: false),
                    PendingResync = table.Column<bool>(type: "boolean", nullable: false),
                    LastErrorCode = table.Column<string>(type: "text", nullable: true),
                    LastErrorMessage = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("srbd_pk_srbd_subscription_catalog_sync_states", x => x.SubscriptionId);
                    table.ForeignKey(
                        name: "srbd_fk_subscription_catalog_sync_state_subscription",
                        column: x => x.SubscriptionId,
                        principalTable: "srbd_subscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "srbd_agent_catalog_product_accesses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AgentId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubscriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubscriptionCatalogProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("srbd_pk_srbd_agent_catalog_product_accesses", x => x.Id);
                    table.ForeignKey(
                        name: "srbd_fk_agent_catalog_access_agent",
                        columns: x => new { x.SubscriptionId, x.AgentId },
                        principalTable: "srbd_agents",
                        principalColumns: new[] { "SubscriptionId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "srbd_fk_agent_catalog_access_product",
                        columns: x => new { x.SubscriptionId, x.SubscriptionCatalogProductId },
                        principalTable: "srbd_subscription_catalog_products",
                        principalColumns: new[] { "SubscriptionId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "srbd_subscription_catalog_product_locations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubscriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubscriptionCatalogProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    SquareVariationId = table.Column<string>(type: "text", nullable: false),
                    LocationId = table.Column<string>(type: "text", nullable: false),
                    PriceOverrideAmount = table.Column<long>(type: "bigint", nullable: true),
                    PriceOverrideCurrency = table.Column<string>(type: "text", nullable: true),
                    IsAvailableForSale = table.Column<bool>(type: "boolean", nullable: false),
                    IsSoldOut = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("srbd_pk_srbd_subscription_catalog_product_locations", x => x.Id);
                    table.ForeignKey(
                        name: "srbd_fk_catalog_product_location_product",
                        columns: x => new { x.SubscriptionId, x.SubscriptionCatalogProductId },
                        principalTable: "srbd_subscription_catalog_products",
                        principalColumns: new[] { "SubscriptionId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "srbd_fk_catalog_product_location_subscription",
                        column: x => x.SubscriptionId,
                        principalTable: "srbd_subscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "srbd_ix_agent_catalog_access_agent",
                table: "srbd_agent_catalog_product_accesses",
                columns: new[] { "SubscriptionId", "AgentId", "Id" });

            migrationBuilder.CreateIndex(
                name: "srbd_ix_agent_catalog_access_agent_product",
                table: "srbd_agent_catalog_product_accesses",
                columns: new[] { "SubscriptionId", "AgentId", "SubscriptionCatalogProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "srbd_ix_agent_catalog_access_product",
                table: "srbd_agent_catalog_product_accesses",
                columns: new[] { "SubscriptionId", "SubscriptionCatalogProductId", "AgentId" });

            migrationBuilder.CreateIndex(
                name: "srbd_ix_agent_sales_zip_agent",
                table: "srbd_agent_sales_zip_codes",
                columns: new[] { "SubscriptionId", "AgentId", "Id" });

            migrationBuilder.CreateIndex(
                name: "srbd_ix_agent_sales_zip_agent_zip",
                table: "srbd_agent_sales_zip_codes",
                columns: new[] { "SubscriptionId", "AgentId", "ZipCodeNormalized" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "srbd_ix_srbd_square_webhook_receipts_event_id",
                table: "srbd_square_webhook_receipts",
                column: "EventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "srbd_ix_srbd_square_webhook_receipts_subscription_id_is_processed_rec~",
                table: "srbd_square_webhook_receipts",
                columns: new[] { "SubscriptionId", "IsProcessed", "ReceivedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "srbd_ix_srbd_square_webhook_receipts_subscription_id_received_at_utc_id",
                table: "srbd_square_webhook_receipts",
                columns: new[] { "SubscriptionId", "ReceivedAtUtc", "Id" });

            migrationBuilder.CreateIndex(
                name: "srbd_ix_catalog_product_location_location",
                table: "srbd_subscription_catalog_product_locations",
                columns: new[] { "SubscriptionId", "LocationId", "Id" });

            migrationBuilder.CreateIndex(
                name: "srbd_ix_catalog_product_location_product",
                table: "srbd_subscription_catalog_product_locations",
                columns: new[] { "SubscriptionId", "SubscriptionCatalogProductId" });

            migrationBuilder.CreateIndex(
                name: "srbd_ix_catalog_product_location_variation_location",
                table: "srbd_subscription_catalog_product_locations",
                columns: new[] { "SubscriptionId", "SquareVariationId", "LocationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "srbd_ix_catalog_product_subscription_active_name",
                table: "srbd_subscription_catalog_products",
                columns: new[] { "SubscriptionId", "IsDeleted", "IsSellable", "ItemName", "Id" });

            migrationBuilder.CreateIndex(
                name: "srbd_ix_catalog_product_subscription_search",
                table: "srbd_subscription_catalog_products",
                columns: new[] { "SubscriptionId", "SearchText" });

            migrationBuilder.CreateIndex(
                name: "srbd_ix_catalog_product_subscription_item",
                table: "srbd_subscription_catalog_products",
                columns: new[] { "SubscriptionId", "SquareItemId" });

            migrationBuilder.CreateIndex(
                name: "srbd_ix_catalog_product_subscription_variation",
                table: "srbd_subscription_catalog_products",
                columns: new[] { "SubscriptionId", "SquareVariationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "srbd_ix_srbd_subscription_catalog_sync_states_subscription_id_next_sc~",
                table: "srbd_subscription_catalog_sync_states",
                columns: new[] { "SubscriptionId", "NextScheduledSyncAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "srbd_agent_catalog_product_accesses");

            migrationBuilder.DropTable(
                name: "srbd_agent_sales_zip_codes");

            migrationBuilder.DropTable(
                name: "srbd_square_webhook_receipts");

            migrationBuilder.DropTable(
                name: "srbd_subscription_catalog_product_locations");

            migrationBuilder.DropTable(
                name: "srbd_subscription_catalog_sync_states");

            migrationBuilder.DropTable(
                name: "srbd_subscription_catalog_products");

            migrationBuilder.DropUniqueConstraint(
                name: "srbd_ak_srbd_agents_subscription_id_id",
                table: "srbd_agents");

            migrationBuilder.CreateIndex(
                name: "srbd_ix_srbd_agents_subscription_id",
                table: "srbd_agents",
                column: "SubscriptionId");
        }
    }
}
