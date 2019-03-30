
namespace ADC.RestApiTools.Models
{
    public struct CacheEntry
    {
        public byte[] EtagValue;
        public byte[] LastModified;
        public bool HasExpires;
        public byte[] Data;
    }
}