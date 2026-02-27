"use client"

import {
  Drawer,
  DrawerClose,
  DrawerContent,
  DrawerTitle,
} from "@/components/admin/Drawer"
import { useIsMobile } from "@/lib/useMobile"
import { cx, focusRing } from "@/lib/utils"
import * as VisuallyHidden from "@radix-ui/react-visually-hidden"
import { RiCloseLine } from "@remixicon/react"
import { PanelLeft } from "lucide-react"
import {
  createContext,
  forwardRef,
  useCallback,
  useContext,
  useMemo,
  useState,
  type ComponentProps,
  type ComponentPropsWithRef,
  type ComponentRef,
  type CSSProperties,
  type ElementType,
  type ReactNode,
} from "react"
import { Button } from "./Button"

// This component is based on shadcn's sidebar component

const SIDEBAR_COOKIE_NAME = "sidebar:state"
const SIDEBAR_COOKIE_MAX_AGE = 60 * 60 * 24 * 7
const SIDEBAR_WIDTH = "16rem"

type SidebarContext = {
  state: "expanded" | "collapsed"
  open: boolean
  setOpen: (open: boolean) => void
  openMobile: boolean
  setOpenMobile: (open: boolean) => void
  isMobile: boolean
  toggleSidebar: () => void
}

const SidebarContext = createContext<SidebarContext | null>(null)

function useSidebar() {
  const context = useContext(SidebarContext)
  if (!context) {
    throw new Error("useSidebar must be used within a SidebarProvider.")
  }

  return context
}

const SidebarProvider = forwardRef<
  HTMLDivElement,
  ComponentProps<"div"> & {
    defaultOpen?: boolean
    open?: boolean
    onOpenChange?: (open: boolean) => void
  }
>(
  (
    {
      defaultOpen = true,
      open: openProp,
      onOpenChange: setOpenProp,
      className,
      style,
      children,
      ...props
    },
    ref,
  ) => {
    const isMobile = useIsMobile()
    const [openMobile, setOpenMobile] = useState(false)
    const [_open, _setOpen] = useState(defaultOpen)
    const open = openProp ?? _open
    const setOpen = useCallback(
      (value: boolean | ((value: boolean) => boolean)) => {
        const openState = typeof value === "function" ? value(open) : value
        if (setOpenProp) {
          setOpenProp(openState)
        } else {
          _setOpen(openState)
        }

        document.cookie = `${SIDEBAR_COOKIE_NAME}=${openState}; path=/; max-age=${SIDEBAR_COOKIE_MAX_AGE}`
      },
      [setOpenProp, open],
    )

    const toggleSidebar = useCallback(() => {
      return isMobile
        ? setOpenMobile((open) => !open)
        : setOpen((open) => !open)
    }, [isMobile, setOpen, setOpenMobile])

    const state = open ? "expanded" : "collapsed"

    const contextValue = useMemo<SidebarContext>(
      () => ({
        state,
        open,
        setOpen,
        isMobile,
        openMobile,
        setOpenMobile,
        toggleSidebar,
      }),
      [
        state,
        open,
        setOpen,
        isMobile,
        openMobile,
        setOpenMobile,
        toggleSidebar,
      ],
    )

    return (
      <SidebarContext.Provider value={contextValue}>
        <div
          style={
            {
              "--sidebar-width": SIDEBAR_WIDTH,
              ...style,
            } as CSSProperties
          }
          className={cx("flex min-h-svh w-full", className)}
          ref={ref}
          {...props}
        >
          {children}
        </div>
      </SidebarContext.Provider>
    )
  },
)
SidebarProvider.displayName = "SidebarProvider"

