using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickShare.DataStore
{
    public class SettingsManager : StorageManager<SettingsRow>
    {
        internal SettingsManager(string _dbPath) : base(_dbPath, "Data")
        {
        }

        public bool ContainsKey(string key)
        {
            return data.Exists(x => x.Key == key);
        }

        public void Add(string key, string value)
        {
            if (ContainsKey(key))
            {
                Remove(key);
            }

            data.Insert(new SettingsRow
            {
                Key = key,
                Value = value,
            });

        }

        public void Remove(string key)
        {
            data.Delete(x => x.Key == key);
        }

        internal SettingsRow GetItem(string key)
        {
            return data.FindOne(Query.EQ("Key", key));
        }

        public string GetItemContent(string key)
        {
            return GetItem(key).Value;
        }
    }
}
