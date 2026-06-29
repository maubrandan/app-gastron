import { AppRole } from '../../shared/models/auth.models';
import { Product, TableState } from '../../shared/models/resto.models';
import { StaffUser } from '../../shared/models/auth.models';

export interface DemoUser {
  id: string;
  email: string;
  displayName: string;
  roles: string[];
  isActive: boolean;
}

export interface DemoSeedData {
  tables: TableState[];
  products: Product[];
  staff: StaffUser[];
  users: DemoUser[];
}

function table(number: number): TableState {
  return { number, status: 'Libre', rowVersion: '1', activeOrderId: null };
}

function product(
  id: string,
  name: string,
  price: number,
  category: string,
  isActive = true,
): Product {
  return { id, name, price, category, isActive };
}

export function createDemoSeed(): DemoSeedData {
  const tables = Array.from({ length: 12 }, (_, i) => table(i + 1));

  const products: Product[] = [
    product('p1', 'Agua mineral', 1200, 'Bebidas'),
    product('p2', 'Cerveza artesanal', 3500, 'Bebidas'),
    product('p3', 'Vino tinto copa', 4200, 'Bebidas'),
    product('p4', 'Coca cola', 1800, 'Bebidas'),
    product('p5', 'Café espresso', 1500, 'Bebidas'),
    product('p6', 'Milanesa con papas', 8900, 'Platos Principales'),
    product('p7', 'Ensalada César', 6500, 'Platos Principales'),
    product('p8', 'Bife de chorizo', 12500, 'Platos Principales'),
    product('p9', 'Pasta bolognesa', 7800, 'Platos Principales'),
    product('p10', 'Pizza muzzarella', 7200, 'Platos Principales'),
    product('p11', 'Flan casero', 4200, 'Postres'),
    product('p12', 'Helado 2 bochas', 3800, 'Postres'),
    product('p13', 'Brownie con helado', 5100, 'Postres'),
    product('p14', 'Tiramisú', 4800, 'Postres'),
    product('p15', 'Ensalada de frutas', 3600, 'Postres'),
  ];

  const users: DemoUser[] = [
    {
      id: 'u-admin',
      email: 'admin@resto.local',
      displayName: 'Administrador',
      roles: [AppRole.Admin],
      isActive: true,
    },
    {
      id: 'u-encargado',
      email: 'encargado@resto.local',
      displayName: 'Encargado Demo',
      roles: [AppRole.Manager],
      isActive: true,
    },
    {
      id: 'u-mozo1',
      email: 'mozo1@resto.local',
      displayName: 'Mozo Demo',
      roles: [AppRole.Waiter],
      isActive: true,
    },
    {
      id: 'u-mozo2',
      email: 'mozo2@resto.local',
      displayName: 'Mozo 2',
      roles: [AppRole.Waiter],
      isActive: true,
    },
    {
      id: 'u-kitchen',
      email: 'kitchen@resto.local',
      displayName: 'Cocina Kiosk',
      roles: [AppRole.Kitchen],
      isActive: true,
    },
  ];

  const staff: StaffUser[] = users.map(({ id, email, displayName, roles, isActive }) => ({
    id,
    email,
    displayName,
    roles,
    isActive,
  }));

  return { tables, products, staff, users };
}
