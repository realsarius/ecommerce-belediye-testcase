export type SeedTestUserRole = 'Admin' | 'Seller' | 'Support' | 'Customer';

export type SeedTestUser = {
  id: number;
  role: SeedTestUserRole;
  email: string;
  password: string;
  description: string;
};

export const seedTestUsers: SeedTestUser[] = [
  {
    id: 1,
    role: 'Admin',
    email: 'testadmin@test.com',
    password: 'Test123!',
    description: 'Tüm yetkilere sahip yönetici hesabı',
  },
  {
    id: 3,
    role: 'Seller',
    email: 'testseller@test.com',
    password: 'Test123!',
    description: 'Satıcı hesabı - Ürün ve sipariş yönetimi',
  },
  {
    id: 4,
    role: 'Support',
    email: 'support@test.com',
    password: 'Test123!',
    description: 'Canlı destek temsilcisi hesabı',
  },
  {
    id: 2,
    role: 'Customer',
    email: 'customer@test.com',
    password: 'Test123!',
    description: 'Standart müşteri hesabı',
  },
];

export const supportAssignableUsers = seedTestUsers.filter((user) => user.role === 'Support');
