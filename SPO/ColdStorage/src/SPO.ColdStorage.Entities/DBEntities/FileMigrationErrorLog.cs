using SPO.ColdStorage.Entities.Abstract;
using System.ComponentModel.DataAnnotations.Schema;

namespace SPO.ColdStorage.Entities.DBEntities
{
    [Table("file_migration_errors")]
    public class FileMigrationErrorLog : BaseFileRelatedClass
    {
        [Column("error")]
        public string Error { get; set; } = string.Empty;

        [Column("timestamp")]
        public DateTime TimeStamp { get; set; } = DateTime.MinValue;
    }
}
