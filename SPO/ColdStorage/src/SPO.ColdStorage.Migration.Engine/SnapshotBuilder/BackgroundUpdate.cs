using SPO.ColdStorage.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPO.ColdStorage.Migration.Engine.SnapshotBuilder
{

    public class BackgroundUpdate
    {
        /// <summary>
        /// Value is either DriveItemVersionInfo or ItemAnalyticsRepsonse
        /// </summary>
        public Dictionary<DocumentSiteWithMetadata, object> UpdateResults { get; set; } = new();
    }
}
