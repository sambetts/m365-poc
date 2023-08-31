using SPO.ColdStorage.Entities.Abstract;
using System.ComponentModel.DataAnnotations.Schema;

namespace SPO.ColdStorage.Entities.DBEntities
{
    [Table("file_migrations_completed")]
    public class FileMigrationCompletedLog : BaseFileRelatedClass
    {
        [Column("migrated")]
        public DateTime Migrated { get; set; } = DateTime.MinValue;


    }
}
