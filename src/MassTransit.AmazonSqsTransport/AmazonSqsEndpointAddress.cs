namespace MassTransit.AmazonSqsTransport
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using Initializers;
    using Initializers.TypeConverters;
    using Topology;
    using Util;


    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + "}")]
    public readonly struct AmazonSqsEndpointAddress
    {
        const string AutoDeleteKey = "autodelete";
        const string DurableKey = "durable";
        const string TemporaryKey = "temporary";
        const string TypeKey = "type";


        public enum AddressType
        {
            Queue = 0,
            Topic = 1
        }


        static readonly ITypeConverter<AddressType, string> _parseConverter = new EnumTypeConverter<AddressType>();

        public readonly string Scheme;
        public readonly string Host;
        public readonly string Scope;

        public readonly string Name;
        public readonly bool AutoDelete;
        public readonly bool Durable;
        public readonly AddressType Type;

        public AmazonSqsEndpointAddress(Uri hostAddress, Uri address)
        {
            Scheme = default;
            Host = default;
            Scope = default;

            Durable = true;
            AutoDelete = false;
            Type = AddressType.Queue;

            var scheme = address.Scheme.ToLowerInvariant();
            switch (scheme)
            {
                case AmazonSqsHostAddress.AmazonSqsScheme:
                    Scheme = address.Scheme;
                    Host = address.Host;

                    address.ParseHostPathAndEntityName(out Scope, out Name);
                    break;

                case "queue":
                    ParseLeft(hostAddress, out Scheme, out Host, out Scope);

                    Name = address.AbsolutePath;
                    break;

                case "topic":
                    ParseLeft(hostAddress, out Scheme, out Host, out Scope);

                    Name = address.AbsolutePath;
                    Type = AddressType.Topic;
                    break;

                default:
                    throw new ArgumentException($"The address scheme is not supported: {address.Scheme}", nameof(address));
            }

            AmazonSqsEntityNameValidator.Validator.ThrowIfInvalidEntityName(Name);

            foreach (var (key, value) in address.SplitQueryString())
            {
                switch (key)
                {
                    case TemporaryKey when bool.TryParse(value, out var result):
                        AutoDelete = result;
                        Durable = !result;
                        break;

                    case DurableKey when bool.TryParse(value, out var result):
                        Durable = result;
                        break;

                    case AutoDeleteKey when bool.TryParse(value, out var result):
                        AutoDelete = result;
                        break;

                    case TypeKey when _parseConverter.TryConvert(value, out AddressType result):
                        Type = result;
                        break;
                }
            }
        }

        public AmazonSqsEndpointAddress(Uri hostAddress, string name, bool durable = true, bool autoDelete = false, AddressType type = AddressType.Queue)
        {
            ParseLeft(hostAddress, out Scheme, out Host, out Scope);

            Name = name;

            Durable = durable;
            AutoDelete = autoDelete;
            Type = type;
        }

        static void ParseLeft(Uri address, out string scheme, out string host, out string scope)
        {
            var hostAddress = new AmazonSqsHostAddress(address);
            scheme = hostAddress.Scheme;
            host = hostAddress.Host;
            scope = hostAddress.Scope;
        }

        public static implicit operator Uri(in AmazonSqsEndpointAddress address)
        {
            var builder = new UriBuilder
            {
                Scheme = address.Scheme,
                Host = address.Host,
                Path = address.Scope == "/" || address.Type == AddressType.Topic
                    ? $"/{address.Name}"
                    : $"/{address.Scope}/{address.Name}"
            };

            builder.Query += string.Join("&", address.GetQueryStringOptions());

            return builder.Uri;
        }

        Uri DebuggerDisplay => this;

        IEnumerable<string> GetQueryStringOptions()
        {
            if (!Durable)
                yield return $"{DurableKey}=false";
            if (AutoDelete)
                yield return $"{AutoDeleteKey}=true";

            if (Type != AddressType.Queue)
                yield return $"{TypeKey}=topic";
        }
    }
}
