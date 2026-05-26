using Models;
using System.Text;

namespace Migration.Engine.SnapshotBuilder;

public class BackgroundUpdate
{
    /// <summary>
    /// Value is either DriveItemVersionInfo or ItemAnalyticsRepsonse
    /// </summary>
    public Dictionary<DocumentSiteWithMetadata, object> UpdateResults { get; set; } = [];
}
