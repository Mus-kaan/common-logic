//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.AzureAd.Icm.Types;
using Microsoft.AzureAd.Icm.XhtmlUtility;
using Microsoft.Liftr.Utilities;
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace Microsoft.Liftr.Prom2IcM
{
    public static class GrafanaIncidentMessageGenerator
    {
        private static string s_alertTemplateContent = File.ReadAllText("grafana-alert-template.html");

        public static AlertSourceIncident GenerateIncidentFromGrafanaAlert(
            GrafanaWebhookMessage webhookMessage,
            MetaInfo computeMeta,
            ICMClientOptions icmOptions,
            Serilog.ILogger logger)
        {
            if (webhookMessage == null)
            {
                throw new ArgumentNullException(nameof(webhookMessage));
            }

            if (string.IsNullOrEmpty(webhookMessage.Title))
            {
                throw new ArgumentException($"The Grafana webhook {nameof(webhookMessage.Title)} cannot be null.");
            }

            if (icmOptions == null)
            {
                throw new ArgumentNullException(nameof(icmOptions));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            // computeMeta can be null when IMDS is not available, e.g. local debug, not in azure.
            var computeInstanceMeta = computeMeta?.InstanceMeta?.Compute;

            var incidentStartTimeBucket = DateTime.UtcNow.RoundUp(TimeSpan.FromMinutes(10));
            var incidentTitle = webhookMessage.Title;

            var incidentId = $"{MessageGeneratorHelper.ComputeSha256Hash(webhookMessage.RuleId + incidentTitle + incidentStartTimeBucket.ToZuluString())}";
            var icmCorrelationId = HttpUtility.UrlEncode($"prom2icm://prom/{webhookMessage.Title}");

            var incidentLocation = new IncidentLocation()
            {
                Environment = "Grafana",
            };

            var severity = ExtractSeverityFromMessage(webhookMessage.Message);

            logger.Information("incidentId: {incidentId}, icmCorrelationId: {icmCorrelationId}, incidentLocation: {incidentLocation}", incidentId, icmCorrelationId, incidentLocation);

            DateTime now = DateTime.UtcNow;

            (var description, var summary) = GenerateXHtmlDescription(
                        webhookMessage,
                        computeInstanceMeta,
                        incidentTitle,
                        now.AddMilliseconds(1));

            // Note that this does not specify all possible incident fields, but does specify the recommended minimum fields and some
            //  of those that folks explicitly ask about
            return new AlertSourceIncident
            {
                // Alert source information is source-specific information provided by the alert source
                Source = new AlertSourceInfo
                {
                    Origin = "Grafana",

                    // Set this to be the alias or email address of the user requesting incident creation if creating on behalf of another user so they will receive notifications.
                    // This can also be an email address that is not associated with any contact that will be CC'ed on all notifications for this incident.
                    // Examples of an alias (without quotes): 'test', 'foo'
                    // Examples of a valid email address (without quotes): 'test@microsoft.com', 'example@microsoft.com'
                    // Email address must be from a white-listed domain (see below for set of whitelisted domain). Email address from arbitrary domains like gmail.com, hotmail.com etc. will be ignored.
                    CreatedBy = icmOptions.NotificationEmail,

                    // for a fire and forget connector, Source.CreateDate and Source.ModifiedDate are pretty much the same.
                    //  However, if this were an update, the ModifiedDate field becomes important.  IcM assumes that multiple
                    //   instances of a connector can be running and potentially updating incidents in IcM.  To that end, the update
                    //   code checks the Source.ModifiedDate of the existing incident and compares it to the Source.ModifiedDate
                    //   being sent here. Updates that have a Source.ModifiedDate date equal or less than the value in the existing
                    //   incident are discarded as they are considered to have been already applied.
                    CreateDate = DateTime.UtcNow,
                    ModifiedDate = now,

                    // the Source.IncidentId field is a id unique to the alert source for an incident. The combination of this field
                    //  and the alert source id associated with the connector id specified when uploading an incident is how IcM
                    //  decides whether this is a new incident or an update to an existing incident.  Using Guid.NewGuid() essentially
                    //  guarentees any incident we submit will be considered a new incident because the NewGuid should (theoretically)
                    //  never return the same GUID twice.
                    IncidentId = incidentId,
                },

                // Correlation id is required if you want the incident to correlate to another incident.  If you want to force an
                //  incident to never correlate, set the value to a unique string each time Guid.NewGuid().ToString() or leave it
                //  empty.
                // We recommend following a URI format so that correlation Ids are human-readable, but there is no enforcement of any
                //  particular formatting (save length limitations) and IcM will perform exact case-insensitive string matches only.
                CorrelationId = icmCorrelationId,

                // Routing id can be used as a hint to figure out which team an incident should be assigned to within the tenant
                //  associated with the connector id specified when uploading an incident.
                // We recommend following a URI format so that correlation Ids are human-readable, but there is no enforcement of any
                //  particular formatting (save length limitations) and IcM will perform exact case-insensitive string matches only.
                RoutingId = icmOptions.IcmRoutingId,

                // Occurring location is used in most rules and is the location experiencing the problem the incident is reporting.
                //  In particular, for correlation to be enabled, an environment MUST be specified.
                //  In addition, if any correlation rules require matching on one of these fields, the field must be set to a non-null
                //   and non-empty value.
                OccurringLocation = incidentLocation,

                // This is the location of the monitor reporting the problem.
                RaisingLocation = incidentLocation,

                // If severity is left empty, the routing rule that chooses the team to which the incident is assigned will set the
                //  severity.  If the routing rule is marked as "forcing" it's severity, then it's severity will be used reagardless
                //  of whether this is set or not.
                Severity = severity,

                // New incidents must be set to Active or Holding.
                //  An active incident is one that immediately enters normal workflow processing.
                //  A holding incident is inserted into the database and sits there as a readonly incident until the connector updates
                //   it to active, mitigated, or resolved.  No workflows are performed on the incident and it stays readonly until
                //   an update occurrs.  This is used, for example, by systems that attempt auto-remediation but want a tracking
                //   incident that can be queried for in IcM while the remediation is attempted. It is very uncommon that connectors
                //   would use this state and may be deprecated in the future.
                //  Note: IcM does not allow insertion of mitigated or resolved incidents, though of course IcM does support updating
                //   existing incidents and changing the status to mitigated or resolved.
                Status = IncidentStatus.Active,

                Summary = summary,

                // one or more description entries may be submitted
                DescriptionEntries = new[]
                {
                    description,
                },

                // Title is a mandatory field and must be non-empty, non-null, and consist of at least one non-whitespace character,
                Title = incidentTitle,

                // Populate a set of contact aliases or email addresses that will be subscribed to all notifications for this incident
                EmailNotificationSubscribers = new CollectionsToUpdate<EmailNotificationSubscriptionRef>
                {
                    Mode = CollectionUpdateMode.Append, // Append is the only value supported here, replace is not supported.
                    Values = new[]
                    {
                        // Use the correct Type to specify whether it is a contact alias or an email address.
                        // If a match is found to a contact for either the alias or the email address, that contact will be added as a subscriber to the incident.
                        // Examples of an alias (without quotes): 'test', 'foo'
                        // Examples of a valid email address (without quotes): 'test@microsoft.com', 'example@microsoft.com'
                        // Email address must be from a white-listed domain (see below for set of whitelisted domain).
                        // Email address from arbitrary domains like gmail.com, hotmail.com etc. will be ignored.
                        new EmailNotificationSubscriptionRef { Type = IcmConstants.IncidentSubscriberIdTypes.EmailAddressIdType, Value = icmOptions.NotificationEmail },
                    },
                },
            };
        }

        public static int ExtractSeverityFromMessage(string message)
        {
            var parsedSeverity = 4;

            if (TryExtractSeverityFromMessage(message, @"\[Severity(?<servStr>\d+)]", out parsedSeverity))
            {
                return parsedSeverity;
            }

            if (TryExtractSeverityFromMessage(message, @"\[Severity (?<servStr>\d+)]", out parsedSeverity))
            {
                return parsedSeverity;
            }

            if (TryExtractSeverityFromMessage(message, @"\[Sev(?<servStr>\d+)]", out parsedSeverity))
            {
                return parsedSeverity;
            }

            if (TryExtractSeverityFromMessage(message, @"\[Sev (?<servStr>\d+)]", out parsedSeverity))
            {
                return parsedSeverity;
            }

            if (TryExtractSeverityFromMessage(message, @"\[severity(?<servStr>\d+)]", out parsedSeverity))
            {
                return parsedSeverity;
            }

            if (TryExtractSeverityFromMessage(message, @"\[severity (?<servStr>\d+)]", out parsedSeverity))
            {
                return parsedSeverity;
            }

            if (TryExtractSeverityFromMessage(message, @"\[sev(?<servStr>\d+)]", out parsedSeverity))
            {
                return parsedSeverity;
            }

            if (TryExtractSeverityFromMessage(message, @"\[sev (?<servStr>\d+)]", out parsedSeverity))
            {
                return parsedSeverity;
            }

            return parsedSeverity;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "<Pending>")]
        private static bool TryExtractSeverityFromMessage(string message, string patternStr, out int parsedSeverity)
        {
            parsedSeverity = 4;
            try
            {
                Regex pattern = new Regex(patternStr);
                Match match = pattern.Match(message);
                if (match.Success && int.TryParse(match.Groups["servStr"].Value, out parsedSeverity))
                {
                    if (parsedSeverity < 0 || parsedSeverity > 4)
                    {
                        parsedSeverity = 4;
                        return false;
                    }

                    return true;
                }
            }
            catch
            {
            }

            return false;
        }

        private static (DescriptionEntry, string) GenerateXHtmlDescription(
            GrafanaWebhookMessage webhookMessage,
            ComputeMetadata computeMeta,
            string alertName,
            DateTime date)
        {
            string xhtmlSanitized;
            string xhtmlValid;
            string htmlRaw = s_alertTemplateContent;
            string errors;

            htmlRaw = htmlRaw.Replace("ALERT_NAME", alertName, StringComparison.OrdinalIgnoreCase);
            htmlRaw = htmlRaw.Replace("MESSAGE_PLACEHOLDER", webhookMessage.Message, StringComparison.OrdinalIgnoreCase);

            var evalMatchesTableContentSB = new StringBuilder();

            foreach (var match in webhookMessage.EvalMatches)
            {
                evalMatchesTableContentSB.Append($"<tr><td>{match.Metric}</td><td>{match.Value}</td></tr>");
            }

            htmlRaw = htmlRaw.Replace("EVAL_MATCH_PLACEHOLDER", evalMatchesTableContentSB.ToString(), StringComparison.OrdinalIgnoreCase);

            // TODO: fix this deep link.
            htmlRaw = htmlRaw.Replace("GRAFANA_LINK_PLACEHOLDER", webhookMessage.RuleUrl, StringComparison.OrdinalIgnoreCase);

            // IcM's web method WILL NOT run this as it expects the connector to submit valid XHTML
            if (XmlSanitizer.TryMakeXHtml(htmlRaw, out xhtmlValid, out errors) == false)
            {
                throw new InvalidOperationException("HTML could not be made into valid XHTML\n" + errors);
            }

            // IcM's web method WILL run this sanitization on all XHMTL it receives as it must validate that no non-whitelisted tags
            //  or attributes are present.  The two "false" parameters are intended to match the way the server calls this utility
            //  method, which causes non-whitelisted elements to be stripped out and will require at least one XHTML tag.
            if (XmlSanitizer.SanitizeXml(xhtmlValid, false, false, out xhtmlSanitized, out errors) == false)
            {
                //// instead of rejecting the incident by throwing an exception, the IcM server will does the following the event that
                ////  a DescriptionEntry fails the SanitizeXml() method to turn the DescriptionEntry into a plain text description.
                ////  1. HtmlEncode the Text field
                ////  2. Set RenderType to DescriptionTextRenderType.Plaintext

                throw new InvalidOperationException("XHTML could not be sanitized\n" + errors);
            }

            // All the comments in the PlainText description entry apply to XHTML description entries as well and will not be
            //  duplicated below except for the important length limitation one.  The server will truncate the Text field if it has
            //  more than 2MB characters, which will in turn likely cause XHTML to become invalid and displayed as plain text.
            var description = new DescriptionEntry
            {
                Cause = DescriptionEntryCause.Created,
                Date = date,
                SubmitDate = date,
                SubmittedBy = "grafana2icm",
                RenderType = DescriptionTextRenderType.Html,

                // this text field must be 2MB or fewer characters, including all markup characters
                Text = xhtmlSanitized,
            };

            return (description, xhtmlSanitized);
        }
    }
}
