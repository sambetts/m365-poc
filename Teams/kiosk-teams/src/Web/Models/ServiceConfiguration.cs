using Engine;

namespace Web.Models;

public class ServiceConfiguration
{
    public LocationInfo ClientLocationInfo { get; set; } = null!;

    public string AcsEndpointVal { get; set; } = null!;
    public string AcsAccessKeyVal { get; set; } = null!;
}
