//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Globalization;

namespace Microsoft.Liftr.EV2.Contracts
{
    /// <summary>
    ///     An enumeration of actions that can occur as part of a deployment.
    /// </summary>
    public enum RolloutActionType
    {
        /// <summary>
        ///     This destructive action involves updating the configuration and binaries of a running service
        ///     if they do not match the version defined in the name
        ///     property.
        /// </summary>
        Deploy = 0,

        /// <summary>
        ///     This non-destructive action queries the health of a resource on MDM and reports whether the service is healthy or not.
        /// </summary>
        MdmHealthCheck = 1,

        /// <summary>
        ///  Http Extension
        /// </summary>
        Extension = 2,

        /// <summary>
        /// Image publishing action
        /// </summary>
        ImagePublish = 3,

        /// <summary>
        /// Shell action.
        /// </summary>
        Shell = 4,

        /// <summary>
        /// Wait action.
        /// </summary>
        Wait = 5,

        /// <summary>
        /// This non-destructive action queries the health of a resource from a REST endpoint and reports whether the service is healthy or not.
        /// </summary>
        RestHealthCheck = 6,
    }

    /// <summary>
    ///     Object of actions that can occur as part of a deployment.
    /// </summary>
    public class RolloutAction
    {
        /// <summary>
        /// letter for split action string
        /// </summary>
        public const char ActionSeparator = '/';

        /// <summary>
        /// Name Index of parts
        /// </summary>
        internal const int NameIndex = 1;

        /// <summary>
        /// Parts Count in action string
        /// </summary>
        private const int PartsCount = 2;

        /// <summary>
        /// Action name length limit
        /// </summary>
        private const int NameSizeLimit = 40;

