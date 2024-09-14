using RemoteBeep.Backend.Services;
using Microsoft.Extensions.Caching.Memory;
using RemoteBeep.BackEnd.Models;
using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

var environmentSettings = builder.Configuration.Get<EnvironmentSpecificSettings>();
var environmentSettingsEntries = typeof(EnvironmentSpecificSettings).GetProperties();

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddUserSecrets<EnvironmentSpecificSettings>(optional: true)
    .AddEnvironmentVariables();
builder.Services.AddSignalR();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<GroupService>();

var app = builder.Build();

// Preload SignalR hubs by making a dummy connection during startup
var hubContext = app.Services.GetService<IHubContext<BeepHub>>();
await hubContext.Clients.All.SendAsync("preload");

app.UseHttpsRedirection();
app.UseRouting();
app.UseEndpoints(endpoints =>
{
    endpoints.MapHub<BeepHub>("/hub");
});
app.MapGet("/devices-in-group",
    (HttpContext context, GroupService groupService) =>
        groupService.GetConnectionsByGroup(context.Request.Query["groupName"])
);
app.MapGet("/healthcheck", () => "1");

app.Run();

public class BeepHub : Hub
{
    private readonly GroupService _groupService;

    public BeepHub(GroupService groupService)
    {
        _groupService = groupService;
    }

    public async Task Play(string freqInKhz, string durationInSeconds, string groupName)
    {
        Console.WriteLine("Play -  freqInKhz=" + freqInKhz + ", durationInSeconds:" + durationInSeconds + ", group:" + groupName);
        await Clients
            .Group(groupName)
            .SendAsync("playCommandReceived", freqInKhz, durationInSeconds);
    }

    public async Task Stop(string groupName)
    {
        Console.WriteLine("Stop, group:" + groupName);
        await Clients
            .Group(groupName)
            .SendAsync("stopCommandReceived");
    }

    public async Task AddToGroup(string groupName)
    {
        _groupService.AddConnectionToGroup(groupName, Context.ConnectionId);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

        var devicesInCurrentGroup = _groupService.GetConnectionsByGroup(groupName);
        await Clients
            .Group(groupName)
            .SendAsync("addedToGroup", Context.ConnectionId, devicesInCurrentGroup);
    }

    public async Task RemoveFromGroup(string groupName)
    {
        _groupService.RemoveConnectionFromGroup(groupName, Context.ConnectionId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);

        var devicesInCurrentGroup = _groupService.GetConnectionsByGroup(groupName);
        await Clients
            .Group(groupName)
            .SendAsync("removedFromGroup", Context.ConnectionId, devicesInCurrentGroup);
    }
}
