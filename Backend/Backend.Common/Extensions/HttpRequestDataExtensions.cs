using Backend.Common.Models;
using Microsoft.Azure.Functions.Worker.Http;
using Newtonsoft.Json;
using System.Net;

namespace Backend.Common.Extensions
{
    // <summary>
    /// HttpRequestData extentions methods
    /// to create the HTTP Response
    /// </summary>
    public static class HttpRequestDataExtensions
    {
        /// <summary>
        /// Returns a HTTP response after executing a method with no parameters
        /// </summary>   
        public static async Task<HttpResponseData> CreateResponse<TResult>(this HttpRequestData request,
            Func<Task<Result<TResult>>> func, Action<Response<TResult>> responseLinks)
        {
            var result = await func();
            return await CreateResponseAsync(request, result, responseLinks);
        }

        /// <summary>
        /// Returns a HTTP response after executing a method with 1 parameters of type T and return a Result<TResult>
        /// </summary>   
        public static async Task<HttpResponseData> CreateResponse<T, TResult>(this HttpRequestData request,
            Func<T, Task<Result<TResult>>> func, T param, Action<Response<TResult>> responseLinks)
        {
            var result = await func(param);
            return await CreateResponseAsync(request, result, responseLinks);
        }

        /// <summary>
        /// Creates a http response based on the result
        /// </summary>    
        public static async Task<HttpResponseData> CreateResponseAsync<TResult>(this HttpRequestData request,
            Result<TResult> result, Action<Response<TResult>> responseLinks)
        {
            var responseData = request.CreateResponse(result.Success ? HttpStatusCode.OK : HttpStatusCode.BadRequest);
            var response = new Response<TResult>(result.Success, result.Data, result.Message);
            if (responseLinks != null) responseLinks(response);
            await responseData.WriteAsJsonAsync(response);
            return responseData;
        }

        /// <summary>
        /// Deserializes the boby of the HttpRequestData
        /// </summary>
        public static async Task<T> DeserializeBody<T>(this HttpRequestData request) where T : class
        {
            if (request.Body == null)
            {
                return null;
            }
            else
            {
                var body = await new StreamReader(request.Body).ReadToEndAsync();
                if (!string.IsNullOrEmpty(body))
                {
                    return (typeof(T) == typeof(string)) ? body as T : JsonConvert.DeserializeObject<T>(body);
                }
            }
            return null;
        }
    }
}
