using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UberPrints.Server.Data;
using UberPrints.Server.DTOs;

namespace UberPrints.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FilamentsController : ControllerBase
{
  private readonly ApplicationDbContext _context;

  public FilamentsController(ApplicationDbContext context)
  {
    _context = context;
  }

  [HttpGet]
  public async Task<IActionResult> GetFilaments([FromQuery] bool? inStock = null)
  {
    var query = _context.Filaments.AsQueryable();

    if (inStock.HasValue && inStock.Value)
    {
      query = query.Where(f => f.StockAmount > 0);
    }

    var filaments = await query
        .OrderBy(f => f.Name)
        .ToListAsync();

    var dtos = filaments.Select(f => new FilamentDto
    {
      Id = f.Id,
      Name = f.Name,
      Material = f.Material,
      Brand = f.Brand,
      Colour = f.Colour,
      StockAmount = f.StockAmount,
      StockUnit = f.StockUnit,
      Link = f.Link,
      PhotoUrl = f.PhotoUrl,
      CreatedAt = f.CreatedAt,
      UpdatedAt = f.UpdatedAt
    }).ToList();

    return Ok(dtos);
  }

  [HttpGet("{id}")]
  public async Task<IActionResult> GetFilament(Guid id)
  {
    var filament = await _context.Filaments.FindAsync(id);

    if (filament == null)
    {
      return NotFound();
    }

    var dto = new FilamentDto
    {
      Id = filament.Id,
      Name = filament.Name,
      Material = filament.Material,
      Brand = filament.Brand,
      Colour = filament.Colour,
      StockAmount = filament.StockAmount,
      StockUnit = filament.StockUnit,
      Link = filament.Link,
      PhotoUrl = filament.PhotoUrl,
      CreatedAt = filament.CreatedAt,
      UpdatedAt = filament.UpdatedAt
    };

    return Ok(dto);
  }
}
