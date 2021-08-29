# Adding observability
I've covered this in other projects, although i'm not sure if I have added those projects in Github or an internal repository. Anyway, adding observability is very simple thanks to Prometheus.

Simply add a reference to the Nuget package `prometheus-net` by executing:
```
dotnet add package prometheus-net --version 4.2.0
```

Then set up a server that Prometheus can scrape by adding a couple of lines to startup code:
```csharp
static void Main(string[] args)
{
    var metricServer = new MetricServer(hostname: "localhost", port: 1234);
    metricServer.Start();
```
Add this server as a target to Prometheus by editing the targets in the configuration YML file:
```yaml
scrape_configs:
  # The job name is added as a label `job=<job_name>` to any timeseries scraped from this config.
  - job_name: 'prometheus'

    # metrics_path defaults to '/metrics'
    # scheme defaults to 'http'.

    static_configs:
    - targets: ['localhost:9090']
    - targets: ['localhost:1234']
```

Then add metrics that are appropriate for the project. Declare a counter, guage or histogram - i'm just using counters for now:

```csharp
private static readonly Counter totalRequestCounter = Metrics.CreateCounter("mifty_requests_total", "Total number of requests");
```

Lastly increment the counter where appropriate:
```csharp            
totalRequestCounter.Inc();
```

And those metrics will flow into Prometheus (every 15 seconds by default) and can then be visualised in Grafana or similar.