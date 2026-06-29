import { expect, test } from '@playwright/test';

const DEMO_PASSWORD = 'Resto123!';

test.describe('Autenticación', () => {
  test('login como mozo redirige a la vista de salón', async ({ page }) => {
    await page.goto('login');

    await page.getByLabel('Email').fill('mozo1@resto.local');
    await page.getByLabel('Contraseña').fill(DEMO_PASSWORD);
    await page.getByRole('button', { name: 'Ingresar' }).click();

    await expect(page.getByRole('heading', { name: 'Salón — Mozo' })).toBeVisible();
    await expect(page).toHaveURL(/\/mozo$/);
  });

  test('login como encargado redirige a estación de control', async ({ page }) => {
    await page.goto('login');

    await page.getByLabel('Email').fill('encargado@resto.local');
    await page.getByLabel('Contraseña').fill(DEMO_PASSWORD);
    await page.getByRole('button', { name: 'Ingresar' }).click();

    await expect(page.getByRole('heading', { name: 'Estación de Control — Encargado' })).toBeVisible();
    await expect(page).toHaveURL(/\/encargado$/);
  });

  test('credenciales inválidas muestran error', async ({ page }) => {
    await page.goto('login');

    await page.getByLabel('Email').fill('mozo1@resto.local');
    await page.getByLabel('Contraseña').fill('incorrecta');
    await page.getByRole('button', { name: 'Ingresar' }).click();

    await expect(page.getByText('Credenciales inválidas.')).toBeVisible();
  });
});
