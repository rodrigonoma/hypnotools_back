using HypnoTools.API.Models;
using HypnoTools.API.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace HypnoTools.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClientsController : ControllerBase
{
    private readonly IRepository<Client> _clientRepository;

    public ClientsController(IRepository<Client> clientRepository)
    {
        _clientRepository = clientRepository;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Client>>> GetClients()
    {
        var clients = await _clientRepository.GetAllAsync();
        return Ok(clients);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Client>> GetClient(int id)
    {
        var client = await _clientRepository.GetByIdAsync(id);

        if (client == null)
            return NotFound();

        return Ok(client);
    }

    [HttpPost]
    public async Task<ActionResult<Client>> CreateClient(Client client)
    {
        // Check if email already exists
        var existingClient = await _clientRepository.FirstOrDefaultAsync(c => c.Email == client.Email);
        if (existingClient != null)
            return BadRequest("A client with this email already exists.");

        var createdClient = await _clientRepository.AddAsync(client);
        return CreatedAtAction(nameof(GetClient), new { id = createdClient.Id }, createdClient);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateClient(int id, Client client)
    {
        if (id != client.Id)
            return BadRequest();

        var existingClient = await _clientRepository.GetByIdAsync(id);
        if (existingClient == null)
            return NotFound();

        // Check if email is being changed and if it already exists
        if (existingClient.Email != client.Email)
        {
            var emailExists = await _clientRepository.ExistsAsync(c => c.Email == client.Email && c.Id != id);
            if (emailExists)
                return BadRequest("A client with this email already exists.");
        }

        await _clientRepository.UpdateAsync(client);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteClient(int id)
    {
        var client = await _clientRepository.GetByIdAsync(id);
        if (client == null)
            return NotFound();

        await _clientRepository.SoftDeleteAsync(id);
        return NoContent();
    }

    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<Client>>> SearchClients([FromQuery] string? name, [FromQuery] string? company, [FromQuery] ClientStatus? status)
    {
        var clients = await _clientRepository.GetAllAsync();

        if (!string.IsNullOrEmpty(name))
            clients = clients.Where(c => c.Name.Contains(name, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrEmpty(company))
            clients = clients.Where(c => c.Company.Contains(company, StringComparison.OrdinalIgnoreCase));

        if (status.HasValue)
            clients = clients.Where(c => c.Status == status.Value);

        return Ok(clients);
    }
}