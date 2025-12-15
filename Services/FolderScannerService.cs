using DuplicateFolderScanner.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DuplicateFolderScanner.Services
{
    public class FolderScannerService
    {
        public event Action<ProgressData>? ProgressUpdated;
        public event Action<List<string>>? ScanCompleted;
        public event Action<string>? ErrorOccurred;
        public event Action? ScanCancelled;

        private readonly FolderEnumeratorService _folderEnumerator;
        private readonly DuplicateDetectionService _duplicateDetector;
        private readonly ProgressTracker _progressTracker;

        private CancellationTokenSource? _cts;
        private volatile bool _isScanning;
        private readonly object _scanLock = new object();

        public bool IsScanning => _isScanning;

        public FolderScannerService(
            FolderEnumeratorService folderEnumerator,
            DuplicateDetectionService duplicateDetector,
            ProgressTracker progressTracker)
        {
            _folderEnumerator = folderEnumerator ?? throw new ArgumentNullException(nameof(folderEnumerator));
            _duplicateDetector = duplicateDetector ?? throw new ArgumentNullException(nameof(duplicateDetector));
            _progressTracker = progressTracker ?? throw new ArgumentNullException(nameof(progressTracker));

            _folderEnumerator.ErrorOccurred += OnEnumeratorError;
            _progressTracker.ProgressUpdated += OnProgressUpdated;
        }

        public void ScanFolders(string rootPath, string blacklistText)
        {
            lock (_scanLock)
            {
                if (_isScanning) return;
                _isScanning = true;
                _cts = new CancellationTokenSource();
            }

            Task.Run(() =>
            {
                try
                {
                    ExecuteScan(rootPath, blacklistText, _cts.Token);
                }
                catch (OperationCanceledException)
                {
                    NotifyCancellation();
                }
                catch (Exception ex)
                {
                    NotifyError($"Error during scan: {ex.Message}");
                }
                finally
                {
                    ResetScannerState();
                }
            });
        }

        public void CancelScan()
        {
            lock (_scanLock)
            {
                if (_isScanning)
                {
                    _cts?.Cancel();
                }
            }
        }

        private void ExecuteScan(string rootPath, string blacklistText, CancellationToken token)
        {
            _progressTracker.Start();

            if (!ValidateRootPath(rootPath))
                return;

            var blacklist = ParseBlacklist(blacklistText);

            // Phase 1: Count total folders with progress
            _progressTracker.UpdateImmediate(0, 1, "Counting folders...");

            int totalFolders = 0;
            int counted = 0;

            // Count folders with incremental progress
            foreach (var folder in _folderEnumerator.GetAllFolders(rootPath))
            {
                totalFolders++;
                counted++;

                // Update progress every 100 folders during counting
                if (counted % 100 == 0 || counted == 1)
                {
                    _progressTracker.UpdateImmediate(0, totalFolders, $"Counting... ({totalFolders} folders found)");
                }

                if (token.IsCancellationRequested)
                    throw new OperationCanceledException();
            }

            if (totalFolders == 0)
            {
                NotifyError("No folders found to scan.");
                return;
            }

            // Phase 2: Scan for duplicates with better progress tracking
            _progressTracker.UpdateImmediate(0, totalFolders, "Starting duplicate detection...");

            var folderBatches = _folderEnumerator.GetFoldersInBatches(rootPath);
            var processed = 0;
            var lastUpdateCount = 0;

            var duplicateFolders = _duplicateDetector.FindDuplicateFolders(
                folderBatches,
                blacklist,
                token,
                (currentProcessed, currentFolder) =>
                {
                    processed = currentProcessed;

                    // Update progress more frequently:
                    // 1. Always update when starting
                    // 2. Update every 10 folders for small totals
                    // 3. Update every 1% for large totals
                    var shouldUpdate = processed == 0 ||
                                      processed == totalFolders ||
                                      (totalFolders < 1000 && processed - lastUpdateCount >= 10) ||
                                      (totalFolders >= 1000 && processed - lastUpdateCount >= totalFolders / 100);

                    if (shouldUpdate)
                    {
                        _progressTracker.Update(processed, totalFolders, currentFolder);
                        lastUpdateCount = processed;
                    }
                });

            if (!token.IsCancellationRequested)
            {
                var results = FormatScanResults(duplicateFolders);
                _progressTracker.Complete();
                NotifyCompletion(results);
            }
        }

        private bool ValidateRootPath(string rootPath)
        {
            if (string.IsNullOrWhiteSpace(rootPath))
            {
                NotifyError("Please enter a folder path.");
                return false;
            }

            if (!Directory.Exists(rootPath))
            {
                NotifyError("The specified folder path does not exist.");
                return false;
            }

            return true;
        }

        private HashSet<string> ParseBlacklist(string blacklistText)
        {
            if (string.IsNullOrWhiteSpace(blacklistText))
                return new HashSet<string>();

            return blacklistText.Split(',', StringSplitOptions.RemoveEmptyEntries)
                              .Select(s => s.Trim().ToLowerInvariant())
                              .Where(s => !string.IsNullOrWhiteSpace(s))
                              .ToHashSet();
        }

        private List<string> FormatScanResults(Dictionary<string, List<string>> duplicateFolders)
        {
            var listItems = new List<string>();

            foreach (var kvp in duplicateFolders.OrderBy(x => x.Key))
            {
                listItems.Add($"{kvp.Key} ({kvp.Value.Count} folders)");
                listItems.AddRange(kvp.Value.OrderBy(x => x));
                listItems.Add("");
            }

            return listItems;
        }

        private void OnEnumeratorError(string errorMessage)
        {
            NotifyError($"Folder enumeration error: {errorMessage}");
        }

        private void OnProgressUpdated(ProgressData progressData)
        {
            ProgressUpdated?.Invoke(progressData);
        }

        private void NotifyError(string message)
        {
            ErrorOccurred?.Invoke(message);
        }

        private void NotifyCompletion(List<string> results)
        {
            ScanCompleted?.Invoke(results);
        }

        private void NotifyCancellation()
        {
            ScanCancelled?.Invoke();
        }

        private void ResetScannerState()
        {
            lock (_scanLock)
            {
                _isScanning = false;
                _cts?.Dispose();
                _cts = null;
            }

            _progressTracker.Reset();
        }
    }
}