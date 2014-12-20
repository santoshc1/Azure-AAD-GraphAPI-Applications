using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

using Microsoft.Azure.ActiveDirectory.GraphClient;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Owin.Security.OpenIdConnect;
using System.Configuration;
using System.Security.Claims;
using WebAppGraphAPI.Utils;

namespace WebAppGraphAPI.Controllers
{
    /// <summary>
    /// Applications controller to get/set/update/delete applications.
    /// </summary>
    [Authorize]
    public class ApplicationsController : Controller
    {
        private string graphResourceId = ConfigurationManager.AppSettings["ida:GraphUrl"];
        private static string clientId = ConfigurationManager.AppSettings["ida:ClientId"];
        private static string appKey = ConfigurationManager.AppSettings["ida:AppKey"];

        /// <summary>
        /// Gets a list of <see cref="Application"/> objects from Graph.
        /// </summary>
        /// <returns>A view with the list of <see cref="Application"/> objects.</returns>
        public ActionResult Index()
        {
            // get access token to call Graph API
            AuthenticationResult result = null;
            List<Application> applicationList = new List<Application>();

            try
            {
                // Get the access token from the cache
                string userObjectID = ClaimsPrincipal.Current.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier").Value;
                AuthenticationContext authContext = new AuthenticationContext(Startup.Authority,
                    new NaiveSessionCache(userObjectID));
                ClientCredential credential = new ClientCredential(clientId, appKey);
                result = authContext.AcquireTokenSilent(graphResourceId, credential,
                    new UserIdentifier(userObjectID, UserIdentifierType.UniqueId));

                // Setup GRaph API connection and get a list of groups
                Guid ClientRequestId = Guid.NewGuid();
                GraphSettings graphSettings = new GraphSettings();
                graphSettings.ApiVersion = GraphConfiguration.GraphApiVersion;
                GraphConnection graphConnection = new GraphConnection(result.AccessToken, ClientRequestId, graphSettings);

                // Get all results in a List
                PagedResults<Application> pagedResults = graphConnection.List<Application>(null, new FilterGenerator());
                applicationList.AddRange(pagedResults.Results);
                while (!pagedResults.IsLastPage)
                {
                    pagedResults = graphConnection.List<Application>(pagedResults.PageToken, new FilterGenerator());
                    applicationList.AddRange(pagedResults.Results);
                }

            }
            catch (Exception e)
            {
                if (Request.QueryString["reauth"] == "True")
                {
                    //
                    // Send an OpenID Connect sign-in request to get a new set of tokens.
                    // If the user still has a valid session with Azure AD, they will not be prompted for their credentials.
                    // The OpenID Connect middleware will return to this controller after the sign-in response has been handled.
                    //
                    HttpContext.GetOwinContext().Authentication.Challenge(OpenIdConnectAuthenticationDefaults.AuthenticationType);
                }

                //
                // The user needs to re-authorize.  Show them a message to that effect.
                //
                ViewBag.ErrorMessage = "AuthorizationRequired";
                return View(applicationList);
            }
            return View(applicationList);
        }

        /// <summary>
        /// Gets details of a single <see cref="Group"/> Graph.
        /// </summary>
        /// <returns>A view with the details of a single <see cref="Group"/>.</returns>
        public ActionResult Details(string objectId)
        {
            //Get the access token as we need it to make a call to the Graph API
            AuthenticationResult result = null;
            Application app = null;

            try
            {
                // Get the access token from the cache
                string userObjectID = ClaimsPrincipal.Current.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier").Value;
                AuthenticationContext authContext = new AuthenticationContext(Startup.Authority,
                    new NaiveSessionCache(userObjectID));
                ClientCredential credential = new ClientCredential(clientId, appKey);
                result = authContext.AcquireTokenSilent(graphResourceId, credential,
                    new UserIdentifier(userObjectID, UserIdentifierType.UniqueId));

                //Setup Graph API connection and get the details of a specific application objectId
                Guid ClientRequestId = Guid.NewGuid();
                GraphSettings graphSettings = new GraphSettings();
                graphSettings.ApiVersion = GraphConfiguration.GraphApiVersion;
                GraphConnection graphConnection = new GraphConnection(result.AccessToken, ClientRequestId, graphSettings);

                app = graphConnection.Get<Application>(objectId);
            }
            catch (Exception e)
            {
                if (Request.QueryString["reauth"] == "True")
                {
                    //
                    // Send an OpenID Connect sign-in request to get a new set of tokens.
                    // If the user still has a valid session with Azure AD, they will not be prompted for their credentials.
                    // The OpenID Connect middleware will return to this controller after the sign-in response has been handled.
                    //
                    HttpContext.GetOwinContext().Authentication.Challenge(OpenIdConnectAuthenticationDefaults.AuthenticationType);
                }

                //
                // The user needs to re-authorize.  Show them a message to that effect.
                //
                ViewBag.ErrorMessage = "AuthorizationRequired";
                return View();

            }

            return View(app);
        }

