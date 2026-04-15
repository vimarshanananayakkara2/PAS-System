using Microsoft.EntityFrameworkCore;
using PAS.Web.Data;
using PAS.Web.Models;
using PAS.Web.Services.Interfaces;
using PAS.Web.ViewModels;

namespace PAS.Web.Services;

public class MatchingService : IMatchingService
{
    private readonly ApplicationDbContext _context;

    public MatchingService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<BlindProjectVm>> GetAnonymousProjectsForSupervisorAsync(string supervisorId)
    {
        return await _context.ProjectProposals
            .Include(p => p.ResearchArea)
            .Where(p => p.Status != ProposalStatus.Matched && !p.IdentityRevealed)
            .Select(p => new BlindProjectVm
            {
                Id = p.Id,
                Title = p.Title,
                Abstract = p.Abstract,
                TechnicalStack = p.TechnicalStack,
                ResearchAreaName = p.ResearchArea.Name,
                Status = p.Status
            })
            .ToListAsync();
    }

    public async Task<bool> ExpressInterestAsync(int proposalId, string supervisorId)
    {
        var proposal = await _context.ProjectProposals.FindAsync(proposalId);

        if (proposal == null)
            return false;

        var alreadyExists = await _context.Matches
            .AnyAsync(m => m.ProjectProposalId == proposalId && m.SupervisorId == supervisorId);

        if (alreadyExists)
            return true;

        _context.Matches.Add(new Match
        {
            ProjectProposalId = proposalId,
            SupervisorId = supervisorId,
            Status = MatchStatus.Interested
        });

        proposal.Status = ProposalStatus.UnderReview;
        proposal.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ConfirmMatchAsync(int proposalId, string supervisorId)
    {
        var proposal = await _context.ProjectProposals
            .FirstOrDefaultAsync(p => p.Id == proposalId);

        if (proposal == null)
            return false;

        var match = await _context.Matches
            .FirstOrDefaultAsync(m => m.ProjectProposalId == proposalId && m.SupervisorId == supervisorId);

        if (match == null)
        {
            match = new Match
            {
                ProjectProposalId = proposalId,
                SupervisorId = supervisorId,
                Status = MatchStatus.Interested
            };

            _context.Matches.Add(match);
        }

        match.Status = MatchStatus.Confirmed;
        match.ConfirmedAt = DateTime.UtcNow;

        proposal.Status = ProposalStatus.Matched;
        proposal.IdentityRevealed = true;
        proposal.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }
}