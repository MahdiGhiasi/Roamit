using LiteDB;
using System;

namespace QuickShare.DataStore
{
    internal class TextReceiveRow
    {
        [BsonIndex(true)]
        public Guid Id { get; internal set; }

        public string Content { get; internal set; }
    }
}