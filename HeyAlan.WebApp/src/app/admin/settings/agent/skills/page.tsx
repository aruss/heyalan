"use client"

import { Alert } from "@/components/admin/alert";

export default function SettingsAgentSettingsPage() {

  return (
    <>
      <section className="mx-4">
        <form >
          <div className="grid grid-cols-1 gap-x-14 gap-y-8 md:grid-cols-3">
            <div>
              <h2 className="scroll-mt-10 text-sm font-semibold text-gray-900 dark:text-gray-50">
                Agent Skills
              </h2>
              <p className="mt-1 text-xs leading-6 text-gray-500">
                Define the specific actions and capabilities your AI agent can perform. Enable tools and workflows—such as order tracking, appointment booking, or database search—to help it resolve user requests effectively.
              </p>
            </div>
            <div className="md:col-span-2">
              <div className="grid grid-cols-1 gap-4 sm:grid-cols-6">
                <Alert title="Feature Under Development" className="col-span-full">
                  This feature is not yet implemented. Our team is currently working to deliver it in an upcoming release.
                </Alert>
              </div>
            </div>
          </div>
        </form>
      </section>
    </>
  );
}