        /// <summary>
        /// Creates a view to for adding a new <see cref="Application"/> to Graph.
        /// </summary>
        /// <returns>A view with the details to add a new <see cref="Application"/> object</returns>
        public ActionResult Create()
        {
            return View();
        }

        /// <summary>
        /// Processes creation of a new <see cref="Application"/> to Graph.
        /// </summary>
        /// <param name="application"><see cref="Application"/> to be created.</param>
        /// <returns>A view with the details to all <see cref="Application"/> object</returns>
        [HttpPost]
        public ActionResult Create([Bind(Include="DisplayName,Homepage,IdentifierUris,ReplyUrls")] Application app)
        {
            //Get the access token as we need it to make a call to the Graph API
            AuthenticationResult result = null;

            try
            {
                // Get the access token from the cache
                string userObjectID = ClaimsPrincipal.Current.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier").Value;
                AuthenticationContext authContext = new AuthenticationContext(Startup.Authority,
                    new NaiveSessionCache(userObjectID));
                ClientCredential credential = new ClientCredential(clientId, appKey);
                result = authContext.AcquireTokenSilent(graphResourceId, credential,
                    new UserIdentifier(userObjectID, UserIdentifierType.UniqueId));
            }
            catch (Exception e)
            {
                if (Request.QueryString["reauth"] == "True")
                {
                    //
                    // Send an OpenID Connect sign-in request to get a new set of tokens.
                    // If the user still has a valid session with Azure AD, they will not be prompted for their credentials.
                    // The OpenID Connect middleware will return to this controller after the sign-in response has been handled.
                    //
                    HttpContext.GetOwinContext().Authentication.Challenge(OpenIdConnectAuthenticationDefaults.AuthenticationType);
                }

                //
                // The user needs to re-authorize.  Show them a message to that effect.
                //
                ViewBag.ErrorMessage = "AuthorizationRequired";
                return View();
            }

            try
            {
                // Setup Graph API connection and add Application
                Guid ClientRequestId = Guid.NewGuid();
                GraphSettings graphSettings = new GraphSettings();
                graphSettings.ApiVersion = GraphConfiguration.GraphApiVersion;
                if (result != null)
                {
                    GraphConnection graphConnection = new GraphConnection(result.AccessToken, ClientRequestId,
                        graphSettings);
                    graphConnection.Add(app);
                    
                    return RedirectToAction("Index");
                }
            }
            catch (Exception exception)
            {
                ModelState.AddModelError("", exception.Message);
                return View();
            }
            ViewBag.ErrorMessage = "AuthorizationRequired";
            return View();
        }

