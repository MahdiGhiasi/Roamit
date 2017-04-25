using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickShare.DataStore
{
    public class TextReceiveContentManager
    {
        LiteDatabase db;
        LiteCollection<TextReceiveRow> data;
        string dbPath;

        //Internal constructor
        internal TextReceiveContentManager(string _dbPath)
        {
            dbPath = _dbPath;
        }

        public bool IsOpened
        {
            get
            {
                return (db != null);
            }
        }

        public void Open()
        {
            try
            {
                db = new LiteDatabase($"Filename={dbPath};");
                data = db.GetCollection<TextReceiveRow>("Data");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Failed to open db.");
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                System.Diagnostics.Debug.WriteLine("*****");
            }
        }

        public bool ContainsKey(Guid guid)
        {
            return data.Exists(x => x.Id == guid);
        }

        public void Add(Guid guid, string v)
        {
            if (ContainsKey(guid))
            {
                var item = GetItem(guid);
                item.Content = v;
                data.Update(guid, item);
            }
            else
            {
                data.Insert(new TextReceiveRow
                {
                    Id = guid,
                    Content = v,
                });
            }
        }

        public void Remove(Guid guid)
        {
            data.Delete(x => x.Id == guid);
        }

        internal TextReceiveRow GetItem(Guid guid)
        {
            return data.FindById(guid);
        }

        public string GetItemContent(Guid guid)
        {
            return GetItem(guid).Content;
        }

        public void Close()
        {
            db.Dispose();
            db = null;
            data = null;
        }
    }
}
