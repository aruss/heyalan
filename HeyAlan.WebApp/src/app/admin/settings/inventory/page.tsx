"use client";

import { useMemo, useState } from "react";
import { useMutation, useQuery } from "@tanstack/react-query";
import { Alert } from "@/components/admin/alert";
import { Badge } from "@/components/admin/badge";
import { Button } from "@/components/admin/button";
import { Card } from "@/components/admin/card";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeaderCell,
  TableRoot,
  TableRow,
} from "@/components/admin/table";
import {
  getSubscriptionsBySubscriptionIdSquareCatalogProductsOptions,
  getSubscriptionsBySubscriptionIdSquareCatalogSyncStateOptions,
  postSubscriptionsBySubscriptionIdSquareCatalogSyncMutation,
} from "@/lib/api/@tanstack/react-query.gen";
import type {
  GetSubscriptionSquareCatalogSyncStateResult as SubscriptionSquareCatalogSyncStateResult,
  SubscriptionCatalogSyncErrorResult,
  SubscriptionSquareCatalogProductItem,
} from "@/lib/api";
import { useSession } from "@/lib/session-context";

const DEFAULT_SYNC_STATE_ERROR = "Unable to load catalog sync state.";
const DEFAULT_PRODUCTS_ERROR = "Unable to load cached catalog products.";
const DEFAULT_SYNC_MUTATION_ERROR = "Unable to trigger catalog sync.";
const MISSING_SUBSCRIPTION_ERROR = "No active subscription is available for this account.";
const POLLING_INTERVAL_MS = 30_000;
const PAGE_SIZE = 10;

type FeedbackState = {
  tone: "error" | "success";
  text: string;
} | null;

type ApiErrorWithMessage = {
  errorCode?: string;
  message?: string;
};

const resolveApiError = (error: unknown): ApiErrorWithMessage => {
  if (!error || typeof error !== "object") {
    return {};
  }

  return error as ApiErrorWithMessage;
};

const resolveApiErrorMessage = (error: unknown, fallback: string): string => {
  const apiError = resolveApiError(error);

  if (typeof apiError.message === "string" && apiError.message.trim().length > 0) {
    return apiError.message;
  }

  return fallback;
};

