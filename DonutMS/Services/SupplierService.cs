using AutoMapper;
using Microsoft.Extensions.Logging;
using DonutMS.Data.Entities;
using DonutMS.Data.Repositories;
using DonutMS.Models.DTOs;

namespace DonutMS.Services;

public interface ISupplierService
{
    Task<IEnumerable<SupplierDto>> GetAllSuppliersAsync();
    Task<IEnumerable<SupplierDto>> GetActiveSuppliersAsync();
    Task<SupplierDto?> GetSupplierByIdAsync(int id);
}

public class SupplierService : ISupplierService
{
    private readonly IRepository<Supplier> _supplierRepository;
    private readonly ILogger<SupplierService> _logger;
    private readonly IMapper _mapper;

    public SupplierService(
        IRepository<Supplier> supplierRepository,
        ILogger<SupplierService> logger,
        IMapper mapper)
    {
        _supplierRepository = supplierRepository;
        _logger = logger;
        _mapper = mapper;
    }

    public async Task<IEnumerable<SupplierDto>> GetAllSuppliersAsync()
    {
        var suppliers = await _supplierRepository.GetAllAsync();
        return _mapper.Map<IEnumerable<SupplierDto>>(suppliers);
    }

    public async Task<IEnumerable<SupplierDto>> GetActiveSuppliersAsync()
    {
        var suppliers = await _supplierRepository.FindAsync(s => s.IsActive && !s.IsDeleted);
        return _mapper.Map<IEnumerable<SupplierDto>>(suppliers);
    }

    public async Task<SupplierDto?> GetSupplierByIdAsync(int id)
    {
        var supplier = await _supplierRepository.GetByIdAsync(id);
        return supplier != null ? _mapper.Map<SupplierDto>(supplier) : null;
    }
}
