using System;

namespace QuickShare.Common.Classes
{
    public class SaveAsFailedException : Exception
    {
        public string ExtraDetails { get; }

        public SaveAsFailedException()
        {
        }

        public SaveAsFailedException(string message) : base(message)
        {
        }

        public SaveAsFailedException(string message, string extraDetails) : base(message)
        {
            ExtraDetails = extraDetails;
        }

        public SaveAsFailedException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}