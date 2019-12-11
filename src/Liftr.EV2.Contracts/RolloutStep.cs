//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Liftr.EV2.Contracts
{
    /// <summary>
    ///     An enumeration defining the potential targets for a given deployment step.
    /// </summary>
    public enum RolloutTarget
    {
        /// <summary>
        ///     Indicates that a deployment step will target an entire resource group as opposed to an individual resource.
        /// </summary>
        ServiceResourceGroup = 0,

        /// <summary>
        ///     Indicates that a deployment step will only target an individual resource in a resource group.
        /// </summary>
        ServiceResource = 1,
    }

    /// <summary>
    ///     A class that represents an individual deployment step in the rollout of an Azure service.
    /// </summary>
    public class RolloutStep
    {
        /// <summary>
        /// The actions that must take place as part of this step.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "Liftr1102:Non-public fields should start with _ or s_", Justification = "<Pending>")]
        private IEnumerable<string> actions;

        /// <summary>
        /// Initializes a new instance of the <see cref="RolloutStep"/> class.
        /// </summary>
        public RolloutStep()
        {
            Actions = Enumerable.Empty<string>();
            DependsOn = Enumerable.Empty<string>();
        }

        /// <summary>
        ///     The name of this particular step.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The type of the intended target of this rollout.
        /// </summary>
        public RolloutTarget TargetType { get; set; }

        /// <summary>
        /// The unique name of the target that is to be updated.
        /// </summary>
        public string TargetName { get; set; }

        /// <summary>
        /// The actions that must take place as part of this step.
        /// </summary>
        public IEnumerable<string> Actions
        {
            get
            {
                return actions;
            }

            set
            {
                actions = value;
                RolloutActions = actions.Select(act => new RolloutAction(act));
            }
        }

        /// <summary>
        /// The names of the <see cref="RolloutStep"/> instances that must be executed prior to the current step being executed.
        /// </summary>
        public IEnumerable<string> DependsOn { get; set; }

        /// <summary>
        /// The action objects that must take place as part of this step.
        /// </summary>
        internal IEnumerable<RolloutAction> RolloutActions { get; private set; }

        /// <summary>
        /// Returns whether or not <paramref name="thisStep"/> is reference-equal or value-equal to
        /// <paramref name="otherStep"/>.
        /// </summary>
        /// <param name="thisStep">An instance of <see cref="RolloutStep"/>. </param>
        /// <param name="otherStep">Another instance of <see cref="RolloutStep"/>. </param>
        public static bool operator ==(RolloutStep thisStep, RolloutStep otherStep)
        {
            return Equals(thisStep, otherStep);
        }

        /// <summary>
        /// Returns whether or not <paramref name="thisStep"/> is NOT reference-equal or value-equal to
        /// <paramref name="otherStep"/>.
        /// </summary>
        /// <param name="thisStep">An instance of <see cref="RolloutStep"/>. </param>
        /// <param name="otherStep">Another instance of <see cref="RolloutStep"/>. </param>
        public static bool operator !=(RolloutStep thisStep, RolloutStep otherStep)
        {
            return !Equals(thisStep, otherStep);
        }

        /// <summary>
        /// Returns whether or not <paramref name="thisStep"/> is reference-equal or value-equal to
        /// <paramref name="otherStep"/>.
        /// </summary>
        /// <param name="thisStep">An instance of <see cref="RolloutStep"/>. </param>
        /// <param name="otherStep">Another instance of <see cref="RolloutStep"/>. </param>
        public static bool Equals(RolloutStep thisStep, RolloutStep otherStep)
        {
            if (ReferenceEquals(thisStep, otherStep))
            {
                return true;
            }

            if (ReferenceEquals(thisStep, null) || ReferenceEquals(otherStep, null))
            {
                return false;
            }

            return thisStep.Name.Equals(otherStep.Name, StringComparison.OrdinalIgnoreCase) &&
                   thisStep.TargetType == otherStep.TargetType &&
                   thisStep.TargetName.Equals(otherStep.TargetName, StringComparison.OrdinalIgnoreCase) &&
                   thisStep.DependsOn.IsEquivalentTo(otherStep.DependsOn) &&
                   thisStep.Actions.SequenceEqual(otherStep.Actions);
        }

        /// <summary>
        /// Returns whether or not the current instance is reference-equal or value-equal to
        /// <paramref name="otherStep"/>.
        /// </summary>
        /// <param name="otherStep">Another instance of <see cref="RolloutStep"/>. </param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Naming",
            "CA1725:ParameterNamesShouldMatchBaseDeclaration",
            MessageId = "0#",
            Justification = "'otherStep' is more relevant than 'obj'. ")]
        public override bool Equals(object otherStep)
        {
            return Equals(this, otherStep as RolloutStep);
        }

        /// <summary>
        /// Gets the hash code of the current instance.
        /// </summary>
        /// <returns>
        /// An integer corresponding to the hash code of the current instance.
        /// </returns>
        public override int GetHashCode()
        {
            return Name.GetHashCode() +
                   TargetType.GetHashCode() +
                   TargetName.GetHashCode();
        }
    }
}