import type { ReactElement } from "react";
import { InviteRedemptionPage } from "./invite-redemption-page";

type InvitePageProps = {
    params: Promise<{
        token: string;
    }>;
};

export default async function InvitePage({ params }: InvitePageProps): Promise<ReactElement> {
    const resolvedParams = await params;
    return <InviteRedemptionPage token={resolvedParams.token} />;
}
