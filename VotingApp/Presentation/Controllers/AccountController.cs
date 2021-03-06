﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.ModelBinding;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OAuth;
using VotingApp.Models;
using VotingApp.Providers;
using VotingApp.Results;
using VotingApp.Domain.Models;
using System.Linq;
using System.Web.Security;
using System.Net;
using AutoMapper;

namespace VotingApp.Controllers {

    [RoutePrefix("api/Account")]
    public class AccountController : ApiController {
        private const string LocalLoginProvider = "Local";
        private ApplicationUserManager _userManager;

        public AccountController() {
        }

        public AccountController(ApplicationUserManager userManager,
            ISecureDataFormat<AuthenticationTicket> accessTokenFormat) {
            UserManager = userManager;
            AccessTokenFormat = accessTokenFormat;
        }

        public ApplicationUserManager UserManager {
            get {
                return _userManager ?? Request.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set {
                _userManager = value;
            }
        }

        public ISecureDataFormat<AuthenticationTicket> AccessTokenFormat { get; private set; }

        /// <summary>
        /// Adds a role to a user. has the URL of:
        /// POST api/Account/AddRole
        /// </summary>
        /// <param name="username"></param>
        /// <param name="roleName"></param>
        /// <returns>returns Ok() HttpActionResult</returns>
        [Route("AddRole")]
        [Authorize(Roles = "Admin")]
        public IHttpActionResult AddRole(string username, string roleName) {
            var curr = UserManager.FindByName(username);
            UserManager.AddToRole(curr.Id, roleName);

            return Ok();
        }

        /// <summary>
        /// Removes a role to a user. has the URL of:
        /// POST api/Account/RemoveRole
        /// </summary>
        /// <param name="username"></param>
        /// <param name="roleName"></param>
        /// <returns>returns Ok() HttpActionResult</returns>
        [Route("RemoveRole")]
        [HttpPost]
       // [Authorize(Roles = "Admin")]
        public IHttpActionResult RemoveRole(Req req) {
            var curr = UserManager.FindById(req.UserId);
            foreach (var roleid in req.Roles) {
                // curr.Roles.Clear(); In the future clear out the roles to maintain one to one relationship.
                var IdUserRole = new IdentityUserRole() { RoleId = roleid, UserId = req.UserId };

                if (curr.Roles.Contains(IdUserRole)) {
                    curr.Roles.Remove(IdUserRole);
                    UserManager.Update(curr);
                }
            }

            return Ok();

        }

        [Route("RoleUpdate")]
        //[Authorize(Roles = "Admin")]
        [HttpPost]
        //public IHttpActionResult UpdateRole(string userId, string roleName) {
        public HttpResponseMessage UpdateRole(Req req) {
            if (ModelState.IsValid) {
                var curr = UserManager.FindById(req.UserId);
                foreach (var roleid in req.Roles) {
                    // curr.Roles.Clear(); In the future clear out the roles to maintain one to one relationship.
                    var IdUserRole = new IdentityUserRole() { RoleId = roleid, UserId = req.UserId };

                    if (curr.Roles.Contains(IdUserRole)) {
                        curr.Roles.Add(IdUserRole);
                        UserManager.Update(curr);
                    }
                    return Request.CreateResponse(HttpStatusCode.Accepted, curr.Roles);
                }
            }

            return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState);
        }

        /// <summary>
        /// Used to display a list of roles by passing the username
        /// Get api/Account/ListUserRoles
        /// </summary>
        /// <returns>Returns an IList of strings</returns>
        [Route("ListRoles")]
        [AllowAnonymous]
        [HttpGet]
        public IList<IdentityRole> ListUserRoles() {
            var roleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>());
            var q = roleManager.Roles;

            var temp = (from r in q
                        select r).ToList();
            return temp;
        }

        /// <summary>
        /// Used to display a list of users by passing a role name
        /// Get api/Account/ListRoleOwners
        /// </summary>
        /// <param name="roleName"></param>
        /// <returns>Returns an IList of strings</returns>
        [Route("ListOfRolesOwner")]
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IList<string> ListRoleOwners(Req req) {
            var curr = UserManager.FindById(req.UserId);

            var roleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>());
            var allRoles = roleManager.Roles;

            var userManager = UserManager.Users;
            var usersByRole = (from u in userManager
                               where u.Roles == req.Roles
                               select u.UserName.ToString()).ToList();
            
