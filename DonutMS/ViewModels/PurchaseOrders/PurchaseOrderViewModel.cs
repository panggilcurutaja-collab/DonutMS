using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using DonutMS.Core.MVVM;
using DonutMS.Models.DTOs;
using DonutMS.Services;

namespace DonutMS.ViewModels;

public partial class PurchaseOrderViewModel : BaseViewModel
{
    private readonly IPurchaseOrderService _purchaseOrderService;
    private readonly IIngredientService _ingredientService;
    private readonly IUnitConversionService _unitService;
    private readonly ISupplierService _supplierService;

    [ObservableProperty]
    private ObservableCollection<PurchaseOrderDto> purchaseOrders = new();

    [ObservableProperty]
    private ObservableCollection<PurchaseOrderItemDto> editingItems = new();

    [ObservableProperty]
    private ObservableCollection<PurchaseOrderReceivingLine> receivingLines = new();

    [ObservableProperty]
    private ObservableCollection<SupplierDto> suppliers = new();

    [ObservableProperty]
    private ObservableCollection<IngredientDto> ingredients = new();

    [ObservableProperty]
    private ObservableCollection<UnitDto> units = new();

    [ObservableProperty]
    private PurchaseOrderDto? selectedPurchaseOrder;

    [ObservableProperty]
    private SupplierDto? selectedSupplier;

    [ObservableProperty]
    private SupplierDto? autoGenerateSupplier;

    [ObservableProperty]
    private IngredientDto? selectedItemIngredient;

    [ObservableProperty]
    private UnitDto? selectedItemUnit;

    [ObservableProperty]
    private decimal itemQuantity;

    [ObservableProperty]
    private decimal itemUnitPrice;

    [ObservableProperty]
    private string? poNumber;

    [ObservableProperty]
    private DateTime requiredDeliveryDate = DateTime.Today.AddDays(3);

    [ObservableProperty]
    private string? deliveryAddress;

    [ObservableProperty]
    private string? paymentStatus = "Unpaid";

    [ObservableProperty]
    private string? notes;

    [ObservableProperty]
    private decimal editingTotalAmount;

    [ObservableProperty]
    private bool isDialogOpen;

    [ObservableProperty]
    private string dialogTitle = "Create Purchase Order";

    [ObservableProperty]
    private string dialogMode = "Create";

    [ObservableProperty]
    private DateTime receivingDate = DateTime.Today;

    [ObservableProperty]
    private string? receivedBy;

    [ObservableProperty]
    private string? receivingNotes;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private int totalOrders;

    [ObservableProperty]
    private int pendingOrders;

    [ObservableProperty]
    private int overdueOrders;

    [ObservableProperty]
    private decimal totalOrderValue;

    public PurchaseOrderViewModel(
        IPurchaseOrderService purchaseOrderService,
        IIngredientService ingredientService,
        IUnitConversionService unitService,
        ISupplierService supplierService,
        ILogger<PurchaseOrderViewModel> logger) : base(logger)
    {
        _purchaseOrderService = purchaseOrderService;
        _ingredientService = ingredientService;
        _unitService = unitService;
        _supplierService = supplierService;
    }

