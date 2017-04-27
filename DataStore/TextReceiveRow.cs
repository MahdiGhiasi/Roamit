using LiteDB;
using System;

namespace QuickShare.DataStore
{
    public class TextReceiveRow
    {
        [BsonIndex(true)]
        public Guid Id { get; internal set; }

        public string Content { get; internal set; }
    }
}