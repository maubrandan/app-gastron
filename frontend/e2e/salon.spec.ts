import { expect, test } from '@playwright/test';

const DEMO_PASSWORD = 'Resto123!';

async function loginAsWaiter(page: import('@playwright/test').Page): Promise<void> {
  await page.goto('login');
  await page.getByLabel('Email').fill('mozo1@resto.local');
  await page.getByLabel('Contraseña').fill(DEMO_PASSWORD);
  await page.getByRole('button', { name: 'Ingresar' }).click();
  await expect(page.getByRole('heading', { name: 'Salón — Mozo' })).toBeVisible();
}

test.describe('Flujo de mozo (demo)', () => {
  test('abrir mesa libre y agregar producto', async ({ page }) => {
    await loginAsWaiter(page);

    await page.getByRole('button', { name: 'Mesa 1, Libre' }).click();
    await expect(page.getByRole('heading', { name: 'Mesa 1' })).toBeVisible();
    await expect(page.getByText('Agregar productos')).toBeVisible();

    await page.getByRole('button', { name: /Agua mineral/i }).click();

    await expect(page.getByText('1× Agua mineral')).toBeVisible();
    await expect(page.getByText(/Total:/)).toBeVisible();
  });

  test('mozo no accede a vista de encargado', async ({ page }) => {
    await loginAsWaiter(page);

    await page.goto('encargado');

    await expect(page).toHaveURL(/\/mozo$/);
  });
});

test.describe('Monitor de cocina (demo)', () => {
  test('muestra el encabezado del monitor', async ({ page }) => {
    await page.goto('login');
    await page.getByLabel('Email').fill('kitchen@resto.local');
    await page.getByLabel('Contraseña').fill(DEMO_PASSWORD);
    await page.getByRole('button', { name: 'Ingresar' }).click();

    await expect(page.getByRole('heading', { name: 'Monitor de Cocina' })).toBeVisible();
  });
});
