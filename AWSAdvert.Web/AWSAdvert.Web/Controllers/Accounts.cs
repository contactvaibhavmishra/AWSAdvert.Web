using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.AspNetCore.Identity.Cognito;
using Amazon.Extensions.CognitoAuthentication;
using Amazon.Runtime.Internal.Transform;
using AWSAdvert.Web.Models.Accounts;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace AWSAdvert.Web.Controllers
{
    public class Accounts : Controller
    {
        private readonly SignInManager<CognitoUser> _signInManager;
        private readonly UserManager<CognitoUser> _userMnManager;
        private readonly CognitoUserPool _pool;

        public Accounts(SignInManager<CognitoUser> signInManager, UserManager<CognitoUser> userMnManager, CognitoUserPool pool)
        {
            _signInManager = signInManager;
            _userMnManager = userMnManager;
            _pool = pool;
        }

        [HttpGet]
        public async Task<IActionResult> Singup()
        {
            SingupModel model = new SingupModel();
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Singup(SingupModel model)
        {
            if (ModelState.IsValid)
            {
                var user = _pool.GetUser(model.Email);
                if (user.Status != null)
                {
                    ModelState.AddModelError("UserExists","User is already in system");
                    return View(model);
                }
                user.Attributes.Add(CognitoAttribute.Email.AttributeName, model.Email);
                var createduser =await _userMnManager.CreateAsync(user, model.Password);
                if (createduser.Succeeded)
                {
                   return RedirectToAction("Confirm","Accounts");
                }
            }
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Confirm()
        {
            ConfirmModel model = new ConfirmModel();
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Confirm(ConfirmModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userMnManager.FindByEmailAsync(model.Email);
                if (user.Status == null)
                {
                    ModelState.AddModelError("UserNotFound", "unable to locate user");
                    return View(model);
                }
                else
                {
                    var result = await (_userMnManager as CognitoUserManager<CognitoUser>).ConfirmSignUpAsync(user,model.Code, true);
                    if (result.Succeeded)
                    {
                        return RedirectToAction("Index", "Home");
                    }
                    else
                    {
                        foreach (var item in result.Errors)
                        {
                            ModelState.AddModelError(item.Code, item.Description);
                        }

                        return View(model);
                    }
                }
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Login(LoginModel model)
        {
            return View(model);
        }

        [HttpPost]
        [ActionName("Login")]
        public async Task<IActionResult> LoginPost(LoginModel model)
        {
            if (ModelState.IsValid)
            {
                var result =
                    await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, false);

                if (result.Succeeded)
                {
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    ModelState.AddModelError("LoginError", "Email and Password doesn't match");
                }
            }
            return View("Login", model);
        }
    }
}
