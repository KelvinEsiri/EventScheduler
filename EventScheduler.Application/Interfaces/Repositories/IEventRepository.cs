using EventScheduler.Domain.Entities;

namespace EventScheduler.Application.Interfaces.Repositories;

public interface IEventRepository
{
    Task<Event?> GetByIdAsync(int id, int userId);
    Task<IEnumerable<Event>> GetAllAsync(int userId);
    Task<IEnumerable<Event>> GetByDateRangeAsync(int userId, DateTime startDate, DateTime endDate);
    Task<Event> CreateAsync(Event eventEntity);
    Task UpdateAsync(Event eventEntity);
    Task DeleteAsync(int id, int userId);
}
