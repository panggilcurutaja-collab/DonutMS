using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DonutMS.Core.Utils;
using DonutMS.Data.Entities;
using DonutMS.Services;

namespace DonutMS.Data.DbContext;

public class DatabaseInitializer
{
    private readonly DonutMSDbContext _context;
    private readonly ILogger<DatabaseInitializer> _logger;
    private readonly IEncryptionService _encryptionService;

    public DatabaseInitializer(
        DonutMSDbContext context,
        IEncryptionService encryptionService,
        ILogger<DatabaseInitializer> logger)
    {
        _context = context;
        _encryptionService = encryptionService;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        try
        {
            _logger.LogInformation("Initializing database...");

            await _context.Database.MigrateAsync();

            // Seed data disabled by request

            // await NormalizeUserDataAsync(); // seed disabled

            _logger.LogInformation("Database initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while initializing the database");
            throw;
        }
    }

    private async Task SeedUnitsAsync()
    {
        var units = new List<Unit>
        {
            new() { Code = "kg", Name = "Kilogram", Category = "Weight", ConversionFactor = 1, BaseUnit = "g" },
            new() { Code = "g", Name = "Gram", Category = "Weight", ConversionFactor = 0.001m, BaseUnit = "g" },
            new() { Code = "mg", Name = "Milligram", Category = "Weight", ConversionFactor = 0.000001m, BaseUnit = "g" },
            new() { Code = "L", Name = "Liter", Category = "Volume", ConversionFactor = 1, BaseUnit = "ml" },
            new() { Code = "ml", Name = "Milliliter", Category = "Volume", ConversionFactor = 0.001m, BaseUnit = "ml" },
            new() { Code = "cl", Name = "Centiliter", Category = "Volume", ConversionFactor = 0.01m, BaseUnit = "ml" },
            new() { Code = "pcs", Name = "Pieces", Category = "Count", ConversionFactor = 1 },
            new() { Code = "box", Name = "Box", Category = "Package", ConversionFactor = 1 },
            new() { Code = "batch", Name = "Batch", Category = "Production", ConversionFactor = 1 },
        };

        _context.Units.AddRange(units);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Seeded {Count} units", units.Count);
    }

    private async Task SeedAllergensAsync()
    {
        var allergens = new List<Allergen>
        {
            new() { Name = "Peanuts", Category = "Nuts", Description = "Contains peanut allergen" },
            new() { Name = "Tree Nuts", Category = "Nuts", Description = "May contain tree nuts (almonds, walnuts, etc.)" },
            new() { Name = "Milk", Category = "Dairy", Description = "Contains milk/dairy products" },
            new() { Name = "Eggs", Category = "Animal Products", Description = "Contains eggs" },
            new() { Name = "Soy", Category = "Legumes", Description = "Contains soy" },
            new() { Name = "Wheat", Category = "Gluten", Description = "Contains wheat/gluten" },
            new() { Name = "Sesame", Category = "Seeds", Description = "Contains sesame seeds" },
        };

        _context.Allergens.AddRange(allergens);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Seeded {Count} allergens", allergens.Count);
    }

    private async Task SeedSuppliersAsync()
    {
        var suppliers = new List<Supplier>
        {
            new()
            {
                Name = "Premium Flour Supplier",
                Email = "sales@premiumflour.local",
                PhoneNumber = "+62812345678",
                Address = "Jl. Perdagangan 123, Jakarta",
                ContactPerson = "Budi Santoso",
                LeadTimeDays = 3,
                MinimumOrderQuantity = 25,
                PaymentTerms = "Net 30"
            },
            new()
            {
                Name = "Quality Sugar Mills",
                Email = "info@qualitysugar.local",
                PhoneNumber = "+62813456789",
                Address = "Jl. Industri 456, Surabaya",
                ContactPerson = "Siti Nurhaliza",
                LeadTimeDays = 2,
                MinimumOrderQuantity = 50,
                PaymentTerms = "Net 15"
            },
            new()
            {
                Name = "Fresh Eggs Cooperative",
                Email = "contact@fresheggs.local",
                PhoneNumber = "+62814567890",
                Address = "Jl. Peternakan 789, Bandung",
                ContactPerson = "Hendra Wijaya",
                LeadTimeDays = 1,
                MinimumOrderQuantity = 100,
                PaymentTerms = "Cash"
            }
        };

        _context.Suppliers.AddRange(suppliers);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Seeded {Count} suppliers", suppliers.Count);
    }

