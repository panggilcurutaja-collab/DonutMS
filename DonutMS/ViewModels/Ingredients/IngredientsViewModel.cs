using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using DonutMS.Core.MVVM;
using DonutMS.Models.DTOs;
using DonutMS.Services;

namespace DonutMS.ViewModels;

public partial class IngredientsViewModel : BaseViewModel
{
    private readonly IIngredientService _ingredientService;
    private readonly IUnitService _unitService;
    private readonly ISupplierService _supplierService;

    [ObservableProperty]
    private ObservableCollection<IngredientDto> ingredients = new();

    [ObservableProperty]
    private ObservableCollection<UnitDto> availableUnits = new();

    [ObservableProperty]
    private ObservableCollection<UnitDto> units = new();

    [ObservableProperty]
    private ObservableCollection<SupplierDto> suppliers = new();

    [ObservableProperty]
    private ObservableCollection<IngredientPriceDto> ingredientPrices = new();

    [ObservableProperty]
    private IngredientDto? selectedIngredient;

    [ObservableProperty]
    private UnitDto? selectedUnit;

    [ObservableProperty]
    private SupplierDto? selectedSupplier;

    [ObservableProperty]
    private IngredientPriceDto? selectedPrice;

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private string unitSearchText = string.Empty;

    [ObservableProperty]
    private string supplierSearchText = string.Empty;

    [ObservableProperty]
    private string priceSearchText = string.Empty;

    [ObservableProperty]
    private string filterCategory = "All";

    [ObservableProperty]
    private bool showInactiveOnly = false;

    [ObservableProperty]
    private bool showInactiveUnits = false;

    [ObservableProperty]
    private bool showInactiveSuppliers = false;

    [ObservableProperty]
    private bool showInactivePrices = false;

    [ObservableProperty]
    private IngredientDto? editingIngredient;

    [ObservableProperty]
    private UnitDto? editingUnit;

    [ObservableProperty]
    private SupplierDto? editingSupplier;

    [ObservableProperty]
    private IngredientPriceDto? editingPrice;

    [ObservableProperty]
    private bool isDialogOpen = false;

    [ObservableProperty]
    private string dialogTitle = "Add Ingredient";

    [ObservableProperty]
    private string dialogMode = "Ingredient";

    [ObservableProperty]
    private ObservableCollection<string> unitCategories = new();

    public IngredientsViewModel(
        IIngredientService ingredientService,
        IUnitService unitService,
        ISupplierService supplierService,
        ILogger<IngredientsViewModel> logger) : base(logger)
    {
        _ingredientService = ingredientService;
        _unitService = unitService;
        _supplierService = supplierService;

        UnitCategories = new ObservableCollection<string>
        {
            "Weight",
            "Volume",
            "Count",
            "Package",
            "Production",
            "Other"
        };
    }

