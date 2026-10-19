namespace Microsoft.Extensions.Hosting.Wpf.GenericHost;

public sealed class WpfLifetimeOptions
{
    /// <summary>
    /// Indicates if host lifetime status messages should be suppressed such as on startup.
    /// The default is false.
    /// </summary>
    public bool SuppressStatusMessages { get; set; }
}