using Microsoft.EntityFrameworkCore;
using ShelfOps.Domain.Enums;
using ShelfOps.Domain.Models;
using ShelfOps.Infrastructure.Security;

namespace ShelfOps.Infrastructure.Data.Seed
{
    public class DataSeeder
    {
        private const string AdminPassword = "Admin@123";
        private const string ManagerPassword = "Manager@123";
        private const string CashierPassword = "Cashier@123";

        public static async Task SeedAsync(ShelfOpsDbContext db, IPasswordHasher passwordHasher)
        {
            if (await db.Categories.AnyAsync())
                return;

            // ─── Категории ───
            var categories = new List<Category>
            {
                new("Ноутбуки и ультрабуки", "Стационарные и портативные компьютеры"),
                new("Смартфоны", "Мобильные телефоны и смартфоны"),
                new("Планшеты и электронные книги", "Планшетные компьютеры и ридеры"),
                new("Комплектующие для ПК", "Процессоры, видеокарты, память, накопители"),
                new("Периферия", "Клавиатуры, мыши, мониторы, принтеры"),
                new("Аудио и гарнитуры", "Наушники, колонки, микрофоны"),
                new("Сетевое оборудование", "Роутеры, коммутаторы, адаптеры"),
                new("Аксессуары", "Кабели, чехлы, зарядки, защитные стёкла"),
            };

            db.Categories.AddRange(categories);
            await db.SaveChangesAsync();

            // ─── Склады ───
            var warehouses = new List<Warehouse>
            {
                new("MSK-01", "Основной склад Москва", "ул. Ленина, д. 15, Москва"),
                new("SPB-01", "Склад Санкт-Петербург", "пр. Невский, д. 100, Санкт-Петербург"),
                new("KZN-01", "Склад Казань", "ул. Баумана, д. 50, Казань"),
            };

            db.Warehouses.AddRange(warehouses);
            await db.SaveChangesAsync();

            // ─── Товары (55 шт) ───
            var products = new List<Product>();

            var productDefs = new (string Sku, string Name, decimal PurchasePrice, decimal SalePrice, string Description, int CategoryIndex)[]
            {
                // Ноутбуки (cat 0)
                ("NBS-001", "ASUS ZenBook 14 OLED", 89900m, 104990m, "Ultrabook 14\" OLED i7-1360P", 0),
                ("NBS-002", "Lenovo ThinkPad X1 Carbon Gen 11", 112000m, 134990m, "Бизнес-ноутбук 14\" i7-1365U", 0),
                ("NBS-003", "HP Spectre x360 16", 105000m, 124990m, "Трансформер 16\" 2-в-1 i7-13700H", 0),
                ("NBS-004", "Dell XPS 15", 115000m, 139990m, "Ноутбук 15.6\" OLED i9-13900H", 0),
                ("NBS-005", "Apple MacBook Air M3 15\"", 119000m, 149990m, "Ноутбук 15.3\" M3 8GB/256GB", 0),
                ("NBS-006", "Acer Swift Go 14", 62000m, 74990m, "Ноутбук 14\" OLED Ryzen 5 7530U", 0),
                ("NBS-007", "Huawei MateBook 14s", 75000m, 89990m, "Ноутбук 14.2\" 3:2 i5-12500H", 0),
                // Смартфоны (cat 1)
                ("SMP-001", "Samsung Galaxy S24 Ultra", 85000m, 99990m, "6.8\" 256GB Exynos 2400", 1),
                ("SMP-002", "iPhone 15 Pro Max", 95000m, 119990m, "6.7\" A17 Pro 256GB", 1),
                ("SMP-003", "Xiaomi 14 Pro", 60000m, 74990m, "6.73\" 256GB Snapdragon 8 Gen 3", 1),
                ("SMP-004", "Google Pixel 8 Pro", 70000m, 84990m, "6.7\" 128GB Tensor G3", 1),
                ("SMP-005", "OnePlus 12", 62000m, 74990m, "6.82\" 256GB Snapdragon 8 Gen 3", 1),
                ("SMP-006", "Nothing Phone (2)", 38000m, 46990m, "6.7\" 256GB Snapdragon 8+ Gen 1", 1),
                ("SMP-007", "Honor Magic6 Pro", 65000m, 79990m, "6.8\" 256GB Snapdragon 8 Gen 3", 1),
                // Планшеты (cat 2)
                ("TAB-001", "Apple iPad Air M2 11\"", 52000m, 64990m, "11\" M2 128GB Wi-Fi", 2),
                ("TAB-002", "Samsung Galaxy Tab S9+", 62000m, 74990m, "12.4\" 256GB Wi-Fi Snapdragon 8 Gen 2", 2),
                ("TAB-003", "Xiaomi Pad 6", 28000m, 34990m, "11\" 256GB Snapdragon 870", 2),
                ("TAB-004", "Huawei MatePad Pro 13.2", 55000m, 67990m, "13.2\" OLED 256GB Kirin 9000S", 2),
                ("TAB-005", "Amazon Kindle Scribe", 29000m, 35990m, "10.2\" 16GB электронная книга с ручкой", 2),
                // Комплектующие (cat 3)
                ("CPU-001", "Intel Core i9-14900K", 52000m, 59990m, "24 ядра / 32 потока, до 6.0 GHz", 3),
                ("CPU-002", "AMD Ryzen 9 7950X", 48000m, 56990m, "16 ядер / 32 потока, AM5", 3),
                ("CPU-003", "Intel Core i5-14600K", 25000m, 29990m, "14 ядер / 20 потоков", 3),
                ("GPU-001", "NVIDIA GeForce RTX 4090", 120000m, 149990m, "24GB GDDR6X, AD102", 3),
                ("GPU-002", "NVIDIA GeForce RTX 4070 Ti", 62000m, 74990m, "12GB GDDR6X, AD104", 3),
                ("GPU-003", "AMD Radeon RX 7800 XT", 42000m, 49990m, "16GB GDDR6, Navi 32", 3),
                ("RAM-001", "Kingston Fury 64GB DDR5", 18000m, 22490m, "2×32GB DDR5-5600 CL40", 3),
                ("SSD-001", "Samsung 990 Pro 2TB", 16000m, 19990m, "NVMe M.2 PCIe 4.0", 3),
                ("SSD-002", "WD Black SN850X 1TB", 10500m, 13490m, "NVMe M.2 PCIe 4.0", 3),
                // Периферия (cat 4)
                ("MON-001", "LG UltraGear 27GP850", 32000m, 39990m, "27\" 2560×1440 165Hz IPS", 4),
                ("MON-002", "Dell U2723QE", 42000m, 49990m, "27\" 4K IPS Black USB-C Hub", 4),
                ("KBD-001", "Logitech MX Mechanical", 12000m, 14990m, "Беспроводная механическая клавиатура", 4),
                ("MSE-001", "Logitech MX Master 3S", 6500m, 7990m, "Беспроводная мышь, 8000 DPI", 4),
                ("MSE-002", "Razer DeathAdder V3 Pro", 9000m, 10990m, "Беспроводная игровая мышь", 4),
                ("PRN-001", "Brother HL-L2370DW", 12000m, 14990m, "Монохромный лазерный принтер A4", 4),
                // Аудио (cat 5)
                ("AUD-001", "Sony WH-1000XM5", 22000m, 26990m, "Беспроводные наушники с ANC", 5),
                ("AUD-002", "Apple AirPods Pro 2", 16000m, 19990m, "TWS с активным шумоподавлением USB-C", 5),
                ("AUD-003", "Bose QuietComfort Ultra", 25000m, 29990m, "Беспроводные наушники с Spatial Audio", 5),
                ("AUD-004", "JBL Charge 5", 10000m, 12990m, "Портативная колонка IP67 20ч", 5),
                ("AUD-005", "Logitech G Pro X", 10000m, 12990m, "Игровая гарнитура Blue VO!CE", 5),
                // Сетевое оборудование (cat 6)
                ("NET-001", "TP-Link Archer AX72", 4500m, 5990m, "Wi-Fi 6 AX5400 роутер", 6),
                ("NET-002", "Asus RT-AX88U Pro", 15000m, 18990m, "Wi-Fi 6 AX6000 роутер", 6),
                ("NET-003", "MikroTik hAP ax2", 7000m, 8990m, "Wi-Fi 6 точка доступа для SOHO", 6),
                ("NET-004", "Ubiquiti UniFi 6 Pro", 10000m, 12990m, "Wi-Fi 6 точка доступа PoE", 6),
                ("NET-005", "TP-Link TL-SG108", 2000m, 2990m, "8-портовый гигабитный коммутатор", 6),
                // Аксессуары (cat 7)
                ("ACS-001", "Apple 20W USB-C адаптер", 1500m, 1990m, "Официальный адаптер питания Apple", 7),
                ("ACS-002", "Samsung 45W Super Fast Charging", 2500m, 3290m, "Адаптер с кабелем USB-C 2м", 7),
                ("ACS-003", "Anker PowerLine III 2м", 1200m, 1590m, "USB-C to USB-C кабель 100W", 7),
                ("ACS-004", "Spigen Ultra Hybrid iPhone 15 Pro", 1500m, 1990m, "Прозрачный чехол с защитой", 7),
                ("ACS-005", "Belkin MagSafe 3-в-1", 8000m, 9990m, "Беспроводное зарядное устройство", 7),
            };

            foreach (var def in productDefs)
            {
                products.Add(new Product(
                    def.Sku, def.Name, categories[def.CategoryIndex].Id,
                    def.PurchasePrice, def.SalePrice, def.Description));
            }

            db.Products.AddRange(products);
            await db.SaveChangesAsync();

            // ─── Пользователи ───
            var users = new List<User>
            {
                new("admin", "Администратор системы", passwordHasher.Hash(AdminPassword), UserRole.Administrator),
                new("manager", "Менеджер склада", passwordHasher.Hash(ManagerPassword), UserRole.Manager),
                new("cashier", "Кассир", passwordHasher.Hash(CashierPassword), UserRole.Cashier),
            };

            db.Users.AddRange(users);
            await db.SaveChangesAsync();

            // ─── Остатки на складах ───
            var rng = new Random(42);
            var stockBalances = new List<StockBalance>();

            foreach (var product in products)
            {
                foreach (var warehouse in warehouses.Take(rng.Next(1, 4)))
                {
                    var qty = rng.Next(5, 61);
                    stockBalances.Add(new StockBalance(warehouse.Id, product.Id, qty));
                }
            }

            db.StockBalances.AddRange(stockBalances);
            await db.SaveChangesAsync();

            // ─── Продажи (40 шт) ───
            var sales = new List<Sale>();
            var saleItems = new List<SaleItem>();

            for (int i = 0; i < 40; i++)
            {
                var createdDate = DateTime.UtcNow.AddDays(-rng.Next(1, 90)).AddHours(rng.Next(8, 20)).AddMinutes(rng.Next(0, 60));
                var warehouse = warehouses[rng.Next(warehouses.Count)];
                var cashier = users[rng.Next(users.Count)];

                var sale = new Sale(warehouse.Id, cashier.Id);

                // Fix the CreatedAtUtc via the auto-property backing field
                var saleType = typeof(Sale);
                BindProperty(saleType, sale, "<CreatedAtUtc>k__BackingField", createdDate);

                var itemCount = rng.Next(1, 6);
                decimal subtotal = 0;
                decimal discountTotal = 0;

                for (int j = 0; j < itemCount; j++)
                {
                    var product = products[rng.Next(products.Count)];
                    var qty = rng.Next(1, 4);
                    var unitPrice = product.SalePrice;
                    var discountPerItem = rng.Next(0, 3) == 0 ? rng.Next(0, (int)(unitPrice * 0.15m / 100) * 100) : 0;

                    var saleItem = new SaleItem(sale.Id, product.Id, product.Sku, product.Name,
                        qty, unitPrice, discountPerItem);

                    saleItems.Add(saleItem);
                    subtotal += unitPrice * qty;
                    discountTotal += discountPerItem;

                    // Decrease stock
                    var balance = stockBalances
                        .Where(sb => sb.ProductId == product.Id && sb.WarehouseId == warehouse.Id)
                        .OrderByDescending(sb => sb.UpdatedAtUtc)
                        .FirstOrDefault();

                    if (balance != null && balance.QuantityOnHand >= qty)
                    {
                        balance.Decrease(qty);
                        BindProperty(typeof(StockBalance), balance, "<UpdatedAtUtc>k__BackingField",
                            createdDate.AddMinutes(j + 1));
                    }
                }

                var grandTotal = subtotal - discountTotal;
                sale.Complete(subtotal, discountTotal, grandTotal);

                // Fix CompletedAtUtc
                BindProperty(saleType, sale, "<CompletedAtUtc>k__BackingField", createdDate.AddMinutes(5));

                sales.Add(sale);
            }

            db.Sales.AddRange(sales);
            await db.SaveChangesAsync();

            foreach (var si in saleItems)
                db.SaleItems.Add(si);
            await db.SaveChangesAsync();

            // ─── Движения по остаткам (от продаж) ───
            var stockMovements = new List<StockMovement>();

            foreach (var sale in sales)
            {
                var saleItemList = saleItems.Where(si => si.SaleId == sale.Id).ToList();
                foreach (var si in saleItemList)
                {
                    var movement = new StockMovement(
                        sale.WarehouseId, si.ProductId, MovementType.Sale, si.Quantity,
                        ReferenceType.Sale, sale.Id, sale.CashierUserId);
                    BindProperty(typeof(StockMovement), movement, "<CreatedAtUtc>k__BackingField",
                        sale.CreatedAtUtc.AddMinutes(1));
                    stockMovements.Add(movement);
                }
            }

            db.StockMovements.AddRange(stockMovements);
            await db.SaveChangesAsync();

            // ─── Audit log ───
            var auditLogs = new List<AuditLog>
            {
                new(users[0].Id, "seed", "Database", "init", "Initial data seeding completed"),
                new(users[0].Id, "create", "User", users[0].Id.ToString(), "Created admin user"),
                new(users[0].Id, "create", "User", users[1].Id.ToString(), "Created manager user"),
                new(users[0].Id, "create", "User", users[2].Id.ToString(), "Created cashier user"),
            };
            db.AuditLogs.AddRange(auditLogs);
            await db.SaveChangesAsync();

            // ─── Возвраты (3 шт) ───
            var completedSales = sales.Where(s => s.Status == SaleStatus.Completed).ToList();
            if (completedSales.Count >= 3)
            {
                for (int i = 0; i < 3; i++)
                {
                    var saleForReturn = completedSales[i];
                    var returnObj = new CustomerReturn(saleForReturn.Id, users[0].Id, "Брак / неисправность");

                    var saleItem = saleItems
                        .Where(si => si.SaleId == saleForReturn.Id)
                        .FirstOrDefault();
                    if (saleItem != null)
                    {
                        var returnItem = new ReturnItem(
                            returnObj.Id, saleItem.Id,
                            1, saleItem.UnitPrice * 0.9m,
                            RestockDisposition.Restock);
                        returnObj.AddItem(returnItem);
                        returnObj.Complete(returnItem.LineTotal);

                        // Restock movement
                        var movement = new StockMovement(
                            saleForReturn.WarehouseId, saleItem.ProductId,
                            MovementType.CustomerReturn, 1,
                            ReferenceType.Return, returnObj.Id,
                            users[0].Id, "Возврат по браку");
                        db.StockMovements.Add(movement);

                        // Restore stock
                        var balance = stockBalances
                            .Where(sb => sb.ProductId == saleItem.ProductId && sb.WarehouseId == saleForReturn.WarehouseId)
                            .FirstOrDefault();
                        if (balance != null)
                            balance.Increase(1);
                    }

                    db.Returns.Add(returnObj);
                }
                await db.SaveChangesAsync();
            }
        }

        private static void BindProperty(Type type, object instance, string fieldName, object value)
        {
            var field = type.GetField(fieldName,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            field?.SetValue(instance, value);
        }
    }
}