const Sidebar = forwardRef<HTMLDivElement, ComponentProps<"div">>(
  ({ className, children, ...props }, ref) => {
    const { isMobile, state, openMobile, setOpenMobile } = useSidebar()

    if (isMobile) {
      return (
        <Drawer open={openMobile} onOpenChange={setOpenMobile} {...props}>
          <DrawerContent
            // data-sidebar="sidebar"
            // data-mobile="true"
            className="bg-gray-50 p-0 text-gray-900"
          >
            <VisuallyHidden.Root>
              <DrawerTitle>Sidebar</DrawerTitle>
            </VisuallyHidden.Root>
            <div className="relative flex h-full w-full flex-col">
              <DrawerClose className="absolute right-4 top-4" asChild>
                <Button
                  variant="ghost"
                  className="!p-2 text-gray-700 hover:text-gray-900 dark:text-gray-300 hover:dark:text-gray-50"
                >
                  <RiCloseLine className="size-5 shrink-0" aria-hidden="true" />
                </Button>
              </DrawerClose>
              {children}
            </div>
          </DrawerContent>
        </Drawer>
      )
    }

    return (
      <div
        ref={ref}
        className="group peer hidden md:block"
        data-state={state}
        data-collapsible={state === "collapsed" ? true : false}
      >
        <div
          className={cx(
            "relative h-svh w-(--sidebar-width) bg-transparent transition-[width] duration-150 ease-in-out will-change-transform",
            "group-data-[collapsible=true]:w-0",
          )}
        />
        <div
          className={cx(
            "fixed inset-y-0 z-10 hidden h-svh w-(--sidebar-width) transition-[left,right,width] duration-150 ease-in-out will-change-transform md:flex",
            "left-0 group-data-[collapsible=true]:left-[calc(var(--sidebar-width)*-1)]",
            "border-r border-gray-200 dark:border-gray-800",
            className,
          )}
          {...props}
        >
          <div
            data-sidebar="sidebar"
            className="flex h-full w-full flex-col"
          >
            {children}
          </div>
        </div>
      </div>
    )
  },
)
Sidebar.displayName = "Sidebar"

const SidebarInset = forwardRef<
  HTMLDivElement,
  ComponentProps<"div">
>(({ className, ...props }, ref) => {
  return (
    <div
      ref={ref}
      className={cx(
        "min-w-0 flex-1 transition-[margin] duration-150 ease-in-out",
        className,
      )}
      {...props}
    />
  )
})
SidebarInset.displayName = "SidebarInset"

const SidebarTrigger = forwardRef<
  ComponentRef<"button">,
  ComponentPropsWithRef<"button">
>(({ className, onClick, ...props }, ref) => {
  const { toggleSidebar } = useSidebar()

  return (
    <button
      ref={ref}
      data-sidebar="trigger"
      className={cx(
        "group inline-flex rounded-md p-1.5 hover:bg-gray-200/50 hover:dark:bg-gray-900",
        focusRing,
      )}
      onClick={(event) => {
        onClick?.(event)
        toggleSidebar()
      }}
      {...props}
    >
      <PanelLeft
        className="size-[18px] shrink-0 text-gray-700 dark:text-gray-300"
        aria-hidden="true"
      />
      <span className="sr-only">Toggle Sidebar</span>
    </button>
  )
})
SidebarTrigger.displayName = "SidebarTrigger"

const SidebarFooter = forwardRef<
  HTMLDivElement,
  ComponentProps<"div">
>(({ className, ...props }, ref) => {
  return (
    <div
      ref={ref}
      data-sidebar="footer"
      className={cx("flex flex-col gap-2 p-2", className)}
      {...props}
    />
  )
})
SidebarFooter.displayName = "SidebarFooter"

const SidebarContent = forwardRef<
  HTMLDivElement,
  ComponentProps<"div">
>(({ className, ...props }, ref) => {
  return (
    <div
      ref={ref}
      data-sidebar="content"
      className={cx(
        "flex min-h-0 flex-1 flex-col gap-2 overflow-auto",
        className,
      )}
      {...props}
    />
  )
})
SidebarContent.displayName = "SidebarContent"

const SidebarHeader = forwardRef<
  HTMLDivElement,
  ComponentProps<"div">
>(({ className, ...props }, ref) => {
  return (
    <div
      ref={ref}
      data-sidebar="header"
      className={cx("flex flex-col gap-2 p-2", className)}
      {...props}
    />
  )
})
SidebarHeader.displayName = "SidebarHeader"

const SidebarLink = forwardRef<
  HTMLAnchorElement,
  ComponentProps<"a"> & {
    children: ReactNode
    icon?: ElementType
    isActive?: boolean
    notifications?: number | boolean
  }