        /// <summary>
        /// Display name of object
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "Liftr1102:Non-public fields should start with _ or s_", Justification = "<Pending>")]
        private string displayName;

        /// <summary>
        /// Initializes a new instance of the <see cref="RolloutAction"/> class.
        /// </summary>
        /// <param name="actionString">Action String</param>
        internal RolloutAction(string actionString)
        {
            if (string.IsNullOrWhiteSpace(actionString))
            {
                throw new ContractValidationException("'RolloutAction' can't be empty or whitespaces");
            }

            displayName = actionString;
            var tokens = actionString.Split(RolloutAction.ActionSeparator);
            ValidateInput(tokens);

            if (tokens.Length == RolloutAction.PartsCount)
            {
                Name = tokens[RolloutAction.NameIndex];
                if (Name.Length > RolloutAction.NameSizeLimit)
                {
                    throw new ContractValidationException(string.Format(
                        CultureInfo.InvariantCulture,
                        "Action name '{0}' length '{1}' exceeds maximum allowed length {2}.",
                        Name,
                        Name.Length,
                        RolloutAction.NameSizeLimit));
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RolloutAction"/> class.
        /// </summary>
        /// <param name="actionType">RolloutAction type</param>
        /// <param name="name">RolloutAction name</param>
        internal RolloutAction(RolloutActionType actionType, string name)
        {
            ActionType = actionType;
            Name = name;
            if (string.IsNullOrWhiteSpace(name))
            {
                displayName = actionType.ToString();
            }
            else
            {
                displayName = string.Concat(actionType, RolloutAction.ActionSeparator, name);
            }
        }

        /// <summary>
        /// Gets ActionType
        /// </summary>
        public RolloutActionType ActionType { get; private set; }

        /// <summary>
        /// Gets ActionName
        /// </summary>
        public string Name { get; internal set; }

        /// <summary>
        /// Gets if the rollout action requires rollout parameter
        /// </summary>
        public bool IsRolloutParameterRequired
        {
            get
            {
                switch (ActionType)
                {
                    case RolloutActionType.MdmHealthCheck:
                    case RolloutActionType.Extension:
                    case RolloutActionType.Shell:
                    case RolloutActionType.Wait:
                        return true;

                    default:
                        return false;
                }
            }
        }

        /// <summary>
        /// Returns whether or not <paramref name="thisAction"/> is reference-equal or value-equal to
        /// <paramref name="otherAction"/>.
        /// </summary>
        /// <param name="thisAction">An instance of <see cref="RolloutAction"/>. </param>
        /// <param name="otherAction">Another instance of <see cref="RolloutAction"/>. </param>
        public static bool operator ==(RolloutAction thisAction, RolloutAction otherAction)
        {
            return Equals(thisAction, otherAction);
        }

        /// <summary>
        /// Returns whether or not <paramref name="thisAction"/> is NOT reference-equal or value-equal to
        /// <paramref name="otherAction"/>.
        /// </summary>
        /// <param name="thisAction">An instance of <see cref="RolloutAction"/>. </param>
        /// <param name="otherAction">Another instance of <see cref="RolloutAction"/>. </param>
        public static bool operator !=(RolloutAction thisAction, RolloutAction otherAction)
        {
            return !Equals(thisAction, otherAction);
        }

        /// <summary>
        /// Returns whether or not <paramref name="thisAction"/> is reference-equal or value-equal to
        /// <paramref name="otherAction"/>.
        /// </summary>
        /// <param name="thisAction">An instance of <see cref="RolloutStep"/>. </param>
        /// <param name="otherAction">Another instance of <see cref="RolloutStep"/>. </param>
        public static bool Equals(RolloutAction thisAction, RolloutAction otherAction)
        {
            if (object.ReferenceEquals(thisAction, otherAction))
            {
                return true;
            }

            if (object.ReferenceEquals(thisAction, null) || object.ReferenceEquals(otherAction, null))
            {
                return false;
            }

            return thisAction.ActionType == otherAction.ActionType &&
                   string.Equals(thisAction.Name, otherAction.Name, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Returns whether or not the current instance is reference-equal or value-equal to
        /// </summary>
        /// <param name="obj">Other Rollout action</param>
        /// <returns>if the current instance is reference-equal or value-equal to <paramref name="obj"/></returns>
        public override bool Equals(object obj)
        {
            return Equals(this, obj as RolloutAction);
        }

        /// <summary>
        /// Gets the hash code of the current instance.
        /// </summary>
        /// <returns>
        /// An integer corresponding to the hash code of the current instance.
        /// </returns>
        public override int GetHashCode()
        {
            var hash = ActionType.GetHashCode();

            if (Name != null)
            {
                hash += Name.GetHashCode();
            }

            return hash;
        }

        /// <summary>
        /// Get Display name for object
        /// </summary>
        /// <returns>Display name</returns>
        public override string ToString()
        {
            return displayName;
        }

        /// <summary>
        /// Validate input
        /// </summary>
        /// <param name="tokens">Input Tokens</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1305:Specify IFormatProvider", Justification = "<Pending>")]
        private void ValidateInput(string[] tokens)
        {
            if (tokens.Length > RolloutAction.PartsCount)
            {
                throw new ContractValidationException(string.Format(
                    CultureInfo.InvariantCulture,
                    "Unrecognized action '{0}'. Should either be one of the standard actions or an Extension.",
                    displayName));
            }

            var actionTypeStr = tokens[0];
            RolloutActionType actionType;
            if (!Enum.TryParse<RolloutActionType>(actionTypeStr, true, out actionType) || !EnumUtil.IsValidEnumValue(actionTypeStr, typeof(RolloutActionType)))
            {
                throw new ContractValidationException("Unknown action type: " + tokens[0]);
            }

            ActionType = actionType;

            if (ActionType == RolloutActionType.Extension ||
                ActionType == RolloutActionType.Shell ||
                ActionType == RolloutActionType.Wait)
            {
                if (tokens.Length != RolloutAction.PartsCount)
                {
                    throw new ContractValidationException(string.Format(
                        "Unexpected format for {0}, should be '{1}/<Name>'",
                        ActionType,
                        ActionType));
                }
            }
        }
    }
}