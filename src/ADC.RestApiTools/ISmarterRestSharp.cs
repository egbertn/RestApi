namespace ADC.RestApiTools
{
    /// <summary>
    /// RestClient factory, use SingleTon
    /// </summary>
    public interface ISmartRestSharp
    {
        RestSharp.IRestClient Instance(string baseUri);
    }
}