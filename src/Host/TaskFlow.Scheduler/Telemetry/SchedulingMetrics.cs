using System.Diagnostics.Metrics;

namespace TaskFlow.Scheduler.Telemetry;

/// <summary>Configures scheduling metrics host behavior for TaskFlow runtime services.</summary>
public sealed class SchedulingMetrics
{
    public const string MeterName = "TaskFlow.Scheduler";

    private readonly Meter _meter = new(MeterName, "1.0.0");
    private readonly Counter<long> _jobExecutionsCounter;
    private readonly Counter<long> _jobFailuresCounter;
    private readonly Counter<long> _jobRetriesCounter;
    private readonly Histogram<double> _jobDurationHistogram;

    /// <summary>Initializes scheduling metrics with required dependencies and default state.</summary>
    public SchedulingMetrics()
    {
        _jobExecutionsCounter = _meter.CreateCounter<long>(
            "scheduler.job.executions",
            "execution",
            "Total number of scheduler job executions.");
        _jobFailuresCounter = _meter.CreateCounter<long>(
            "scheduler.job.failures",
            "failure",
            "Total number of scheduler job failures.");
        _jobRetriesCounter = _meter.CreateCounter<long>(
            "scheduler.job.retries",
            "retry",
            "Total number of scheduler job retries.");
        _jobDurationHistogram = _meter.CreateHistogram<double>(
            "scheduler.job.duration",
            "ms",
            "Scheduler job duration in milliseconds.");
    }

    /// <summary>Records job success for scheduler diagnostics.</summary>
    public void RecordJobSuccess(string jobName, double durationMs)
    {
        _jobExecutionsCounter.Add(1,
            new KeyValuePair<string, object?>("job.name", jobName),
            new KeyValuePair<string, object?>("job.status", "success"));
        _jobDurationHistogram.Record(durationMs,
            new KeyValuePair<string, object?>("job.name", jobName));
    }

    /// <summary>Records job failure for scheduler diagnostics.</summary>
    public void RecordJobFailure(string jobName, string reason)
    {
        _jobExecutionsCounter.Add(1,
            new KeyValuePair<string, object?>("job.name", jobName),
            new KeyValuePair<string, object?>("job.status", "failure"));
        _jobFailuresCounter.Add(1,
            new KeyValuePair<string, object?>("job.name", jobName),
            new KeyValuePair<string, object?>("failure.reason", reason));
    }

    /// <summary>Records job retry for scheduler diagnostics.</summary>
    public void RecordJobRetry(string jobName, int attempt)
    {
        _jobRetriesCounter.Add(1,
            new KeyValuePair<string, object?>("job.name", jobName),
            new KeyValuePair<string, object?>("retry.attempt", attempt));
    }
}
