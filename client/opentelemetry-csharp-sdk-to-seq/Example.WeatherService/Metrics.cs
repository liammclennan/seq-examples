using System.Diagnostics.Metrics;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;

public static class Metrics
{
    public static Meter weatherServiceMetrics = new("Example.WeatherService", "1.0");
    public static Counter<long> Seconds = weatherServiceMetrics.CreateCounter<long>(
        "Seconds", "s", "Counts the number of seconds elapsed.");
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

    public static readonly Histogram<double> WaitTime =
        weatherServiceMetrics.CreateHistogram<double>(
            name: "wait.time",
            unit: "s",
            description: "Exponential histogram");

    public static readonly Histogram<double> WaitTimeFixed =
        weatherServiceMetrics.CreateHistogram<double>(
            name: "wait.time.fixed",
            unit: "s",
            description: "Fixed wait time");

    public static readonly Histogram<double> FixedHistogram =
        weatherServiceMetrics.CreateHistogram<double>(
            name: "fixed.histogram",
            unit: "na",
            description: "Fixed histogram");

    public static List<Histogram<double>> BulkHistograms = [];

    
    public static readonly Histogram<double> DistributionLogNormal =
        weatherServiceMetrics.CreateHistogram<double>(
            name: "distribution.lognormal",
            unit: "na",
            description: "Log normal distribution");

    public static readonly Histogram<double> DistributionExponential =
        weatherServiceMetrics.CreateHistogram<double>(
            name: "distribution.exponential",
            unit: "na",
            description: "Exponential distribution");

    public static readonly Histogram<double> Evens =
        weatherServiceMetrics.CreateHistogram<double>(
            name: "evens",
            unit: "na",
            description: "Odd even distribution");

    public static readonly Histogram<double> DistributionCauchy =
        weatherServiceMetrics.CreateHistogram<double>(
            name: "distribution.cauchy",
            unit: "na",
            description: "Cauchy distribution");
    
    
    

    public static MeterProvider CreateMeterProvider(string serviceName)
    {
        for (var i = 0; i < 10; i++)
        {
            BulkHistograms.Add(weatherServiceMetrics.CreateHistogram<double>(
                name: "bulk.histogram." + i.ToString(),
                unit: "na",
                description: "Fixed histogram"));
        }
        
        
        return Sdk.CreateMeterProviderBuilder()
            .AddMeter("Example.WeatherService")
            .AddView(
                instrumentName: "exponential.histogram",
                new Base2ExponentialBucketHistogramConfiguration()
            )
            .AddView(
                instrumentName: "wait.time",
                new Base2ExponentialBucketHistogramConfiguration()
            )
            .AddView(
                instrumentName: "distribution.lognormal",
                new Base2ExponentialBucketHistogramConfiguration()
            )
            .AddView(
                instrumentName: "distribution.exponential",
                new Base2ExponentialBucketHistogramConfiguration()
            )
            .AddView(
                instrumentName: "evens",
                new Base2ExponentialBucketHistogramConfiguration()
            )
            .AddView(
                instrumentName: "distribution.cauchy",
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
                metricReaderOptions.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = 10000;
                metricReaderOptions.TemporalityPreference = MetricReaderTemporalityPreference.Delta;
            })
            .Build();
    }
}