// Add this file to keylogger core project (e.g., Core/Utils/NotificationHelper.cs)
using CommunityToolkit.WinUI.Notifications; // Requires NuGet package
using System;
using VisualKeyloggerDetector.Core; // For DetectionResult

namespace VisualKeyloggerDetector.Core.Utils
{
    public static class NotificationHelper
    {
        /// <summary>
        /// Shows a toast notification indicating a potential keylogger detection.
        /// </summary>
        /// <param name="result">The detection result details.</param>
        public static void ShowDetectionNotification(DetectionResult result)
        {
            if (result == null || !result.IsDetected) return;

            try
            {
                new ToastContentBuilder()
                    .AddAppLogoOverride(new Uri("file:///" + System.IO.Path.GetFullPath("Resources/warning_icon.png")), ToastGenericAppLogoCrop.Circle) // Optional: Add an icon (ensure path is valid)
                    .AddText("Potential Keylogger Detected!", hintMaxLines: 1)
                    .AddText($"Process: {result.ProcessName} (PID: {result.ProcessId})")
                    .AddText($"Correlation: {result.Correlation:F4}")
                    // Optional: Add buttons for actions (requires more setup for activation handling)
                    // .AddButton(new ToastButton().SetContent("Suspend").SetArguments($"action=suspend&pid={result.ProcessId}"))
                    // .AddButton(new ToastButton().SetContent("Terminate").SetArguments($"action=terminate&pid={result.ProcessId}"))
                    .Show();

                Console.WriteLine($"Notification shown for PID: {result.ProcessId}");
            }
            catch (Exception ex)
            {
                // Handle exceptions if notifications fail (e.g., permissions, platform issues)
                Console.WriteLine($"Failed to show notification: {ex.Message}");
            }
        }
    }
}
