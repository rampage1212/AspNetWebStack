using System.Net;
using System.Net.Http;

namespace System.Web.Http.Controllers
{
    /// <summary>
    /// A converter for creating responses from actions that return an arbitrary T value.
    /// </summary>
    /// <typeparam name="T">The declared return type of an action.</typeparam>
    public class ValueResultConverter<T> : IActionResultConverter
    {
        public HttpResponseMessage Convert(HttpControllerContext controllerContext, object actionResult)
        {
            HttpResponseMessage resultAsResponse = actionResult as HttpResponseMessage;
            if (resultAsResponse != null)
            {
                resultAsResponse.EnsureResponseHasRequest(controllerContext.Request);
                return resultAsResponse;
            }

            T value = (T)actionResult;
            return controllerContext.Request.CreateResponse<T>(HttpStatusCode.OK, value, controllerContext.Configuration);
        }
    }
}
