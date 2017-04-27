using System;
using System.Collections.Generic;
using System.Text;

namespace QuickShare.DataStore
{
    public static class DataStorageProviders
    {
        private static string _workingDirectory;

        private static TextReceiveContentManager _textReceiveContentManager = null;
        public static TextReceiveContentManager TextReceiveContentManager
        {
            get
            {
                if (_textReceiveContentManager == null)
                {
                    throw new Exception("DataStore is not initialized yet.");
                }
                return _textReceiveContentManager;
            }
        }

        private static HistoryManager _historyManager = null;
        public static HistoryManager HistoryManager
        {
            get
            {
                if (_historyManager == null)
                {
                    throw new Exception("DataStore is not initialized yet.");
                }
                return _historyManager;
            }
        }

        public static void Init(string workingDirectory)
        {
            _workingDirectory = workingDirectory;

            _textReceiveContentManager = new TextReceiveContentManager(System.IO.Path.Combine(_workingDirectory, "TextReceive.db"));
            _historyManager = new HistoryManager(System.IO.Path.Combine(_workingDirectory, "History.db"));
        }
    }
}
