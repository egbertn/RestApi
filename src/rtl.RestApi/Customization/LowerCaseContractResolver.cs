using Newtonsoft.Json.Serialization;

namespace rtl.RestApi.Customization
{
    public class LowerCaseContractResolver: DefaultContractResolver
    {
        protected override string ResolvePropertyName(string propertyName)
        {
            return propertyName.ToLower();
        }
    }
}