    [RelayCommand]
    public async Task LoadIngredientsAsync()
    {
        try
        {
            IsLoading = true;
            ClearError();

            var ingredients = await _ingredientService.GetAllIngredientsAsync();
            var filteredIngredients = ingredients
                .Where(i => ShowInactiveOnly ? !i.IsActive : i.IsActive)
                .ToList();
            Ingredients = new ObservableCollection<IngredientDto>(filteredIngredients);

            var units = await _unitService.GetAllUnitsAsync();
            var activeUnits = units.Where(u => u.IsActive).ToList();
            AvailableUnits = new ObservableCollection<UnitDto>(activeUnits);
            Units = new ObservableCollection<UnitDto>(
                units.Where(u => ShowInactiveUnits ? !u.IsActive : u.IsActive));

            var suppliers = await _supplierService.GetAllSuppliersAsync();
            Suppliers = new ObservableCollection<SupplierDto>(
                suppliers.Where(s => ShowInactiveSuppliers ? !s.IsActive : s.IsActive));

            if (SelectedIngredient != null)
            {
                await LoadPricesForSelectedIngredientAsync();
            }

            LogInfo($"Loaded {ingredients.Count()} ingredients");
        }
        catch (Exception ex)
        {
            SetError($"Error loading ingredients: {ex.Message}");
            LogError($"LoadIngredientsAsync failed: {ex}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task SearchIngredientsAsync()
    {
        try
        {
            ClearError();

            var allIngredients = await _ingredientService.GetAllIngredientsAsync();
            
            var filtered = allIngredients
                .Where(i => ShowInactiveOnly ? !i.IsActive : i.IsActive)
                .Where(i => string.IsNullOrEmpty(SearchText) || 
                    i.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    i.SKU.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                .ToList();

            Ingredients = new ObservableCollection<IngredientDto>(filtered);
            LogInfo($"Found {filtered.Count} ingredients matching criteria");
        }
        catch (Exception ex)
        {
            SetError($"Error searching ingredients: {ex.Message}");
        }
    }

    [RelayCommand]
    public void OpenAddDialog()
    {
        EditingIngredient = new IngredientDto { IsActive = true };
        DialogTitle = "Add New Ingredient";
        DialogMode = "Ingredient";
        IsDialogOpen = true;
        ClearError();
    }

    [RelayCommand]
    public void OpenEditDialog(IngredientDto ingredient)
    {
        if (ingredient == null)
        {
            SetError("Please select an ingredient to edit");
            return;
        }

        EditingIngredient = new IngredientDto
        {
            Id = ingredient.Id,
            Name = ingredient.Name,
            SKU = ingredient.SKU,
            Description = ingredient.Description,
            Notes = ingredient.Notes,
            ConsumptionUnitId = ingredient.ConsumptionUnitId,
            PurchaseUnitId = ingredient.PurchaseUnitId,
            MinimumStockLevel = ingredient.MinimumStockLevel,
            ReorderPoint = ingredient.ReorderPoint,
            ReorderQuantity = ingredient.ReorderQuantity,
            ShelfLifeDays = ingredient.ShelfLifeDays,
            IsActive = ingredient.IsActive
        };
        
        DialogTitle = $"Edit Ingredient - {ingredient.Name}";
        DialogMode = "Ingredient";
        IsDialogOpen = true;
        ClearError();
    }

    [RelayCommand]
    public async Task SaveIngredientAsync()
    {
        try
        {
            if (EditingIngredient == null)
            {
                SetError("No ingredient data to save");
                return;
            }

            // Validate required fields
            if (string.IsNullOrWhiteSpace(EditingIngredient.Name))
            {
                SetError("Ingredient name is required");
                return;
            }

            if (string.IsNullOrWhiteSpace(EditingIngredient.SKU))
            {
                SetError("SKU is required");
                return;
            }

            IsLoading = true;
            ClearError();

            if (EditingIngredient.Id == 0)
            {
                // Add new ingredient
                var createDto = new CreateIngredientDto
                {
                    Name = EditingIngredient.Name,
                    SKU = EditingIngredient.SKU,
                    Description = EditingIngredient.Description,
                    Notes = EditingIngredient.Notes,
                    ConsumptionUnitId = EditingIngredient.ConsumptionUnitId,
                    PurchaseUnitId = EditingIngredient.PurchaseUnitId,
                    MinimumStockLevel = EditingIngredient.MinimumStockLevel,
                    ReorderPoint = EditingIngredient.ReorderPoint,
                    ReorderQuantity = EditingIngredient.ReorderQuantity,
                    ShelfLifeDays = EditingIngredient.ShelfLifeDays
                };

                var newIngredient = await _ingredientService.CreateIngredientAsync(createDto);
                Ingredients.Add(newIngredient);
                LogInfo($"Ingredient '{newIngredient.Name}' created successfully");
            }
            else
            {
                // Update existing ingredient
                var updateDto = new UpdateIngredientDto
                {
                    Name = EditingIngredient.Name,
                    Description = EditingIngredient.Description,
                    Notes = EditingIngredient.Notes,
                    ConsumptionUnitId = EditingIngredient.ConsumptionUnitId,
                    PurchaseUnitId = EditingIngredient.PurchaseUnitId,
                    MinimumStockLevel = EditingIngredient.MinimumStockLevel,
                    ReorderPoint = EditingIngredient.ReorderPoint,
                    ReorderQuantity = EditingIngredient.ReorderQuantity,
                    ShelfLifeDays = EditingIngredient.ShelfLifeDays,
                    IsActive = EditingIngredient.IsActive
                };

                await _ingredientService.UpdateIngredientAsync(EditingIngredient.Id, updateDto);
                
                // Refresh the list
                await LoadIngredientsAsync();
                LogInfo($"Ingredient '{EditingIngredient.Name}' updated successfully");
            }

            IsDialogOpen = false;
            EditingIngredient = null;
        }
        catch (Exception ex)
        {
            SetError($"Error saving ingredient: {ex.Message}");
            LogError($"SaveIngredientAsync failed: {ex}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task DeleteIngredientAsync(IngredientDto ingredient)
    {
        try
        {
            if (ingredient == null)
            {
                SetError("No ingredient selected");
                return;
            }

            IsLoading = true;
            ClearError();

            await _ingredientService.DeleteIngredientAsync(ingredient.Id);
            Ingredients.Remove(ingredient);
            SelectedIngredient = null;

            LogInfo($"Ingredient '{ingredient.Name}' deleted successfully");
        }
        catch (Exception ex)
        {
            SetError($"Error deleting ingredient: {ex.Message}");
            LogError($"DeleteIngredientAsync failed: {ex}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public void CancelDialog()
    {
        IsDialogOpen = false;
        EditingIngredient = null;
        EditingUnit = null;
        EditingSupplier = null;
        EditingPrice = null;
        ClearError();
    }

    [RelayCommand]
    public async Task SelectIngredientAsync(IngredientDto ingredient)
    {
        try
        {
            ClearError();
            SelectedIngredient = ingredient;
            await LoadPricesForSelectedIngredientAsync();
            LogInfo($"Selected ingredient: {ingredient.Name}");
        }
        catch (Exception ex)
        {
            SetError($"Error selecting ingredient: {ex.Message}");
        }
    }

    partial void OnSelectedIngredientChanged(IngredientDto? value)
    {
        _ = LoadPricesForSelectedIngredientAsync();
    }

    [RelayCommand]
    public async Task SearchUnitsAsync()
    {
        try
        {
            ClearError();

            var allUnits = await _unitService.GetAllUnitsAsync();
            var filtered = allUnits
                .Where(u => ShowInactiveUnits ? !u.IsActive : u.IsActive)
                .Where(u => string.IsNullOrWhiteSpace(UnitSearchText) ||
                    u.Code.Contains(UnitSearchText, StringComparison.OrdinalIgnoreCase) ||
                    u.Name.Contains(UnitSearchText, StringComparison.OrdinalIgnoreCase) ||
                    u.Category.Contains(UnitSearchText, StringComparison.OrdinalIgnoreCase))
                .ToList();

            Units = new ObservableCollection<UnitDto>(filtered);
        }
        catch (Exception ex)
        {
            SetError($"Error searching units: {ex.Message}");
        }
    }

    [RelayCommand]
    public void OpenAddUnitDialog()
    {
        EditingUnit = new UnitDto
        {
            IsActive = true,
            ConversionFactor = 1,
            Category = UnitCategories.FirstOrDefault() ?? "Weight"
        };
        DialogTitle = "Add Unit";
        DialogMode = "Unit";
        IsDialogOpen = true;
        ClearError();
    }

    [RelayCommand]
    public void OpenEditUnitDialog(UnitDto unit)
    {
        if (unit == null)
        {
            SetError("Please select a unit to edit");
            return;
        }

        EditingUnit = new UnitDto
        {
            Id = unit.Id,
            Code = unit.Code,
            Name = unit.Name,
            Description = unit.Description,
            Category = unit.Category,
            ConversionFactor = unit.ConversionFactor,
            BaseUnit = unit.BaseUnit,
            IsActive = unit.IsActive
        };

        DialogTitle = $"Edit Unit - {unit.Code}";
        DialogMode = "Unit";
        IsDialogOpen = true;
        ClearError();
    }

    [RelayCommand]
    public async Task SaveUnitAsync()
    {
        try
        {
            if (EditingUnit == null)
            {
                SetError("No unit data to save");
                return;
            }

            if (string.IsNullOrWhiteSpace(EditingUnit.Code) || string.IsNullOrWhiteSpace(EditingUnit.Name))
            {
                SetError("Unit code and name are required");
                return;
            }

            IsLoading = true;
            ClearError();

            if (EditingUnit.Id == 0)
            {
                var createDto = new CreateUnitDto
                {
                    Code = EditingUnit.Code,
                    Name = EditingUnit.Name,
                    Description = EditingUnit.Description,
                    Category = EditingUnit.Category,
                    ConversionFactor = EditingUnit.ConversionFactor,
                    BaseUnit = EditingUnit.BaseUnit,
                    IsActive = EditingUnit.IsActive
                };

                var created = await _unitService.CreateUnitAsync(createDto);
                Units.Add(created);
                AvailableUnits.Add(created);
            }
            else
            {
                var updateDto = new UpdateUnitDto
                {
                    Code = EditingUnit.Code,
                    Name = EditingUnit.Name,
                    Description = EditingUnit.Description,
                    Category = EditingUnit.Category,
                    ConversionFactor = EditingUnit.ConversionFactor,
                    BaseUnit = EditingUnit.BaseUnit,
                    IsActive = EditingUnit.IsActive
                };

                await _unitService.UpdateUnitAsync(EditingUnit.Id, updateDto);
                await RefreshUnitsAsync();
                await RefreshAvailableUnitsAsync();
            }

            IsDialogOpen = false;
            EditingUnit = null;
        }
        catch (Exception ex)
        {
            SetError($"Error saving unit: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task DeleteUnitAsync(UnitDto unit)
    {
        try
        {
            if (unit == null)
            {
                SetError("No unit selected");
                return;
            }

            IsLoading = true;
            ClearError();

            await _unitService.DeleteUnitAsync(unit.Id);
            Units.Remove(unit);
            AvailableUnits.Remove(unit);
            SelectedUnit = null;
        }
        catch (Exception ex)
        {
            SetError($"Error deleting unit: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task SearchSuppliersAsync()
    {
        try
        {
            ClearError();

            var allSuppliers = await _supplierService.GetAllSuppliersAsync();
            var filtered = allSuppliers
                .Where(s => ShowInactiveSuppliers ? !s.IsActive : s.IsActive)
                .Where(s => string.IsNullOrWhiteSpace(SupplierSearchText) ||
                    s.Name.Contains(SupplierSearchText, StringComparison.OrdinalIgnoreCase) ||
                    (!string.IsNullOrWhiteSpace(s.ContactPerson) && s.ContactPerson.Contains(SupplierSearchText, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrWhiteSpace(s.Email) && s.Email.Contains(SupplierSearchText, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            Suppliers = new ObservableCollection<SupplierDto>(filtered);
        }
        catch (Exception ex)
        {
            SetError($"Error searching suppliers: {ex.Message}");
        }
    }

    [RelayCommand]
    public void OpenAddSupplierDialog()
    {
        EditingSupplier = new SupplierDto { IsActive = true };
        DialogTitle = "Add Supplier";
        DialogMode = "Supplier";
        IsDialogOpen = true;
        ClearError();
    }

    [RelayCommand]
    public void OpenEditSupplierDialog(SupplierDto supplier)
    {
        if (supplier == null)
        {
            SetError("Please select a supplier to edit");
            return;
        }

        EditingSupplier = new SupplierDto
        {
            Id = supplier.Id,
            Name = supplier.Name,
            Address = supplier.Address,
            PhoneNumber = supplier.PhoneNumber,
            Email = supplier.Email,
            ContactPerson = supplier.ContactPerson,
            MinimumOrderQuantity = supplier.MinimumOrderQuantity,
            LeadTimeDays = supplier.LeadTimeDays,
            PaymentTerms = supplier.PaymentTerms,
            IsActive = supplier.IsActive
        };

        DialogTitle = $"Edit Supplier - {supplier.Name}";
        DialogMode = "Supplier";
        IsDialogOpen = true;
        ClearError();
    }

    [RelayCommand]
    public async Task SaveSupplierAsync()
    {
        try
        {
            if (EditingSupplier == null)
            {
                SetError("No supplier data to save");
                return;
            }

            if (string.IsNullOrWhiteSpace(EditingSupplier.Name))
            {
                SetError("Supplier name is required");
                return;
            }

            IsLoading = true;
            ClearError();

            if (EditingSupplier.Id == 0)
            {
                var createDto = new CreateSupplierDto
                {
                    Name = EditingSupplier.Name,
                    Address = EditingSupplier.Address,
                    PhoneNumber = EditingSupplier.PhoneNumber,
                    Email = EditingSupplier.Email,
                    ContactPerson = EditingSupplier.ContactPerson,
                    MinimumOrderQuantity = EditingSupplier.MinimumOrderQuantity,
                    LeadTimeDays = EditingSupplier.LeadTimeDays,
                    PaymentTerms = EditingSupplier.PaymentTerms,
                    IsActive = EditingSupplier.IsActive
                };

                var created = await _supplierService.CreateSupplierAsync(createDto);
                Suppliers.Add(created);
            }
            else
            {
                var updateDto = new UpdateSupplierDto
                {
                    Name = EditingSupplier.Name,
                    Address = EditingSupplier.Address,
                    PhoneNumber = EditingSupplier.PhoneNumber,
                    Email = EditingSupplier.Email,
                    ContactPerson = EditingSupplier.ContactPerson,
                    MinimumOrderQuantity = EditingSupplier.MinimumOrderQuantity,
                    LeadTimeDays = EditingSupplier.LeadTimeDays,
                    PaymentTerms = EditingSupplier.PaymentTerms,
                    IsActive = EditingSupplier.IsActive
                };

                await _supplierService.UpdateSupplierAsync(EditingSupplier.Id, updateDto);
                await RefreshSuppliersAsync();
            }

            IsDialogOpen = false;
            EditingSupplier = null;
        }
        catch (Exception ex)
        {
            SetError($"Error saving supplier: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task DeleteSupplierAsync(SupplierDto supplier)
    {
        try
        {
            if (supplier == null)
            {
                SetError("No supplier selected");
                return;
            }

            IsLoading = true;
            ClearError();

            await _supplierService.DeleteSupplierAsync(supplier.Id);
            Suppliers.Remove(supplier);
            SelectedSupplier = null;
        }
        catch (Exception ex)
        {
            SetError($"Error deleting supplier: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task SearchPricesAsync()
    {
        try
        {
            ClearError();
            await LoadPricesForSelectedIngredientAsync();
        }
        catch (Exception ex)
        {
            SetError($"Error searching prices: {ex.Message}");
        }
    }

    [RelayCommand]
    public void OpenAddPriceDialog()
    {
        if (SelectedIngredient == null)
        {
            SetError("Select an ingredient first");
            return;
        }

        EditingPrice = new IngredientPriceDto
        {
            IngredientId = SelectedIngredient.Id,
            UnitId = SelectedIngredient.PurchaseUnitId,
            EffectiveDate = DateTime.UtcNow,
            IsActive = true
        };

        DialogTitle = $"Add Price - {SelectedIngredient.Name}";
        DialogMode = "Price";
        IsDialogOpen = true;
        ClearError();
    }

    [RelayCommand]
    public void OpenEditPriceDialog(IngredientPriceDto price)
    {
        if (price == null)
        {
            SetError("Please select a price to edit");
            return;
        }

        EditingPrice = new IngredientPriceDto
        {
            Id = price.Id,
            IngredientId = price.IngredientId,
            SupplierId = price.SupplierId,
            SupplierName = price.SupplierName,
            Price = price.Price,
            UnitId = price.UnitId,
            UnitCode = price.UnitCode,
            EffectiveDate = price.EffectiveDate,
            EndDate = price.EndDate,
            IsActive = price.IsActive,
            MinimumQuantity = price.MinimumQuantity,
            Notes = price.Notes
        };

        DialogTitle = "Edit Ingredient Price";
        DialogMode = "Price";
        IsDialogOpen = true;
        ClearError();
    }

    [RelayCommand]
    public async Task SavePriceAsync()
    {
        try
        {
            if (EditingPrice == null)
            {
                SetError("No price data to save");
                return;
            }

            if (EditingPrice.IngredientId <= 0 || EditingPrice.SupplierId <= 0 || EditingPrice.UnitId <= 0)
            {
                SetError("Ingredient, supplier, and unit are required");
                return;
            }

            if (EditingPrice.Price <= 0)
            {
                SetError("Price must be greater than 0");
                return;
            }

            IsLoading = true;
            ClearError();

            if (EditingPrice.Id == 0)
            {
                var createDto = new CreateIngredientPriceDto
                {
                    IngredientId = EditingPrice.IngredientId,
                    SupplierId = EditingPrice.SupplierId,
                    UnitId = EditingPrice.UnitId,
                    Price = EditingPrice.Price,
                    EffectiveDate = EditingPrice.EffectiveDate,
                    EndDate = EditingPrice.EndDate,
                    MinimumQuantity = EditingPrice.MinimumQuantity,
                    Notes = EditingPrice.Notes,
                    IsActive = EditingPrice.IsActive
                };

                await _ingredientService.CreateIngredientPriceAsync(createDto);
            }
            else
            {
                var updateDto = new UpdateIngredientPriceDto
                {
                    Price = EditingPrice.Price,
                    UnitId = EditingPrice.UnitId,
                    EffectiveDate = EditingPrice.EffectiveDate,
                    EndDate = EditingPrice.EndDate,
                    MinimumQuantity = EditingPrice.MinimumQuantity,
                    Notes = EditingPrice.Notes,
                    IsActive = EditingPrice.IsActive
                };

                await _ingredientService.UpdateIngredientPriceAsync(EditingPrice.Id, updateDto);
            }

            await LoadPricesForSelectedIngredientAsync();
            IsDialogOpen = false;
            EditingPrice = null;
        }
        catch (Exception ex)
        {
            SetError($"Error saving price: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task DeletePriceAsync(IngredientPriceDto price)
    {
        try
        {
            if (price == null)
            {
                SetError("No price selected");
                return;
            }

            IsLoading = true;
            ClearError();

            await _ingredientService.DeleteIngredientPriceAsync(price.Id);
            IngredientPrices.Remove(price);
            SelectedPrice = null;
        }
        catch (Exception ex)
        {
            SetError($"Error deleting price: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadPricesForSelectedIngredientAsync()
    {
        if (SelectedIngredient == null)
        {
            IngredientPrices = new ObservableCollection<IngredientPriceDto>();
            return;
        }

        var prices = await _ingredientService.GetIngredientPricesAsync(SelectedIngredient.Id);
        var filtered = prices
            .Where(p => ShowInactivePrices ? !p.IsActive : p.IsActive)
            .Where(p => string.IsNullOrWhiteSpace(PriceSearchText) ||
                p.SupplierName.Contains(PriceSearchText, StringComparison.OrdinalIgnoreCase) ||
                p.UnitCode.Contains(PriceSearchText, StringComparison.OrdinalIgnoreCase))
            .ToList();

        IngredientPrices = new ObservableCollection<IngredientPriceDto>(filtered);
    }

    private async Task RefreshUnitsAsync()
    {
        var units = await _unitService.GetAllUnitsAsync();
        Units = new ObservableCollection<UnitDto>(
            units.Where(u => ShowInactiveUnits ? !u.IsActive : u.IsActive));
    }

    private async Task RefreshAvailableUnitsAsync()
    {
        var units = await _unitService.GetAllUnitsAsync();
        AvailableUnits = new ObservableCollection<UnitDto>(units.Where(u => u.IsActive));
    }

    private async Task RefreshSuppliersAsync()
    {
        var suppliers = await _supplierService.GetAllSuppliersAsync();
        Suppliers = new ObservableCollection<SupplierDto>(
            suppliers.Where(s => ShowInactiveSuppliers ? !s.IsActive : s.IsActive));
    }
}