const formatDateTime = (value: string | null): string => {
  if (!value) {
    return "Not available";
  }

  const parsedDate = new Date(value);
  if (Number.isNaN(parsedDate.getTime())) {
    return "Not available";
  }

  return new Intl.DateTimeFormat("en-US", {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(parsedDate);
};

const formatPrice = (amount: number | null, currency: string | null): string => {
  if (amount === null || !currency) {
    return "Not set";
  }

  try {
    return new Intl.NumberFormat("en-US", {
      currency,
      style: "currency",
    }).format(amount / 100);
  } catch {
    return `${amount / 100} ${currency}`;
  }
};

const formatTriggerSource = (value: string | null): string => {
  if (!value) {
    return "Not available";
  }

  return value.replaceAll("_", " ");
};

const formatStatus = (status: string): string => {
  return status.replaceAll("_", " ");
};

const getStatusVariant = (
  status: string,
): "default" | "error" | "neutral" | "success" | "warning" => {
  switch (status) {
    case "failed":
      return "error";
    case "in_progress":
      return "warning";
    case "idle":
      return "success";
    case "pending_resync":
      return "warning";
    default:
      return "neutral";
  }
};

const getProductStateLabel = (product: SubscriptionSquareCatalogProductItem): string => {
  if (product.isDeleted) {
    return "Deleted";
  }

  if (!product.isSellable) {
    return "Not sellable";
  }

  return "Active";
};

const getProductStateVariant = (
  product: SubscriptionSquareCatalogProductItem,
): "default" | "error" | "neutral" | "success" | "warning" => {
  if (product.isDeleted) {
    return "error";
  }

  if (!product.isSellable) {
    return "warning";
  }

  return "success";
};

const SyncStateGrid = ({
  syncState,
}: {
  syncState: SubscriptionSquareCatalogSyncStateResult;
}) => {
  const metrics = [
    {
      label: "Last synced",
      value: formatDateTime(syncState.lastSyncedBeginTimeUtc),
    },
    {
      label: "Last started",
      value: formatDateTime(syncState.lastSyncStartedAtUtc),
    },
    {
      label: "Last completed",
      value: formatDateTime(syncState.lastSyncCompletedAtUtc),
    },
    {
      label: "Next scheduled",
      value: formatDateTime(syncState.nextScheduledSyncAtUtc),
    },
    {
      label: "Trigger source",
      value: formatTriggerSource(syncState.lastTriggerSource),
    },
    {
      label: "Cached products",
      value: syncState.cachedProductCount.toString(),
    },
    {
      label: "Sellable products",
      value: syncState.sellableProductCount.toString(),
    },
    {
      label: "Deleted products",
      value: syncState.deletedProductCount.toString(),
    },
  ];

  return (
    <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-4">
      {metrics.map((metric) => (
        <div key={metric.label} className="rounded-lg border border-gray-200 bg-gray-50 p-4">
          <p className="text-xs font-medium uppercase tracking-wide text-gray-500">{metric.label}</p>
          <p className="mt-2 text-sm font-semibold text-gray-900">{metric.value}</p>
        </div>
      ))}
    </div>
  );
};

export default function SettingInventoryPage() {
  const { currentUser, errorMessage: sessionErrorMessage, isLoading: isSessionLoading } = useSession();
  const [syncFeedback, setSyncFeedback] = useState<FeedbackState>(null);
  const [productSkip, setProductSkip] = useState<number>(0);

  const subscriptionId = currentUser?.activeSubscriptionId ?? null;

  const syncStateQuery = useQuery({
    ...getSubscriptionsBySubscriptionIdSquareCatalogSyncStateOptions({
      path: {
        subscriptionId: subscriptionId ?? "",
      },
    }),
    refetchInterval: POLLING_INTERVAL_MS,
    enabled: subscriptionId !== null,
    retry: false,
  });

  const productsQuery = useQuery({
    ...getSubscriptionsBySubscriptionIdSquareCatalogProductsOptions({
      path: {
        subscriptionId: subscriptionId ?? "",
      },
      query: {
        skip: productSkip,
        take: PAGE_SIZE,
      },
    }),
    enabled: subscriptionId !== null,
    retry: false,
  });

  const syncMutation = useMutation(postSubscriptionsBySubscriptionIdSquareCatalogSyncMutation());

  const syncStateErrorMessage = useMemo(() => {
    if (sessionErrorMessage) {
      return sessionErrorMessage;
    }

    if (!subscriptionId && !isSessionLoading) {
      return MISSING_SUBSCRIPTION_ERROR;
    }

    if (syncStateQuery.error) {
      return resolveApiErrorMessage(syncStateQuery.error, DEFAULT_SYNC_STATE_ERROR);
    }

    return null;
  }, [isSessionLoading, sessionErrorMessage, subscriptionId, syncStateQuery.error]);

  const productsErrorMessage = useMemo(() => {
    if (sessionErrorMessage) {
      return sessionErrorMessage;
    }

    if (!subscriptionId && !isSessionLoading) {
      return MISSING_SUBSCRIPTION_ERROR;
    }

    if (productsQuery.error) {
      return resolveApiErrorMessage(productsQuery.error, DEFAULT_PRODUCTS_ERROR);
    }

    return null;
  }, [isSessionLoading, productsQuery.error, sessionErrorMessage, subscriptionId]);

  const handleRefresh = async (): Promise<void> => {
    setSyncFeedback(null);
    await Promise.all([
      syncStateQuery.refetch(),
      productsQuery.refetch(),
    ]);
  };

  const handleSyncNow = async (): Promise<void> => {
    if (!subscriptionId) {
      setSyncFeedback({
        text: MISSING_SUBSCRIPTION_ERROR,
        tone: "error",
      });
      return;
    }

    setSyncFeedback(null);

    try {
      const result = await syncMutation.mutateAsync({
        path: {
          subscriptionId,
        },
      });

      setSyncFeedback({
        text: result.enqueued
          ? "Catalog sync request accepted."
          : "Catalog sync request was received but not enqueued.",
        tone: "success",
      });

      await Promise.all([
        syncStateQuery.refetch(),
        productsQuery.refetch(),
      ]);
    } catch (error: unknown) {
      const apiError = resolveApiError(error) as SubscriptionCatalogSyncErrorResult;

      setSyncFeedback({
        text: resolveApiErrorMessage(apiError, DEFAULT_SYNC_MUTATION_ERROR),
        tone: "error",
      });
    }
  };

  const syncState = syncStateQuery.data ?? null;
  const products = productsQuery.data?.items ?? [];
  const totalProducts = productsQuery.data?.total ?? 0;
  const canGoToPreviousPage = productSkip > 0;
  const canGoToNextPage = productSkip + products.length < totalProducts;

  return (
    <section>
      <div className="grid grid-cols-1 gap-x-14 gap-y-8 md:grid-cols-3">
        <div>
          <h2 className="scroll-mt-10 text-sm font-semibold text-gray-900 dark:text-gray-50">
            Inventory sync operations
          </h2>
          <p className="mt-1 text-xs leading-6 text-gray-500">
            Trigger a manual Square catalog refresh, monitor current sync health, and inspect the cached catalog snapshot used for operational verification.
          </p>
        </div>
        <div className="space-y-6 md:col-span-2">
          {!subscriptionId && !isSessionLoading ? (
            <Alert title="Subscription required" type="warn">
              {MISSING_SUBSCRIPTION_ERROR}
            </Alert>
          ) : null}

          <Card className="space-y-4">
            <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
              <div>
                <h3 className="text-sm font-semibold text-gray-900">Manual sync</h3>
                <p className="mt-1 text-sm text-gray-500">
                  Use this to enqueue a catalog refresh when operators need the latest Square changes immediately.
                </p>
              </div>
              <div className="flex gap-3">
                <Button
                  disabled={!subscriptionId}
                  isLoading={syncMutation.isPending}
                  onClick={() => void handleSyncNow()}
                  type="button"
                  variant="primary"
                >
                  Sync now
                </Button>
              </div>
            </div>

            {syncFeedback ? (
              <Alert title={syncFeedback.tone === "success" ? "Sync update" : "Sync failed"} type={syncFeedback.tone === "success" ? "info" : "error"}>
                {syncFeedback.text}
              </Alert>
            ) : null}
          </Card>

          <Card className="space-y-4">
            <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
              <div>
                <div className="flex items-center gap-3">
                  <h3 className="text-sm font-semibold text-gray-900">Sync state</h3>
                  {syncState ? (
                    <Badge variant={getStatusVariant(syncState.status)}>
                      {formatStatus(syncState.status)}
                    </Badge>
                  ) : null}
                </div>
                <p className="mt-1 text-sm text-gray-500">
                  This panel auto-refreshes every 30 seconds and can be refreshed manually at any time.
                </p>
              </div>
              <Button
                disabled={!subscriptionId}
                isLoading={syncStateQuery.isFetching || productsQuery.isFetching}
                onClick={() => void handleRefresh()}
                type="button"
                variant="secondary"
              >
                Refresh
              </Button>
            </div>

            {isSessionLoading || (syncStateQuery.isLoading && !syncState) ? (
              <p className="text-sm text-gray-500">Loading sync state...</p>
            ) : null}

            {syncStateErrorMessage ? (
              <Alert title="Unable to load sync state" type="error">
                {syncStateErrorMessage}
              </Alert>
            ) : null}

            {syncState ? (
              <>
                <SyncStateGrid syncState={syncState} />
                {syncState.lastErrorMessage ? (
                  <Alert title={syncState.lastErrorCode ?? "Last sync error"} type="error">
                    {syncState.lastErrorMessage}
                  </Alert>
                ) : null}
              </>
            ) : null}
          </Card>

          <Card className="space-y-4">
            <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
              <div>
                <h3 className="text-sm font-semibold text-gray-900">Cached catalog snapshot</h3>
                <p className="mt-1 text-sm text-gray-500">
                  Minimal paged view of the subscription catalog cache for operational verification.
                </p>
              </div>
              <div className="text-sm text-gray-500">
                {totalProducts > 0 ? `${productSkip + 1}-${Math.min(productSkip + products.length, totalProducts)} of ${totalProducts}` : "0 products"}
              </div>
            </div>

            {productsErrorMessage ? (
              <Alert title="Unable to load cached products" type="error">
                {productsErrorMessage}
              </Alert>
            ) : null}

            {!productsErrorMessage && productsQuery.isLoading ? (
              <p className="text-sm text-gray-500">Loading cached products...</p>
            ) : null}

            {!productsErrorMessage && !productsQuery.isLoading && products.length === 0 ? (
              <Alert title="No cached products" type="info">
                No catalog products are cached for this subscription yet.
              </Alert>
            ) : null}

            {!productsErrorMessage && products.length > 0 ? (
              <TableRoot>
                <Table>
                  <TableHead>
                    <TableRow>
                      <TableHeaderCell>Product</TableHeaderCell>
                      <TableHeaderCell>Variation</TableHeaderCell>
                      <TableHeaderCell>SKU</TableHeaderCell>
                      <TableHeaderCell>Price</TableHeaderCell>
                      <TableHeaderCell>State</TableHeaderCell>
                      <TableHeaderCell>Locations</TableHeaderCell>
                      <TableHeaderCell>Updated</TableHeaderCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {products.map((product) => (
                      <TableRow key={product.subscriptionCatalogProductId}>
                        <TableCell className="font-medium text-gray-900">
                          <div className="flex flex-col">
                            <span>{product.itemName}</span>
                            <span className="text-xs text-gray-500">{product.squareVariationId}</span>
                          </div>
                        </TableCell>
                        <TableCell>{product.variationName}</TableCell>
                        <TableCell>{product.sku ?? "Not set"}</TableCell>
                        <TableCell>{formatPrice(product.basePriceAmount, product.basePriceCurrency)}</TableCell>
                        <TableCell>
                          <Badge variant={getProductStateVariant(product)}>
                            {getProductStateLabel(product)}
                          </Badge>
                        </TableCell>
                        <TableCell>{product.locationCount}</TableCell>
                        <TableCell>{formatDateTime(product.squareUpdatedAtUtc)}</TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </TableRoot>
            ) : null}

            <div className="flex items-center justify-end gap-3">
              <Button
                disabled={!canGoToPreviousPage}
                onClick={() => setProductSkip(Math.max(productSkip - PAGE_SIZE, 0))}
                type="button"
                variant="secondary"
              >
                Previous
              </Button>
              <Button
                disabled={!canGoToNextPage}
                onClick={() => setProductSkip(productSkip + PAGE_SIZE)}
                type="button"
                variant="secondary"
              >
                Next
              </Button>
            </div>
          </Card>
        </div>
      </div>
    </section>
  );
}
