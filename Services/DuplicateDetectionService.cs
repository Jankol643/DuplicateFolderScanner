using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace DuplicateFolderScanner.Services
{
    public class DuplicateDetectionService
    {
        public Dictionary<string, List<string>> FindDuplicateFolders(
            IEnumerable<List<string>> folderStream,
            HashSet<string> blacklist,
            CancellationToken token,
            Action<int, string>? progressCallback = null)
        {
            var folderData = new Dictionary<string, List<string>>();
            int processedCount = 0;

            foreach (var batch in folderStream)
            {
                foreach (var folderPath in batch)
                {
                    token.ThrowIfCancellationRequested();
                    processedCount++;

                    try
                    {
                        var folderInfo = GetFolderInfo(folderPath, blacklist);
                        if (folderInfo != null)
                        {
                            if (!folderData.ContainsKey(folderInfo))
                            {
                                folderData[folderInfo] = new List<string>();
                            }
                            folderData[folderInfo].Add(folderPath);
                        }

                        // Report progress
                        progressCallback?.Invoke(processedCount, folderPath);
                    }
                    catch (Exception ex) when (IsNonCriticalException(ex))
                    {
                        continue;
                    }
                }
            }

            // Return only duplicates
            return folderData.Where(kvp => kvp.Value.Count > 1)
                         .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        private string? GetFolderInfo(string folderPath, HashSet<string> blacklist)
        {
            var folderName = Path.GetFileName(folderPath);

            // Skip blacklisted folders
            if (blacklist.Contains(folderName.ToLowerInvariant()))
                return null;

            try
            {
                var fileCount = Directory.EnumerateFiles(folderPath, "*", SearchOption.TopDirectoryOnly).Count();
                var folderSize = CalculateFolderSize(folderPath);

                return $"{folderName} - Files: {fileCount} - Size: {folderSize}";
            }
            catch (Exception ex) when (IsNonCriticalException(ex))
            {
                return null;
            }
        }

        private long CalculateFolderSize(string folderPath)
        {
            long size = 0;
            try
            {
                foreach (var file in Directory.EnumerateFiles(folderPath, "*", SearchOption.TopDirectoryOnly))
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.Exists)
                        size += fileInfo.Length;
                }
            }
            catch
            {
                // Ignore errors in size calculation
            }

            return size;
        }

        private bool IsNonCriticalException(Exception ex)
        {
            return ex is UnauthorizedAccessException ||
                   ex is PathTooLongException ||
                   ex is IOException;
        }
    }
}