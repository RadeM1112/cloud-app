using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CloudBackend.Data;
using CloudBackend.Models;

namespace CloudBackend.Controllers;

[ApiController]
[Route("api/[controller]")] // Adres: http://localhost:8081/api/tasks
public class TasksController : ControllerBase
{
    private readonly AppDbContext _context;

    public TasksController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult> GetAll()
    {
        return Ok(await _context.Tasks.ToListAsync());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult> GetById(int id)
    {
        var task = await _context.Tasks.FindAsync(id);
        return task == null ? NotFound() : Ok(task);
    }

    [HttpPost]
    public async Task<ActionResult> Create(CloudTask task)
    {
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = task.Id }, task);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> Update(int id, CloudTask task)
    {
        if (id != task.Id)
            return BadRequest("ID mismatch");

        _context.Entry(task).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        return NoContent(); // 204 - operacja udana
    }

    // 5. Usuń (DELETE)
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var task = await _context.Tasks.FindAsync(id);
        if (task == null)
            return NotFound();

        _context.Tasks.Remove(task);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
