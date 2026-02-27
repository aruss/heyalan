import type { HTMLAttributes, ReactElement } from "react";
import { cn } from "@/lib/utils";

export function Badge({ className, ...props }: HTMLAttributes<HTMLSpanElement>): ReactElement {
    return (
        <span
            className={cn(
                "inline-flex items-center rounded-md bg-slate-100 px-2 py-1 text-xs font-medium text-slate-700",
                className,
            )}
            {...props}
        />
    );
}
