using System.ComponentModel.DataAnnotations;

namespace Entities.Abstract;

/// <summary>
/// Base database object
/// </summary>
public abstract class BaseDBObject
{
    public bool IsUnsaved => this.ID > 0;

    [Key]
    public int ID { get; set; }


    public override string ToString()
    {
        return $"{this.GetType().Name} ID={ID}";
    }
}

public abstract class BaseDBObjectWithUrl : BaseDBObject
{
    [Required]
    public string Url { get; set; } = string.Empty;

}
