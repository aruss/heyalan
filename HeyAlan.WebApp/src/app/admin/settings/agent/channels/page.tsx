"use client"

import { Button } from "@/components/admin/Button";
import { Card } from "@/components/admin/Card";
import { Input } from "@/components/admin/Input";
import { SiGooglemessages, SiTelegram, SiWhatsapp } from "react-icons/si";


export default function SettingsAgentPage() {
  return (
    <>
      <section>
        <form>
          <div className="grid grid-cols-1 gap-x-14 gap-y-8 md:grid-cols-3">
            <div>
              <h2 className="scroll-mt-10 font-semibold text-gray-900 dark:text-gray-50">
                Channels
              </h2>
              <p className="mt-1 text-sm leading-6 text-gray-500">
                Lorem ipsum dolor sit amet, consetetur sadipscing elitr.
              </p>
            </div>
            <div className="md:col-span-2">
              <div className="grid grid-cols-1 gap-4 sm:grid-cols-6">
                <div className="col-span-full sm:col-span-6">

                  <Card className=" mt-4 overflow-hidden border-gray-300 p-0 dark:border-gray-800 ">

                    <div className="overflow-hidden border-l-4  p-6 ">
                      <div className="flex items-center gap-4 pr-4">
                        <SiTelegram
                          className="size-8 shrink-0 "
                          aria-hidden="true"
                        />
                        <div className="truncate">
                          <h4 className="text-sm font-medium capitalize text-gray-900 dark:text-gray-50">
                            Telegram
                          </h4>
                          <p className="text-sm text-gray-600 dark:text-gray-400 truncate">
                            Lorem ipsum dolor sit amet, consetetur sadipscing elitr.
                          </p>
                        </div>
                      </div>
                      <div className="mt-6">
                        <label

                          className="text-sm leading-none text-gray-900 dark:text-gray-50 font-medium"
                        >
                          Telegram token
                        </label>
                        <Input
                          type="text"
                          id="first-name"
                          name="first-name"
                          autoComplete="given-name"
                          placeholder="7326267594:AA3dUee0CjDYCoFVEtemWmpwv3O5WWGpsXE"
                          className="mt-2"
                        />
                      </div>

                      <div className="col-span-full mt-6 flex justify-end gap-4">
                        <Button className="gap-2" variant="primary">
                          Save
                        </Button>
                      </div>

                    </div>
                  </Card>

                  <Card className=" mt-4 overflow-hidden border-gray-300 p-0 dark:border-gray-800 ">

                    <div className="overflow-hidden border-l-4  p-6 ">
                      <div className="flex items-center gap-4 pr-4">
                        <SiWhatsapp
                          className="size-8 shrink-0 "
                          aria-hidden="true"
                        />
                        <div className="truncate">
                          <h4 className="text-sm font-medium capitalize text-gray-900 dark:text-gray-50">
                            WhatsApp
                          </h4>
                          <p className="text-sm text-gray-600 dark:text-gray-400 truncate">
                            Lorem ipsum dolor sit amet, consetetur sadipscing elitr.
                          </p>
                        </div>
                      </div>
                      <div className="mt-6">
                        <label

                          className="text-sm leading-none text-gray-900 dark:text-gray-50 font-medium"
                        >
                          WhatsApp number
                        </label>
                        <Input
                          type="text"
                          id="first-name"
                          name="first-name"
                          autoComplete="given-name"
                          placeholder="323-702-3679"
                          className="mt-2"
                        />
                      </div>

                      <div className="col-span-full mt-6 flex justify-end gap-4">
                        <Button className="gap-2" variant="primary">
                          Save
                        </Button>
                      </div>

                    </div>
                  </Card>

                  <Card className=" mt-4 overflow-hidden border-gray-300 p-0 dark:border-gray-800 ">

                    <div className="overflow-hidden border-l-4  p-6 ">
                      <div className="flex items-center gap-4 pr-4">
                        <SiGooglemessages
                          className="size-8 shrink-0 "
                          aria-hidden="true"
                        />
                        <div className="truncate">
                          <h4 className="text-sm font-medium capitalize text-gray-900 dark:text-gray-50">
                            SMS
                          </h4>
                          <p className="text-sm text-gray-600 dark:text-gray-400 truncate">
                            Lorem ipsum dolor sit amet, consetetur sadipscing elitr.
                          </p>
                        </div>
                      </div>
                      <div className="mt-6">
                        <label

                          className="text-sm leading-none text-gray-900 dark:text-gray-50 font-medium"
                        >
                          Phone number
                        </label>
                        <Input
                          type="text"
                          id="first-name"
                          name="first-name"
                          autoComplete="given-name"
                          placeholder="323-702-3679"
                          className="mt-2"
                        />
                      </div>

                      <div className="col-span-full mt-6 flex justify-end gap-4">
                        <Button className="gap-2" variant="primary">
                          Save
                        </Button>
                      </div>

                    </div>
                  </Card>

                </div>

              </div>
            </div>
          </div>
        </form>
      </section>
    </>
  );
}