        /// <summary>
        /// Creates a view to for adding a key credential to an existing <see cref="Application"/> in Graph.
        /// </summary>
        /// <param name="objectId">Unique identifier of the <see cref="Application"/>.</param>
        /// <returns>A view with details to add a key to the <see cref="Application"/>.</returns>
        public ActionResult AddKey(string objectId)
        {
            //Get the access token as we need it to make a call to the Graph API
            AuthenticationResult result = null;
            Application app = null;

            try
            {
                // Get the access token from the cache
                string userObjectID = ClaimsPrincipal.Current.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier").Value;
                AuthenticationContext authContext = new AuthenticationContext(Startup.Authority,
                    new NaiveSessionCache(userObjectID));
                ClientCredential credential = new ClientCredential(clientId, appKey);
                result = authContext.AcquireTokenSilent(graphResourceId, credential,
                    new UserIdentifier(userObjectID, UserIdentifierType.UniqueId));

                //Setup Graph API connection and get a list of groups
                Guid ClientRequestId = Guid.NewGuid();
                GraphSettings graphSettings = new GraphSettings();
                graphSettings.ApiVersion = GraphConfiguration.GraphApiVersion;
                GraphConnection graphConnection = new GraphConnection(result.AccessToken, ClientRequestId, graphSettings);

                app = graphConnection.Get<Application>(objectId);
            }
            catch (Exception e)
            {
                if (Request.QueryString["reauth"] == "True")
                {
                    //
                    // Send an OpenID Connect sign-in request to get a new set of tokens.
                    // If the user still has a valid session with Azure AD, they will not be prompted for their credentials.
                    // The OpenID Connect middleware will return to this controller after the sign-in response has been handled.
                    //
                    HttpContext.GetOwinContext().Authentication.Challenge(OpenIdConnectAuthenticationDefaults.AuthenticationType);
                }

                //
                // The user needs to re-authorize.  Show them a message to that effect.
                //
                ViewBag.ErrorMessage = "AuthorizationRequired";
                return View();
            }
            KeyCredential key = new KeyCredential();
            key.KeyId = System.Guid.NewGuid();
            key.Type = "Symmetric";
            key.Usage = "Verify";
            String keyValue = "VSoJ0xLgSyQv60M+mJCtJOMM6yflDz5pLnAVNzGT6do=";
            byte[] keyBytes = System.Text.Encoding.ASCII.GetBytes(keyValue);
            key.Value = keyBytes;
            app.KeyCredentials.Add(key);

            return View(app);
        }

        /// <summary>
        /// Adds a new key credential to an application <see cref="Application"/> in the Graph.
        /// </summary>
        /// <param name="application"><see cref="Application"/> to which key should be added.</param>
        /// <returns>A view with the details to all <see cref="Application"/> object</returns>
        [HttpPost]
        public ActionResult AddKey([Bind(Include = "ObjectId,DisplayName,Homepage,IdentifierUris,ReplyUrls,KeyCredentials")] Application app)
        {
            //Get the access token as we need it to make a call to the Graph API
            AuthenticationResult result = null;

            try
            {
                // Get the access token from the cache
                string userObjectID = ClaimsPrincipal.Current.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier").Value;
                AuthenticationContext authContext = new AuthenticationContext(Startup.Authority,
                    new NaiveSessionCache(userObjectID));
                ClientCredential credential = new ClientCredential(clientId, appKey);
                result = authContext.AcquireTokenSilent(graphResourceId, credential,
                    new UserIdentifier(userObjectID, UserIdentifierType.UniqueId));
            }
            catch (Exception e)
            {
                if (Request.QueryString["reauth"] == "True")
                {
                    //
                    // Send an OpenID Connect sign-in request to get a new set of tokens.
                    // If the user still has a valid session with Azure AD, they will not be prompted for their credentials.
                    // The OpenID Connect middleware will return to this controller after the sign-in response has been handled.
                    //
                    HttpContext.GetOwinContext().Authentication.Challenge(OpenIdConnectAuthenticationDefaults.AuthenticationType);
                }

                //
                // The user needs to re-authorize.  Show them a message to that effect.
                //
                ViewBag.ErrorMessage = "AuthorizationRequired";
                return View();
            }

            try
            {
                // Setup Graph API connection and add Application
                Guid ClientRequestId = Guid.NewGuid();
                GraphSettings graphSettings = new GraphSettings();
                graphSettings.ApiVersion = GraphConfiguration.GraphApiVersion;
                if (result != null)
                {
                    GraphConnection graphConnection = new GraphConnection(result.AccessToken, ClientRequestId,
                        graphSettings);
                    graphConnection.Update(app);

                    return RedirectToAction("Index");
                }
            }
            catch (Exception exception)
            {
                ModelState.AddModelError("", exception.Message);
                return View();
            }
            ViewBag.ErrorMessage = "AuthorizationRequired";
            return View();
        }

