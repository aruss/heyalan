"use client";

import type { ComponentProps, ReactElement } from "react";
import * as DropdownMenuPrimitive from "@radix-ui/react-dropdown-menu";
import { cn } from "@/lib/utils";

export function DropdownMenu({
    ...props
}: ComponentProps<typeof DropdownMenuPrimitive.Root>): ReactElement {
    return <DropdownMenuPrimitive.Root {...props} />;
}

export function DropdownMenuTrigger({
    ...props
}: ComponentProps<typeof DropdownMenuPrimitive.Trigger>): ReactElement {
    return <DropdownMenuPrimitive.Trigger {...props} />;
}

export function DropdownMenuContent({
    className,
    sideOffset = 8,
    ...props
}: ComponentProps<typeof DropdownMenuPrimitive.Content>): ReactElement {
    return (
        <DropdownMenuPrimitive.Portal>
            <DropdownMenuPrimitive.Content
                sideOffset={sideOffset}
                className={cn(
                    "z-50 min-w-56 origin-[--radix-dropdown-menu-content-transform-origin] rounded-lg border border-slate-200 bg-white p-1.5 text-slate-900 shadow-xl outline-none data-[state=open]:animate-[dropdown-scale-in_140ms_cubic-bezier(0.16,1,0.3,1)] data-[state=closed]:animate-[dropdown-scale-out_90ms_cubic-bezier(0.16,1,0.3,1)]",
                    className,
                )}
                {...props}
            />
        </DropdownMenuPrimitive.Portal>
    );
}

export function DropdownMenuLabel({
    className,
    ...props
}: ComponentProps<typeof DropdownMenuPrimitive.Label>): ReactElement {
    return (
        <DropdownMenuPrimitive.Label
            className={cn("px-2 py-1.5 text-xs font-semibold uppercase tracking-wide text-slate-500", className)}
            {...props}
        />
    );
}

export function DropdownMenuSeparator({
    className,
    ...props
}: ComponentProps<typeof DropdownMenuPrimitive.Separator>): ReactElement {
    return <DropdownMenuPrimitive.Separator className={cn("my-1 h-px bg-slate-200", className)} {...props} />;
}

export function DropdownMenuItem({
    className,
    ...props
}: ComponentProps<typeof DropdownMenuPrimitive.Item>): ReactElement {
    return (
        <DropdownMenuPrimitive.Item
            className={cn(
                "relative flex cursor-default select-none items-center gap-2 rounded-md px-2 py-2 text-sm text-slate-700 outline-none transition-colors data-[disabled]:pointer-events-none data-[disabled]:opacity-50 data-[highlighted]:bg-slate-100 data-[highlighted]:text-slate-900",
                className,
            )}
            {...props}
        />
    );
}