    private async Task SeedIngredientsAsync()
    {
        var kgUnit = await _context.Units.FirstAsync(u => u.Code == "kg");
        var gUnit = await _context.Units.FirstAsync(u => u.Code == "g");
        var lUnit = await _context.Units.FirstAsync(u => u.Code == "L");
        var mlUnit = await _context.Units.FirstAsync(u => u.Code == "ml");
        var pcsUnit = await _context.Units.FirstAsync(u => u.Code == "pcs");

        var ingredients = new List<Ingredient>
        {
            new()
            {
                Name = "All-Purpose Flour",
                SKU = "ING-001",
                ConsumptionUnitId = gUnit.Id,
                PurchaseUnitId = kgUnit.Id,
                MinimumStockLevel = 5000,
                ReorderPoint = 10000,
                ReorderQuantity = 25,
                ShelfLifeDays = 365
            },
            new()
            {
                Name = "Granulated Sugar",
                SKU = "ING-002",
                ConsumptionUnitId = gUnit.Id,
                PurchaseUnitId = kgUnit.Id,
                MinimumStockLevel = 3000,
                ReorderPoint = 8000,
                ReorderQuantity = 50,
                ShelfLifeDays = 730
            },
            new()
            {
                Name = "Fresh Eggs",
                SKU = "ING-003",
                ConsumptionUnitId = pcsUnit.Id,
                PurchaseUnitId = pcsUnit.Id,
                MinimumStockLevel = 100,
                ReorderPoint = 300,
                ReorderQuantity = 240,
                ShelfLifeDays = 30
            },
            new()
            {
                Name = "Vegetable Oil",
                SKU = "ING-004",
                ConsumptionUnitId = mlUnit.Id,
                PurchaseUnitId = lUnit.Id,
                MinimumStockLevel = 5000,
                ReorderPoint = 15000,
                ReorderQuantity = 20,
                ShelfLifeDays = 365
            },
            new()
            {
                Name = "Vanilla Extract",
                SKU = "ING-005",
                ConsumptionUnitId = mlUnit.Id,
                PurchaseUnitId = mlUnit.Id,
                MinimumStockLevel = 200,
                ReorderPoint = 500,
                ReorderQuantity = 1000,
                ShelfLifeDays = 730
            }
        };

        _context.Ingredients.AddRange(ingredients);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Seeded {Count} ingredients", ingredients.Count);
    }

    private async Task SeedOperatorsAsync()
    {
        var operators = new List<Operator>
        {
            new()
            {
                Name = "Ahmad Kusuma",
                EmployeeId = "EMP-001",
                Email = "ahmad@donutms.local",
                PhoneNumber = "+62812111111",
                JobTitle = "Senior Production Operator",
                HireDate = new DateTime(2022, 1, 15),
                BaseSalary = 5000000,
                SalaryPeriod = "Monthly"
            },
            new()
            {
                Name = "Dewi Lestari",
                EmployeeId = "EMP-002",
                Email = "dewi@donutms.local",
                PhoneNumber = "+62812222222",
                JobTitle = "Production Operator",
                HireDate = new DateTime(2023, 6, 1),
                BaseSalary = 4000000,
                SalaryPeriod = "Monthly"
            },
            new()
            {
                Name = "Rudi Hermawan",
                EmployeeId = "EMP-003",
                Email = "rudi@donutms.local",
                PhoneNumber = "+62812333333",
                JobTitle = "Quality Control Officer",
                HireDate = new DateTime(2023, 3, 10),
                BaseSalary = 4500000,
                SalaryPeriod = "Monthly"
            }
        };

        _context.Operators.AddRange(operators);
        await _context.SaveChangesAsync();

        var operators_list = await _context.Operators.ToListAsync();
        var laborRates = operators_list.Select(op => new LaborRate
        {
            OperatorId = op.Id,
            HourlyRate = op.BaseSalary / 160m,
            OvertimeMultiplier = 1.5m,
            EffectiveDate = DateTime.UtcNow,
            ShiftType = "Regular"
        }).ToList();

        _context.LaborRates.AddRange(laborRates);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Seeded {Count} operators with labor rates", operators.Count);
    }

