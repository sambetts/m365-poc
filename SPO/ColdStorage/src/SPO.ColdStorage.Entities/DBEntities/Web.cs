using SPO.ColdStorage.Entities.Abstract;
using System.ComponentModel.DataAnnotations.Schema;

namespace SPO.ColdStorage.Entities.DBEntities
{
    [Table("webs")]
    public class Web : BaseDBObjectWithUrl
    {
        [ForeignKey(nameof(Site))]
        [Column("site_id")]
        public int SiteId { get; set; }

        public Site Site { get; set; } = null!;
    }
}
