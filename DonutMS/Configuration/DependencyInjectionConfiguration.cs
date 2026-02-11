using Microsoft.Extensions.DependencyInjection;
using AutoMapper;
using DonutMS.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DonutMS.Data.DbContext;
using DonutMS.Data.Repositories;
using DonutMS.Services;
using DonutMS.ViewModels;

namespace DonutMS.Configuration;

public static class DependencyInjectionConfiguration
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, string? connectionString = null)
    {
        connectionString ??= "Data Source=donutms.db";

        // Database
        services.AddDbContext<DonutMSDbContext>(options =>
        {
            options.UseSqlite(connectionString);
        });

        // AutoMapper
        services.AddAutoMapper(typeof(AutoMapperProfile));

        // Validation
        services.AddValidation();

        // Database Initialization
        services.AddScoped<DonutMS.Data.DbContext.DatabaseInitializer>();

        // Generic Repository
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        // Specialized Repositories
        services.AddScoped<IRecipeRepository, RecipeRepository>();
        services.AddScoped<IIngredientRepository, IngredientRepository>();
        services.AddScoped<IInventoryRepository, InventoryRepository>();
        services.AddScoped<IProductionRepository, ProductionRepository>();
        services.AddScoped<IPurchaseOrderRepository, PurchaseOrderRepository>();
        services.AddScoped<ISKURepository, SKURepository>();
        services.AddScoped<IAuditRepository, AuditRepository>();

        // Business Services
        services.AddScoped<IRecipeService, RecipeService>();
        services.AddScoped<IIngredientService, IngredientService>();
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<IProductionService, ProductionService>();
        services.AddScoped<ICostCalculationService, CostCalculationService>();
        services.AddScoped<IPricingService, PricingService>();
        services.AddScoped<IPurchaseOrderService, PurchaseOrderService>();
        services.AddScoped<IReportingService, ReportingService>();
        services.AddScoped<IExportService, ExportService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IEncryptionService, EncryptionService>();
        services.AddScoped<IBackupService, BackupService>();
        services.AddScoped<IWindowService, WindowService>();
        services.AddScoped<ICredentialStore, CredentialStore>();
        services.AddScoped<IUnitConversionService, UnitConversionService>();
        services.AddScoped<IUnitService, UnitService>();
        services.AddScoped<ISKUService, SKUService>();
        services.AddScoped<ISupplierService, SupplierService>();
        services.AddScoped<ILaborOverheadService, LaborOverheadService>();
        services.AddScoped<IPromoService, PromoService>();

        // Navigation & UI Services
        services.AddScoped<INavigationService, NavigationService>();
        services.AddSingleton<IViewLocator, ViewLocator>();

        // Views - Register all views so they can be resolved
        services.AddTransient<DonutMS.Views.Main.DashboardView>();
        services.AddTransient<DonutMS.Views.Main.HomeView>();
        services.AddTransient<DonutMS.Views.Ingredients.IngredientListView>();
        services.AddTransient<DonutMS.Views.Recipes.RecipeListView>();
        services.AddTransient<DonutMS.Views.Recipes.SubstitutionManagerView>();
        services.AddTransient<DonutMS.Views.Costing.CostCalculationView>();
        services.AddTransient<DonutMS.Views.Inventory.InventoryView>();
        services.AddTransient<DonutMS.Views.SKUs.SKUMasterView>();
        services.AddTransient<DonutMS.Views.PurchaseOrders.PurchaseOrdersView>();
        services.AddTransient<DonutMS.Views.Production.BatchManagementView>();
        services.AddTransient<DonutMS.Views.Labor.LaborOverheadViewV2>();
        services.AddTransient<DonutMS.Views.Pricing.PricingCalculatorView>();
        services.AddTransient<DonutMS.Views.Promotions.PromoManagerViewV2>();
        services.AddTransient<DonutMS.Views.Reports.ReportsView>();
        services.AddTransient<DonutMS.Views.Admin.SecurityView>();
        services.AddTransient<DonutMS.Views.Auth.RegisterWindow>();
        services.AddTransient<DonutMS.Views.Auth.LoginWindow>();

        // ViewModels - Register with proper dependency resolution
        // Note: NavigationViewModel must be registered first as it's a dependency of MainWindowViewModel
        services.AddScoped<NavigationViewModel>(sp =>
            new NavigationViewModel(
                sp.GetRequiredService<INavigationService>(),
                sp.GetRequiredService<ILogger<NavigationViewModel>>()));
        
        services.AddScoped<MainWindowViewModel>(sp =>
            new MainWindowViewModel(
                sp.GetRequiredService<INavigationService>(),
                sp.GetRequiredService<NavigationViewModel>(),
                sp.GetRequiredService<IWindowService>(),
                sp.GetRequiredService<ILogger<MainWindowViewModel>>()));
        
        services.AddScoped<DashboardViewModel>(sp =>
            new DashboardViewModel(
                sp.GetRequiredService<IReportingService>(),
                sp.GetRequiredService<ICostCalculationService>(),
                sp.GetRequiredService<IInventoryService>(),
                sp.GetRequiredService<IPricingService>(),
                sp.GetRequiredService<ILogger<DashboardViewModel>>()));

        services.AddScoped<HomeViewModel>(sp =>
            new HomeViewModel(
                sp.GetRequiredService<ILogger<HomeViewModel>>()));
        
        services.AddScoped<RecipeEditorViewModel>(sp =>
            new RecipeEditorViewModel(
                sp.GetRequiredService<IRecipeService>(),
                sp.GetRequiredService<IIngredientService>(),
                sp.GetRequiredService<IUnitConversionService>(),
                sp.GetRequiredService<FluentValidation.IValidator<DonutMS.Data.Entities.Recipe>>(),
                sp.GetRequiredService<ILogger<RecipeEditorViewModel>>()));
        
        services.AddScoped<InventoryManagerViewModel>(sp =>
            new InventoryManagerViewModel(
                sp.GetRequiredService<IInventoryService>(),
                sp.GetRequiredService<IIngredientService>(),
                sp.GetRequiredService<ILogger<InventoryManagerViewModel>>()));
        
        services.AddScoped<BatchManagementViewModel>(sp =>
            new BatchManagementViewModel(
                sp.GetRequiredService<IProductionService>(),
                sp.GetRequiredService<IRecipeService>(),
                sp.GetRequiredService<IInventoryService>(),
                sp.GetRequiredService<ILogger<BatchManagementViewModel>>()));
        
        services.AddScoped<PricingCalculatorViewModel>(sp =>
            new PricingCalculatorViewModel(
                sp.GetRequiredService<IPricingService>(),
                sp.GetRequiredService<ICostCalculationService>(),
                sp.GetRequiredService<ISKURepository>(),
                sp.GetRequiredService<ILogger<PricingCalculatorViewModel>>()));
        
        services.AddScoped<IngredientsViewModel>(sp =>
            new IngredientsViewModel(
                sp.GetRequiredService<IIngredientService>(),
                sp.GetRequiredService<IUnitService>(),
                sp.GetRequiredService<ISupplierService>(),
                sp.GetRequiredService<ILogger<IngredientsViewModel>>()));

        services.AddScoped<SubstitutionManagerViewModel>(sp =>
            new SubstitutionManagerViewModel(
                sp.GetRequiredService<IRecipeService>(),
                sp.GetRequiredService<IIngredientService>(),
                sp.GetRequiredService<IUnitConversionService>(),
                sp.GetRequiredService<ILogger<SubstitutionManagerViewModel>>()));

        services.AddScoped<CostCalculationViewModel>(sp =>
            new CostCalculationViewModel(
                sp.GetRequiredService<IRecipeService>(),
                sp.GetRequiredService<ICostCalculationService>(),
                sp.GetRequiredService<ILogger<CostCalculationViewModel>>()));

        services.AddScoped<SKUMasterViewModel>(sp =>
            new SKUMasterViewModel(
                sp.GetRequiredService<ISKUService>(),
                sp.GetRequiredService<IRecipeService>(),
                sp.GetRequiredService<ICostCalculationService>(),
                sp.GetRequiredService<ILogger<SKUMasterViewModel>>()));

        services.AddScoped<PurchaseOrderViewModel>(sp =>
            new PurchaseOrderViewModel(
                sp.GetRequiredService<IPurchaseOrderService>(),
                sp.GetRequiredService<IIngredientService>(),
                sp.GetRequiredService<IUnitConversionService>(),
                sp.GetRequiredService<ISupplierService>(),
                sp.GetRequiredService<ILogger<PurchaseOrderViewModel>>()));

        services.AddScoped<LaborOverheadViewModel>(sp =>
            new LaborOverheadViewModel(
                sp.GetRequiredService<ILaborOverheadService>(),
                sp.GetRequiredService<ILogger<LaborOverheadViewModel>>()));

        services.AddScoped<PromoManagerViewModel>(sp =>
            new PromoManagerViewModel(
                sp.GetRequiredService<IPromoService>(),
                sp.GetRequiredService<IPricingService>(),
                sp.GetRequiredService<ISKURepository>(),
                sp.GetRequiredService<ILogger<PromoManagerViewModel>>()));

        services.AddScoped<ReportsViewModel>(sp =>
            new ReportsViewModel(
                sp.GetRequiredService<IInventoryService>(),
                sp.GetRequiredService<IIngredientService>(),
                sp.GetRequiredService<IPurchaseOrderService>(),
                sp.GetRequiredService<IProductionService>(),
                sp.GetRequiredService<ISKUService>(),
                sp.GetRequiredService<IRecipeService>(),
                sp.GetRequiredService<ICostCalculationService>(),
                sp.GetRequiredService<IExportService>(),
                sp.GetRequiredService<ILogger<ReportsViewModel>>()));

        services.AddScoped<SecurityViewModel>(sp =>
            new SecurityViewModel(
                sp.GetRequiredService<IAuditService>(),
                sp.GetRequiredService<IAuthService>(),
                sp.GetRequiredService<IBackupService>(),
                sp.GetRequiredService<ILogger<SecurityViewModel>>()));

        services.AddScoped<RegisterViewModel>(sp =>
            new RegisterViewModel(
                sp.GetRequiredService<IAuthService>(),
                sp.GetRequiredService<ILogger<RegisterViewModel>>()));

        services.AddScoped<LoginViewModel>(sp =>
            new LoginViewModel(
                sp.GetRequiredService<IAuthService>(),
                sp.GetRequiredService<ICredentialStore>(),
                sp.GetRequiredService<IWindowService>(),
                sp.GetRequiredService<ILogger<LoginViewModel>>()));

        return services;
    }
}




