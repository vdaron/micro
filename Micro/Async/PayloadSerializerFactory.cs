using System.Collections.Generic;
using System.Linq;

namespace dFakto.Queue
{
    public class PayloadSerializerFactory
    {
        private readonly IDictionary<string,IPayloadSerializer> _serializers;

        public PayloadSerializerFactory(IEnumerable<IPayloadSerializer> serializers)
        {
            _serializers = serializers.ToDictionary(x =>x.ContentType,x=> x);
            DefaultContentType = _serializers.Values.FirstOrDefault()?.ContentType;
        }

        public IPayloadSerializer GetSerializer(string? contentType)
        {
            if (contentType == null)
                return _serializers.Values.FirstOrDefault();
            return _serializers[contentType];
        }

        public static string? DefaultContentType
        {
            get;
            set;
        }
    }
}