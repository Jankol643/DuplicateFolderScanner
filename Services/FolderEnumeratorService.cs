using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DuplicateFolderScanner.Services
{
    public class FolderEnumeratorService
    {
        public event Action<int>? FolderCountUpdated;
        public event Action<string>? ErrorOccurred;

        private const int BATCH_SIZE = 100;

        public int CountTotalFolders(string rootPath)
        {
            var count = 0;
            foreach (var folder in GetAllFolders(rootPath))
            {
                count++;
                FolderCountUpdated?.Invoke(count);
            }
            return count;
        }

        public IEnumerable<string> GetAllFolders(string rootPath)
        {
            var stack = new Stack<string>();
            stack.Push(rootPath);

            while (stack.Count > 0)
            {
                var currentDir = stack.Pop();
                yield return currentDir;

                try
                {
                    foreach (var subDir in Directory.EnumerateDirectories(currentDir))
                    {
                        stack.Push(subDir);
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    continue;
                }
                catch (Exception ex) when (IsNonCriticalException(ex))
                {
                    ErrorOccurred?.Invoke($"Non-critical error accessing {currentDir}: {ex.Message}");
                    continue;
                }
            }
        }

        public IEnumerable<List<string>> GetFoldersInBatches(string rootPath)
        {
            var currentBatch = new List<string>();

            foreach (var folder in GetAllFolders(rootPath))
            {
                currentBatch.Add(folder);

                if (currentBatch.Count >= BATCH_SIZE)
                {
                    yield return currentBatch;
                    currentBatch = new List<string>();
                }
            }

            if (currentBatch.Count > 0)
            {
                yield return currentBatch;
            }
        }

        private bool IsNonCriticalException(Exception ex)
        {
            return ex is UnauthorizedAccessException ||
                   ex is PathTooLongException ||
                   ex is IOException ioEx && IsResourceExhaustionError(ioEx);
        }

        private bool IsResourceExhaustionError(Exception ex)
        {
            return ex is IOException ioEx &&
                   (ioEx.Message.Contains("Insufficient system resources") ||
                    ioEx.HResult == -2147024882);
        }
    }
}