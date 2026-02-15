using AutoMapper;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
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
    Task<SupplierDto> CreateSupplierAsync(CreateSupplierDto dto);
    Task<SupplierDto> UpdateSupplierAsync(int id, UpdateSupplierDto dto);
    Task<bool> DeleteSupplierAsync(int id);
}

public class SupplierService : ISupplierService
{
    private readonly IRepository<Supplier> _supplierRepository;
    private readonly IValidator<Supplier> _validator;
    private readonly ILogger<SupplierService> _logger;
    private readonly IMapper _mapper;

    public SupplierService(
        IRepository<Supplier> supplierRepository,
        IValidator<Supplier> validator,
        ILogger<SupplierService> logger,
        IMapper mapper)
    {
        _supplierRepository = supplierRepository;
        _validator = validator;
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

    public async Task<SupplierDto> CreateSupplierAsync(CreateSupplierDto dto)
    {
        var existing = await _supplierRepository.AsQueryable()
            .IgnoreQueryFilters()
            .AnyAsync(s => s.Name == dto.Name);
        if (existing)
            throw new InvalidOperationException($"Supplier '{dto.Name}' already exists");

        var supplier = _mapper.Map<Supplier>(dto);
        var validation = await _validator.ValidateAsync(supplier);
        if (!validation.IsValid)
        {
            var errors = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage));
            throw new ValidationException($"Supplier validation failed: {errors}");
        }

        await _supplierRepository.AddAsync(supplier);
        await _supplierRepository.SaveChangesAsync();

        _logger.LogInformation($"Supplier '{supplier.Name}' created");
        return _mapper.Map<SupplierDto>(supplier);
    }

    public async Task<SupplierDto> UpdateSupplierAsync(int id, UpdateSupplierDto dto)
    {
        var supplier = await _supplierRepository.GetByIdAsync(id);
        if (supplier == null)
            throw new KeyNotFoundException($"Supplier with ID {id} not found");

        if (!string.IsNullOrWhiteSpace(dto.Name) && !string.Equals(dto.Name, supplier.Name, StringComparison.OrdinalIgnoreCase))
        {
            var duplicate = await _supplierRepository.AsQueryable()
                .IgnoreQueryFilters()
                .AnyAsync(s => s.Name == dto.Name && s.Id != id);
            if (duplicate)
                throw new InvalidOperationException($"Supplier '{dto.Name}' already exists");
            supplier.Name = dto.Name;
        }

        if (dto.Address != null)
            supplier.Address = dto.Address;

        if (dto.PhoneNumber != null)
            supplier.PhoneNumber = dto.PhoneNumber;

        if (dto.Email != null)
            supplier.Email = dto.Email;

        if (dto.ContactPerson != null)
            supplier.ContactPerson = dto.ContactPerson;

        if (dto.MinimumOrderQuantity.HasValue)
            supplier.MinimumOrderQuantity = dto.MinimumOrderQuantity.Value;

        if (dto.LeadTimeDays.HasValue)
            supplier.LeadTimeDays = dto.LeadTimeDays.Value;

        if (dto.PaymentTerms != null)
            supplier.PaymentTerms = dto.PaymentTerms;

        if (dto.IsActive.HasValue)
            supplier.IsActive = dto.IsActive.Value;

        var validation = await _validator.ValidateAsync(supplier);
        if (!validation.IsValid)
        {
            var errors = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage));
            throw new ValidationException($"Supplier validation failed: {errors}");
        }

        await _supplierRepository.UpdateAsync(supplier);
        await _supplierRepository.SaveChangesAsync();

        _logger.LogInformation($"Supplier '{supplier.Name}' updated");
        return _mapper.Map<SupplierDto>(supplier);
    }

    public async Task<bool> DeleteSupplierAsync(int id)
    {
        var supplier = await _supplierRepository.GetByIdAsync(id);
        if (supplier == null)
            return false;

        supplier.IsDeleted = true;
        supplier.IsActive = false;
        await _supplierRepository.UpdateAsync(supplier);
        await _supplierRepository.SaveChangesAsync();

        _logger.LogInformation($"Supplier '{supplier.Name}' deleted");
        return true;
    }
}
