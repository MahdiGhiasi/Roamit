using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickShare.Common.Classes
{
    public class DownloadGroupByItem
    {
        public static IReadOnlyList<DownloadGroupByItem> GroupItems { get; } = null;

        static DownloadGroupByItem()
        {
            GroupItems = new List<DownloadGroupByItem>
            {
                new DownloadGroupByItem
                {
                    State = DownloadGroupByState.None,
                    Name = "Don't group",
                    Decider = x => "",
                    ShowExample = false,
                },
                new DownloadGroupByItem
                {
                    State = DownloadGroupByState.Month1,
                    Name = "Monthly",
                    Decider = x => x.ToString("yyyy MMMM"),
                    ShowExample = true,
                },
                new DownloadGroupByItem
                {
                    State = DownloadGroupByState.Month2,
                    Name = "Monthly",
                    Decider = x => x.ToString("yyyy-MM"),
                    ShowExample = true,
                },
                new DownloadGroupByItem
                {
                    State = DownloadGroupByState.Day1,
                    Name = "Daily",
                    Decider = x => x.ToString("yyyy MMMM dd"),
                    ShowExample = true,
                },
                new DownloadGroupByItem
                {
                    State = DownloadGroupByState.Day2,
                    Name = "Daily",
                    Decider = x => x.ToString("yyyy-MM-dd"),
                    ShowExample = true,
                },
                new DownloadGroupByItem
                {
                    State = DownloadGroupByState.Second1,
                    Name = "Per item",
                    Decider = x => x.ToString("yyyy MMMM dd - hh-mm-ss tt"),
                    ShowExample = true,
                },
                new DownloadGroupByItem
                {
                    State = DownloadGroupByState.Second2,
                    Name = "Per item",
                    Decider = x => x.ToString("yyyy-MM-dd hh-mm-ss-tt"),
                    ShowExample = true,
                },
            };
        }

        public DownloadGroupByState State { get; set; }
        public bool ShowExample { get; set; }
        public Func<DateTime, string> Decider { get; set; }
        public DateTime ExampleDateTime { get; } = DateTime.Now;

        private string name;
        public string Name
        {
            get { return name + (ShowExample ? $" ({Decider(ExampleDateTime)})" : ""); }
            set { name = value; }
        }


        public override string ToString()
        {
            return Name;
        }
    }

    public enum DownloadGroupByState
    {
        None = 0,
        Month1 = 1,
        Month2 = 2,
        Day1 = 3,
        Day2 = 4,
        Second1 = 5,
        Second2 = 6,
    }
}
