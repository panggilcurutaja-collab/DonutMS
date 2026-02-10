using AutoMapper;
using Microsoft.Extensions.Logging;
using DonutMS.Data.Entities;
using DonutMS.Data.Repositories;
using DonutMS.Models.DTOs;

namespace DonutMS.Services;

public interface IPurchaseOrderService
{
    Task<PurchaseOrderDto> CreatePurchaseOrderAsync(CreatePurchaseOrderDto dto);
    Task<PurchaseOrderDto?> GetPurchaseOrderAsync(int id);
    Task<IEnumerable<PurchaseOrderDto>> GetPendingOrdersAsync();
    Task<bool> ConfirmPurchaseOrderAsync(int id);
    Task<bool> ReceivePurchaseOrderAsync(int id);
    Task<bool> CancelPurchaseOrderAsync(int id);
}

public class PurchaseOrderService : IPurchaseOrderService
{
    private readonly IPurchaseOrderRepository _poRepository;
    private readonly IInventoryService _inventoryService;
    private readonly ILogger<PurchaseOrderService> _logger;
    private readonly IMapper _mapper;

    public PurchaseOrderService(
        IPurchaseOrderRepository poRepository,
        IInventoryService inventoryService,
        ILogger<PurchaseOrderService> logger,
        IMapper mapper)
    {
        _poRepository = poRepository;
        _inventoryService = inventoryService;
        _logger = logger;
        _mapper = mapper;
    }

    public async Task<PurchaseOrderDto> CreatePurchaseOrderAsync(CreatePurchaseOrderDto dto)
    {
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
}
