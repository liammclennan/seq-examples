using System.Diagnostics;
using System.Diagnostics.Metrics;
using MathNet.Numerics.Distributions;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
// ReSharper disable ExplicitCallerInfoArgument

var exampleActivitySource = new ActivitySource("Example.WeatherService");

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(serviceName: builder.Environment.ApplicationName))
    .WithTracing(tracing =>
    {
        tracing.AddSource(exampleActivitySource.Name);
        tracing.AddAspNetCoreInstrumentation();
        tracing.AddConsoleExporter();
        tracing.AddOtlpExporter(opt =>
        {
            opt.Endpoint = new Uri("http://localhost:5341/ingest/otlp/v1/traces");
            opt.Protocol = OtlpExportProtocol.HttpProtobuf;
            //opt.Headers = "X-Seq-ApiKey=abcde12345";
        });
    });

builder.Services.AddLogging(logging => logging.AddOpenTelemetry(openTelemetryLoggerOptions =>  
{  
    openTelemetryLoggerOptions.SetResourceBuilder(  
        ResourceBuilder.CreateEmpty()
            .AddService(serviceName: builder.Environment.ApplicationName));

    // Some important options to improve data quality
    openTelemetryLoggerOptions.IncludeScopes = true;

    openTelemetryLoggerOptions.AddConsoleExporter();
    openTelemetryLoggerOptions.AddOtlpExporter(opt =>
    {
        opt.Endpoint = new Uri("http://localhost:5341/ingest/otlp/v1/logs");
        opt.Protocol = OtlpExportProtocol.HttpProtobuf;
        //opt.Headers = "X-Seq-ApiKey=abcde12345";
    });
}));

using var meterProvider = Metrics.CreateMeterProvider(builder.Environment.ApplicationName);

var logNormal = new LogNormal(0, 1.0);
var exponential = new Exponential(2.0);
var cauchy = new Cauchy(10.0, 1.0);

Random random = new Random();
using var timer = new Timer((s) =>
{
    Metrics.RandomValue.Record(random.Next());
    
    Metrics.FixedHistogram.Record(DateTime.Now.Minute + DateTime.Now.Second + DateTime.Now.Millisecond);
    
    Metrics.ExponentialHistogram.Record(0);
    Metrics.ExponentialHistogram.Record(4);
    Metrics.ExponentialHistogram.Record(0);
    Metrics.ExponentialHistogram.Record(4);
    Metrics.ExponentialHistogram.Record(4);
    Metrics.ExponentialHistogram.Record(9);
    Metrics.ExponentialHistogram.Record(8);
    Metrics.ExponentialHistogram.Record(9);
    Metrics.ExponentialHistogram.Record(4);
    Metrics.ExponentialHistogram.Record(3);
    
    // Exponential distribution
    var wait = -Math.Log(1.0 - random.NextDouble()) / 0.5;
    Metrics.WaitTime.Record(wait);
    Metrics.WaitTimeFixed.Record(wait);
    
    
    Metrics.DistributionLogNormal.Record(logNormal.Sample());
    Metrics.DistributionExponential.Record(exponential.Sample());
    Metrics.DistributionCauchy.Record(cauchy.Sample());
    
    var offset = DateTime.Now.Minute % 2;
    Metrics.Evens.Record(2 + offset);
    Metrics.Evens.Record(4 + offset);
    Metrics.Evens.Record(6 + offset);
    Metrics.Evens.Record(8 + offset);
    
}, null, TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(1)); 

using var timer2 = new Timer((s) =>
{
    Metrics.BulkHistograms.ForEach(bh => bh.Record(Random.Shared.NextDouble() * 10000));
}, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(1));

var app = builder.Build();
app.Logger.LogInformation("Starting {App}", builder.Environment.ApplicationName);

var forecastByPostcode = Directory.GetFiles("./data")
    .ToDictionary(f => Path.GetFileNameWithoutExtension(f)!, f => File.ReadAllText(f).Trim());

app.MapGet("/{postcode}", (string postcode) =>
{
    using var activity = exampleActivitySource.StartActivity("Look up forecast for postcode {Postcode}");
    activity?.SetTag("Postcode", postcode);

    if (forecastByPostcode.ContainsKey(postcode))
    {
        Metrics.FoundPostcodes.Add(1);
    }
    else
    {
        Metrics.MissingPostcodes.Add(1);
    }
    
    var forecast = forecastByPostcode[postcode];
    activity?.SetTag("Forecast", forecast);
    
    return forecast;
});

app.Run();

static string GetRandomStarSign()
{
    string[] starSigns = {
        "Aries", "Taurus", "Gemini", "Cancer",
        "Leo", "Virgo", "Libra", "Scorpio",
        "Sagittarius", "Capricorn", "Aquarius", "Pisces"
    };

    return starSigns[Random.Shared.Next(starSigns.Length)];
}