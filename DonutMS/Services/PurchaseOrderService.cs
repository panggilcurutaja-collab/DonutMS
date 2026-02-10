using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DonutMS.Data.Entities;
using DonutMS.Data.Repositories;
using DonutMS.Models.DTOs;

namespace DonutMS.Services;

public interface IPurchaseOrderService
{
    Task<PurchaseOrderDto> CreatePurchaseOrderAsync(CreatePurchaseOrderDto dto);
    Task<PurchaseOrderDto?> GetPurchaseOrderAsync(int id);
    Task<IEnumerable<PurchaseOrderDto>> GetAllPurchaseOrdersAsync();
    Task<IEnumerable<PurchaseOrderDto>> GetPendingOrdersAsync();
    Task<bool> ConfirmPurchaseOrderAsync(int id);
    Task<bool> ReceivePurchaseOrderAsync(int id);
    Task<PurchaseOrderDto?> ReceivePurchaseOrderAsync(CreatePurchaseOrderReceivingDto dto);
    Task<bool> CancelPurchaseOrderAsync(int id);
    Task<IEnumerable<PurchaseOrderDto>> AutoGeneratePurchaseOrdersAsync(int? supplierId = null);
}

public class PurchaseOrderService : IPurchaseOrderService
{
    private readonly IPurchaseOrderRepository _poRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IIngredientRepository _ingredientRepository;
    private readonly IRepository<Supplier> _supplierRepository;
    private readonly IRepository<PurchaseOrderReceiving> _receivingRepository;
    private readonly IInventoryService _inventoryService;
    private readonly ILogger<PurchaseOrderService> _logger;
    private readonly IMapper _mapper;

    public PurchaseOrderService(
        IPurchaseOrderRepository poRepository,
        IInventoryRepository inventoryRepository,
        IIngredientRepository ingredientRepository,
        IRepository<Supplier> supplierRepository,
        IRepository<PurchaseOrderReceiving> receivingRepository,
        IInventoryService inventoryService,
        ILogger<PurchaseOrderService> logger,
        IMapper mapper)
    {
        _poRepository = poRepository;
        _inventoryRepository = inventoryRepository;
        _ingredientRepository = ingredientRepository;
        _supplierRepository = supplierRepository;
        _receivingRepository = receivingRepository;
        _inventoryService = inventoryService;
        _logger = logger;
        _mapper = mapper;
    }

