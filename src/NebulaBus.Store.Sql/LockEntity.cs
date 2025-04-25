namespace NebulaBus.Store.Sql
{
    internal class LockEntity
    {
        [SqlSugar.SugarColumn(IsPrimaryKey = true)]
        public string Id { get; set; }
        public string Value { get; set; }
        public long ExpireTime { get; set; }
    }
}
