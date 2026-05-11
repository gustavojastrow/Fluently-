using System.Text.Json;
using System.Text.Json.Serialization;

namespace EnglishTeacher.Models
{
    public class SchemaModel
    {
        public Dictionary<string, TableInfo> Tables { get; set; } = new();
    }

    public class TableInfo
    {
        public string Description { get; set; } = string.Empty;
        public Dictionary<string, object> Columns { get; set; } = new(); 
        public string[]? Relationships { get; set; }
    }

    public class ColumnInfo
    {
        public string Description { get; set; } = string.Empty;
        public string[]? Allowed_Values { get; set; }
    }
}