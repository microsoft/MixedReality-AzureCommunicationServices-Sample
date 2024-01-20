using Azure.Communication.Identity;
using Azure.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace azure_communications_helper_func
{
    public static class CreateUserAndToken
    {
        [FunctionName("CreateUserAndToken")]          
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] 
            HttpRequest request,
            ILogger log, 
            ExecutionContext context)
        {       
            var tokenRequest = await AccessTokenRequestHelpers.Parse(request, log);  
            var scopes = tokenRequest?.scopes;
            var createToken = scopes != null && scopes.Length > 0;

            var configuration = Helpers.LoadConfiguration(context, log);
            string communicationsEndpointString = configuration[Constants.CommunicationsEndpointKey];
            
            IdentityAndAccessTokenResponse resultObject = new IdentityAndAccessTokenResponse();
            var communicationsEndpoint = new Uri(communicationsEndpointString);
            var communicationsCredential = new ManagedIdentityCredential();
            CommunicationIdentityClient client = new CommunicationIdentityClient(communicationsEndpoint, communicationsCredential);
            try
            {
                if (createToken)
                {
                    var idAndToken = client.CreateUserAndToken(scopes);
                    resultObject.accessToken.token = idAndToken.Value.AccessToken.Token;
                    resultObject.accessToken.expiresOn = idAndToken.Value.AccessToken.ExpiresOn;
                    resultObject.identity.id = idAndToken.Value.User.Id;
                }
                else
                {
                    var id = client.CreateUser();
                    resultObject.identity.id = id.Value.Id;
                }
            }
            catch (Exception ex)
            {
                log.LogInformation($"Creating ACS id and token failed. Exception: {ex}");
                return new Microsoft.AspNetCore.Mvc.NotFoundResult();
            }

            string responseMessage = Helpers.SerializeWithCamelCasing(resultObject);
            return new OkObjectResult(responseMessage);
        }
    }
}