            return usersByRole;
        }

        [Route("GetRoleByOwner")]
        // [Authorize(Roles = "Admin")]
        [HttpGet]
        public List<string> GetRoleByOwner(string id) {
            var curr = UserManager.FindById(id);
            var roleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>());
            var allRoles = roleManager.Roles;
            var roleByUser = (from r in allRoles
                              where r.Users == curr
                              select r.Name.ToString()).ToList();
            return roleByUser;
        }

        // GET api/Account/UserInfo
        [HostAuthentication(DefaultAuthenticationTypes.ExternalBearer)]
        [Route("UserInfo")]
        public UserInfoViewModel GetUserInfo() {
            ExternalLoginData externalLogin = ExternalLoginData.FromIdentity(User.Identity as ClaimsIdentity);

            return new UserInfoViewModel {
                Email = User.Identity.GetUserName(),
                HasRegistered = externalLogin == null,
                LoginProvider = externalLogin != null ? externalLogin.LoginProvider : null
            };
        }

        // POST api/Account/Logout
        [Route("Logout")]
        public IHttpActionResult Logout() {
            Authentication.SignOut(CookieAuthenticationDefaults.AuthenticationType);
            return Ok();
        }

        // GET api/Account/ManageInfo?returnUrl=%2F&generateState=true
        [Route("ManageInfo")]
        public async Task<ManageInfoViewModel> GetManageInfo(string returnUrl, bool generateState = false) {
            IdentityUser user = await UserManager.FindByIdAsync(User.Identity.GetUserId());

            if (user == null) {
                return null;
            }

            List<UserLoginInfoViewModel> logins = new List<UserLoginInfoViewModel>();

            foreach (IdentityUserLogin linkedAccount in user.Logins) {
                logins.Add(new UserLoginInfoViewModel {
                    LoginProvider = linkedAccount.LoginProvider,
                    ProviderKey = linkedAccount.ProviderKey
                });
            }

            if (user.PasswordHash != null) {
                logins.Add(new UserLoginInfoViewModel {
                    LoginProvider = LocalLoginProvider,
                    ProviderKey = user.UserName,
                });
            }

            return new ManageInfoViewModel {
                LocalLoginProvider = LocalLoginProvider,
                Email = user.UserName,
                Logins = logins,
                ExternalLoginProviders = GetExternalLogins(returnUrl, generateState)
            };
        }

        // POST api/Account/ChangePassword
        [Route("ChangePassword")]
        public async Task<IHttpActionResult> ChangePassword(ChangePasswordBindingModel model) {
            if (!ModelState.IsValid) {
                return BadRequest(ModelState);
            }

            IdentityResult result = await UserManager.ChangePasswordAsync(User.Identity.GetUserId(), model.OldPassword,
                model.NewPassword);

            if (!result.Succeeded) {
                return GetErrorResult(result);
            }

            return Ok();
        }

        // POST api/Account/SetPassword
        [Route("SetPassword")]
        public async Task<IHttpActionResult> SetPassword(SetPasswordBindingModel model) {
            if (!ModelState.IsValid) {
                return BadRequest(ModelState);
            }

            IdentityResult result = await UserManager.AddPasswordAsync(User.Identity.GetUserId(), model.NewPassword);

            if (!result.Succeeded) {
                return GetErrorResult(result);
            }

            return Ok();
        }

        // POST api/Account/AddExternalLogin
        [Route("AddExternalLogin")]
        public async Task<IHttpActionResult> AddExternalLogin(AddExternalLoginBindingModel model) {
            if (!ModelState.IsValid) {
                return BadRequest(ModelState);
            }

            Authentication.SignOut(DefaultAuthenticationTypes.ExternalCookie);

            AuthenticationTicket ticket = AccessTokenFormat.Unprotect(model.ExternalAccessToken);

            if (ticket == null || ticket.Identity == null || (ticket.Properties != null
                && ticket.Properties.ExpiresUtc.HasValue
                && ticket.Properties.ExpiresUtc.Value < DateTimeOffset.UtcNow)) {
                return BadRequest("External login failure.");
            }

            ExternalLoginData externalData = ExternalLoginData.FromIdentity(ticket.Identity);

            if (externalData == null) {
                return BadRequest("The external login is already associated with an account.");
            }

            IdentityResult result = await UserManager.AddLoginAsync(User.Identity.GetUserId(),
                new UserLoginInfo(externalData.LoginProvider, externalData.ProviderKey));

            if (!result.Succeeded) {
                return GetErrorResult(result);
            }

            return Ok();
        }

        // POST api/Account/RemoveLogin
        [Route("RemoveLogin")]
        public async Task<IHttpActionResult> RemoveLogin(RemoveLoginBindingModel model) {
            if (!ModelState.IsValid) {
                return BadRequest(ModelState);
            }

            IdentityResult result;

            if (model.LoginProvider == LocalLoginProvider) {
                result = await UserManager.RemovePasswordAsync(User.Identity.GetUserId());
            }
            else {
                result = await UserManager.RemoveLoginAsync(User.Identity.GetUserId(),
                    new UserLoginInfo(model.LoginProvider, model.ProviderKey));
            }

            if (!result.Succeeded) {
                return GetErrorResult(result);
            }

            return Ok();
        }

        // GET api/Account/ExternalLogin
        [OverrideAuthentication]
        [HostAuthentication(DefaultAuthenticationTypes.ExternalCookie)]
        [AllowAnonymous]
        [Route("ExternalLogin", Name = "ExternalLogin")]
        public async Task<IHttpActionResult> GetExternalLogin(string provider, string error = null) {
            if (error != null) {
                return Redirect(Url.Content("~/") + "#error=" + Uri.EscapeDataString(error));
            }

            if (!User.Identity.IsAuthenticated) {
                return new ChallengeResult(provider, this);
            }

            ExternalLoginData externalLogin = ExternalLoginData.FromIdentity(User.Identity as ClaimsIdentity);

            if (externalLogin == null) {
                return InternalServerError();
            }

            if (externalLogin.LoginProvider != provider) {
                Authentication.SignOut(DefaultAuthenticationTypes.ExternalCookie);
                return new ChallengeResult(provider, this);
            }

            ApplicationUser user = await UserManager.FindAsync(new UserLoginInfo(externalLogin.LoginProvider,
                externalLogin.ProviderKey));

            bool hasRegistered = user != null;

            if (hasRegistered) {
                Authentication.SignOut(DefaultAuthenticationTypes.ExternalCookie);

                ClaimsIdentity oAuthIdentity = await user.GenerateUserIdentityAsync(UserManager,
                   OAuthDefaults.AuthenticationType);
                ClaimsIdentity cookieIdentity = await user.GenerateUserIdentityAsync(UserManager,
                    CookieAuthenticationDefaults.AuthenticationType);

                AuthenticationProperties properties = ApplicationOAuthProvider.CreateProperties(user.UserName);
                Authentication.SignIn(properties, oAuthIdentity, cookieIdentity);
            }
            else {
                IEnumerable<Claim> claims = externalLogin.GetClaims();
                ClaimsIdentity identity = new ClaimsIdentity(claims, OAuthDefaults.AuthenticationType);
                Authentication.SignIn(identity);
            }

            return Ok();
        }

        // GET api/Account/ExternalLogins?returnUrl=%2F&generateState=true
        [AllowAnonymous]
        [Route("ExternalLogins")]
        public IEnumerable<ExternalLoginViewModel> GetExternalLogins(string returnUrl, bool generateState = false) {
            IEnumerable<AuthenticationDescription> descriptions = Authentication.GetExternalAuthenticationTypes();
            List<ExternalLoginViewModel> logins = new List<ExternalLoginViewModel>();

            string state;

            if (generateState) {
                const int strengthInBits = 256;
                state = RandomOAuthStateGenerator.Generate(strengthInBits);
            }
            else {
                state = null;
            }

            foreach (AuthenticationDescription description in descriptions) {
                ExternalLoginViewModel login = new ExternalLoginViewModel {
                    Name = description.Caption,
                    Url = Url.Route("ExternalLogin", new {
                        provider = description.AuthenticationType,
                        response_type = "token",
                        client_id = Startup.PublicClientId,
                        redirect_uri = new Uri(Request.RequestUri, returnUrl).AbsoluteUri,
                        state = state
                    }),
                    State = state
                };
                logins.Add(login);
            }

            return logins;
        }

        // POST api/Account/Register
        [AllowAnonymous]
        [Route("Register")]
        public async Task<IHttpActionResult> Register(RegisterBindingModel model) {
            if (!ModelState.IsValid) {
                return BadRequest(ModelState);
            }

            var user = new ApplicationUser() { UserName = model.Email, Email = model.Email };

            IdentityResult result = await UserManager.CreateAsync(user, model.Password);

            if (!result.Succeeded) {
                return GetErrorResult(result);
            }
            UserManager.AddToRole(user.Id, "Staff");

            return Ok();
        }

        // POST api/Account/RegisterExternal
        [OverrideAuthentication]
        [HostAuthentication(DefaultAuthenticationTypes.ExternalBearer)]
        [Route("RegisterExternal")]
        public async Task<IHttpActionResult> RegisterExternal(RegisterExternalBindingModel model) {
            if (!ModelState.IsValid) {
                return BadRequest(ModelState);
            }

            var info = await Authentication.GetExternalLoginInfoAsync();
            if (info == null) {
                return InternalServerError();
            }

            var user = new ApplicationUser() { UserName = model.Email, Email = model.Email };

            IdentityResult result = await UserManager.CreateAsync(user);
            if (!result.Succeeded) {
                return GetErrorResult(result);
            }

            result = await UserManager.AddLoginAsync(user.Id, info.Login);
            if (!result.Succeeded) {
                return GetErrorResult(result);
            }
            return Ok();
        }

        protected override void Dispose(bool disposing) {
            if (disposing && _userManager != null) {
                _userManager.Dispose();
                _userManager = null;
            }

            base.Dispose(disposing);
        }

        #region Helpers

        private IAuthenticationManager Authentication {
            get { return Request.GetOwinContext().Authentication; }
        }

        private IHttpActionResult GetErrorResult(IdentityResult result) {
            if (result == null) {
                return InternalServerError();
            }

            if (!result.Succeeded) {
                if (result.Errors != null) {
                    foreach (string error in result.Errors) {
                        ModelState.AddModelError("", error);
                    }
                }

                if (ModelState.IsValid) {
                    // No ModelState errors are available to send, so just return an empty BadRequest.
                    return BadRequest();
                }

                return BadRequest(ModelState);
            }

            return null;
        }

        private class ExternalLoginData {
            public string LoginProvider { get; set; }
            public string ProviderKey { get; set; }
            public string UserName { get; set; }

            public IList<Claim> GetClaims() {
                IList<Claim> claims = new List<Claim>();
                claims.Add(new Claim(ClaimTypes.NameIdentifier, ProviderKey, null, LoginProvider));

                if (UserName != null) {
                    claims.Add(new Claim(ClaimTypes.Name, UserName, null, LoginProvider));
                }

                return claims;
            }

            public static ExternalLoginData FromIdentity(ClaimsIdentity identity) {
                if (identity == null) {
                    return null;
                }

                Claim providerKeyClaim = identity.FindFirst(ClaimTypes.NameIdentifier);

                if (providerKeyClaim == null || String.IsNullOrEmpty(providerKeyClaim.Issuer)
                    || String.IsNullOrEmpty(providerKeyClaim.Value)) {
                    return null;
                }

                if (providerKeyClaim.Issuer == ClaimsIdentity.DefaultIssuer) {
                    return null;
                }

                return new ExternalLoginData {
                    LoginProvider = providerKeyClaim.Issuer,
                    ProviderKey = providerKeyClaim.Value,
                    UserName = identity.FindFirstValue(ClaimTypes.Name)
                };
            }
        }

        private static class RandomOAuthStateGenerator {
            private static RandomNumberGenerator _random = new RNGCryptoServiceProvider();

            public static string Generate(int strengthInBits) {
                const int bitsPerByte = 8;

                if (strengthInBits % bitsPerByte != 0) {
                    throw new ArgumentException("strengthInBits must be evenly divisible by 8.", "strengthInBits");
                }

                int strengthInBytes = strengthInBits / bitsPerByte;

                byte[] data = new byte[strengthInBytes];
                _random.GetBytes(data);
                return HttpServerUtility.UrlTokenEncode(data);
            }
        }

        #endregion
    }

    public class Req {
        public string UserId { get; set; }
        public List<string> Roles { get; set; }

        public Req() {
            Roles = new List<string>();
        }
    }
}
