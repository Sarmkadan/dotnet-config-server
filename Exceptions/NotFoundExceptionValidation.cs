using System;
using System.Collections.Generic;

namespace DotnetConfigServer.Exceptions
{
    /// <summary>
    /// Provides validation helpers for <see cref="NotFoundException"/> instances.
    /// </summary>
    public static class NotFoundExceptionValidation
    {
        /// <summary>
        /// Validates the specified <see cref="NotFoundException"/> instance.
        /// </summary>
        /// <param name="value">The exception to validate.</param>
        /// <returns>A list of validation problems; empty if the exception is valid.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
        public static IReadOnlyList<string> Validate(this NotFoundException value)
        {
            ArgumentNullException.ThrowIfNull(value);

            return value switch
            {
                ApplicationNotFoundException appEx => ValidateApplicationId(appEx),
                UserNotFoundException userEx => ValidateUserId(userEx),
                WebhookDeliveryNotFoundException deliveryEx => ValidateDeliveryId(deliveryEx),
                ChangeRequestNotFoundException changeEx => ValidateChangeRequestId(changeEx),
                _ => Array.Empty<string>()
            };
        }

        /// <summary>
        /// Determines whether the specified <see cref="NotFoundException"/> instance is valid.
        /// </summary>
        /// <param name="value">The exception to validate.</param>
        /// <returns><see langword="true"/> if the exception is valid; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
        public static bool IsValid(this NotFoundException value)
        {
            ArgumentNullException.ThrowIfNull(value);
            return Validate(value).Count == 0;
        }

        /// <summary>
        /// Ensures that the specified <see cref="NotFoundException"/> instance is valid.
        /// </summary>
        /// <param name="value">The exception to validate.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if the exception is invalid, containing a list of problems.</exception>
        public static void EnsureValid(this NotFoundException value)
        {
            ArgumentNullException.ThrowIfNull(value);

            var problems = Validate(value);
            if (problems.Count > 0)
            {
                throw new ArgumentException(
                    $"The NotFoundException is invalid. Problems: {string.Join(" ", problems)}");
            }
        }

        private static IReadOnlyList<string> ValidateApplicationId(ApplicationNotFoundException value)
        {
            var details = value.Details as dynamic;
            var appId = details?.ApplicationId;

            return appId switch
            {
                string appIdString when string.IsNullOrEmpty(appIdString) =>
                    new[] { "Application ID must be a non-empty string." },
                Guid appIdGuid when appIdGuid == Guid.Empty =>
                    new[] { "Application ID must be a non-empty Guid." },
                _ => Array.Empty<string>()
            };
        }

        private static IReadOnlyList<string> ValidateUserId(UserNotFoundException value)
        {
            var details = value.Details as dynamic;
            var userId = details?.UserId;

            return userId switch
            {
                string userIdString when string.IsNullOrEmpty(userIdString) =>
                    new[] { "User ID must be a non-empty string." },
                Guid userIdGuid when userIdGuid == Guid.Empty =>
                    new[] { "User ID must be a non-empty Guid." },
                _ => Array.Empty<string>()
            };
        }

        private static IReadOnlyList<string> ValidateDeliveryId(WebhookDeliveryNotFoundException value)
        {
            var details = value.Details as dynamic;
            var deliveryId = details?.DeliveryId;

            return deliveryId switch
            {
                string deliveryIdString when string.IsNullOrEmpty(deliveryIdString) =>
                    new[] { "Webhook delivery ID must be a non-empty string." },
                Guid deliveryIdGuid when deliveryIdGuid == Guid.Empty =>
                    new[] { "Webhook delivery ID must be a non-empty Guid." },
                _ => Array.Empty<string>()
            };
        }

        private static IReadOnlyList<string> ValidateChangeRequestId(ChangeRequestNotFoundException value)
        {
            var details = value.Details as dynamic;
            var changeRequestId = details?.ChangeRequestId;

            return changeRequestId switch
            {
                string changeRequestIdString when string.IsNullOrEmpty(changeRequestIdString) =>
                    new[] { "Change request ID must be a non-empty string." },
                Guid changeRequestIdGuid when changeRequestIdGuid == Guid.Empty =>
                    new[] { "Change request ID must be a non-empty Guid." },
                _ => Array.Empty<string>()
            };
        }
    }
}