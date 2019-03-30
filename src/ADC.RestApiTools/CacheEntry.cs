
namespace ADC.RestApiTools
{
    internal
    struct CacheEntry
    {
        public byte[] EtagValue;
        public byte[] LastModified;
        public bool HasExpires;
        public byte[] Data;
        public byte[] ContentType;
        public byte[] ContentEncoding;
    }
}