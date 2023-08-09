

using Entities.Abstract;

namespace Entities;

public class PlayListItem : BaseDBObjectWithUrl
{
    public string? Scope { get; set; } = null!;
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
}

public class LocationIpRule : BaseDBObject
{
    public string IpAddress { get; set; } = null!;
    public string Subnet { get; set; } = null!;
    public int Order { get; set; } = 0;
    public string Name { get; set; } = null!;
}
