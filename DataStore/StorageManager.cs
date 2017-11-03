using LiteDB;
using System;
using System.Threading.Tasks;

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

        public async Task OpenAsync()
        {
            System.Diagnostics.Debug.WriteLine($"{this.GetType().ToString()}.Open()");

            while (IsOpened)
                await Task.Delay(100);

            OpenIfPossible();
        }

        public bool OpenIfPossible()
        {
            try
            {
                db = new LiteDatabase($"Filename={dbPath};");
                data = db.GetCollection<T>(collectionName);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to open {dbPath}");
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                System.Diagnostics.Debug.WriteLine("*****");
                return false;
            }
        }

        public void Close()
        {
            db?.Dispose();
            db = null;
            data = null;
        }

        public void Clear()
        {
            db.DropCollection(collectionName);
        }
    }
}