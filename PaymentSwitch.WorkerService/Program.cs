using PaymentSwitch.Utility;
using PaymentSwitch.WorkerService.BackgroundServices;
using Serilog;

var logPath = Path.Combine(AppContext.BaseDirectory, StaticData.Logs, StaticData.WorkerServiceLog);
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console() // Only works when running interactively
    .WriteTo.File(logPath, rollingInterval: RollingInterval.Day, shared: true)
    .CreateLogger();

try
{
    var builder = Host.CreateApplicationBuilder(args);
    builder.Services.AddWindowsService(options =>
    {
        options.ServiceName = "PaymentSwitchWorkerService";
    });
    // Replace default logging with Serilog
    builder.Logging.ClearProviders();
    builder.Logging.AddSerilog(Log.Logger);

    //builder.Services.AddServices(builder.Configuration);
    builder.Services.AddHostedService<ReconcilerBackgroundService>();

    var host = builder.Build();
    host.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Worker service terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
