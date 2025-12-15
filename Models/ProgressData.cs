using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DuplicateFolderScanner.Models
{
    public class ProgressData
    {
        public int Current { get; set; }
        public int Total { get; set; }
        public string CurrentItem { get; set; } = string.Empty;
        public string StatusMessage { get; set; } = string.Empty;
        public double Percentage => Total > 0 ? (double)Current / Total * 100 : 0;

        public double Progress { get; internal set; }
        public string ProgressText { get; internal set; }
        public string ElapsedText { get; internal set; }
        public string EtaText { get; internal set; }
        public string FilesProcessedText { get; internal set; }
        public string CurrentFolderText { get; internal set; }
        public int ProcessedFolders { get; internal set; }
        public int TotalFolders { get; internal set; }
    }
}
