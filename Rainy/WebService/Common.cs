using ServiceStack.ServiceInterface;
using log4net;
using System;
using ServiceStack.ServiceHost;
using Rainy.WebService.OAuth;
using System.Linq;
using ServiceStack.Text;
using Rainy.Interfaces;
using Rainy.OAuth;
using ServiceStack.OrmLite;
using DevDefined.OAuth.Storage.Basic;
using Rainy.Crypto;
using Rainy.Db;

namespace Rainy.WebService
{
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

				DBUser db_user;
				using (var db = connFactory.OpenDbConnection ()) {
					db_user = db.First<DBUser> (u => u.Username == user.Username);
				}

				var access_token_repo = new DbAccessTokenRepository<AccessToken> (connFactory);
				var access_token = access_token_repo.GetToken (full_auth_token);
				var token_key = access_token.GetTokenKey ();
				var master_key = full_auth_token.DecryptWithKey (token_key, db_user.MasterKeySalt);

				user.EncryptionMasterKey = master_key;
				_requestingUser = user;

				return user;
			}
		}
	}

	public static class ResponseShortcuts
	{

		public static void HttpCreated (this IHttpResponse response, IHttpRequest request, string location = null)
		{
			response.StatusCode = 204;
			response.StatusDescription = "Created";
			response.ContentType = request.ContentType;

			if (!string.IsNullOrEmpty (location))
				response.AddHeader ("Location", location);

			response.End ();
		}
	}

}
