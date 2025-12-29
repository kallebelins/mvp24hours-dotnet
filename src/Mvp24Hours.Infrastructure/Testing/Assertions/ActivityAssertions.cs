//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
#nullable enable
using Mvp24Hours.Infrastructure.Testing.Observability;
using System;
using System.Diagnostics;
using System.Linq;

namespace Mvp24Hours.Infrastructure.Testing.Assertions
{
    /// <summary>
    /// Provides assertion helpers for activities (spans) captured by FakeActivityListener.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These helpers provide fluent assertions for verifying distributed tracing
    /// spans in tests. They work with <see cref="FakeActivityListener"/>.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// using var listener = new FakeActivityListener("Mvp24Hours.*");
    /// 
    /// await service.ProcessAsync();
    /// 
    /// ActivityAssertions.AssertActivityRecorded(listener, "Mvp24Hours.Pipe.Pipeline");
    /// ActivityAssertions.AssertNoErrorActivities(listener);
    /// ActivityAssertions.AssertActivityHasTag(listener, "Mvp24Hours.Data.Query", "db.system", "postgresql");
    /// </code>
    /// </example>
    public static class ActivityAssertions
    {
        /// <summary>
        /// Asserts that an activity was recorded with the specified operation name.
        /// </summary>
        /// <param name="listener">The fake activity listener to check.</param>
        /// <param name="operationName">The expected operation name.</param>
        /// <exception cref="AssertionException">Thrown when no matching activity was found.</exception>
        public static void AssertActivityRecorded(FakeActivityListener listener, string operationName)
        {
            if (listener == null) throw new ArgumentNullException(nameof(listener));
            if (string.IsNullOrEmpty(operationName)) throw new ArgumentNullException(nameof(operationName));

            if (!listener.HasActivity(operationName))
            {
                var operations = string.Join(", ", listener.Activities.Select(a => a.OperationName).Distinct());
                throw new AssertionException(
                    $"Expected an activity with operation name '{operationName}', but none was found.\n" +
                    $"Recorded operations: [{operations}]");
            }
        }

        /// <summary>
        /// Asserts that an activity was recorded from the specified source.
        /// </summary>
        /// <param name="listener">The fake activity listener to check.</param>
        /// <param name="sourceName">The expected source name.</param>
        /// <exception cref="AssertionException">Thrown when no matching activity was found.</exception>
        public static void AssertActivityFromSource(FakeActivityListener listener, string sourceName)
        {
            if (listener == null) throw new ArgumentNullException(nameof(listener));
            if (string.IsNullOrEmpty(sourceName)) throw new ArgumentNullException(nameof(sourceName));

            if (!listener.HasActivityFromSource(sourceName))
            {
                var sources = string.Join(", ", listener.Activities.Select(a => a.SourceName).Distinct());
                throw new AssertionException(
                    $"Expected an activity from source '{sourceName}', but none was found.\n" +
                    $"Recorded sources: [{sources}]");
            }
        }

        /// <summary>
        /// Asserts that exactly the specified number of activities were recorded with the given operation name.
        /// </summary>
        /// <param name="listener">The fake activity listener to check.</param>
        /// <param name="operationName">The operation name to count.</param>
        /// <param name="expectedCount">The expected count.</param>
        /// <exception cref="AssertionException">Thrown when the count doesn't match.</exception>
        public static void AssertActivityCount(FakeActivityListener listener, string operationName, int expectedCount)
        {
            if (listener == null) throw new ArgumentNullException(nameof(listener));
            if (string.IsNullOrEmpty(operationName)) throw new ArgumentNullException(nameof(operationName));

            var actualCount = listener.GetActivities(operationName).Count;
            if (actualCount != expectedCount)
            {
                throw new AssertionException(
                    $"Expected {expectedCount} activity(ies) with operation '{operationName}', but found {actualCount}.");
            }
        }

        /// <summary>
        /// Asserts that no error activities were recorded.
        /// </summary>
        /// <param name="listener">The fake activity listener to check.</param>
        /// <exception cref="AssertionException">Thrown when an error activity was found.</exception>
        public static void AssertNoErrorActivities(FakeActivityListener listener)
        {
            if (listener == null) throw new ArgumentNullException(nameof(listener));

            if (listener.HasErrors())
            {
                var errors = listener.GetErrorActivities();
                var errorDetails = string.Join("\n", errors.Select(e =>
                    $"  - {e.OperationName}: {e.StatusDescription ?? "No description"}"));
                throw new AssertionException(
                    $"Expected no error activities, but found {errors.Count}:\n{errorDetails}");
            }
        }

