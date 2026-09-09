import { devices, type Locator, type Page } from '@playwright/test';
import { readFileSync } from 'node:fs';
import { expect, test } from '../pageErrors';

const { defaultBrowserType: _defaultBrowserType, ...ipadMini } = devices['iPad Mini'];
const localeManifest = JSON.parse(
  readFileSync(new URL('../../src/i18n/locales.generated.json', import.meta.url), 'utf8'),
) as { locales: Array<{ code: string }> };

test.use(ipadMini);
test.skip(({ browserName }) => browserName !== 'webkit', 'iPad Safari layout is exercised with WebKit');

async function waitForWorkspace(page: Page) {
  await expect(page.locator('.app-shell')).toHaveAttribute('data-workspace-ready', 'true');
  await expect(page.locator('.canvas-stack canvas, .empty-workspace').first()).toBeVisible();
}

async function expectInsideViewport(page: Page, element: Locator) {
  const viewport = page.viewportSize()!;
  const box = await element.boundingBox();
  expect(box, 'element has a rendered box').not.toBeNull();
  expect(box!.x, 'left edge').toBeGreaterThanOrEqual(-1);
  expect(box!.y, 'top edge').toBeGreaterThanOrEqual(-1);
  expect(box!.x + box!.width, 'right edge').toBeLessThanOrEqual(viewport.width + 1);
  expect(box!.y + box!.height, 'bottom edge').toBeLessThanOrEqual(viewport.height + 1);
  await expect.poll(() => element.evaluate((node) => node.scrollWidth <= node.clientWidth + 1)).toBe(true);
}

async function expectMenuFullyReachable(page: Page, popover: Locator) {
  await expect(popover).toBeVisible();
  await expectInsideViewport(page, popover);
  const lastItem = popover.getByRole('menuitem').last();
  await lastItem.scrollIntoViewIfNeeded();
  await expect(lastItem).toBeInViewport();
  const [popoverBox, itemBox] = await Promise.all([popover.boundingBox(), lastItem.boundingBox()]);
  expect(popoverBox).not.toBeNull();
  expect(itemBox).not.toBeNull();
  expect(itemBox!.x).toBeGreaterThanOrEqual(popoverBox!.x - 1);
  expect(itemBox!.x + itemBox!.width).toBeLessThanOrEqual(popoverBox!.x + popoverBox!.width + 1);
  expect(itemBox!.y + itemBox!.height).toBeLessThanOrEqual(popoverBox!.y + popoverBox!.height + 1);
}

test.describe('iPad Safari menus', () => {
  for (const viewport of [
    { name: 'portrait', width: 768, height: 1024 },
    { name: 'landscape', width: 1024, height: 768 },
  ]) {
    test(`keeps every ${viewport.name} top-level popup visible and scroll-reachable`, async ({ page }) => {
      await page.setViewportSize(viewport);
      await page.goto('/');
      await waitForWorkspace(page);

      const menuBar = page.locator('.macos-menu-bar');
      await expect(menuBar).toBeVisible();
      await expect.poll(() => menuBar.evaluate((node) => node.scrollWidth <= node.clientWidth + 1)).toBe(true);

      const menuNames = await page
        .locator('.macos-menu-button')
        .evaluateAll((buttons) => buttons.map((button) => (button as HTMLElement).dataset.menuName!));
      for (const name of menuNames) {
        await page.keyboard.press('Escape');
        const button = page.locator(`.macos-menu-button[data-menu-name="${name}"]`);
        await expect(button).toBeInViewport();
        await button.click();
        await expectMenuFullyReachable(page, button.locator('..').locator('.macos-menu-popover'));
      }
    });
  }

  for (const { code } of localeManifest.locales) {
    test(`${code}: keeps every localized menu reachable at tablet widths`, async ({ page }) => {
      await page.setViewportSize({ width: 768, height: 1024 });
      await page.goto(code === 'en' ? '/' : `/${code}/`);
      await waitForWorkspace(page);

      for (const width of [768, 694]) {
        await page.setViewportSize({ width, height: 1024 });
        const menuBar = page.locator('.macos-menu-bar');
        await expect(menuBar, `${code} menu bar`).toBeVisible();
        await expect
          .poll(() => menuBar.evaluate((node) => node.scrollWidth <= node.clientWidth + 1), {
            message: `${code} menu bar fits ${width}px`,
          })
          .toBe(true);
        for (const button of await page.locator('.macos-menu-button').all()) await expect(button).toBeInViewport();

        // LTR Help and RTL Pinta occupy the physical viewport edges and exercise both alignment rules.
        const edgeName = (await page.locator('html').getAttribute('dir')) === 'rtl' ? 'pinta' : 'help';
        const edgeButton = page.locator(`.macos-menu-button[data-menu-name="${edgeName}"]`);
        await page.keyboard.press('Escape');
        await edgeButton.click();
        await expectMenuFullyReachable(page, edgeButton.locator('..').locator('.macos-menu-popover'));
      }
    });
  }

  test('keeps header menus complete in narrow iPad split view', async ({ page }) => {
    await page.setViewportSize({ width: 507, height: 768 });
    await page.goto('/');
    await waitForWorkspace(page);
    await expect(page.locator('.macos-menu-bar')).toBeHidden();

    for (const name of ['Effects', 'Main Menu']) {
      await page.keyboard.press('Escape');
      const button = page.locator('.header-cluster-end').getByRole('button', { name, exact: true });
      await button.click();
      await expectMenuFullyReachable(page, button.locator('..').locator('.popover'));
    }
  });
});
