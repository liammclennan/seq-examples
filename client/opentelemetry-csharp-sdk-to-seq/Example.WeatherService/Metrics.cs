using System.Diagnostics.Metrics;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;

public static class Metrics
{
    public static Meter weatherServiceMetrics = new("Example.WeatherService", "1.0");
    public static Counter<long> FoundPostcodes = weatherServiceMetrics.CreateCounter<long>(
        "FoundPostcodes", "ones", "Counts the number of successfully found postcodes.");
    public static Counter<long> MissingPostcodes = weatherServiceMetrics.CreateCounter<long>(
        "MissingPostcodes", "ones", "Counts the number of postcodes that could not be found.");
    public static Gauge<int> RandomValue = weatherServiceMetrics.CreateGauge<int>(
        "RandomValue", "", "A random integer."); 
    
    
    public static readonly Histogram<double> ExponentialHistogram =
        weatherServiceMetrics.CreateHistogram<double>(
            name: "exponential.histogram",
            unit: "na",
            description: "Exponential histogram");

    public static readonly Histogram<double> FixedHistogram =
        weatherServiceMetrics.CreateHistogram<double>(
            name: "fixed.histogram",
            unit: "na",
            description: "Fixed histogram");


    public static MeterProvider CreateMeterProvider(string serviceName)
    {
        return Sdk.CreateMeterProviderBuilder()
            .AddMeter("Example.WeatherService")
            .AddView(
                instrumentName: "exponential.histogram",
                new Base2ExponentialBucketHistogramConfiguration()
            )
            .AddAspNetCoreInstrumentation()
            .AddMeter("Microsoft.AspNetCore.Hosting")
            .AddMeter("Microsoft.AspNetCore.Server.Kestrel")
            .AddMeter("System.Net.Http")
            .AddMeter("System.Net.NameResolution")
            .SetResourceBuilder(ResourceBuilder.CreateEmpty().AddService(serviceName))
            .AddConsoleExporter()
            .AddOtlpExporter((exporterOptions, metricReaderOptions) =>
            {
                exporterOptions.Endpoint = new Uri("http://localhost:5341/ingest/otlp/v1/metrics");
                exporterOptions.Protocol = OtlpExportProtocol.HttpProtobuf;
                metricReaderOptions.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = 1000;
            })
            .Build();
    }
}