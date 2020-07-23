// https://github.com/serilog/serilog-formatting-compact/blob/dev/src/Serilog.Formatting.Compact/Formatting/Compact/CompactJsonFormatter.cs
// Copyright 2016 Serilog Contributors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Json;
using Serilog.Parsing;
using System;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Microsoft.Liftr.Logging.Formatter
{
    /// <summary>
    /// An <see cref="ITextFormatter"/> that writes events in a compact JSON format.
    /// </summary>
    public class CompactJsonFormatter : ITextFormatter
    {
        private readonly JsonValueFormatter _valueFormatter;
        private readonly bool _renderMessage;

        /// <summary>
        /// Construct a <see cref="CompactJsonFormatter"/>, optionally supplying a formatter for
        /// <see cref="LogEventPropertyValue"/>s on the event.
        /// </summary>
        /// <param name="valueFormatter">A value formatter, or null.</param>
        /// <param name="renderMessage">show rendered messsage or not.</param>
        public CompactJsonFormatter(JsonValueFormatter valueFormatter = null, bool renderMessage = false)
        {
            _valueFormatter = valueFormatter ?? new JsonValueFormatter(typeTagName: "$type");
            _renderMessage = renderMessage;
        }

        /// <summary>
        /// Format the log event into the output. Subsequent events will be newline-delimited.
        /// </summary>
        /// <param name="logEvent">The event to format.</param>
        /// <param name="output">The output.</param>
        public void Format(LogEvent logEvent, TextWriter output)
        {
            FormatEvent(logEvent, output, _valueFormatter);
            output.WriteLine();
        }

        /// <summary>
        /// Format the log event into the output.
        /// </summary>
        /// <param name="logEvent">The event to format.</param>
        /// <param name="output">The output.</param>
        /// <param name="valueFormatter">A value formatter for <see cref="LogEventPropertyValue"/>s on the event.</param>
        public void FormatEvent(LogEvent logEvent, TextWriter output, JsonValueFormatter valueFormatter)
        {
            if (logEvent == null)
            {
                throw new ArgumentNullException(nameof(logEvent));
            }

            if (output == null)
            {
                throw new ArgumentNullException(nameof(output));
            }

            if (valueFormatter == null)
            {
                throw new ArgumentNullException(nameof(valueFormatter));
            }

            output.Write("{\"t\":\"");
            output.Write(logEvent.Timestamp.UtcDateTime.ToString("O", CultureInfo.InvariantCulture));

            if (_renderMessage)
            {
                output.Write("\",\"msg\":");
                JsonValueFormatter.WriteQuotedJsonString(logEvent.RenderMessage(), output);
            }
            else
            {
                output.Write("\",\"mt\":");
                JsonValueFormatter.WriteQuotedJsonString(logEvent.MessageTemplate.Text, output);
            }

            var tokensWithFormat = logEvent.MessageTemplate.Tokens
                .OfType<PropertyToken>()
                .Where(pt => pt.Format != null);

            // Better not to allocate an array in the 99.9% of cases where this is false
            // ReSharper disable once PossibleMultipleEnumeration
            if (tokensWithFormat.Any())
            {
                output.Write(",\"r\":[");
                var delim = string.Empty;
                foreach (var r in tokensWithFormat)
                {
                    output.Write(delim);
                    delim = ",";
                    using var space = new StringWriter();
                    r.Render(logEvent.Properties, space);
                    JsonValueFormatter.WriteQuotedJsonString(space.ToString(), output);
                }

                output.Write(']');
            }

            if (logEvent.Level != LogEventLevel.Information)
            {
                output.Write(",\"l\":\"");
                output.Write(logEvent.Level);
                output.Write('\"');
            }

            if (logEvent.Exception != null)
            {
                output.Write(",\"x\":");
                JsonValueFormatter.WriteQuotedJsonString(logEvent.Exception.ToString(), output);
            }

            foreach (var property in logEvent.Properties)
            {
                var name = property.Key;
                output.Write(',');
                JsonValueFormatter.WriteQuotedJsonString(name, output);
                output.Write(':');
                valueFormatter.Format(property.Value, output);
            }

            output.Write('}');
        }
    }
}
