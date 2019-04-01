
namespace ADC.RestApiTools
{
    public
    struct CacheEntry
    {
        public byte[] EtagValue;
        //"r" format DateTimeOffset 
        // stored as FileTime
        public long? LastModified;
        public bool HasExpires;
        public byte[] Data;
        public byte[] ContentType;
        public byte[] ContentEncoding;
    }
}