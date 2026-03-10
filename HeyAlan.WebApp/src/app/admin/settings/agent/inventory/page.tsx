"use client"

import { Alert } from "@/components/admin/alert";

export default function SettingsAgentInventoryPage() {

  return (
    <>
      <section className="mx-4">
        <form >
          <div className="grid grid-cols-1 gap-x-14 gap-y-8 md:grid-cols-3">
            <div>
              <h2 className="scroll-mt-10 text-sm font-semibold text-gray-900 dark:text-gray-50">
                Product Inventory
              </h2>
              <p className="mt-1 text-xs leading-6 text-gray-500">
                Manage the products this agent can access and discuss. Sync your catalog from your external e-commerce platform and choose whether to assign your entire inventory or a specific selection of products to this agent.
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
