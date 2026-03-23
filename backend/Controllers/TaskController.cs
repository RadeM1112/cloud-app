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
   public async Task<ActionResult<IEnumerable<TaskReadDto>>> GetAll()
   {
 
    	var tasks = await _context.Tasks.ToListAsync();
    	var tasksDto = tasks.Select(t => new TaskReadDto
    	{
        	Id = t.Id,
        	Name = t.Name,
        	IsCompleted = t.IsCompleted
        });
    	return Ok(tasksDto);
    }

   
    [HttpGet("{id}")]
    public async Task<ActionResult<TaskReadDto>> GetById(int id)
    {
    	var task = await _context.Tasks.FindAsync(id);
    	if (task == null) return NotFound();  // Zwracamy DTO zamiast czystej encji
    	return Ok(new TaskReadDto 
    	{ 
        	Id = task.Id, 
        	Name = task.Name, 
        	IsCompleted = task.IsCompleted 
    	});
    }

    [HttpPost]
    public async Task<ActionResult<TaskReadDto>> Create(TaskCreateDto taskDto)
    {

    var newTask = new CloudTask
    {
        Name = taskDto.Name,
        IsCompleted = false // Domyślnie nowe zadanie nie jest gotowe
    };


    _context.Tasks.Add(newTask);
    await _context.SaveChangesAsync();

    var readDto = new TaskReadDto
    {
        Id = newTask.Id,
        Name = newTask.Name,
        IsCompleted = newTask.IsCompleted
    };

    	return CreatedAtAction(nameof(GetById), new { id = readDto.Id }, readDto);
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
