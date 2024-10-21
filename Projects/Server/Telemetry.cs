using System;
using System.Diagnostics.Metrics;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;

namespace Server;

public class Telemetry
{
    public static MeterProvider MeterProvider { get; private set; }
    public static readonly Meter AccountingMeter = new("ModernUO.Accounting");
    public static readonly Meter ItemsMeter = new("ModernUO.Items");
    public static readonly Meter MobilesMeter = new("ModernUO.Mobiles");
    public static readonly Meter EconomyMeter = new("ModernUO.Economy");

    public static void Start()
    {
        MeterProvider = Sdk.CreateMeterProviderBuilder()
            .ConfigureResource(r => r.AddService("ModernUO"))
            .AddOtlpExporter(options =>
            {
                options.BatchExportProcessorOptions.ScheduledDelayMilliseconds = TimeSpan.FromSeconds(10.0).Milliseconds;
            })
            .AddMeter("ModernUO.Accounting")
            .AddMeter("ModernUO.Items")
            .AddMeter("ModernUO.Mobiles")
            .AddMeter("ModernUO.Economy")
            .AddView(
                "gold_created_histogram",
                new ExplicitBucketHistogramConfiguration()
                    { Boundaries = new double[] { 100, 200, 300, 400, 500, 700, 1000, 1500, 2000 } }
            )
            .Build();
    }
}
