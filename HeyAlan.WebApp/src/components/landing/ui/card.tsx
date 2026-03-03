import type { HTMLAttributes, ReactElement } from "react";
import { cn } from "@/lib/utils";

export function Card({ className, ...props }: HTMLAttributes<HTMLDivElement>): ReactElement {
    return (
        <div
            className={cn("rounded-xl border border-slate-200 bg-white shadow-sm", className)}
            {...props}
        />
    );
}

export function CardHeader({ className, ...props }: HTMLAttributes<HTMLDivElement>): ReactElement {
    return <div className={cn("px-6 pt-6", className)} {...props} />;
}

export function CardTitle({ className, ...props }: HTMLAttributes<HTMLHeadingElement>): ReactElement {
    return <h3 className={cn("text-base font-semibold text-slate-900", className)} {...props} />;
}

export function CardDescription({
    className,
    ...props
}: HTMLAttributes<HTMLParagraphElement>): ReactElement {
    return <p className={cn("mt-1 text-sm text-slate-500", className)} {...props} />;
}

export function CardContent({ className, ...props }: HTMLAttributes<HTMLDivElement>): ReactElement {
    return <div className={cn("px-6 pb-6 pt-4", className)} {...props} />;
}
