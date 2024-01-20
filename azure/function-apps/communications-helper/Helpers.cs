using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;

namespace azure_communications_helper_func
{
    internal static class Helpers
    {      
        internal static IConfigurationRoot LoadConfiguration(
            ExecutionContext context,
            ILogger log)
        {
            IConfigurationRoot configuration;
            
            try
            {
                configuration = new ConfigurationBuilder()
                    .SetBasePath(context.FunctionAppDirectory)
                    .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true) 
                    .AddEnvironmentVariables() 
                    .Build();
            }
            catch (Exception ex)
            {
                log.LogInformation($"Failed to load configuration. Exception: {ex}");
                return null;
            }

            return configuration;
        }

        internal static string SerializeWithCamelCasing(object data)
        {            
            DefaultContractResolver contractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy()
            };

            string json = JsonConvert.SerializeObject(data, new JsonSerializerSettings
            {
                ContractResolver = contractResolver,
                Formatting = Formatting.Indented
            });

            return json;
        }
    }
}
