import Link from "next/link";
import type { ButtonHTMLAttributes, ComponentProps, ReactElement, ReactNode } from "react";
import { cn } from "@/lib/utils";

type ActionButtonSize = "sm" | "lg";

type SharedActionButtonProps = {
    children: ReactNode;
    className?: string;
    fullWidth?: boolean;
    size?: ActionButtonSize;
};

type ActionLinkProps = SharedActionButtonProps &
    Omit<ComponentProps<typeof Link>, "className" | "children"> & {
        href: string;
    };

type ActionNativeButtonProps = SharedActionButtonProps &
    Omit<ButtonHTMLAttributes<HTMLButtonElement>, "className" | "children"> & {
        href?: undefined;
    };

export type PrimaryActionButtonProps = ActionLinkProps | ActionNativeButtonProps;
export type SecondaryActionButtonProps = ActionLinkProps | ActionNativeButtonProps;

const sizeStyles: Record<ActionButtonSize, string> = {
    lg: "px-8 py-4 text-lg",
    sm: "px-5 py-2.5 text-sm"
};

const primaryBaseStyles =
    "rounded-xl bg-zinc-900 font-medium text-white transition-colors hover:bg-zinc-800 disabled:cursor-not-allowed disabled:bg-zinc-200 disabled:text-zinc-400";

const secondaryBaseStyles =
    "rounded-xl border border-zinc-200 bg-white font-medium text-zinc-900 transition-colors hover:bg-zinc-50 disabled:cursor-not-allowed disabled:border-zinc-200 disabled:text-zinc-400";

const buildClassName = (
    baseStyles: string,
    size: ActionButtonSize,
    fullWidth: boolean,
    className?: string
): string => {
    return cn(baseStyles, sizeStyles[size], fullWidth && "w-full", className);
};

export const PrimaryActionButton = (props: PrimaryActionButtonProps): ReactElement => {
    if (typeof props.href === "string") {
        const { size = "lg", fullWidth = false, className, children, href, ...linkProps } = props;
        const classes = buildClassName(primaryBaseStyles, size, fullWidth, className);
        return (
            <Link href={href} {...linkProps} className={classes}>
                {children}
            </Link>
        );
    }

    const { size = "lg", fullWidth = false, className, children, type = "button", ...buttonProps } = props;
    const classes = buildClassName(primaryBaseStyles, size, fullWidth, className);
    return (
        <button type={type} {...buttonProps} className={classes}>
            {children}
        </button>
    );
};

export const SecondaryActionButton = (props: SecondaryActionButtonProps): ReactElement => {
    if (typeof props.href === "string") {
        const { size = "lg", fullWidth = false, className, children, href, ...linkProps } = props;
        const classes = buildClassName(secondaryBaseStyles, size, fullWidth, className);
        return (
            <Link href={href} {...linkProps} className={classes}>
                {children}
            </Link>
        );
    }

    const { size = "lg", fullWidth = false, className, children, type = "button", ...buttonProps } = props;
    const classes = buildClassName(secondaryBaseStyles, size, fullWidth, className);
    return (
        <button type={type} {...buttonProps} className={classes}>
            {children}
        </button>
    );
};
