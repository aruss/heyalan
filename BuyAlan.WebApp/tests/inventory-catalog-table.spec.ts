import { expect, test } from "@playwright/test";
import type { SubscriptionSquareCatalogProductItem } from "../src/lib/api";
import {
  CATALOG_SNAPSHOT_HEADERS,
  getCatalogProductDescription,
} from "../src/app/admin/settings/inventory/inventory-catalog-table";

const createProduct = (description: string | null): SubscriptionSquareCatalogProductItem => {
  return {
    subscriptionCatalogProductId: "11111111-1111-1111-1111-111111111111",
    squareItemId: "item-1",
    squareVariationId: "var-1",
    itemName: "Coffee",
    variationName: "Large",
    description,
    sku: "COF-L",
    basePriceAmount: 1299,
    basePriceCurrency: "USD",
    isSellable: true,
    isDeleted: false,
    squareUpdatedAtUtc: "2026-03-09T10:00:00Z",
    locationCount: 2,
  };
};

test("catalog snapshot headers omit the locations column", () => {
  expect(CATALOG_SNAPSHOT_HEADERS).toEqual([
    "Product",
    "Variation",
    "SKU",
    "Price",
    "State",
    "Updated",
  ]);
});

test("catalog product description uses cached description instead of square variation id", () => {
  const product = createProduct("Fresh roast");

  expect(getCatalogProductDescription(product)).toBe("Fresh roast");
  expect(getCatalogProductDescription(product)).not.toBe(product.squareVariationId);
});

test("blank catalog product descriptions are suppressed", () => {
  const product = createProduct("   ");

  expect(getCatalogProductDescription(product)).toBeNull();
});