    public async Task<PurchaseOrderDto> CreatePurchaseOrderAsync(CreatePurchaseOrderDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.PONumber))
        {
            dto.PONumber = await GeneratePONumberAsync();
        }

        var po = _mapper.Map<PurchaseOrder>(dto);
        po.Status = "Draft";
        po.OrderDate = DateTime.UtcNow;

        var items = new List<PurchaseOrderItem>();
        decimal totalAmount = 0;
        int lineNumber = 1;

        foreach (var item in dto.Items)
        {
            var poItem = _mapper.Map<PurchaseOrderItem>(item);
            poItem.LineNumber = lineNumber++;
            poItem.LineTotal = item.OrderedQuantity * item.UnitPrice;
            totalAmount += poItem.LineTotal;
            items.Add(poItem);
        }

        po.Items = items;
        po.TotalAmount = totalAmount;

        await _poRepository.AddAsync(po);
        await _poRepository.SaveChangesAsync();

        _logger.LogInformation($"PO {po.PONumber} created with {items.Count} items, total: {totalAmount}");
        return _mapper.Map<PurchaseOrderDto>(po);
    }

    public async Task<PurchaseOrderDto?> GetPurchaseOrderAsync(int id)
    {
        var po = await _poRepository.GetWithItemsAsync(id);
        return po != null ? _mapper.Map<PurchaseOrderDto>(po) : null;
    }

    public async Task<IEnumerable<PurchaseOrderDto>> GetPendingOrdersAsync()
    {
        var pos = await _poRepository.GetPendingAsync();
        return _mapper.Map<IEnumerable<PurchaseOrderDto>>(pos);
    }

    public async Task<IEnumerable<PurchaseOrderDto>> GetAllPurchaseOrdersAsync()
    {
        var pos = await _poRepository
            .AsQueryable()
            .Include(po => po.Supplier)
            .Include(po => po.Items)
            .ThenInclude(i => i.Ingredient)
            .Include(po => po.Items)
            .ThenInclude(i => i.Unit)
            .Where(po => !po.IsDeleted)
            .OrderByDescending(po => po.OrderDate)
            .ToListAsync();

        return _mapper.Map<IEnumerable<PurchaseOrderDto>>(pos);
    }

    public async Task<bool> ConfirmPurchaseOrderAsync(int id)
    {
        var po = await _poRepository.GetByIdAsync(id);
        if (po == null || po.Status != "Draft")
            return false;

        po.Status = "Confirmed";
        await _poRepository.UpdateAsync(po);
        await _poRepository.SaveChangesAsync();

        _logger.LogInformation($"PO {po.PONumber} confirmed");
        return true;
    }

    public async Task<bool> ReceivePurchaseOrderAsync(int id)
    {
        var po = await _poRepository.GetWithItemsAsync(id);
        if (po == null)
            return false;

        po.Status = "Received";
        po.ActualDeliveryDate = DateTime.UtcNow;

        foreach (var item in po.Items!)
        {
            item.Status = "Received";
            // Would normally update inventory here via IInventoryService
        }

        await _poRepository.UpdateAsync(po);
        await _poRepository.SaveChangesAsync();

        _logger.LogInformation($"PO {po.PONumber} received");
        return true;
    }

    public async Task<PurchaseOrderDto?> ReceivePurchaseOrderAsync(CreatePurchaseOrderReceivingDto dto)
    {
        var po = await _poRepository.GetWithItemsAsync(dto.PurchaseOrderId);
        if (po == null || po.Items == null)
            return null;

        if (po.Status == "Cancelled")
            throw new InvalidOperationException("Cannot receive a cancelled PO");

        var receiving = _mapper.Map<PurchaseOrderReceiving>(dto);
        receiving.ReceivingDate = dto.ReceivingDate == default ? DateTime.UtcNow : dto.ReceivingDate;

        decimal totalReceived = 0m;
        var detailEntities = new List<PurchaseOrderReceivingDetail>();

        foreach (var detail in dto.Details)
        {
            if (detail.ReceivedQuantity <= 0)
                continue;

            var item = po.Items.FirstOrDefault(i => i.Id == detail.PurchaseOrderItemId);
            if (item == null)
                continue;

            var entity = _mapper.Map<PurchaseOrderReceivingDetail>(detail);
            totalReceived += detail.ReceivedQuantity;

            item.ReceivedQuantity = (item.ReceivedQuantity ?? 0) + detail.ReceivedQuantity;
            if (item.ReceivedQuantity >= item.OrderedQuantity)
            {
                item.Status = "Received";
            }
            else
            {
                item.Status = "Partial";
            }

            detailEntities.Add(entity);

            await _inventoryService.AddStockInAsync(
                item.IngredientId,
                detail.ReceivedQuantity,
                item.UnitId,
                receiving.ReceivingDate,
                detail.ExpiryDate,
                detail.SupplierBatchNumber,
                po.PONumber,
                po.Id);
        }

        receiving.TotalReceivedQuantity = totalReceived;
        receiving.Details = detailEntities;

        await _receivingRepository.AddAsync(receiving);

        var allReceived = po.Items.All(i => (i.ReceivedQuantity ?? 0) >= i.OrderedQuantity);
        po.Status = allReceived ? "Received" : "Partially Received";
        po.ActualDeliveryDate = allReceived ? receiving.ReceivingDate : po.ActualDeliveryDate;

        await _poRepository.UpdateAsync(po);
        await _poRepository.SaveChangesAsync();
        await _receivingRepository.SaveChangesAsync();

        _logger.LogInformation($"PO {po.PONumber} received (qty: {totalReceived})");
        return _mapper.Map<PurchaseOrderDto>(po);
    }

    public async Task<bool> CancelPurchaseOrderAsync(int id)
    {
        var po = await _poRepository.GetByIdAsync(id);
        if (po == null)
            return false;

        po.Status = "Cancelled";
        await _poRepository.UpdateAsync(po);
        await _poRepository.SaveChangesAsync();

        _logger.LogInformation($"PO {po.PONumber} cancelled");
        return true;
    }

    public async Task<IEnumerable<PurchaseOrderDto>> AutoGeneratePurchaseOrdersAsync(int? supplierId = null)
    {
        var stocks = await _inventoryRepository.AsQueryable()
            .Include(s => s.Ingredient)
            .ThenInclude(i => i.Prices)
            .ThenInclude(p => p.Supplier)
            .ToListAsync();

        var groupedItems = new Dictionary<int, List<CreatePurchaseOrderItemDto>>();

        foreach (var stock in stocks)
        {
            var ingredient = stock.Ingredient;
            if (ingredient == null || !ingredient.IsActive)
                continue;

            if (stock.AvailableQuantity > ingredient.ReorderPoint)
                continue;

            var activePrice = ingredient.Prices?
                .Where(p => p.IsActive && p.EffectiveDate <= DateTime.UtcNow && (!p.EndDate.HasValue || p.EndDate >= DateTime.UtcNow))
                .OrderByDescending(p => p.EffectiveDate)
                .FirstOrDefault();

            var resolvedSupplierId = activePrice?.SupplierId ?? supplierId;
            if (!resolvedSupplierId.HasValue)
                continue;

            if (supplierId.HasValue && resolvedSupplierId != supplierId)
                continue;

            var reorderQty = ingredient.ReorderQuantity > 0
                ? ingredient.ReorderQuantity
                : Math.Max(ingredient.ReorderPoint - stock.AvailableQuantity, 0);

            if (reorderQty <= 0)
                continue;

            var unitId = activePrice?.UnitId ?? ingredient.PurchaseUnitId;
            var unitPrice = activePrice?.Price ?? 0m;

            if (!groupedItems.TryGetValue(resolvedSupplierId.Value, out var list))
            {
                list = new List<CreatePurchaseOrderItemDto>();
                groupedItems[resolvedSupplierId.Value] = list;
            }

            list.Add(new CreatePurchaseOrderItemDto
            {
                IngredientId = ingredient.Id,
                OrderedQuantity = reorderQty,
                UnitId = unitId,
                UnitPrice = unitPrice
            });
        }

        var result = new List<PurchaseOrderDto>();
        foreach (var group in groupedItems)
        {
            var supplier = await _supplierRepository.GetByIdAsync(group.Key);
            var requiredDate = DateTime.UtcNow.AddDays(supplier?.LeadTimeDays ?? 3);

            var poDto = new CreatePurchaseOrderDto
            {
                PONumber = await GeneratePONumberAsync(),
                SupplierId = group.Key,
                RequiredDeliveryDate = requiredDate,
                Items = group.Value
            };

            var created = await CreatePurchaseOrderAsync(poDto);
            result.Add(created);
        }

        return result;
    }

    private async Task<string> GeneratePONumberAsync()
    {
        var today = DateTime.UtcNow.Date;
        var countToday = await _poRepository.AsQueryable()
            .Where(po => po.OrderDate.Date == today)
            .CountAsync();

        return $"PO-{today:yyyyMMdd}-{countToday + 1:000}";
    }
}
