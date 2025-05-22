using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using VisualKeyloggerDetector.Core.Translation; // Needs access to KeystrokeStreamSchedule
using VisualKeyloggerDetector;
using VisualKeyloggerDetector.Core.Monitoring;

namespace VisualKeyloggerDetector.Core.Injection
{
    public class InjectorResult : Dictionary<uint, List<ulong>> { }
    /// <summary>
    /// Responsible for injecting simulated keystrokes into the system based on a schedule.
    /// Uses unprivileged APIs to mimic user input.
    /// </summary>
    public class Injector
    {
        private readonly Random _random = new Random();
        // Characters to inject. Can be customized.
        private readonly string _charsToInject = "abcdefghijklmnopqrstuvwxyz";

        private  ExperimentConfiguration _config;
        MonitoringResult monitoringResult;
        /// <summary>
        /// Occurs when there is a status update message from the injector.
        /// </summary>
        public event EventHandler<string> StatusUpdate;

        /// <summary>
        /// Occurs when the injector completes an interval, reporting the index of the completed interval (0-based).
        /// </summary>
        public event EventHandler<int> ProgressUpdate;

        /// <summary>
        /// Raises the StatusUpdate event.
        /// </summary>
        /// <param name="message">The status message.</param>
        protected virtual void OnStatusUpdate(string message) => StatusUpdate?.Invoke(this, message);

        /// <summary>
        /// Raises the ProgressUpdate event.
        /// </summary>
        /// <param name="intervalIndex">The index of the interval just completed (0-based).</param>
        protected virtual void OnProgressUpdate(int intervalIndex) => ProgressUpdate?.Invoke(this, intervalIndex);

        /// <summary>
        /// Asynchronously injects keystrokes according to the provided schedule.
        /// Attempts to distribute the keys somewhat evenly within each interval.
        /// </summary>
        /// <param name="schedule">The schedule defining the number of keys per interval and interval duration.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A Task representing the asynchronous injection operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="schedule"/> is null.</exception>
        /// <exception cref="OperationCanceledException">Thrown if the operation is cancelled via the <paramref name="cancellationToken"/>.</exception>
        public async Task<InjectorResult> InjectStreamAsync(KeystrokeStreamSchedule schedule, IEnumerable<uint> processIdsToMonitor, ExperimentConfiguration _config1, CancellationToken cancellationToken = default)
        {
            _config = _config1 ?? throw new ArgumentNullException(nameof(_config1));
            if (schedule == null) throw new ArgumentNullException(nameof(schedule));

            OnStatusUpdate("Starting keystroke injection...");
            int totalIntervals = schedule.KeysPerInterval.Count;
            var stopwatch = new Stopwatch();

            for (int i = 0; i < totalIntervals; i++)
            {
                // Check for cancellation at the start of each interval
                cancellationToken.ThrowIfCancellationRequested();

                int keysInThisInterval = schedule.KeysPerInterval[i];
                int intervalDuration = schedule.IntervalDurationMs;
                OnStatusUpdate($"Interval {i + 1}/{totalIntervals}: Injecting {keysInThisInterval} keys over {intervalDuration}ms.");

                stopwatch.Restart();

                if (keysInThisInterval > 0 && intervalDuration > 0) // Ensure duration is positive for delay calculation
                {
                    // Distribute keys somewhat evenly within the interval
                    // Calculate average delay, handling potential division by zero if intervalDuration is 0
                    double delayBetweenKeys = (double)intervalDuration / keysInThisInterval;
                    double accumulatedDelayError = 0; // Accumulates fractional parts of delays

                    for (int k = 0; k < keysInThisInterval; k++)
                    {
                        // Check for cancellation before each key injection
                        cancellationToken.ThrowIfCancellationRequested();

                        // Inject a random character
                        try
                        {
                            char charToSend = _charsToInject[_random.Next(_charsToInject.Length)];
                            // Uses the static helper class KeyInputInjector (defined elsewhere)
                            KeyInputInjector.SendCharacter(charToSend);
                            Console.WriteLine($"Injected Charater {DateTime.Now.ToString("HH:mm:ss.fff")} " + charToSend);
                        }
                        catch (Exception ex)
                        {
                            // Log error but continue injection if possible
                            OnStatusUpdate($"Error sending key: {ex.Message}. Skipping key.");
                        }

                        // Calculate delay until the *next* key injection
                        // Only delay if there are more keys to send in this interval
                        if (k < keysInThisInterval - 1)
                        {
                            double currentDelay = delayBetweenKeys + accumulatedDelayError;
                            int waitTimeMs = (int)Math.Floor(currentDelay);
                            accumulatedDelayError = currentDelay - waitTimeMs; // Carry over the fractional part

                            if (waitTimeMs > 0)
                            {
                                await Task.Delay(waitTimeMs, cancellationToken);
                            }
                        }
                    } // End key loop (k)
                } // End if keysInThisInterval > 0

                stopwatch.Stop();
               Task< MonitoringResult> result;
                result = new Monitors(_config).MonitorProcessesAsync(processIdsToMonitor, cancellationToken);

                monitoringResult=await  result;
                


                // Ensure the full interval duration is respected by waiting for any remaining time.
                int elapsedTime = (int)stopwatch.ElapsedMilliseconds;
                int remainingTime = intervalDuration - elapsedTime+_config.T;
                if (remainingTime > 0)
                {
                    await Task.Delay(remainingTime, cancellationToken);
                }
                Console.WriteLine($"interval ended {DateTime.Now.ToString("HH:mm:ss.fff")} " + i);
                OnProgressUpdate(i); // Report progress after completing interval i

            } // End interval loop (i)
            return monitoringResult;
            OnProgressUpdate(totalIntervals - 1); // Indicate completion of the last interval
            OnStatusUpdate("Injection finished.");
        }
    }
}
