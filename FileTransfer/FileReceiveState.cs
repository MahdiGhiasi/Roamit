using Newtonsoft.Json;
using PCLStorage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickShare.FileTransfer
{
    class FileReceiveState
    {
        IFolder folder;

        public FileReceiveState(IFolder _folder)
        {
            folder = _folder;
        }

        public List<string> Downloading { get; } = new List<string>();

        public bool IsQueue { get; set; } = false;
        public long QueueTotalSlices { get; set; } = 0;
        public int QueueSlicesFinished { get; set; } = 0;
        public int QueuedSlicesYet { get; set; } = 0;
        public List<Dictionary<string, object>> QueueItems { get; } = new List<Dictionary<string, object>>();
        public string QueueFinishUrl { get; set; } = "";
        public int FilesCount { get; set; } = 0;
        public string QueueParentDirectory { get; set; } = "";

        public int LatestReceivedQueueItemGroupId { get; set; } = -1;

        public Guid RequestGuid { get; set; }
        public string SenderName { get; set; } = "remote device";

        public async Task SaveState()
        {
            var file = await folder.CreateFileAsync($"receive-{RequestGuid}.txt", CreationCollisionOption.ReplaceExisting);

            var text = JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.Indented);
            await file.WriteAllTextAsync(text);
        }

        internal static async Task<FileReceiveState> LoadState(Guid requestGuid, IFolder folder)
        {
            var file = await folder.GetFileAsync($"receive-{requestGuid}.txt");
            var text = await file.ReadAllTextAsync();
            var state = JsonConvert.DeserializeObject<FileReceiveState>(text);
            state.folder = folder;

            return state;
        }
    }
}
