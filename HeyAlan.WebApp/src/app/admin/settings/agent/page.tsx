"use client"

import { Button } from "@/components/admin/Button";
import { Card } from "@/components/admin/Card";
import { Divider } from "@/components/admin/Divider";
import { Input } from "@/components/admin/Input";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/admin/Select";
import { Textarea } from "@/components/admin/Textarea";
import { ArrowDownToDot, Plus, Trash2 } from "lucide-react";
import { SiGooglemessages, SiTelegram, SiWhatsapp } from "react-icons/si";

// https://dashboard.tremor.so/settings/general#
// https://insights.tremor.so/settings/users

const personality: { value: string; label: string }[] = [
  {
    value: "casual",
    label: "Casual",
  },
  {
    value: "balanced",
    label: "Balanced",
  },
  {
    value: "busines",
    label: "Busines",
  },
]

export default function SettingsAgentPage() {
  return (
    <>

      <section>
        <form>
          <div className="grid grid-cols-1 gap-x-14 gap-y-8 md:grid-cols-3">
            <div>
              <h2 className="scroll-mt-10 font-semibold text-gray-900 dark:text-gray-50">
                Personal information
              </h2>
              <p className="mt-1 text-sm leading-6 text-gray-500">
                Lorem ipsum dolor sit amet, consetetur sadipscing elitr.
              </p>
            </div>
            <div className="md:col-span-2">
              <div className="grid grid-cols-1 gap-4 sm:grid-cols-6">
                <div className="col-span-full sm:col-span-3">
                  <label
                    htmlFor="first-name"
                    className="text-sm leading-none text-gray-900 dark:text-gray-50 font-medium"
                  >
                    Agent name
                  </label>
                  <Input
                    type="text"
                    id="first-name"
                    name="first-name"
                    autoComplete="given-name"
                    placeholder="Alan"
                    className="mt-2"
                  />
                </div>
                <div className="col-span-full sm:col-span-3">
                  <label
                    htmlFor="last-name"
                    className="text-sm leading-none text-gray-900 dark:text-gray-50 font-medium"
                  >
                    Personality
                  </label>
                  <Select name="permission" defaultValue="">
                    <SelectTrigger
                      id="new-user-permission"
                      className="mt-2 w-full"
                    >
                      <SelectValue placeholder="Select Personality" />
                    </SelectTrigger>
                    <SelectContent>
                      {personality.map((item) => (
                        <SelectItem key={item.value} value={item.label}>
                          {item.label}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
                <div className="col-span-full">
                  <label
                    htmlFor="email"
                    className="text-sm leading-none text-gray-900 dark:text-gray-50 font-medium"
                  >
                    Personality instructions
                  </label>
                  <Textarea
                    id="comment"
                    name="comment"
                    rows={12}
                    placeholder="Add your instructions..."
                    className="mt-2"
                  />

                </div>
                <div className="col-span-full mt-6 flex justify-end gap-4">
                  <Button className="gap-2" variant="primary">
                    Save settings
                  </Button>
                </div>
              </div>
            </div>
          </div>
        </form>
      </section>

    </>
  );
}



