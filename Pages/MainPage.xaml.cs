using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using DuplicateFolderScanner.Services;
using DuplicateFolderScanner.Models;

namespace DuplicateFolderScanner.Pages
{
    public partial class MainPage : ContentPage
    {
        private readonly FolderScannerService _scannerService;

        public MainPage(FolderScannerService scannerService)
        {
            InitializeComponent();
            _scannerService = scannerService;

            // Subscribe to events
            _scannerService.ProgressUpdated += OnProgressUpdated;
            _scannerService.ScanCompleted += OnScanCompleted;
            _scannerService.ErrorOccurred += OnErrorOccurred;
            _scannerService.ScanCancelled += OnScanCancelled;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            // Set initial state when page is visible
            UpdateUIState(false);
        }

        private void OnProgressUpdated(ProgressData progressData)
        {
            // Update UI on main thread
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (ProgressBar != null) ProgressBar.Progress = progressData.Progress;
                if (ProgressValueLabel != null) ProgressValueLabel.Text = progressData.ProgressText;
                if (ElapsedTimeValueLabel != null) ElapsedTimeValueLabel.Text = progressData.ElapsedText;
                if (ETAValueLabel != null) ETAValueLabel.Text = progressData.EtaText;
                if (FilesProcessedValueLabel != null) FilesProcessedValueLabel.Text = progressData.FilesProcessedText;
                if (CurrentFolderValueLabel != null) CurrentFolderValueLabel.Text = progressData.CurrentFolderText;
            });
        }

        private void OnScanCompleted(List<string> results)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (DuplicatesCollection != null)
                    DuplicatesCollection.ItemsSource = results;

                int duplicateGroups = results?.Count > 0 ? results.Count - 1 : 0;

                // Show completion message
                DisplayAlert("Scan Complete",
                    $"Scan completed! Found {duplicateGroups} duplicate groups.", "OK");
            });
        }

        private void OnErrorOccurred(string errorMessage)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                UpdateUIState(false);
                DisplayAlert("Error", errorMessage, "OK");
            });
        }

        private void OnScanCancelled()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                UpdateUIState(false);
                DisplayAlert("Cancelled", "Scan was cancelled.", "OK");
            });
        }

        private void OnStartScan(object sender, EventArgs e)
        {
            var folderPath = FolderPathEntry?.Text?.Trim();
            var blacklist = BlacklistEditor?.Text?.Trim();

            // Clear previous results
            if (DuplicatesCollection != null)
                DuplicatesCollection.ItemsSource = null;

            // Start the scan
            UpdateUIState(true);
            _scannerService.ScanFolders(folderPath, blacklist);
        }

        private void OnCancelScan(object sender, EventArgs e)
        {
            _scannerService.CancelScan();
        }

        private void UpdateUIState(bool isScanning)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                // Null checks for all UI elements
                if (StartButton != null) StartButton.IsEnabled = !isScanning;
                if (CancelButton != null) CancelButton.IsEnabled = isScanning;

                if (!isScanning)
                {
                    if (ProgressBar != null) ProgressBar.Progress = 0;
                    if (ProgressValueLabel != null) ProgressValueLabel.Text = "0%";
                    if (ElapsedTimeValueLabel != null) ElapsedTimeValueLabel.Text = "0s";
                    if (ETAValueLabel != null) ETAValueLabel.Text = "waiting...";
                    if (FilesProcessedValueLabel != null) FilesProcessedValueLabel.Text = "0";
                    if (CurrentFolderValueLabel != null) CurrentFolderValueLabel.Text = "Ready to scan...";
                }
            });
        }
    }
}