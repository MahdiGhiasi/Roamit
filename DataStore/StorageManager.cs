using LiteDB;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace QuickShare.DataStore
{
    public class StorageManager<T>
    {
        protected LiteDatabase db;
        protected LiteCollection<T> data;
        protected string dbPath;
        protected string collectionName;
        protected SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);

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

            await semaphore.WaitAsync();

            db = new LiteDatabase($"Filename={dbPath};");
            data = db.GetCollection<T>(collectionName);
        }
        
        public void Close()
        {
            db?.Dispose();
            db = null;
            data = null;
            semaphore.Release();
        }

        public void Clear()
        {
            db.DropCollection(collectionName);
        }
    }
}