import { FaMicrosoft } from "react-icons/fa6";
import type { IconType } from "react-icons";
import { SiGoogle, SiSimpleicons, SiSquare } from "react-icons/si";

const GOOGLE_PROVIDER_NAME = "google";
const MICROSOFT_PROVIDER_NAME = "microsoft";
const SQUARE_PROVIDER_NAME = "square";
const UNKNOWN_PROVIDER_ICON_KEY = "unknown";
const PROVIDER_ICON_BY_NAME: Record<string, IconType> = {
    [GOOGLE_PROVIDER_NAME]: SiGoogle,
    [MICROSOFT_PROVIDER_NAME]: FaMicrosoft,
    [SQUARE_PROVIDER_NAME]: SiSquare,
};

export function resolveProviderIconKey(providerName: string): string {
    const normalizedProviderName = providerName.toLowerCase();
    const iconByName = PROVIDER_ICON_BY_NAME[normalizedProviderName];
    if (!iconByName) {
        return UNKNOWN_PROVIDER_ICON_KEY;
    }

    return normalizedProviderName;
}

export function resolveProviderIcon(providerName: string): IconType {
    const iconKey = resolveProviderIconKey(providerName);
    if (iconKey === UNKNOWN_PROVIDER_ICON_KEY) {
        return SiSimpleicons;
    }

    return PROVIDER_ICON_BY_NAME[iconKey];
}
