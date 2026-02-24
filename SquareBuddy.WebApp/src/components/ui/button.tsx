import type { ButtonHTMLAttributes, ReactElement } from "react";
import { cn } from "@/lib/utils";

type ButtonVariant = "solid" | "ghost";
type ButtonSize = "sm" | "md";

export interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
    variant?: ButtonVariant;
    size?: ButtonSize;
}

const BUTTON_VARIANT_CLASS: Record<ButtonVariant, string> = {
    solid: "bg-slate-900 text-white hover:bg-slate-800",
    ghost: "bg-transparent text-slate-700 hover:bg-slate-100",
};

const BUTTON_SIZE_CLASS: Record<ButtonSize, string> = {
    sm: "h-8 px-3 text-sm",
    md: "h-10 px-4 text-sm",
};

export function Button({
    className,
    type = "button",
    variant = "solid",
    size = "md",
    ...props
}: ButtonProps): ReactElement {
    return (
        <button
            type={type}
            className={cn(
                "inline-flex items-center justify-center rounded-md border border-transparent font-medium transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-slate-400 focus-visible:ring-offset-2 disabled:pointer-events-none disabled:opacity-50",
                BUTTON_VARIANT_CLASS[variant],
                BUTTON_SIZE_CLASS[size],
                className,
            )}
            {...props}
        />
    );
}