        /// <summary>
        /// Creates a view to for adding a key credential to an existing <see cref="Application"/> in Graph.
        /// </summary>
        /// <param name="objectId">Unique identifier of the <see cref="Application"/>.</param>
        /// <returns>A view with details to add a key to the <see cref="Application"/>.</returns>
        public ActionResult AddSaml(string objectId)
        {
            //Get the access token as we need it to make a call to the Graph API
            AuthenticationResult result = null;
            Application app = null;

            try
            {
                // Get the access token from the cache
                string userObjectID = ClaimsPrincipal.Current.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier").Value;
                AuthenticationContext authContext = new AuthenticationContext(Startup.Authority,
                    new NaiveSessionCache(userObjectID));
                ClientCredential credential = new ClientCredential(clientId, appKey);
                result = authContext.AcquireTokenSilent(graphResourceId, credential,
                    new UserIdentifier(userObjectID, UserIdentifierType.UniqueId));

                //Setup Graph API connection and get a list of groups
                Guid ClientRequestId = Guid.NewGuid();
                GraphSettings graphSettings = new GraphSettings();
                graphSettings.ApiVersion = GraphConfiguration.GraphApiVersion;
                GraphConnection graphConnection = new GraphConnection(result.AccessToken, ClientRequestId, graphSettings);

                app = graphConnection.Get<Application>(objectId);
            }
            catch (Exception e)
            {
                if (Request.QueryString["reauth"] == "True")
                {
                    //
                    // Send an OpenID Connect sign-in request to get a new set of tokens.
                    // If the user still has a valid session with Azure AD, they will not be prompted for their credentials.
                    // The OpenID Connect middleware will return to this controller after the sign-in response has been handled.
                    //
                    HttpContext.GetOwinContext().Authentication.Challenge(OpenIdConnectAuthenticationDefaults.AuthenticationType);
                }

                //
                // The user needs to re-authorize.  Show them a message to that effect.
                //
                ViewBag.ErrorMessage = "AuthorizationRequired";
                return View();
            }
            
            return View(app);
        }

        /// <summary>
        /// Adds a new key credential to an application <see cref="Application"/> in the Graph.
        /// </summary>
        /// <param name="application"><see cref="Application"/> to which key should be added.</param>
        /// <returns>A view with the details to all <see cref="Application"/> object</returns>
        [HttpPost]
        public ActionResult AddSaml([Bind(Include = "ObjectId,DisplayName,Homepage,IdentifierUris,ReplyUrls,SamlMetadataUrl")] Application app)
        {
            //Get the access token as we need it to make a call to the Graph API
            AuthenticationResult result = null;

            try
            {
                // Get the access token from the cache
                string userObjectID = ClaimsPrincipal.Current.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier").Value;
                AuthenticationContext authContext = new AuthenticationContext(Startup.Authority,
                    new NaiveSessionCache(userObjectID));
                ClientCredential credential = new ClientCredential(clientId, appKey);
                result = authContext.AcquireTokenSilent(graphResourceId, credential,
                    new UserIdentifier(userObjectID, UserIdentifierType.UniqueId));
            }
            catch (Exception e)
            {
                if (Request.QueryString["reauth"] == "True")
                {
                    //
                    // Send an OpenID Connect sign-in request to get a new set of tokens.
                    // If the user still has a valid session with Azure AD, they will not be prompted for their credentials.
                    // The OpenID Connect middleware will return to this controller after the sign-in response has been handled.
                    //
                    HttpContext.GetOwinContext().Authentication.Challenge(OpenIdConnectAuthenticationDefaults.AuthenticationType);
                }

                //
                // The user needs to re-authorize.  Show them a message to that effect.
                //
                ViewBag.ErrorMessage = "AuthorizationRequired";
                return View();
            }

            try
            {
                // Setup Graph API connection and add Application
                Guid ClientRequestId = Guid.NewGuid();
                GraphSettings graphSettings = new GraphSettings();
                graphSettings.ApiVersion = GraphConfiguration.GraphApiVersion;
                if (result != null)
                {
                    GraphConnection graphConnection = new GraphConnection(result.AccessToken, ClientRequestId,
                        graphSettings);
                    graphConnection.Update(app);

                    return RedirectToAction("Index");
                }
            }
            catch (Exception exception)
            {
                ModelState.AddModelError("", exception.Message);
                return View();
            }
            ViewBag.ErrorMessage = "AuthorizationRequired";
            return View();
        }


    }
}