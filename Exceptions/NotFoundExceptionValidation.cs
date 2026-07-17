using System;
using System.Collections.Generic;
using System.Globalization;

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

            var problems = new List<string>();

            if (value is ApplicationNotFoundException)
            {
                if (!IsValidApplicationId(value))
                {
                    problems.Add("Application ID must be a non-empty string or a non-empty Guid.");
                }
            }
            else if (value is UserNotFoundException)
            {
                if (!IsValidUserId(value))
                {
                    problems.Add("User ID must be a non-empty string or a non-empty Guid.");
                }
            }
            else if (value is WebhookDeliveryNotFoundException)
            {
                if (!IsValidDeliveryId(value))
                {
                    problems.Add("Webhook delivery ID must be a non-empty string or a non-empty Guid.");
                }
            }
            else if (value is ChangeRequestNotFoundException)
            {
                if (!IsValidChangeRequestId(value))
                {
                    problems.Add("Change request ID must be a non-empty string or a non-empty Guid.");
                }
            }

            return problems.AsReadOnly();
        }

        /// <summary>
        /// Determines whether the specified <see cref="NotFoundException"/> instance is valid.
        /// </summary>
        /// <param name="value">The exception to check.</param>
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
            if (problems.Count == 0)
            {
                return;
            }

            throw new ArgumentException(
                $"The NotFoundException is invalid. Problems: {string.Join(" ", problems)}");
        }

        private static bool IsValidApplicationId(NotFoundException value)
        {
            var details = value.Details as dynamic;
            if (details?.ApplicationId is string appIdString)
            {
                return !string.IsNullOrEmpty(appIdString);
            }
            else if (details?.ApplicationId is Guid appIdGuid)
            {
                return appIdGuid != Guid.Empty;
            }
            return false;
        }

        private static bool IsValidUserId(NotFoundException value)
        {
            var details = value.Details as dynamic;
            if (details?.UserId is string userIdString)
            {
                return !string.IsNullOrEmpty(userIdString);
            }
            else if (details?.UserId is Guid userIdGuid)
            {
                return userIdGuid != Guid.Empty;
            }
            return false;
        }

        private static bool IsValidDeliveryId(NotFoundException value)
        {
            var details = value.Details as dynamic;
            if (details?.DeliveryId is string deliveryIdString)
            {
                return !string.IsNullOrEmpty(deliveryIdString);
            }
            else if (details?.DeliveryId is Guid deliveryIdGuid)
            {
                return deliveryIdGuid != Guid.Empty;
            }
            return false;
        }

        private static bool IsValidChangeRequestId(NotFoundException value)
        {
            var details = value.Details as dynamic;
            if (details?.ChangeRequestId is string changeRequestIdString)
            {
                return !string.IsNullOrEmpty(changeRequestIdString);
            }
            else if (details?.ChangeRequestId is Guid changeRequestIdGuid)
            {
                return changeRequestIdGuid != Guid.Empty;
            }
            return false;
        }
    }
}