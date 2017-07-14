namespace QuickShare.ViewModels.History
{
    public class FileInfo
    {
        public string FileName { get; set; }
        public string Path { get; set; }

        public override string ToString()
        {
            return FileName;
        }
    }
}