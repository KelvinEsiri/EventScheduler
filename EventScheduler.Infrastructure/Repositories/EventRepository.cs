using EventScheduler.Application.Interfaces.Repositories;
using EventScheduler.Domain.Entities;
using EventScheduler.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EventScheduler.Infrastructure.Repositories;

public class EventRepository : IEventRepository
{
    private readonly EventSchedulerDbContext _context;

    public EventRepository(EventSchedulerDbContext context)
    {
        _context = context;
    }

    public async Task<Event?> GetByIdAsync(int id, int userId)
    {
        return await _context.Events
            .Include(e => e.Category)
            .Include(e => e.Invitations)
            .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
    }

    public async Task<IEnumerable<Event>> GetAllAsync(int userId)
    {
        return await _context.Events
            .Include(e => e.Category)
            .Include(e => e.Invitations)
            .Where(e => e.UserId == userId)
            .OrderBy(e => e.StartDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Event>> GetByDateRangeAsync(int userId, DateTime startDate, DateTime endDate)
    {
        return await _context.Events
            .Include(e => e.Category)
            .Include(e => e.Invitations)
            .Where(e => e.UserId == userId &&
                       e.StartDate >= startDate &&
                       e.StartDate <= endDate)
            .OrderBy(e => e.StartDate)
            .ToListAsync();
    }

    public async Task<Event> CreateAsync(Event eventEntity)
    {
        _context.Events.Add(eventEntity);
        await _context.SaveChangesAsync();
        return eventEntity;
    }

    public async Task UpdateAsync(Event eventEntity)
    {
        _context.Events.Update(eventEntity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id, int userId)
    {
        var eventEntity = await GetByIdAsync(id, userId);
        
        if (eventEntity != null)
        {
            _context.Events.Remove(eventEntity);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<Event>> GetPublicEventsAsync()
    {
        return await _context.Events
            .Include(e => e.Category)
            .Include(e => e.Invitations)
            .Include(e => e.User)
            .Where(e => e.IsPublic)
            .OrderBy(e => e.StartDate)
            .ToListAsync();
    }

    public async Task<Event?> GetPublicEventByIdAsync(int id)
    {
        return await _context.Events
            .Include(e => e.Category)
            .Include(e => e.Invitations)
            .Include(e => e.User)
            .FirstOrDefaultAsync(e => e.Id == id && e.IsPublic);
    }
}
