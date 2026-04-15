using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PAS.Web.Data;
using PAS.Web.Models;

namespace PAS.Web.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _context;

    public AdminController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Dashboard()
    {
        var matches = await _context.Matches
            .Include(m => m.ProjectProposal)
            .ThenInclude(p => p.Student)
            .Include(m => m.Supervisor)
            .ToListAsync();

        return View(matches);
    }

    [HttpGet]
    public async Task<IActionResult> ResearchAreas()
    {
        var areas = await _context.ResearchAreas.OrderBy(r => r.Name).ToListAsync();
        return View(areas);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddResearchArea(string name)
    {
        if (!string.IsNullOrWhiteSpace(name))
        {
            var exists = await _context.ResearchAreas.AnyAsync(r => r.Name == name.Trim());

            if (!exists)
            {
                _context.ResearchAreas.Add(new ResearchArea { Name = name.Trim() });
                await _context.SaveChangesAsync();
            }
        }
        TempData["Success"] = "Action completed successfully!";
        return RedirectToAction(nameof(ResearchAreas));
    }
    public async Task<IActionResult> Reassign(int id)
    {
        var match = await _context.Matches.FindAsync(id);

        ViewBag.Supervisors = _context.Users.ToList();

        return View(match);
    }
    [HttpPost]
    public async Task<IActionResult> Reassign(int id, string supervisorId)
    {
        var match = await _context.Matches.FindAsync(id);

        match.SupervisorId = supervisorId;

        await _context.SaveChangesAsync();

        TempData["Success"] = "Supervisor reassigned successfully!";
        return RedirectToAction("Dashboard");
    }
    public IActionResult Users()
    {
        var users = _context.Users.ToList();
        return View(users);
    }
}