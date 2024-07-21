using Backend.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using RemoteBeep.BackEnd.Models;

var builder = WebApplication.CreateBuilder(args);

var environmentSettings = builder.Configuration.Get<EnvironmentSpecificSettings>();
var environmentSettingsEntries = typeof(EnvironmentSpecificSettings).GetProperties();

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddUserSecrets<EnvironmentSpecificSettings>(optional: true)
    .AddEnvironmentVariables();

// Add services to the container.

builder.Services.AddSignalR();
builder.Services.AddCors();
//builder.Services.AddCors(options =>
//{
//    var frontEndUrl = environmentSettings.FrontEndUrl ?? "";
//    var allowedOrigins = new[] { frontEndUrl };
//    options.AddDefaultPolicy(builder =>
//    {
//        builder.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
//    });
//});
builder.Services.AddApplicationInsightsTelemetry(environmentSettings.ApplicationInsightsInstrumentationKey);
builder.Services.Add(ServiceDescriptor.Singleton<IMemoryCache, MemoryCache>());

builder.Services.AddSingleton<GroupService>();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseCors(builder =>
{
    builder.WithOrigins(origins: environmentSettings.AllowedCorsOrigins ?? []);
    builder.AllowAnyHeader();
    builder.AllowAnyMethod();
});

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

        var devicesInCurrentGroup =_groupService.GetConnectionsByGroup(groupName);
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


