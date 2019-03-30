using Newtonsoft.Json.Converters;

namespace TvMazeScraper.Entities.Converter
{
    class YMDConverter : IsoDateTimeConverter
    {
        public YMDConverter()
        {
            base.DateTimeFormat = "yyyy-MM-dd";
        }
    }
}