    private async Task SeedUsersAsync()
    {
        var seedPassword = Environment.GetEnvironmentVariable("DONUTMS_SEED_PASSWORD");
        byte[]? seedHash = null;
        byte[]? seedSalt = null;

        if (!string.IsNullOrWhiteSpace(seedPassword))
        {
            (seedHash, seedSalt) = PasswordHasher.CreateHash(seedPassword);
        }
        else
        {
            _logger.LogWarning("DONUTMS_SEED_PASSWORD not set. Seeded users will have no password until updated.");
        }

        var users = new List<User>
        {
            new()
            {
                Username = "admin",
                FullName = "Administrator",
                Email = _encryptionService.Protect("admin@donutms.local"),
                Role = "Admin",
                IsActive = true,
                PasswordHash = seedHash,
                PasswordSalt = seedSalt
            },
            new()
            {
                Username = "manager",
                FullName = "Production Manager",
                Email = _encryptionService.Protect("manager@donutms.local"),
                Role = "ProduksionManager",
                IsActive = true,
                PasswordHash = seedHash,
                PasswordSalt = seedSalt
            },
            new()
            {
                Username = "operator",
                FullName = "Operator User",
                Email = _encryptionService.Protect("operator@donutms.local"),
                Role = "Operator",
                IsActive = true,
                PasswordHash = seedHash,
                PasswordSalt = seedSalt
            },
            new()
            {
                Username = "cashier",
                FullName = "Cashier User",
                Email = _encryptionService.Protect("cashier@donutms.local"),
                Role = "Kasir",
                IsActive = true,
                PasswordHash = seedHash,
                PasswordSalt = seedSalt
            }
        };

        _context.Users.AddRange(users);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Seeded {Count} users", users.Count);
    }

    private async Task NormalizeUserDataAsync()
    {
        var users = await _context.Users.ToListAsync();
        if (users.Count == 0)
            return;

        var seedPassword = Environment.GetEnvironmentVariable("DONUTMS_SEED_PASSWORD");
        var hasSeedPassword = !string.IsNullOrWhiteSpace(seedPassword);

        var updatedAny = false;
        foreach (var user in users)
        {
            var updated = false;

            if (string.Equals(user.Role, "ProductionManager", StringComparison.OrdinalIgnoreCase))
            {
                user.Role = "ProduksionManager";
                updated = true;
            }

            if (string.Equals(user.Role, "Cashier", StringComparison.OrdinalIgnoreCase))
            {
                user.Role = "Kasir";
                updated = true;
            }

            if (!string.IsNullOrWhiteSpace(user.Email) &&
                !user.Email.StartsWith("enc:", StringComparison.OrdinalIgnoreCase))
            {
                user.Email = _encryptionService.Protect(user.Email);
                updated = true;
            }

            if (!string.IsNullOrWhiteSpace(user.Notes) &&
                !user.Notes.StartsWith("enc:", StringComparison.OrdinalIgnoreCase))
            {
                user.Notes = _encryptionService.Protect(user.Notes);
                updated = true;
            }

            if (hasSeedPassword && (user.PasswordHash == null || user.PasswordSalt == null))
            {
                var (hash, salt) = PasswordHasher.CreateHash(seedPassword!);
                user.PasswordHash = hash;
                user.PasswordSalt = salt;
                updated = true;
            }

            if (updated)
            {
                _context.Users.Update(user);
                updatedAny = true;
            }
        }

        if (updatedAny)
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("Normalized user roles/encryption/passwords");
        }
    }
}
