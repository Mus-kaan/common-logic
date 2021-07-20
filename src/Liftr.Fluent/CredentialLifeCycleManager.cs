//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Fluent
{
    public enum ActiveCredentialType
    {
        Primary,
        Secondary,
    }

    public class ActiveCredentailTagState
    {
        public bool NeedTagUpdate { get; set; }

        public ActiveCredentialType ActiveType { get; set; } = ActiveCredentialType.Primary;

        public DateTime LastRotationTime { get; set; }
    }

    public class CredentialLifeCycleManager
    {
        protected const string c_activeCredentailTypeTagName = "ActiveCredentialType";
        protected const string c_lastRotationTimeTagName = "LastCredentialRotationUTC";

        protected readonly ITimeSource _timeSource;
        protected readonly Serilog.ILogger _logger;
        protected readonly int _rotateAfterDays;

        public CredentialLifeCycleManager(ITimeSource timeSource, Serilog.ILogger logger, int rotateAfterDays)
        {
            _timeSource = timeSource;
            _logger = logger;
            _rotateAfterDays = rotateAfterDays;
        }

        public virtual Task RotateCredentialAsync()
        {
            throw new NotImplementedException();
        }

        protected ActiveCredentailTagState GetCurrentStateFromTags(IReadOnlyDictionary<string, string> tags)
        {
            if (tags == null)
            {
                throw new ArgumentNullException(nameof(tags));
            }

            var caseInsensitiveTags = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in tags)
            {
                caseInsensitiveTags[kvp.Key] = kvp.Value;
            }

            bool needTagUpdate = false;

            var activeCredential = ActiveCredentialType.Primary;
            if (caseInsensitiveTags.ContainsKey(c_activeCredentailTypeTagName) &&
                Enum.TryParse<ActiveCredentialType>(caseInsensitiveTags[c_activeCredentailTypeTagName], out var parsedActiveCredentail))
            {
                activeCredential = parsedActiveCredentail;
            }
            else
            {
                _logger.Information($"Cannot find {c_activeCredentailTypeTagName} tag. Treat the primary credential as active.");
                needTagUpdate = true;
            }

            var lastRotationTime = _timeSource.UtcNow;
            if (caseInsensitiveTags.ContainsKey(c_lastRotationTimeTagName))
            {
                try
                {
                    lastRotationTime = caseInsensitiveTags[c_lastRotationTimeTagName].FromBase64().ParseZuluDateTime();
                }
                catch
                {
                    _logger.Information($"Cannot parse {c_lastRotationTimeTagName} tag. Use UtcNow as last rotation time. The invalid value is: {caseInsensitiveTags[c_lastRotationTimeTagName]}");
                    needTagUpdate = true;
                }
            }
            else
            {
                _logger.Information($"Cannot find {c_lastRotationTimeTagName} tag. Use UtcNow as last rotation time.");
                needTagUpdate = true;
            }

            var state = new ActiveCredentailTagState()
            {
                NeedTagUpdate = needTagUpdate,
                ActiveType = activeCredential,
                LastRotationTime = lastRotationTime,
            };

            return state;
        }
    }
}
