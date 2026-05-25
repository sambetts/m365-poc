using SPO.ColdStorage.Models;
using System.Text;

namespace SPO.ColdStorage.Migration.Engine.SnapshotBuilder;

public class BackgroundUpdate
{
    /// <summary>
    /// Value is either DriveItemVersionInfo or ItemAnalyticsRepsonse
    /// </summary>
    public Dictionary<DocumentSiteWithMetadata, object> UpdateResults { get; set; } = [];
}
