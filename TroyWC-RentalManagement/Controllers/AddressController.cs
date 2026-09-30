using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TroyWC_RentalManagement.Data;
using TroyWC_RentalManagement.Models;

namespace TroyWC_RentalManagement.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AddressController : ControllerBase
{
    ApplicationDbContext _context;

    public AddressController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<Address>> GetAddresses()
    {
        var result = await _context.Addresses.ToListAsync();
        return Ok(result);
             
    }
}
