import type { SubscriptionSquareCatalogProductItem } from "@/lib/api";

export const CATALOG_SNAPSHOT_HEADERS = [
  "Product",
  "Variation",
  "SKU",
  "Price",
  "State",
  "Updated",
] as const;

export const getCatalogProductDescription = (
  product: SubscriptionSquareCatalogProductItem,
): string | null => {
  const description = product.description?.trim() ?? "";

  if (description.length === 0) {
    return null;
  }

  return description;
};
