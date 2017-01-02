using System;
using System.IO;
using System.Net;
using System.Linq;
using DevDefined.OAuth.Storage.Basic;
using Rainy.Db;
using ServiceStack;
using ServiceStack.Common.Web;
using ServiceStack.OrmLite;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints;
using log4net;
using Rainy.Crypto;
using Rainy.Interfaces;
using Rainy.OAuth;
using Rainy.WebService.OAuth;

namespace Rainy.WebService
{
	/// <summary>
	/// This is an ugly hack to prevent chunked transfer encoding which tomboy on linux has sometimes problem with.
	/// We prevent the usual ServiceStack pipeline from running; instead the last response filter closes the connection.
	/// This is bad, as we for example have to override the ContentType which would else be configured through SS.
	/// </summary>
	public class PreventChunkedTransferEncodingResponseFilterAttribute : Attribute, IHasResponseFilter {
		#region IHasResponseFilter implementation
		public void ResponseFilter (IHttpRequest req, IHttpResponse res, object responseDto)
		{
			using (var ms = new MemoryStream())
			{
				EndpointHost.ContentTypeFilter.SerializeToStream(
					new SerializationContext(req.ResponseContentType), responseDto, ms);

				var bytes = ms.ToArray();

				var listenerResponse = (HttpListenerResponse)res.OriginalResponse;
				listenerResponse.ContentType = "application/json";
				listenerResponse.SendChunked = false;
				listenerResponse.ContentLength64 = bytes.Length;
				listenerResponse.OutputStream.Write(bytes, 0, bytes.Length);

				res.EndRequest ();
			}
		}
		public IHasResponseFilter Copy ()
		{
			return this;
		}
		// this filter prevents any filters afterwards from running so make it min priority to
		// assert it runs last
		public int Priority { get { return 100; } }
		#endregion
	}

	public class RequestLogFilterAttribute : Attribute, IHasRequestFilter
	{
		protected ILog Logger;
		public RequestLogFilterAttribute ()
		{
			Logger = LogManager.GetLogger (GetType ());
		}
		public void RequestFilter (IHttpRequest req, IHttpResponse res, object requestDto)
		{
			if (requestDto is OAuthRequestTokenRequest) {
				return;
			}
			Logger.DebugFormat ("Received request at: {0}\nDeserialized data (JSV):\n{1}", req.RawUrl, requestDto.Dump ());

			Logger.Debug ("Received request headers:\n");
			foreach(var key in req.Headers.AllKeys) {
				// we don't want to have admin passwords in it
				if (key != "Authority")
					Logger.DebugFormat ("\t {0}: {1}", key, req.Headers[key]);
			}
		}
		public IHasRequestFilter Copy ()
		{
			return this;
		}
		public int Priority {
			get { return 1; }
		}
	}

	public class ResponseLogFilterAttribute : Attribute, IHasResponseFilter
	{
		protected ILog Logger;
		public ResponseLogFilterAttribute ()
		{
			Logger = LogManager.GetLogger (GetType ());
		}
		public void ResponseFilter (IHttpRequest req, IHttpResponse res, object responseDto)
		{
			Logger.Debug ("Unserialized response data to send (JSV):\n" + responseDto.Dump ());
		}
		public IHasResponseFilter Copy ()
		{
			return this;
		}
		public int Priority {
			get { return 1; }
		}
	}

	public class RequestingUser : IUser {
		public string Username { get; set; }
		public string EncryptionMasterKey { get; set; }
	}

	public abstract class RainyNoteServiceBase : OAuthServiceBase
	{
		protected IDataBackend dataBackend;
		public RainyNoteServiceBase (IDataBackend backend) : base ()
		{
			this.dataBackend = backend;
		}
		public RainyNoteServiceBase (IDbConnectionFactory factory) : base (factory)
		{
		}
		public RainyNoteServiceBase (IDbConnectionFactory factory, IDataBackend backend) : base (factory)
		{
			this.dataBackend = backend;
		}
		protected INoteRepository GetNotes ()
		{
			return dataBackend.GetNoteRepository (requestingUser);
		}
	}

	[RequestLogFilter]
	[ResponseLogFilter]
	[PreventChunkedTransferEncodingResponseFilterAttribute]
	public abstract class ServiceBase : Service
	{
		protected IDbConnectionFactory connFactory;
		protected ILog Logger;
		public ServiceBase () : base ()
		{
			Logger = LogManager.GetLogger (GetType ());
		}
		public ServiceBase (IDbConnectionFactory factory) : this ()
		{
			this.connFactory = factory;
		}
	}


	[OAuthRequired]
	public abstract class OAuthServiceBase : ServiceBase
	{
		public OAuthServiceBase () : base ()
		{
		}
		public OAuthServiceBase (IDbConnectionFactory factory) : base (factory)
		{
		}
		private IUser _requestingUser;
		protected IUser requestingUser {
			get {
				if (this._requestingUser != null)
					return _requestingUser;

				// TODO ugly as heck - don't access DB* classes as we might not operate with a DB
				var base_req = base.RequestContext.Get<IHttpRequest> ();
				var user = new RequestingUser ();
				user.Username = (string) base_req.Items["Username"];
				var full_auth_token = (string) base_req.Items["AccessToken"];

				// for now, encryption is always enabled
				if (JsonConfig.Config.Global.UseNoteEncryption) {

					DBUser db_user;
					using (var db = connFactory.OpenDbConnection ()) {
						db_user = db.First<DBUser> (u => u.Username == user.Username);
					}

					var access_token_repo = new DbAccessTokenRepository<AccessToken> (connFactory);
					var access_token = access_token_repo.GetToken (full_auth_token);
					var token_key = access_token.GetTokenKey ();
					var master_key = full_auth_token.DecryptWithKey (token_key, db_user.MasterKeySalt);

					user.EncryptionMasterKey = master_key;
				}
				_requestingUser = user;

				return user;
			}
		}
	}


	public static class BaseUrlMapper
	{
		public static string GetBaseUrl (this HttpListenerRequest request)
		{
			string scheme =
				request.Headers.AllKeys.Contains ("X-Forwarded-Proto") ?
				request.Headers["X-Forwarded-Proto"] :
				request.Url.Scheme;
			string authority =
				request.Headers.AllKeys.Contains ("X-Forwarded-Host") ?
				request.Headers["X-Forwarded-Host"] :
				request.Url.Authority;
			return scheme + "://" + authority + "/";
		}
	}
}
