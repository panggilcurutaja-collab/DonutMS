using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DonutMS.Data.Entities;

namespace DonutMS.Data.DbContext;

public class DatabaseInitializer
{
    private readonly DonutMSDbContext _context;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(DonutMSDbContext context, ILogger<DatabaseInitializer> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        try
        {
            _logger.LogInformation("Initializing database...");

            await _context.Database.MigrateAsync();

            if (!await _context.Units.AnyAsync())
            {
                await SeedUnitsAsync();
            }

            if (!await _context.Allergens.AnyAsync())
            {
                await SeedAllergensAsync();
            }

            if (!await _context.Suppliers.AnyAsync())
            {
                await SeedSuppliersAsync();
            }

            if (!await _context.Ingredients.AnyAsync())
            {
                await SeedIngredientsAsync();
            }

            if (!await _context.Operators.AnyAsync())
            {
                await SeedOperatorsAsync();
            }

            if (!await _context.Users.AnyAsync())
            {
                await SeedUsersAsync();
            }

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
        var users = new List<User>
        {
            new()
            {
                Username = "admin",
                FullName = "Administrator",
                Email = "admin@donutms.local",
                Role = "Admin",
                IsActive = true
            },
            new()
            {
                Username = "manager",
                FullName = "Production Manager",
                Email = "manager@donutms.local",
                Role = "ProductionManager",
                IsActive = true
            },
            new()
            {
                Username = "operator",
                FullName = "Operator User",
                Email = "operator@donutms.local",
                Role = "Operator",
                IsActive = true
            },
            new()
            {
                Username = "cashier",
                FullName = "Cashier User",
                Email = "cashier@donutms.local",
                Role = "Cashier",
                IsActive = true
            }
        };

        _context.Users.AddRange(users);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Seeded {Count} users", users.Count);
    }
}
