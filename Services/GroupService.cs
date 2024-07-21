using Microsoft.Extensions.Caching.Memory;

namespace Backend.Services;

public class GroupService
{

    private IMemoryCache _cache;

    public GroupService(IMemoryCache memoryCache)
    {
        _cache = memoryCache;
    }

    public void AddConnectionToGroup(string groupName, string connectionId)
    {
        // Set cache options.
        var cacheEntryOptions = new MemoryCacheEntryOptions()
            // Keep in cache for this time, reset time if accessed.
            .SetSlidingExpiration(TimeSpan.FromDays(1));

        List<string> devicesInCurrentGroup;

        _cache.TryGetValue(groupName, out devicesInCurrentGroup);
            
        if(devicesInCurrentGroup == null)
        {
            devicesInCurrentGroup = new List<string>();
        }

        devicesInCurrentGroup.Add(connectionId);

        // Save data in cache.
        _cache.Set(groupName, devicesInCurrentGroup, cacheEntryOptions);
    }

    public void RemoveConnectionFromGroup(string groupName, string connectionId)
    {
        // Set cache options.
        var cacheEntryOptions = new MemoryCacheEntryOptions()
            // Keep in cache for this time, reset time if accessed.
            .SetSlidingExpiration(TimeSpan.FromDays(1));

        List<string> devicesInCurrentGroup;

        _cache.TryGetValue(groupName, out devicesInCurrentGroup);

        if (devicesInCurrentGroup == null)
        {
            return;
        }

        if(devicesInCurrentGroup.Any(d => d == connectionId)){ 
            devicesInCurrentGroup.Remove(connectionId);
        }

        // Save data in cache.
        _cache.Set(groupName, devicesInCurrentGroup, cacheEntryOptions);
    }

    public List<string> GetConnectionsByGroup(string groupName) {
        List<string> devicesInCurrentGroup;
        _cache.TryGetValue(groupName, out devicesInCurrentGroup);
        return devicesInCurrentGroup ?? new List<string>();
    }
}
