using LiteDB;

namespace QuickShare.DataStore
{
    public class SettingsRow
    {
        [BsonIndex(true)]
        public string Key { get; internal set; }

        public string Value { get; internal set; }
    }
}