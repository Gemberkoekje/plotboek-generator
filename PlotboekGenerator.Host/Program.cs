using PlotboekGenerator.Host;
using PlotboekGenerator.WebApp;

// Everything the product registers lives in PlotboekApp; this host adds only its template and its
// example reference data (DefaultProfile). Everything else is configuration.
await PlotboekApp.RunAsync(args, new DefaultProfile());
