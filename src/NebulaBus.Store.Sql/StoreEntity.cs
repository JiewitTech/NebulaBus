using SqlSugar;

namespace NebulaBus.Store.Sql
{
    [SugarIndex("index_timestamp", nameof(TimeStamp), OrderByType.Asc)]
    internal class StoreEntity
    {
        [SugarColumn(IsPrimaryKey = true)]
        public string Id { get; set; }
        [SugarColumn(ColumnDataType = "longtext")]
        public string Content { get; set; }
        public long TimeStamp { get; set; }
    }
}