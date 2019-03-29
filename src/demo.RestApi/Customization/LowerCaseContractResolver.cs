using Newtonsoft.Json.Serialization;

namespace demo.RestApi.Customization
{
    /// <summary>
    /// makes all JSON properties lowercase
    /// </summary>
    public class LowerCaseContractResolver: DefaultContractResolver
    {
        protected override string ResolvePropertyName(string propertyName)
        {
            return propertyName.ToLower();
        }
    }
}
