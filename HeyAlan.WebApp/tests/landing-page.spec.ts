import { expect, test } from "@playwright/test";

const LANDING_TITLE = /HeyAlan Builder/i;
const HERO_HEADING = /Build Worlds\./i;
const HARDWARE_HEADING = "Choose Your Kit";
const HERO_IMAGE_ALT_TEXT = "HeyAlan board preview";
const SHOP_HARDWARE_LINK_NAME = "Shop Hardware";
const SHOP_HARDWARE_LINK_HREF = "#hardware";
const MOBILE_VIEWPORT = { width: 390, height: 844 };
const MOBILE_MENU_BUTTON_LABEL = "Toggle menu";
const MOBILE_MENU_LINK_NAME = "How it Works";

test("landing page renders the hero and hardware sections", async ({ page }) => {
  await page.goto("/");

  await expect(page).toHaveTitle(LANDING_TITLE);

  const heroHeading = page.getByRole("heading", { level: 1, name: HERO_HEADING });
  await expect(heroHeading).toBeVisible();

  const shopHardwareLink = page.getByRole("link", { name: SHOP_HARDWARE_LINK_NAME });
  await expect(shopHardwareLink).toHaveAttribute("href", SHOP_HARDWARE_LINK_HREF);

  const heroImage = page.getByAltText(HERO_IMAGE_ALT_TEXT);
  await expect(heroImage).toBeVisible();

  const hardwareHeading = page.getByRole("heading", { name: HARDWARE_HEADING });
  await expect(hardwareHeading).toBeVisible();
});

test("mobile menu expands when the hamburger button is pressed", async ({ page }) => {
  await page.setViewportSize(MOBILE_VIEWPORT);
  await page.goto("/");

  const menuButton = page.getByRole("button", { name: MOBILE_MENU_BUTTON_LABEL });
  await expect(menuButton).toHaveAttribute("aria-expanded", "false");

  const mobileMenuLink = page.getByRole("link", { name: MOBILE_MENU_LINK_NAME });
  await expect(mobileMenuLink).not.toBeVisible();

  await menuButton.click();

  await expect(menuButton).toHaveAttribute("aria-expanded", "true");
  await expect(mobileMenuLink).toBeVisible();
});
