using Hecateon.Data;
using Hecateon.Models;
using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;

namespace Hecateon.Services;

public class ChatLogService(ChatDbContext dbContext)
{
    private readonly ChatDbContext _dbContext = dbContext;
    private readonly ConcurrentQueue<ChatMessage> _memoryCache = new();

    public IEnumerable<ChatMessage> GetHistory(int limit = 100)
    {
        try
        {
            return _dbContext.ChatMessages
                .OrderByDescending(m => m.Timestamp)
                .Take(limit)
                .OrderBy(m => m.Timestamp)
                .ToList();
        }
        catch
        {
            // Fall back to memory cache if database fails
            return _memoryCache.Reverse().Take(limit).Reverse();
        }
    }

    public async Task<IEnumerable<ChatMessage>> GetHistoryAsync(int limit = 100)
    {
        try
        {
            return await _dbContext.ChatMessages
                .OrderByDescending(m => m.Timestamp)
                .Take(limit)
                .OrderBy(m => m.Timestamp)
                .ToListAsync();
        }
        catch
        {
            // Fall back to memory cache if database fails
            return _memoryCache.Reverse().Take(limit).Reverse();
        }
    }

    public void AddMessage(ChatMessage message)
    {
        try
        {
            var alreadyExists = _dbContext.ChatMessages.Any(m => m.Id == message.Id);
            if (alreadyExists)
            {
                return;
            }

            _memoryCache.Enqueue(message);
            _dbContext.ChatMessages.Add(message);
            _dbContext.SaveChanges();
        }
        catch
        {
            // If database save fails, message is still in memory cache
        }
    }

    public async Task AddMessageAsync(ChatMessage message)
    {
        try
        {
            var alreadyExists = await _dbContext.ChatMessages.AnyAsync(m => m.Id == message.Id);
            if (alreadyExists)
            {
                return;
            }

            _memoryCache.Enqueue(message);
            _dbContext.ChatMessages.Add(message);
            await _dbContext.SaveChangesAsync();
        }
        catch
        {
            // If database save fails, message is still in memory cache
        }
    }

    public void ClearHistory()
    {
        try
        {
            _dbContext.ChatMessages.ExecuteDelete();
            _memoryCache.Clear();
        }
        catch
        {
            _memoryCache.Clear();
        }
    }

    public async Task ClearHistoryAsync()
    {
        try
        {
            await _dbContext.ChatMessages.ExecuteDeleteAsync();
            _memoryCache.Clear();
        }
        catch
        {
            _memoryCache.Clear();
        }
    }

    public int GetMessageCount()
    {
        try
        {
            return _dbContext.ChatMessages.Count();
        }
        catch
        {
            return _memoryCache.Count;
        }
    }

    public async Task<int> GetMessageCountAsync()
    {
        try
        {
            return await _dbContext.ChatMessages.CountAsync();
        }
        catch
        {
            return _memoryCache.Count;
        }
    }

    public IEnumerable<ChatMessage> SearchMessages(string query)
    {
        try
        {
            return _dbContext.ChatMessages
                .Where(m => EF.Functions.Like(m.Message, $"%{query}%"))
                .OrderByDescending(m => m.Timestamp)
                .ToList();
        }
        catch
        {
            return _memoryCache
                .Where(m => m.Message.Contains(query, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
    }

    public async Task<IEnumerable<ChatMessage>> SearchMessagesAsync(string query)
    {
        try
        {
            return await _dbContext.ChatMessages
                .Where(m => EF.Functions.Like(m.Message, $"%{query}%"))
                .OrderByDescending(m => m.Timestamp)
                .ToListAsync();
        }
        catch
        {
            return _memoryCache
                .Where(m => m.Message.Contains(query, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
    }
}
