using System;
using System.Net;
using ServiceStack.ServiceHost;
using ServiceStack.WebHost.Endpoints.Extensions;

namespace Rainy.ErrorHandling
{
	public class RainyBaseException : WebException
	{
		public string ErrorMessage { get; set; }
	}

	public class UnauthorizedException : RainyBaseException
	{
		public string Username;
	}

	public class InvalidRequestDtoException : RainyBaseException
	{
	}

	public class ConflictException : RainyBaseException
	{
	}

	public class ValidationException : RainyBaseException
	{
	}

	public static class ExceptionHandler
	{
		private class ExceptionWrapper : Exception
		{
			public Exception WrappedException;
			public object RequestDto;

			public ExceptionWrapper (Exception wrapped_ex, object request_dto)
			{
				WrappedException = wrapped_ex;
				RequestDto = request_dto;
			}
		}
		public static object CustomServiceExceptionHandler (object request_dto, Exception e)
		{
			// since we can access the Request/Response here, we wrap the exception
			// and rethrow it so that the CustomExceptionHandler can handle it
			throw new ExceptionWrapper (e, request_dto);
		}

		public static void CustomExceptionHandler (IHttpRequest request, IHttpResponse response,
		                                           string operation_name, Exception e)
		{
			Exception inner_exception = e;
			object request_dto = null;

			if (e is ExceptionWrapper) {				
				inner_exception = ((ExceptionWrapper)e).WrappedException;
				request_dto = ((ExceptionWrapper)e).RequestDto;
			} 
			HandleException (request, response, operation_name,
			                 inner_exception, request_dto);

		}

		private static void HandleException (IHttpRequest request, IHttpResponse response,
		                                    string operation_name, Exception e,
		                                    object request_dto = null)
		{
			// log the exception
			// create appropriate response
			if (e is UnauthorizedException) {
				var ex = (UnauthorizedException) e;
				response.StatusCode = 401;
				response.StatusDescription = "Unauthorized";
				response.ContentType = request.ContentType;
				// TODO provide JSON error objects
			} else if (e is ValidationException) {
				var ex = (ValidationException) e;
				response.StatusCode = 400;
				response.StatusDescription = "Bad request. Detail:" + e.Message;
			} else if (e is RainyBaseException) {
				var ex = (RainyBaseException) e;
				response.StatusCode = 400;
				response.StatusDescription = ex.ErrorMessage;
			} else {
				response.StatusCode = 500;
				response.StatusDescription = "Internal server error.";
			}
			response.EndServiceStackRequest ();
		}
	}
}
