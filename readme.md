# MIFTY (Mine Is Faster Than Yours)
## It's really... nifty...

It crossed my mind that after 27 years (on and off) of programming various protocols and distributed systems, i've never actually developed a DNS client/server/resolver/forwarder. I'm bored and figured, why not?!

DNS is at the root of everything and without it we would have no internet as you know it, so it's good to understand how it works. Plus I saw a thing about "faster than light" DNS and was triggered - can I make the fastest DNS "thing" but also make it cross platform?

And so another sideline project was born...

---

The basics are there - I essentially have a UDP tunnel at the moment that has a little specialist knowledge about DNS. This could easily be extended to do anything UDP can do, it could be a gateway between networks, a proxy, inspection agent, a server, a client. By adding multicast it could be the basis of a clustering protocol.

I'm going to continue with RFC 1035 and the full implementation of a DNS server. It doesn't have to be DNS of course, it's basically just a distributed key/value pair database server :)

Until I decide exactly what to do I'm going to carry on with the import of zone files for DNS - I can then tailor it to my own needs as inspiration hits.

I have decided to use my parser/scanner from the `simple-dsl` project here to parse the zone files. It may seem like overkill but the format is a little floppy so I wanted to ensure I have a robust way of loading files. Plus, it's a real use case for my DSL project and i've already come up with a few ideas on how to enhance that from using it here. So that's nice. I am using a cut down version of it though because I don't need symbol tables and icode because this isn't a script that will be executed. Of course I might end up adding it back in when I decide to add some sort of scripting capability to this project.

---

I downloaded the dns blacklist from here: https://github.com/notracking/hosts-blocklists/ - and modified it a bit to suit my needs (mainly reversing the DNS order for top-down processing)

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