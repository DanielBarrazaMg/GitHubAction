using Backend.Common.Models;

namespace Backend.Services.Helpers
{
    public static class ExceptionHandlerHelper
    {
        public static Result<T> CreateResultFromException<T>(this Exception ex)
        {
            return new Result<T> { Success = false, Message = ex.Message };
        }
    }
}
