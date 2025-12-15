using DuplicateFolderScanner.Models;
using System;
using System.Diagnostics;

namespace DuplicateFolderScanner.Services
{
    public class ProgressTracker
    {
        public event Action<ProgressData>? ProgressUpdated;

        private readonly Stopwatch _stopwatch;
        private readonly int _updateIntervalMs;
        private DateTime _lastUpdateTime;

        private int _processedFolders;
        private int _totalFolders;
        private string _currentFolder;
        private bool _isRunning;

        public ProgressTracker(int updateIntervalMs = 100) // Reduced to 100ms for smoother updates
        {
            _updateIntervalMs = updateIntervalMs;
            _stopwatch = new Stopwatch();
            Reset();
        }

        public void Start()
        {
            _isRunning = true;
            _stopwatch.Restart();
            _lastUpdateTime = DateTime.Now;
        }

        public void Update(int processed, int total, string currentFolder)
        {
            _processedFolders = processed;
            _totalFolders = total;
            _currentFolder = currentFolder;

            var now = DateTime.Now;
            if ((now - _lastUpdateTime).TotalMilliseconds >= _updateIntervalMs)
            {
                ForceUpdate();
                _lastUpdateTime = now;
            }
        }

        // New method for immediate updates when progress is critical
        public void UpdateImmediate(int processed, int total, string currentFolder)
        {
            _processedFolders = processed;
            _totalFolders = total;
            _currentFolder = currentFolder;
            ForceUpdate();
        }

        public void Complete()
        {
            // Force final update with 100% progress
            _processedFolders = _totalFolders;
            ForceUpdate();
            _isRunning = false;
            _stopwatch.Stop();
        }

        public void Reset()
        {
            _isRunning = false;
            _stopwatch.Reset();
            _processedFolders = 0;
            _totalFolders = 0;
            _currentFolder = "Starting scan...";
            _lastUpdateTime = DateTime.MinValue;
        }

        private void ForceUpdate()
        {
            if (!_isRunning) return;

            var progressData = CalculateProgressData();
            ProgressUpdated?.Invoke(progressData);
        }

        private ProgressData CalculateProgressData()
        {
            var progress = _totalFolders > 0 ? (double)_processedFolders / _totalFolders : 0;
            var elapsedSeconds = _stopwatch.Elapsed.TotalSeconds;

            double etaSeconds = 0;
            if (progress > 0 && progress < 1)
            {
                var totalEstimatedSeconds = elapsedSeconds / progress;
                etaSeconds = totalEstimatedSeconds - elapsedSeconds;
            }

            var displayFolder = _currentFolder.Length > 60
                ? "..." + _currentFolder.Substring(_currentFolder.Length - 57)
                : _currentFolder;

            return new ProgressData
            {
                Progress = progress,
                ProgressText = $"Progress: {(int)(progress * 100)}%",
                ElapsedText = $"Elapsed Time: {FormatTime(elapsedSeconds)}",
                EtaText = progress >= 1 ? "Estimated Time Remaining: Completed" : $"Estimated Time Remaining: {FormatTime(etaSeconds)}",
                FilesProcessedText = $"Folders Processed: {_processedFolders}/{_totalFolders}",
                CurrentFolderText = $"Scanning: {displayFolder}",
                ProcessedFolders = _processedFolders,
                TotalFolders = _totalFolders
            };
        }

        private string FormatTime(double seconds)
        {
            if (seconds < 1) return $"{seconds * 1000:F0}ms";
            else if (seconds < 60) return $"{seconds:F1}s";
            else
            {
                var timeSpan = TimeSpan.FromSeconds(seconds);
                return timeSpan.TotalHours >= 1
                    ? $"{(int)timeSpan.TotalHours}h {timeSpan.Minutes}m {timeSpan.Seconds}s"
                    : $"{timeSpan.Minutes}m {timeSpan.Seconds}s";
            }
        }
    }
}