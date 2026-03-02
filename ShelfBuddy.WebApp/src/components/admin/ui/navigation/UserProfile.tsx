"use client"

import { Button } from "@/components/admin/Button"
import { useSession } from "@/lib/session-context"
import { cx, focusRing } from "@/lib/utils"
import { ChevronsUpDown } from "lucide-react"

import { DropdownUserProfile } from "./DropdownUserProfile"

const EMPTY_INITIALS = "??"
const LOADING_LABEL = "Loading..."
const FALLBACK_LABEL = "Account"

function normalizeValue(value: string): string {
  return value.trim().toLowerCase()
}

function resolveDisplayLabel(email: string, displayName: string): string {
  const trimmedEmail = email.trim()
  const trimmedDisplayName = displayName.trim()

  if (trimmedDisplayName.length === 0) {
    return trimmedEmail
  }

  if (
    trimmedEmail.length > 0 &&
    normalizeValue(trimmedDisplayName) === normalizeValue(trimmedEmail)
  ) {
    return trimmedEmail
  }

  return trimmedDisplayName
}

function resolveInitials(label: string): string {
  const trimmedLabel = label.trim()
  if (trimmedLabel.length === 0) {
    return EMPTY_INITIALS
  }

  const words = trimmedLabel.split(/\s+/).filter((word) => word.length > 0)
  if (words.length >= 2) {
    return `${words[0][0]}${words[1][0]}`.toUpperCase()
  }

  const characters = words[0]
    .replace(/[^a-zA-Z0-9]/g, "")
    .slice(0, 2)
    .toUpperCase()

  if (characters.length === 0) {
    return EMPTY_INITIALS
  }

  if (characters.length === 1) {
    return `${characters}?`
  }

  return characters
}

export function UserProfile() {
  const { currentUser, isLoading } = useSession()
  const profileLabel = isLoading
    ? LOADING_LABEL
    : currentUser
      ? resolveDisplayLabel(currentUser.email, currentUser.displayName)
      : FALLBACK_LABEL
  const initials = resolveInitials(profileLabel)
  const emailLabel = currentUser?.email ?? null

  return (
    <DropdownUserProfile emailLabel={emailLabel}>
      <Button
        aria-label="User settings"
        variant="ghost"
        className={cx(
          "group flex w-full items-center justify-between rounded-md px-1 py-2 text-sm font-medium text-gray-900 hover:bg-gray-200/50 data-[state=open]:bg-gray-200/50 hover:dark:bg-gray-800/50 data-[state=open]:dark:bg-gray-900",
          focusRing,
        )}
      >
        <span className="flex items-center gap-3">
          <span
            className={cx(
              "flex size-8 shrink-0 items-center justify-center rounded-full border text-xs",
              isLoading
                ? "animate-pulse border-gray-200 bg-gray-100 dark:border-gray-800 dark:bg-gray-800"
                : "border-gray-300 bg-white text-gray-700 dark:border-gray-800 dark:bg-gray-900 dark:text-gray-300",
            )}
            aria-hidden="true"
          >
            {isLoading ? (
              <span className="h-2.5 w-4 rounded-sm bg-gray-300 dark:bg-gray-700" />
            ) : (
              initials
            )}
          </span>
          <span
            className={cx(
              isLoading &&
                "inline-block h-4 w-24 animate-pulse rounded bg-gray-200 text-transparent dark:bg-gray-800",
            )}
          >
            {profileLabel}
          </span>
        </span>
        <ChevronsUpDown
          className="size-4 shrink-0 text-gray-500 group-hover:text-gray-700 group-hover:dark:text-gray-400"
          aria-hidden="true"
        />
      </Button>
    </DropdownUserProfile>
  )
}