        /// <summary>
        /// Asserts that an activity with the specified operation has the specified tag.
        /// </summary>
        /// <param name="listener">The fake activity listener to check.</param>
        /// <param name="operationName">The operation name.</param>
        /// <param name="tagKey">The tag key.</param>
        /// <param name="expectedValue">Optional expected value. If null, only checks for presence.</param>
        /// <exception cref="AssertionException">Thrown when the tag is not found or doesn't match.</exception>
        public static void AssertActivityHasTag(FakeActivityListener listener, string operationName, string tagKey, string? expectedValue = null)
        {
            if (listener == null) throw new ArgumentNullException(nameof(listener));
            if (string.IsNullOrEmpty(operationName)) throw new ArgumentNullException(nameof(operationName));
            if (string.IsNullOrEmpty(tagKey)) throw new ArgumentNullException(nameof(tagKey));

            var activity = listener.GetActivities(operationName).FirstOrDefault();
            if (activity == null)
            {
                throw new AssertionException(
                    $"No activity found with operation name '{operationName}'.");
            }

            if (!activity.HasTag(tagKey))
            {
                var existingTags = string.Join(", ", activity.Tags.Keys);
                throw new AssertionException(
                    $"Activity '{operationName}' does not have tag '{tagKey}'.\n" +
                    $"Existing tags: [{existingTags}]");
            }

            if (expectedValue != null)
            {
                var actualValue = activity.GetTag(tagKey);
                if (!string.Equals(actualValue, expectedValue, StringComparison.Ordinal))
                {
                    throw new AssertionException(
                        $"Activity '{operationName}' tag '{tagKey}' expected '{expectedValue}' but was '{actualValue}'.");
                }
            }
        }

        /// <summary>
        /// Asserts that an activity with the specified operation has an event.
        /// </summary>
        /// <param name="listener">The fake activity listener to check.</param>
        /// <param name="operationName">The operation name.</param>
        /// <param name="eventName">The event name to look for.</param>
        /// <exception cref="AssertionException">Thrown when the event is not found.</exception>
        public static void AssertActivityHasEvent(FakeActivityListener listener, string operationName, string eventName)
        {
            if (listener == null) throw new ArgumentNullException(nameof(listener));
            if (string.IsNullOrEmpty(operationName)) throw new ArgumentNullException(nameof(operationName));
            if (string.IsNullOrEmpty(eventName)) throw new ArgumentNullException(nameof(eventName));

            var activity = listener.GetActivities(operationName).FirstOrDefault();
            if (activity == null)
            {
                throw new AssertionException(
                    $"No activity found with operation name '{operationName}'.");
            }

            if (!activity.HasEvent(eventName))
            {
                var existingEvents = string.Join(", ", activity.Events.Select(e => e.Name));
                throw new AssertionException(
                    $"Activity '{operationName}' does not have event '{eventName}'.\n" +
                    $"Existing events: [{existingEvents}]");
            }
        }

        /// <summary>
        /// Asserts that an activity with the specified operation has the expected kind.
        /// </summary>
        /// <param name="listener">The fake activity listener to check.</param>
        /// <param name="operationName">The operation name.</param>
        /// <param name="expectedKind">The expected activity kind.</param>
        /// <exception cref="AssertionException">Thrown when the kind doesn't match.</exception>
        public static void AssertActivityKind(FakeActivityListener listener, string operationName, ActivityKind expectedKind)
        {
            if (listener == null) throw new ArgumentNullException(nameof(listener));
            if (string.IsNullOrEmpty(operationName)) throw new ArgumentNullException(nameof(operationName));

            var activity = listener.GetActivities(operationName).FirstOrDefault();
            if (activity == null)
            {
                throw new AssertionException(
                    $"No activity found with operation name '{operationName}'.");
            }

            if (activity.Kind != expectedKind)
            {
                throw new AssertionException(
                    $"Activity '{operationName}' expected kind '{expectedKind}' but was '{activity.Kind}'.");
            }
        }

