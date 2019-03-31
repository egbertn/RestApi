using Microsoft.Azure.Documents.Client;
using Microsoft.Extensions.Options;
using System;

namespace Demo.DataLayer
{
    public class DocumentDBContext
    {
        /// <summary>
        /// singleton is ok according to SDK
        /// </summary>
        private static DocumentClient Client;
        private static CosmosDBOptions _cosmosDBOptions;
        private static object locker = new object();
        public DocumentDBContext(IOptions<CosmosDBOptions> cosmosDBOptions)
        {
            if (Client == null)
            {
                lock (locker)
                {
                    _cosmosDBOptions = cosmosDBOptions.Value;
                    var endpoint = _cosmosDBOptions.EndPoint;
                    var authKey = _cosmosDBOptions.AuthKey;

                    Client = new DocumentClient(new Uri(endpoint), authKey);
                }
            }
        }


    }
}
