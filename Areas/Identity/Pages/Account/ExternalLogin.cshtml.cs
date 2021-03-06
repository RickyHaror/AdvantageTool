﻿using AdvantageTool.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using static AdvantageTool.Constants;

namespace AdvantageTool.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    public class ExternalLoginModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<ExternalLoginModel> _logger;

        public ExternalLoginModel(
            SignInManager<IdentityUser> signInManager,
            UserManager<IdentityUser> userManager,
            ILogger<ExternalLoginModel> logger,
            IEmailSender emailSender)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
            _emailSender = emailSender;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ProviderDisplayName { get; set; }

        public string ReturnUrl { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; }
        }

        public IActionResult OnGetAsync()
        {
            return RedirectToPage("./Login");
        }

        public IActionResult OnPost([FromForm] IFormCollection value)
        {
            var provider = "oidc";
            var returnUrl = "/";
            // Request a redirect to the external login provider.
            var redirectUrl = Url.Page("./ExternalLogin", pageHandler: "Callback", values: new { returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);

            var oidcModel = new OidcModel
            {
                ClientId = value["client_id"],
                Issuer = value["iss"],
                LoginHint = value["login_hint"],
                LtiDeploymentId = value["lti_deployment_id"],
                LtiMessageHint = value["lti_message_hint"],
                TargetLinkUri = value["target_link_uri"]
            };

            properties.Items.Add(nameof(OidcModel.ClientId), oidcModel.ClientId);
            properties.Items.Add(nameof(OidcModel.LoginHint), oidcModel.LoginHint);
            properties.Items.Add(nameof(OidcModel.LtiMessageHint), oidcModel.LtiMessageHint);
            properties.Items.Add(nameof(OidcModel.Issuer), oidcModel.Issuer);
            properties.Items.Add(nameof(OidcModel.LtiDeploymentId), oidcModel.LtiDeploymentId);
            properties.Items.Add(nameof(OidcModel.TargetLinkUri), oidcModel.TargetLinkUri);

            return new ChallengeResult(provider, properties);
        }

        public async Task<IActionResult> OnGetCallbackAsync(string returnUrl = null, string remoteError = null)
        {
            if (remoteError != null)
            {
                ErrorMessage = $"Error from external provider: {remoteError}";
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                ErrorMessage = "Error loading external login information.";
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }

            // Sign in the user with this external login provider if the user already has a login.
            var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);
            if (result.Succeeded)
            {
                _logger.LogInformation("{Name} logged in with {LoginProvider} provider.", info.Principal.Identity.Name, info.LoginProvider);
                var idToken = info.AuthenticationTokens
                    .Select(i => i.Value)
                    .SingleOrDefault();

                var handler = new JwtSecurityTokenHandler();
                var jwt = handler.ReadJwtToken(idToken);

                var user = await _userManager.FindByNameAsync(jwt.Subject);
                var createResult = await _userManager.AddClaimsAsync(user, new List<Claim>
                {
                    new Claim(JwtRegisteredClaimNames.Sub, jwt.Subject),
                    new Claim(JwtRegisteredClaimNames.GivenName, jwt.Payload[JwtRegisteredClaimNames.GivenName].ToString()),
                    new Claim(JwtRegisteredClaimNames.FamilyName, jwt.Payload[JwtRegisteredClaimNames.FamilyName].ToString()),
                    new Claim("name", jwt.Payload["name"].ToString()),
                    new Claim(LtiClaims.MessageType, jwt.Payload[LtiClaims.MessageType].ToString()),
                    new Claim(LtiClaims.DeploymentId, jwt.Payload[LtiClaims.DeploymentId].ToString()),
                    new Claim(LtiClaims.TargetLinkUri, jwt.Payload[LtiClaims.TargetLinkUri].ToString()),
                    new Claim(LtiClaims.ResourceLink, JsonConvert.SerializeObject(jwt.Payload[LtiClaims.ResourceLink])),
                    new Claim(LtiClaims.Roles, JsonConvert.SerializeObject(jwt.Payload[LtiClaims.Roles])),
                    new Claim(LtiClaims.Context, JsonConvert.SerializeObject(jwt.Payload[LtiClaims.Context])),
                    new Claim(LtiClaims.Lis, JsonConvert.SerializeObject(jwt.Payload[LtiClaims.Lis])),
                    new Claim(LtiClaims.LaunchPresentation, JsonConvert.SerializeObject(jwt.Payload[LtiClaims.LaunchPresentation])),
                    new Claim("http://www.brightspace.com", JsonConvert.SerializeObject(jwt.Payload["http://www.brightspace.com"])),
                    new Claim(LtiClaims.Platform, JsonConvert.SerializeObject(jwt.Payload[LtiClaims.Platform])),
                    new Claim(LtiClaims.Version, JsonConvert.SerializeObject(jwt.Payload[LtiClaims.Version])),
                });

                if (createResult.Succeeded)
                {
                    var properties = new AuthenticationProperties
                    {
                        IsPersistent = true,
                    };
                    properties.StoreTokens(info.AuthenticationTokens);
                    await _signInManager.SignInAsync(user, properties, info.LoginProvider);
                    return LocalRedirect(returnUrl);
                }

                return LocalRedirect(returnUrl);
            }
            if (result.IsLockedOut)
            {
                return RedirectToPage("./Lockout");
            }
            else
            {
                // If the user does not have an account, then ask the user to create an account.
                ReturnUrl = returnUrl;
                ProviderDisplayName = info.ProviderDisplayName;

                var idToken = info.AuthenticationTokens
                    .Select(i => i.Value)
                    .SingleOrDefault();
                if (string.IsNullOrEmpty(idToken))
                {
                    return RedirectToPage("./Lockout");
                }

                var handler = new JwtSecurityTokenHandler();
                var jwt = handler.ReadJwtToken(idToken);

                var user = new IdentityUser
                {
                    UserName = jwt.Subject
                };

                user.Email = jwt.Payload[JwtRegisteredClaimNames.Email].ToString();

                var createResult = await _userManager.CreateAsync(user);

                if (createResult.Succeeded)
                {
                    createResult = await _userManager.AddLoginAsync(user, info);

                    if (createResult.Succeeded)
                    {
                        createResult = await _userManager.AddClaimsAsync(user, new List<Claim>
                        {
                            new Claim(JwtRegisteredClaimNames.Sub, jwt.Subject),
                            new Claim(JwtRegisteredClaimNames.GivenName, jwt.Payload[JwtRegisteredClaimNames.GivenName].ToString()),
                            new Claim(JwtRegisteredClaimNames.FamilyName, jwt.Payload[JwtRegisteredClaimNames.FamilyName].ToString()),
                            new Claim("name", jwt.Payload["name"].ToString()),
                            new Claim(LtiClaims.MessageType, jwt.Payload[LtiClaims.MessageType].ToString()),
                            new Claim(LtiClaims.DeploymentId, jwt.Payload[LtiClaims.DeploymentId].ToString()),
                            new Claim(LtiClaims.TargetLinkUri, jwt.Payload[LtiClaims.TargetLinkUri].ToString()),
                            new Claim(LtiClaims.ResourceLink, JsonConvert.SerializeObject(jwt.Payload[LtiClaims.ResourceLink])),
                            new Claim(LtiClaims.Roles, JsonConvert.SerializeObject(jwt.Payload[LtiClaims.Roles])),
                            new Claim(LtiClaims.Context, JsonConvert.SerializeObject(jwt.Payload[LtiClaims.Context])),
                            new Claim(LtiClaims.Lis, JsonConvert.SerializeObject(jwt.Payload[LtiClaims.Lis])),
                            new Claim(LtiClaims.LaunchPresentation, JsonConvert.SerializeObject(jwt.Payload[LtiClaims.LaunchPresentation])),
                            new Claim("http://www.brightspace.com", JsonConvert.SerializeObject(jwt.Payload["http://www.brightspace.com"])),
                            new Claim(LtiClaims.Platform, JsonConvert.SerializeObject(jwt.Payload[LtiClaims.Platform])),
                            new Claim(LtiClaims.Version, JsonConvert.SerializeObject(jwt.Payload[LtiClaims.Version])),
                        });

                        if (createResult.Succeeded)
                        {
                            var properties = new AuthenticationProperties
                            {
                                IsPersistent = false,
                            };
                            properties.StoreTokens(info.AuthenticationTokens);
                            await _signInManager.SignInAsync(user, properties, info.LoginProvider);
                            return LocalRedirect(returnUrl);
                        }
                    }
                }

                foreach (var error in createResult.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);

                return Page();
            }
        }

        public async Task<IActionResult> OnPostConfirmationAsync(string returnUrl = null)
        {
            returnUrl = returnUrl ?? Url.Content("~/");
            // Get the information about the user from the external login provider
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                ErrorMessage = "Error loading external login information during confirmation.";
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }

            if (ModelState.IsValid)
            {
                var user = new IdentityUser { UserName = Input.Email, Email = Input.Email };

                var result = await _userManager.CreateAsync(user);
                if (result.Succeeded)
                {
                    result = await _userManager.AddLoginAsync(user, info);
                    if (result.Succeeded)
                    {
                        _logger.LogInformation("User created an account using {Name} provider.", info.LoginProvider);

                        var userId = await _userManager.GetUserIdAsync(user);
                        var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                        code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                        var callbackUrl = Url.Page(
                            "/Account/ConfirmEmail",
                            pageHandler: null,
                            values: new { area = "Identity", userId = userId, code = code },
                            protocol: Request.Scheme);

                        await _emailSender.SendEmailAsync(Input.Email, "Confirm your email",
                            $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

                        // If account confirmation is required, we need to show the link if we don't have a real email sender
                        if (_userManager.Options.SignIn.RequireConfirmedAccount)
                        {
                            return RedirectToPage("./RegisterConfirmation", new { Email = Input.Email });
                        }

                        await _signInManager.SignInAsync(user, isPersistent: false, info.LoginProvider);

                        return LocalRedirect(returnUrl);
                    }
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            ProviderDisplayName = info.ProviderDisplayName;
            ReturnUrl = returnUrl;
            return Page();
        }
    }
}