        /// <summary>
        /// Asserts that an activity completed within the specified maximum duration.
        /// </summary>
        /// <param name="listener">The fake activity listener to check.</param>
        /// <param name="operationName">The operation name.</param>
        /// <param name="maxDuration">The maximum allowed duration.</param>
        /// <exception cref="AssertionException">Thrown when the duration exceeds the maximum.</exception>
        public static void AssertActivityDurationLessThan(FakeActivityListener listener, string operationName, TimeSpan maxDuration)
        {
            if (listener == null) throw new ArgumentNullException(nameof(listener));
            if (string.IsNullOrEmpty(operationName)) throw new ArgumentNullException(nameof(operationName));

            var activity = listener.GetActivities(operationName).FirstOrDefault();
            if (activity == null)
            {
                throw new AssertionException(
                    $"No activity found with operation name '{operationName}'.");
            }

            if (activity.Duration > maxDuration)
            {
                throw new AssertionException(
                    $"Activity '{operationName}' duration ({activity.Duration.TotalMilliseconds:F2}ms) " +
                    $"exceeded maximum ({maxDuration.TotalMilliseconds:F2}ms).");
            }
        }

        /// <summary>
        /// Asserts that no activities were recorded.
        /// </summary>
        /// <param name="listener">The fake activity listener to check.</param>
        /// <exception cref="AssertionException">Thrown when any activity was recorded.</exception>
        public static void AssertNoActivitiesRecorded(FakeActivityListener listener)
        {
            if (listener == null) throw new ArgumentNullException(nameof(listener));

            if (listener.ActivityCount > 0)
            {
                var activities = string.Join("\n", listener.Activities.Select(a => $"  - {a}"));
                throw new AssertionException(
                    $"Expected no activities to be recorded, but found {listener.ActivityCount}:\n{activities}");
            }
        }

        /// <summary>
        /// Asserts that activities form a valid parent-child relationship.
        /// </summary>
        /// <param name="listener">The fake activity listener to check.</param>
        /// <param name="parentOperationName">The parent operation name.</param>
        /// <param name="childOperationName">The child operation name.</param>
        /// <exception cref="AssertionException">Thrown when the relationship is not valid.</exception>
        public static void AssertParentChildRelationship(FakeActivityListener listener, string parentOperationName, string childOperationName)
        {
            if (listener == null) throw new ArgumentNullException(nameof(listener));
            if (string.IsNullOrEmpty(parentOperationName)) throw new ArgumentNullException(nameof(parentOperationName));
            if (string.IsNullOrEmpty(childOperationName)) throw new ArgumentNullException(nameof(childOperationName));

            var parent = listener.GetActivities(parentOperationName).FirstOrDefault();
            var child = listener.GetActivities(childOperationName).FirstOrDefault();

            if (parent == null)
            {
                throw new AssertionException(
                    $"Parent activity '{parentOperationName}' not found.");
            }

            if (child == null)
            {
                throw new AssertionException(
                    $"Child activity '{childOperationName}' not found.");
            }

            // Check if child has parent's span ID as its parent
            if (child.ParentId != parent.Id)
            {
                throw new AssertionException(
                    $"Activity '{childOperationName}' is not a child of '{parentOperationName}'.\n" +
                    $"Child ParentId: {child.ParentId}, Expected Parent Id: {parent.Id}");
            }
        }

        /// <summary>
        /// Gets the first recorded activity with the specified operation name for further assertions.
        /// </summary>
        /// <param name="listener">The fake activity listener to check.</param>
        /// <param name="operationName">The operation name.</param>
        /// <returns>The recorded activity.</returns>
        /// <exception cref="AssertionException">Thrown when no matching activity is found.</exception>
        public static RecordedActivity GetActivity(FakeActivityListener listener, string operationName)
        {
            if (listener == null) throw new ArgumentNullException(nameof(listener));
            if (string.IsNullOrEmpty(operationName)) throw new ArgumentNullException(nameof(operationName));

            var activity = listener.GetActivities(operationName).FirstOrDefault();
            if (activity == null)
            {
                var operations = string.Join(", ", listener.Activities.Select(a => a.OperationName).Distinct());
                throw new AssertionException(
                    $"No activity found with operation name '{operationName}'.\n" +
                    $"Recorded operations: [{operations}]");
            }

            return activity;
        }
    }
}

