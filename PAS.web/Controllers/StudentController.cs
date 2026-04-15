using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PAS.Web.Data;
using PAS.Web.Models;
using PAS.Web.ViewModels;

namespace PAS.Web.Controllers;

[Authorize(Roles = "Student")]
public class StudentController : Controller
{
    private readonly ApplicationDbContext _context;

    public StudentController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Dashboard()
    {
        var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var proposals = await _context.ProjectProposals
            .Include(p => p.ResearchArea)
            .Where(p => p.StudentId == studentId)
            .ToListAsync();

        return View(proposals);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        ViewBag.ResearchAreas = new SelectList(
            await _context.ResearchAreas.OrderBy(r => r.Name).ToListAsync(),
            "Id",
            "Name");

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProjectProposalCreateVm vm)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.ResearchAreas = new SelectList(
                await _context.ResearchAreas.OrderBy(r => r.Name).ToListAsync(),
                "Id",
                "Name");

            return View(vm);
        }

        var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var proposal = new ProjectProposal
        {
            Title = vm.Title,
            Abstract = vm.Abstract,
            TechnicalStack = vm.TechnicalStack,
            ResearchAreaId = vm.ResearchAreaId,
            StudentId = studentId,
            Status = ProposalStatus.Pending
        };

        _context.ProjectProposals.Add(proposal);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Action completed successfully!";
        return RedirectToAction(nameof(Dashboard));
    }

    // GET: Edit
    public async Task<IActionResult> Edit(int id)
    {
        var proposal = await _context.ProjectProposals.FindAsync(id);
        if (proposal == null) return NotFound();

        return View(proposal);
    }

    // POST: Edit
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ProjectProposal proposal)
    {
        if (ModelState.IsValid)
        {
            _context.Update(proposal);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Proposal updated successfully!";
            return RedirectToAction(nameof(Dashboard));
        }
        return View(proposal);
    }
    // Withdraw
    public async Task<IActionResult> Withdraw(int id)
    {
        var proposal = await _context.ProjectProposals.FindAsync(id);
        if (proposal == null) return NotFound();

        _context.ProjectProposals.Remove(proposal);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Proposal withdrawn successfully!";
        return RedirectToAction(nameof(Dashboard));
    }
}