>(({ children, isActive, icon, notifications, className, ...props }, ref) => {
  const Icon = icon
  return (
    <a
      ref={ref}
      aria-current={isActive ? "page" : undefined}
      data-active={isActive}
      className={cx(
        "flex items-center justify-between rounded-md p-2 text-base transition hover:bg-gray-200/50 sm:text-sm hover:dark:bg-gray-900",
        "text-gray-900 dark:text-gray-400 hover:dark:text-gray-50",
        "data-[active=true]:bg-white data-[active=true]:text-gray-600 data-[active=true]:shadow data-[active=true]:ring-1 data-[active=true]:ring-gray-200",
        "data-[active=true]:dark:bg-gray-900 data-[active=true]:dark:text-gray-500 data-[active=true]:dark:ring-gray-800",
        focusRing,
      )}
      {...props}
    >
      <span className="flex items-center gap-x-2.5">
        {Icon && <Icon className="size-[18px] shrink-0" aria-hidden="true" />}
        {children}
      </span>
      {notifications && (
        <span className="inline-flex size-5 items-center justify-center rounded bg-gray-100 text-sm font-medium text-gray-600 sm:text-xs dark:bg-gray-500/10 dark:text-gray-500">
          {notifications}
        </span>
      )}
    </a>
  )
})
SidebarLink.displayName = "SidebarLink"

const SidebarGroup = forwardRef<
  HTMLDivElement,
  ComponentProps<"div">
>(({ className, ...props }, ref) => {
  return (
    <div
      ref={ref}
      data-sidebar="group"
      className={cx("relative flex w-full min-w-0 flex-col p-4", className)}
      {...props}
    />
  )
})
SidebarGroup.displayName = "SidebarGroup"

const SidebarGroupContent = forwardRef<
  HTMLDivElement,
  ComponentProps<"div">
>(({ className, ...props }, ref) => (
  <div
    ref={ref}
    data-sidebar="group-content"
    className={cx("w-full text-sm", className)}
    {...props}
  />
))
SidebarGroupContent.displayName = "SidebarGroupContent"

const SidebarMenu = forwardRef<
  HTMLUListElement,
  ComponentProps<"ul">
>(({ className, ...props }, ref) => (
  <ul
    ref={ref}
    data-sidebar="menu"
    className={cx("flex w-full min-w-0 flex-col gap-1", className)}
    {...props}
  />
))
SidebarMenu.displayName = "SidebarMenu"

const SidebarMenuItem = forwardRef<
  HTMLLIElement,
  ComponentProps<"li">
>(({ ...props }, ref) => <li ref={ref} {...props} />)
SidebarMenuItem.displayName = "SidebarMenuItem"

const SidebarSubLink = forwardRef<
  HTMLAnchorElement,
  ComponentProps<"a"> & {
    children: ReactNode
    isActive?: boolean
  }
>(({ isActive, children, className, ...props }, ref) => {
  return (
    <a
      ref={ref}
      aria-current={isActive ? "page" : undefined}
      data-active={isActive}
      className={cx(
        "relative flex gap-2 rounded-md py-1.5 pl-9 pr-3 text-base transition sm:text-sm",
        "text-gray-700 hover:text-gray-900 dark:text-gray-400 dark:hover:text-gray-50",
        "data-[active=true]:rounded data-[active=true]:bg-white data-[active=true]:text-gray-600 data-[active=true]:shadow data-[active=true]:ring-1 data-[active=true]:ring-gray-200 data-[active=true]:dark:bg-gray-900 data-[active=true]:dark:text-gray-500 data-[active=true]:dark:ring-gray-800",
        focusRing,
      )}
      {...props}
    >
      {isActive && (
        <div
          className="absolute left-4 top-1/2 h-5 w-px -translate-y-1/2 bg-gray-500 dark:bg-gray-500"
          aria-hidden="true"
        />
      )}
      {children}
    </a>
  )
})
SidebarSubLink.displayName = "SidebarSubLink"

const SidebarMenuSub = forwardRef<
  HTMLUListElement,
  ComponentProps<"ul">
>(({ className, ...props }, ref) => (
  <ul
    ref={ref}
    data-sidebar="menu-sub"
    className={cx("relative space-y-1 border-l border-transparent", className)}
    {...props}
  />
))
SidebarMenuSub.displayName = "SidebarMenuSub"

export {
  Sidebar,
  SidebarContent,
  SidebarFooter,
  SidebarGroup,
  SidebarGroupContent,
  SidebarHeader,
  SidebarInset,
  SidebarLink,
  SidebarMenu,
  SidebarMenuItem,
  SidebarMenuSub,
  SidebarProvider,
  SidebarSubLink,
  SidebarTrigger,
  useSidebar,
}
