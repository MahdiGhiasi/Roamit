
using System;
using System.IO;
using System.Security.AccessControl;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace PCLStorage.Droid
{
    public class CopiedReadOnlyFileStream : FileStream
    {
        string tempFile;

        private CopiedReadOnlyFileStream(string tempFile) :
            base(tempFile, FileMode.Open, System.IO.FileAccess.Read)
        {
            this.tempFile = tempFile;
        }

        public static async Task<CopiedReadOnlyFileStream> CreateForStream(Stream stream)
        {
            var tempFileName = GetTempFileName();

            using (var newStream = new FileStream(tempFileName, FileMode.Create, System.IO.FileAccess.ReadWrite))
            {
                await stream.CopyToAsync(newStream);
                newStream.Close();
            }

            return new CopiedReadOnlyFileStream(tempFileName);
        }

        private static string GetTempFileName()
        {
            string fileName;
            var tempPath = GetTempPath();

            do
            {
                fileName = Path.Combine(tempPath, Path.GetRandomFileName());
            } while (File.Exists(fileName));

            return fileName;
        }

        private static string GetTempPath()
        {
            var path = Path.Combine(Path.GetTempPath(), "tempFiles");
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            return path;
        }

        private void DeleteTempFile()
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }

        public override void Close()
        {
            base.Close();
            DeleteTempFile();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            DeleteTempFile();
        }
    }
}