    [RelayCommand]
    public async Task LoadPurchaseOrdersAsync()
    {
        try
        {
            IsLoading = true;
            ClearError();

            var suppliers = await _supplierService.GetActiveSuppliersAsync();
            Suppliers = new ObservableCollection<SupplierDto>(suppliers);

            var ingredients = await _ingredientService.GetAllIngredientsAsync();
            Ingredients = new ObservableCollection<IngredientDto>(ingredients.Where(i => i.IsActive));

            var units = await _unitService.GetAllUnitsAsync();
            Units = new ObservableCollection<UnitDto>(units.Where(u => u.IsActive));

            var orders = await _purchaseOrderService.GetAllPurchaseOrdersAsync();
            PurchaseOrders = new ObservableCollection<PurchaseOrderDto>(orders);

            CalculateSummary();
            StatusMessage = $"Loaded {PurchaseOrders.Count} purchase orders";
        }
        catch (Exception ex)
        {
            SetError($"Error loading purchase orders: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task RefreshAsync()
    {
        await LoadPurchaseOrdersAsync();
    }

    [RelayCommand]
    public void OpenCreateDialog()
    {
        DialogMode = "Create";
        DialogTitle = "Create Purchase Order";
        IsDialogOpen = true;

        PoNumber = string.Empty;
        RequiredDeliveryDate = DateTime.Today.AddDays(3);
        DeliveryAddress = string.Empty;
        Notes = string.Empty;
        PaymentStatus = "Unpaid";
        SelectedSupplier = Suppliers.FirstOrDefault();

        EditingItems = new ObservableCollection<PurchaseOrderItemDto>();
        EditingTotalAmount = 0;
        ClearError();
    }

    [RelayCommand]
    public void AddItem()
    {
        if (SelectedItemIngredient == null)
        {
            SetError("Please select an ingredient");
            return;
        }

        if (SelectedItemUnit == null)
        {
            SetError("Please select a unit");
            return;
        }

        if (ItemQuantity <= 0)
        {
            SetError("Quantity must be greater than 0");
            return;
        }

        ClearError();

        var lineTotal = ItemQuantity * ItemUnitPrice;
        var item = new PurchaseOrderItemDto
        {
            IngredientId = SelectedItemIngredient.Id,
            IngredientName = SelectedItemIngredient.Name,
            UnitId = SelectedItemUnit.Id,
            UnitCode = SelectedItemUnit.Code,
            OrderedQuantity = ItemQuantity,
            UnitPrice = ItemUnitPrice,
            LineTotal = lineTotal,
            LineNumber = EditingItems.Count + 1,
            Status = "Pending"
        };

        EditingItems.Add(item);
        RecalculateTotals();

        ItemQuantity = 0;
        ItemUnitPrice = SelectedItemIngredient.CurrentPrice?.Price ?? ItemUnitPrice;
    }

    [RelayCommand]
    public void RemoveItem(PurchaseOrderItemDto item)
    {
        if (item == null)
            return;

        EditingItems.Remove(item);
        RecalculateTotals();
    }

    [RelayCommand]
    public async Task SavePurchaseOrderAsync()
    {
        try
        {
            if (SelectedSupplier == null)
            {
                SetError("Supplier is required");
                return;
            }

            if (!EditingItems.Any())
            {
                SetError("Please add at least one item");
                return;
            }

            IsLoading = true;
            ClearError();

            var dto = new CreatePurchaseOrderDto
            {
                PONumber = PoNumber ?? string.Empty,
                SupplierId = SelectedSupplier.Id,
                RequiredDeliveryDate = RequiredDeliveryDate,
                Items = EditingItems.Select(i => new CreatePurchaseOrderItemDto
                {
                    IngredientId = i.IngredientId,
                    OrderedQuantity = i.OrderedQuantity,
                    UnitId = i.UnitId,
                    UnitPrice = i.UnitPrice
                }).ToList()
            };

            await _purchaseOrderService.CreatePurchaseOrderAsync(dto);
            IsDialogOpen = false;

            await LoadPurchaseOrdersAsync();
            StatusMessage = "Purchase order created successfully";
        }
        catch (Exception ex)
        {
            SetError($"Error saving purchase order: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public void OpenReceiveDialog(PurchaseOrderDto purchaseOrder)
    {
        if (purchaseOrder == null)
        {
            SetError("Please select a purchase order");
            return;
        }

        if (purchaseOrder.Status == "Cancelled")
        {
            SetError("Cannot receive a cancelled purchase order");
            return;
        }

        SelectedPurchaseOrder = purchaseOrder;
        DialogMode = "Receive";
        DialogTitle = $"Receive PO {purchaseOrder.PONumber}";
        ReceivingDate = DateTime.Today;
        ReceivedBy = string.Empty;
        ReceivingNotes = string.Empty;

        var lines = new ObservableCollection<PurchaseOrderReceivingLine>();
        foreach (var item in purchaseOrder.Items)
        {
            var receivedToDate = item.ReceivedQuantity ?? 0m;
            var remaining = Math.Max(item.OrderedQuantity - receivedToDate, 0);

            lines.Add(new PurchaseOrderReceivingLine
            {
                PurchaseOrderItemId = item.Id,
                IngredientName = item.IngredientName,
                UnitCode = item.UnitCode,
                OrderedQuantity = item.OrderedQuantity,
                ReceivedToDate = receivedToDate,
                RemainingQuantity = remaining,
                ReceiveQuantity = remaining
            });
        }

        ReceivingLines = lines;
        IsDialogOpen = true;
        ClearError();
    }

    [RelayCommand]
    public async Task SubmitReceivingAsync()
    {
        try
        {
            if (SelectedPurchaseOrder == null)
            {
                SetError("No purchase order selected");
                return;
            }

            var details = new List<CreatePurchaseOrderReceivingDetailDto>();
            foreach (var line in ReceivingLines)
            {
                if (line.ReceiveQuantity <= 0)
                    continue;

                if (line.ReceiveQuantity > line.RemainingQuantity)
                {
                    SetError($"Received quantity exceeds remaining for {line.IngredientName}");
                    return;
                }

                details.Add(new CreatePurchaseOrderReceivingDetailDto
                {
                    PurchaseOrderItemId = line.PurchaseOrderItemId,
                    ReceivedQuantity = line.ReceiveQuantity,
                    SupplierBatchNumber = line.SupplierBatchNumber,
                    ExpiryDate = line.ExpiryDate,
                    Notes = null
                });
            }

            if (!details.Any())
            {
                SetError("Please enter received quantities");
                return;
            }

            IsLoading = true;
            ClearError();

            var dto = new CreatePurchaseOrderReceivingDto
            {
                PurchaseOrderId = SelectedPurchaseOrder.Id,
                ReceivingDate = ReceivingDate,
                ReceivedBy = string.IsNullOrWhiteSpace(ReceivedBy) ? null : ReceivedBy,
                Notes = string.IsNullOrWhiteSpace(ReceivingNotes) ? null : ReceivingNotes,
                Details = details
            };

            await _purchaseOrderService.ReceivePurchaseOrderAsync(dto);

            IsDialogOpen = false;
            await LoadPurchaseOrdersAsync();
            StatusMessage = $"PO {SelectedPurchaseOrder.PONumber} received";
        }
        catch (Exception ex)
        {
            SetError($"Error receiving purchase order: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task ConfirmPurchaseOrderAsync(PurchaseOrderDto purchaseOrder)
    {
        if (purchaseOrder == null)
            return;

        if (!string.Equals(purchaseOrder.Status, "Draft", StringComparison.OrdinalIgnoreCase))
        {
            SetError("Only draft purchase orders can be confirmed");
            return;
        }

        try
        {
            IsLoading = true;
            ClearError();

            await _purchaseOrderService.ConfirmPurchaseOrderAsync(purchaseOrder.Id);
            await LoadPurchaseOrdersAsync();
            StatusMessage = $"PO {purchaseOrder.PONumber} confirmed";
        }
        catch (Exception ex)
        {
            SetError($"Error confirming purchase order: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task CancelPurchaseOrderAsync(PurchaseOrderDto purchaseOrder)
    {
        if (purchaseOrder == null)
            return;

        if (string.Equals(purchaseOrder.Status, "Received", StringComparison.OrdinalIgnoreCase))
        {
            SetError("Cannot cancel a received purchase order");
            return;
        }

        try
        {
            IsLoading = true;
            ClearError();

            await _purchaseOrderService.CancelPurchaseOrderAsync(purchaseOrder.Id);
            await LoadPurchaseOrdersAsync();
            StatusMessage = $"PO {purchaseOrder.PONumber} cancelled";
        }
        catch (Exception ex)
        {
            SetError($"Error cancelling purchase order: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task AutoGenerateAsync()
    {
        try
        {
            IsLoading = true;
            ClearError();

            var results = await _purchaseOrderService.AutoGeneratePurchaseOrdersAsync(AutoGenerateSupplier?.Id);
            await LoadPurchaseOrdersAsync();

            var count = results.Count();
            StatusMessage = count > 0
                ? $"Auto-generated {count} purchase order(s)"
                : "No purchase orders generated (stock levels ok)";
        }
        catch (Exception ex)
        {
            SetError($"Error auto-generating POs: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public void CloseDialog()
    {
        IsDialogOpen = false;
        ClearError();
    }

    partial void OnSelectedItemIngredientChanged(IngredientDto? value)
    {
        if (value == null)
            return;

        var unit = Units.FirstOrDefault(u => u.Id == value.PurchaseUnitId);
        if (unit != null)
        {
            SelectedItemUnit = unit;
        }

        if (value.CurrentPrice != null && value.CurrentPrice.Price > 0)
        {
            ItemUnitPrice = value.CurrentPrice.Price;
        }
    }

    private void RecalculateTotals()
    {
        var lineNumber = 1;
        decimal total = 0m;

        foreach (var item in EditingItems)
        {
            item.LineNumber = lineNumber++;
            item.LineTotal = item.OrderedQuantity * item.UnitPrice;
            total += item.LineTotal;
        }

        EditingTotalAmount = total;
    }

    private void CalculateSummary()
    {
        TotalOrders = PurchaseOrders.Count;
        PendingOrders = PurchaseOrders.Count(po => po.Status != "Received" && po.Status != "Cancelled");
        OverdueOrders = PurchaseOrders.Count(po => po.Status != "Received" && po.Status != "Cancelled" && po.RequiredDeliveryDate.Date < DateTime.Today);
        TotalOrderValue = PurchaseOrders.Sum(po => po.TotalAmount);
    }
}

public partial class PurchaseOrderReceivingLine : ObservableObject
{
    [ObservableProperty]
    private int purchaseOrderItemId;

    [ObservableProperty]
    private string ingredientName = string.Empty;

    [ObservableProperty]
    private string unitCode = string.Empty;

    [ObservableProperty]
    private decimal orderedQuantity;

    [ObservableProperty]
    private decimal receivedToDate;

    [ObservableProperty]
    private decimal remainingQuantity;

    [ObservableProperty]
    private decimal receiveQuantity;

    [ObservableProperty]
    private string? supplierBatchNumber;

    [ObservableProperty]
    private DateTime? expiryDate;
}
