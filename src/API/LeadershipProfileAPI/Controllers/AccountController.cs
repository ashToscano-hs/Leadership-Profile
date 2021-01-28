﻿using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityServer4;
using IdentityServer4.Events;
using IdentityServer4.Extensions;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using LeadershipProfileAPI.Data;
using LeadershipProfileAPI.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;
using System.Collections.Generic;

namespace LeadershipProfileAPI.Controllers
{   
    [TypeFilter(typeof(ApiExceptionFilter))]
    [ApiController]
    [Route("[controller]")]
    [AllowAnonymous]
    public class AccountController : ControllerBase
    {
        private readonly IEventService _events;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly EdFiDbContext _dbContext;

        public AccountController(
            IIdentityServerInteractionService interaction,
            IClientStore clientStore,
            IAuthenticationSchemeProvider schemeProvider,
            IEventService events,
            SignInManager<IdentityUser> signInManager,
            UserManager<IdentityUser> userManager,
            EdFiDbContext dbContext)
        {
            _events = events;
            _signInManager = signInManager;
            _userManager = userManager;
            _dbContext = dbContext;
        }

        [HttpPost("listTpdmUsers")]
        public List<Staff> GetlistTpdmUsers()
        {
            var staff = _dbContext.Staff.Where(s => s.TpdmUsername.Length > 0).ToList();
            return staff;
        }

        [HttpPost("register")]
        public async Task<LoginResultModel> Register(RegisterModel model)
        {
            // Query Staff table to get the recerd that match staffUniqueID 
            var staff = _dbContext.Staff.SingleOrDefault(s => s.StaffUniqueId == model.StaffUniqueId);            
            // Check if it the staff exist
            if(staff == null)
                throw new ApiExceptionFilter.ApiException("Staff record not found.");

            // Check if input information is valid
            if (!ModelState.IsValid)
            { 
                throw new ApiExceptionFilter.ApiException("Missing important properties.");
            }

            var user = new IdentityUser(model.Username) {Email = model.Email};

            var result = await _userManager.CreateAsync(user, model.Password).ConfigureAwait(false);
            
            if (!result.Succeeded)
            {
                throw new ApiExceptionFilter.ApiException("Couldn't create a user");
            }

            result = await _userManager.AddClaimsAsync(user, new Claim[]
            {
                new ("role","Admin") // check db before adding this when we implement roles

            });

            if (!result.Succeeded)
            {
                throw new ApiExceptionFilter.ApiException("Couldn't create claims");
            }

            //create relationship with staff
            staff.TpdmUsername = user.UserName;
            await _dbContext.SaveChangesAsync();
            
            // await _signInManager.SignInAsync(user, false).ConfigureAwait(false);
            await Login(new LoginInputModel {Username = user.UserName, Password = model.Password});
            
            return new LoginResultModel {Result = true};
        
        }

        [HttpPost("login")]
        public async Task<ObjectResult> Login(LoginInputModel model)
        {
            if (!ModelState.IsValid)
            {
                throw new ApiExceptionFilter.ApiException("Missing required properties");
            }

            // find user by username
            var user = await _signInManager.UserManager.FindByNameAsync(model.Username);

            // validate username/password using ASP.NET Identity
            if (user != null && (await _signInManager.CheckPasswordSignInAsync(user, model.Password, true)) == SignInResult.Success)
            {
                await _events.RaiseAsync(new UserLoginSuccessEvent(user.UserName, user.Id, user.UserName, clientId: "interactive"));

                // issue authentication cookie with subject ID and username
                var identityServerUser = new IdentityServerUser(user.Id) { DisplayName = user.UserName };
                
                var claims = await _signInManager.UserManager.GetClaimsAsync(user);
                
                foreach (var claim in claims)
                {
                    identityServerUser.AdditionalClaims.Add(claim);
                }

                await HttpContext.SignInAsync(identityServerUser, null);

                return Ok(new LoginResultModel { Result = true });
            }

            await _events.RaiseAsync(new UserLoginFailureEvent(model.Username, "Invalid credentials", clientId: "interactive"));

            ModelState.AddModelError(string.Empty, "Invalid credentials");

            return Unauthorized(new LoginResultModel { ResultMessage = "Invalid credentials" });
        }

        ///// <summary>
        ///// Handle logout page postback
        ///// </summary>
        [HttpPost("logout")]
        [Authorize]
        public async Task<LoggedOutViewModel> Logout(LogoutInputModel model)
        {   
            var vm = new LoggedOutViewModel{Result = true};

            if (User?.Identity.IsAuthenticated != true) 
                return vm;

            if (!User.Identity.Name.ToLower().Equals(model.LogoutId.ToLower()))
                throw new ApiExceptionFilter.ApiException("Invalid session logout request");

            // delete local authentication cookie
            await HttpContext.SignOutAsync();
            
            ExplicitlyDeleteAuthCookies();
            // raise the logout event
            await _events.RaiseAsync(new UserLogoutSuccessEvent(User.GetSubjectId(), User.GetDisplayName()));

            return vm;
        }

        private void ExplicitlyDeleteAuthCookies()
        {
            if (HttpContext.Request.Cookies.Count <= 0)
                return;

            var siteCookies = HttpContext.Request.Cookies.Where(c => c.Key.Contains(".AspNetCore.")
                                                                     || c.Key.Contains("Microsoft.Authentication")
                                                                     || c.Key.Contains("idsrv"));
            foreach (var cookie in siteCookies)
            {
                Response.Cookies.Delete(cookie.Key);
            }
        }
    }

    public class RegisterModel  
    {
        [Required]
        public string Username { get; set; }
        [Required]
        public string Password { get; set; }
        public string Email { get; set; }
        [Required]
        public string StaffUniqueId { get; set; }
    }

    public class LoginResultModel   
    {
        public bool Result { get; set; }
        public string ResultMessage { get; set; }
    }

    public class LogoutInputModel
    {
        public string LogoutId { get; set; }
    }

    public class LoggedOutViewModel
    {
        public bool Result { get; set; }
    }

    public class LoginInputModel
    {
        [Required]
        public string Username { get; set; }
        [Required]
        public string Password { get; set; }
    }
}
