using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json.Linq;

namespace Microsoft.AzureCAT.Extensions.AppInsight.Utils
{
    public class OpenSchemaCallback
    {
        public static async Task PostCallback(            
            CloudBlockBlob blob,
            Uri endpoint, 
            string schemaName,
            string iKey)
        {
            var sasPolicy = new SharedAccessBlobPolicy();
            sasPolicy.SharedAccessExpiryTime = DateTime.UtcNow.AddHours(24);
            sasPolicy.Permissions = SharedAccessBlobPermissions.Read;            
            var sasToken = blob.GetSharedAccessSignature(sasPolicy);
            var sasUri = blob.Uri + sasToken;
          
            var payload = new JObject(
                new JProperty("data",
                    new JObject(
                        new JProperty("baseType", "OpenSchemaData"),
                        new JProperty("baseData", new JObject(
                            new JProperty("ver", "2"),
                            new JProperty("blobSasUri", sasUri),
                            new JProperty("sourceName", schemaName),
                            new JProperty("sourceVersion", "1.0")
                            )
                        )
                    )
                ),
                new JProperty("ver", "1"),
                new JProperty("name", "Microsoft.ApplicationInsights.OpenSchema"),
                new JProperty("time", DateTime.UtcNow),
                new JProperty("iKey", iKey)
            );

            // TODO; pool the http client
            var httpClient = new HttpClient();
            await httpClient
                .PostAsync(endpoint, new StringContent(payload.ToString()))
                .ConfigureAwait(false);

        }
    }
}
