using LiteDB;
using System;

namespace QuickShare.DataStore
{
    public class StorageManager<T>
    {
        protected LiteDatabase db;
        protected LiteCollection<T> data;
        protected string dbPath;
        protected string collectionName;

        protected StorageManager(string _dbPath, string _collectionName)
        {
            dbPath = _dbPath;
            collectionName = _collectionName;
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
                data = db.GetCollection<T>(collectionName);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to open {dbPath}");
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                System.Diagnostics.Debug.WriteLine("*****");
            }
        }

        public void Close()
        {
            db.Dispose();
            db = null;
            data = null;
        }
    }
}