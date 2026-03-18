using DAL;
using EmailHandling;
using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Wikimedia;
using static Controllers.AccessControl;

namespace Controllers
{
    public class AccountsController : Controller
    {
        public JsonResult EmailExist(string Email)
        {
            return Json(DB.Users.ToList().Where(u => u.Email == Email).Any(), JsonRequestBehavior.AllowGet);
        }
        public JsonResult EmailAvailable(string Email)
        {
            bool NotAvailable = false;
            int currentId = Models.User.ConnectedUser != null ? Models.User.ConnectedUser.Id : 0;
            User foundUser = DB.Users.ToList().Where(u => u.Email == Email && u.Id != currentId).FirstOrDefault();
            NotAvailable = foundUser != null;
            return Json(NotAvailable, JsonRequestBehavior.AllowGet);
        }

        public ActionResult ExpiredSession()
        {
            return Redirect("/Accounts/Login?message=Session expirée, veuillez vous reconnecter.&success=false");
        }
        public ActionResult Logout()
        {
            return Redirect("/Accounts/Login");
        }

        public ActionResult Login(string message = "", bool success = true)
        {
            if (Models.User.ConnectedUser != null)
            {
                if (success) DB.Events.Add("Logout"); else DB.Events.Add("Expired/blocked");
                if (Models.User.ConnectedUser != null)
                    DB.Logins.UpdateLogoutByUserId(Models.User.ConnectedUser.Id);
                Models.User.ConnectedUser.Online = false;
                Models.User.ConnectedUser = null;
            }

            Session["LoginSuccess"] = success;
            Session["LoginMessage"] = message;
            if (Session["CurrentLoginEmail"] == null) Session["currentLoginEmail"] = "";
            LoginCredential credential = new LoginCredential
            {
                Email = (string)Session["currentLoginEmail"]
            };

            return View(credential);
        }
        [HttpPost]
        [ValidateAntiForgeryToken()]
        public ActionResult Login(LoginCredential credential)
        {
            DateTime serverDate = DateTime.Now;
            int serverTimeZoneOffset = serverDate.Hour - serverDate.ToUniversalTime().Hour;
            Session["TimeZoneOffset"] = -(credential.TimeZoneOffset + serverTimeZoneOffset);

            credential.Email = credential.Email.Trim();
            credential.Password = credential.Password.Trim();
            Session["CurrentLoginEmail"] = credential.Email;
            User loginUser = DB.Users.GetUser(credential).Copy();

            if (loginUser == null)
            {
                Session["LoginSuccess"] = false;
                Session["LoginMessage"] = "Courriel ou mot de passe incorrect";
                return View(credential);
            }
            else
            {
                if (loginUser.Online)
                {
                    return Redirect("/Accounts/Login?message=Il y a déjà une session ouverte avec cet usager!&success=false");
                }
                if (loginUser.Blocked)
                {
                    return Redirect("/Accounts/Login?message=Votre compte a été bloqué!&success=false");
                }
                if (!loginUser.Verified)
                {
                    return Redirect("/Accounts/Login?message=Votre compte n'a pas été vérifié. Veuillez consultez le courriel de confirmation d'adresse de courriel.!&success=false");
                }
            }
            Models.User.ConnectedUser = loginUser;
            loginUser.Online = true;
            DB.Logins.Add(Models.User.ConnectedUser.Id);
            DB.Events.Add("Login");
            return Redirect(RouteConfig.DefaultAction());
        }
        public ActionResult Subscribe()
        {
            Models.User.ConnectedUser = null;
            Session["CurrentLoginEmail"] = "";
            return View(new User());
        }
        [HttpPost]
        [ValidateAntiForgeryToken()]
        public ActionResult Subscribe(User user, string NotifyCB = "off")
        {
            user.Notify = NotifyCB == "on";
            DB.Users.Add(user);
            Models.User.ConnectedUser = user;
            DB.Events.Add("Subscribe");
            Models.User.ConnectedUser = null;
            AccountsEmailing.SendEmailVerification(Url.Action("VerifyUser", "Accounts", null, Request.Url.Scheme), user);
            return Redirect("/Accounts/Login?message=Création de compte effectuée avec succès! Un courriel de confirmation d'adresse vous a été envoyé.");
        }
        public ActionResult VerifyUser(string code)
        {
            UnverifiedEmail UnverifiedEmail = DB.UnverifiedEmails.ToList().Where(u => u.VerificationCode == code).FirstOrDefault();
            if (UnverifiedEmail != null)
            {
                User newlySubscribedUser = DB.Users.Get(UnverifiedEmail.UserId);

                DB.UnverifiedEmails.Delete(UnverifiedEmail.Id);
                if (newlySubscribedUser != null)
                {
                    newlySubscribedUser.Verified = true;
                    Session["CurrentLoginEmail"] = newlySubscribedUser.Email;
                    DB.Users.Update(newlySubscribedUser);
                    DB.Events.Add("User verified");
                    AccountsEmailing.SendEmailUserStatusChanged("Votre adresse de courriel a été confirmée.", newlySubscribedUser);
                    return Redirect("/Accounts/Login?message=Votre adresse de courriel a été vérifiée avec succès!");
                }
            }
            return Redirect("/Accounts/Login?message=Erreur de vérification de courriel!&success=false");
        }

        public ActionResult RenewPasswordCommand()
        {
            ViewBag.EmailNotFound = false;
            return View(new EmailView());
        }
        [HttpPost]
        [ValidateAntiForgeryToken()]
        public ActionResult RenewPasswordCommand(EmailView EmailView)
        {
            var user = DB.Users.ToList().Where(u => u.Email == EmailView.Email).FirstOrDefault();
            if (user != null)
            {
                AccountsEmailing.SendEmailRenewPasswordCommand(Url.Action("RenewPassword", "Accounts", null, Request.Url.Scheme), EmailView.Email);
                return Redirect("/Accounts/Login?message=Un courriel de commande de changement de mot de passe vous a été envoyé si l'adresse fournie est valide.");
            }
            ViewBag.EmailNotFound = true;
            return View(EmailView);
        }
        public ActionResult RenewPassword(string code)
        {
            RenewPasswordCommand command = DB.RenewPasswordCommands.ToList().Where(r => r.VerificationCode == code).FirstOrDefault();
            if (command != null)
            {
                RenewPasswordView passwordView = new RenewPasswordView();
                return View(passwordView);
            }
            return Redirect("/Accounts/Login?message=Commande de changement de mot de passe introuvable!&success=false");

        }
        public ActionResult RenewPasswordCancelled(string code)
        {
            return Redirect("/Accounts/Login?message=Commande de changement de mot de passe annulée!&success=false");

        }
        [HttpPost]
        [ValidateAntiForgeryToken()]
        public ActionResult RenewPassword(RenewPasswordView passwordView)
        {
            RenewPasswordCommand command = DB.RenewPasswordCommands.ToList().Where(r => r.VerificationCode == passwordView.Code).FirstOrDefault();
            if (command != null)
            {
                User user = DB.Users.Get(command.UserId);
                DB.RenewPasswordCommands.Delete(command.Id);
                user.Password = passwordView.Password;
                DB.Users.ChangePassword(user);
                AccountsEmailing.SendEmailUserStatusChanged("Votre mot de passe a été modifiée avec succès!", user);
                return Redirect("/Accounts/Login?message=Votre mot de passe a été modifié avec succès!");
            }
            else
                View(passwordView);
            return Redirect("/Accounts/Login?message=Commande de changement de mot de passe introuvable!&success=false");

        }

        public ActionResult VerifyNewEmail(string code)
        {
            UnverifiedEmail UnverifiedEmail = DB.UnverifiedEmails.ToList().Where(u => u.VerificationCode == code).FirstOrDefault();
            if (UnverifiedEmail != null)
            {
                User user = DB.Users.Get(UnverifiedEmail.UserId);
                if (user != null)
                {
                    user.Verified = true;
                    user.Email = UnverifiedEmail.Email;
                    Session["CurrentLoginEmail"] = UnverifiedEmail.Email;
                    DB.UnverifiedEmails.Delete(UnverifiedEmail.Id);
                    DB.Users.Update(user);
                    AccountsEmailing.SendEmailUserStatusChanged("Votre changement d'adresse de courriel a été effectuée avec succès!", user);
                    return Redirect("/Accounts/Login?message=Votre adresse de courriel a été modifiée avec succès!");
                }
            }
            return Redirect("/Accounts/Login?message=Erreur de modification de courriel!&success=false");
        }
        [UserAccess(Models.Access.View)]
        public ActionResult EditProfil()
        {
            User connectedUser = Models.User.ConnectedUser;
            if (connectedUser != null)
            {
                Session["CurrentEditingUserPassword"] = DateTime.Now.Ticks.ToString();
                return View(connectedUser);
            }
            return Redirect(RouteConfig.DefaultAction());
        }

        [UserAccess(Models.Access.View)]
        [HttpPost]
        [ValidateAntiForgeryToken()]
        public ActionResult EditProfil(User user, string NotifyCB = "off")
        {
            /* 
                important note:
                form checkbox have odd behavior :
                nothing in playload if not checked
                "on" if checked 
            */

            DB.Events.Add("EditProfil");
            bool newEmail = false;
            User connectedUser = Models.User.ConnectedUser;
            user.Id = connectedUser.Id;
            user.Blocked = connectedUser.Blocked;
            user.Access = connectedUser.Access;
            user.Verified = connectedUser.Verified;
            user.Notify = NotifyCB == "on";
            // check password has been changed 
            if (user.Password == (string)Session["CurrentEditingUserPassword"])
                user.Password = connectedUser.Password; // no password change
            // check if Email has been changed
            if (user.Email != connectedUser.Email)
            {
                newEmail = true;
                AccountsEmailing.SendEmailChangedVerification(Url.Action("VerifyNewEmail", "Accounts", null, Request.Url.Scheme), user);
                user.Email = connectedUser.Email; // new Email will commited on verification
            }
            if (DB.Users.Update(user))
            {
                Models.User.ConnectedUser = DB.Users.Get(user.Id);
                DB.Notifications.Push(user.Id, "Votre profil a été modifié avec succès!");
            }
            if (newEmail)
                return Redirect("/Accounts/Login?message=Un courriel de vérification d'adresse de courriel vous a été envoyé!");
            else
                return Redirect(RouteConfig.DefaultAction());
        }
        [UserAccess(Models.Access.View)]
        public ActionResult DeleteProfil()
        {
            DB.Events.Add("DeleteProfil");
            User connectedUser = Models.User.ConnectedUser;
            DB.Users.Delete(connectedUser.Id);
            return RedirectToAction("Login?message=Votre compte a été effacé avec succès!");
        }

        [UserAccess(Access.Write)]
        public ActionResult GetUsers(bool forceRefresh = false)
        {
            if (DB.Users.HasChanged || DB.Logins.HasChanged || forceRefresh)
            {
                return PartialView(DB.Users.ToList().Where(u => u.Id != Models.User.ConnectedUser.Id).OrderBy(u => u.Name).ToList());
            }
            return null;
        }

        [UserAccess(Access.Admin)]
        public ActionResult ManageUsers()
        {
            DB.Events.Add("ManageUsers");
            return View();
        }
        [UserAccess(Access.Admin)]
        public ActionResult SetUserAccess(int userid, int access)
        {
            DB.Events.Add("SetUserAccess");
            if (userid != 1)
            {
                User user = DB.Users.Get(userid);
                if (user != null)
                {
                    int previousAccess = (int)user.Access;
                    user.Access = (Models.Access)access;

                    DB.Users.Update(user);
                    string accessTitle = "Anonyme";
                    switch (user.Access)
                    {
                        case Models.Access.View: accessTitle = "Lecture seule"; break;
                        case Models.Access.Write: accessTitle = "Lecture/Écriture"; break;
                        case Models.Access.Admin: accessTitle = "Administrateur"; break;
                    }

                    string message = "Vos ayant droits ont été modifiés : " + accessTitle;

                    AccountsEmailing.SendEmailUserStatusChanged(message, user);
                }
            }
            return null;
        }
        [UserAccess(Access.Admin)]
        public ActionResult ToggleBlockUser(int id)
        {
            DB.Events.Add("ToggleBlockUser");
            if (id != 1)
            {
                User user = DB.Users.Get(id);
                if (user != null)
                {
                    user.Blocked = !user.Blocked;
                    user.Online = false;
                    DB.Users.Update(user);
                    string message = user.Blocked ?
                        "Votre compte a été bloqué par l'administrateur du site." :
                        "Votre compte a été débloqué par l'administrateur du site.";
                    AccountsEmailing.SendEmailUserStatusChanged(message, user);
                }
            }
            return null;
        }
        [UserAccess(Access.Admin)]
        public ActionResult ForceVerifyUser(int id)
        {
            if (id != 1)
            {
                User user = DB.Users.Get(id);
                if (user != null)
                {
                    user.Verified = true;
                    DB.Users.Update(user);
                    string message = "Votre adresse de courriel a été confirmée par l'administrateur du site.";
                    AccountsEmailing.SendEmailUserStatusChanged(message, user);

                }
            }
            return null;
        }
        [UserAccess(Access.Admin)]
        public ActionResult DeleteUser(int id)
        {

            if (id != 1)
            {
                User user = DB.Users.Get(id);
                if (user != null)
                {
                    DB.Events.Add("DeleteUser " + user.Name);
                    string message = "Votre compte a été effacé par l'administrateur du site.";
                    DB.Users.Delete(id);
                    AccountsEmailing.SendEmailUserStatusChanged(message, user);
                }
            }
            return null;
        }
        #region Login journal
        [UserAccess(Access.Admin)]
        public ActionResult LoginsJournal()
        {
            return View();
        }
        [UserAccess(Access.Admin)] // RefreshTimout = false otherwise periodical refresh with lead to never timed out session
        public ActionResult GetLoginsList(bool forceRefresh = false)
        {
            if (DB.Logins.HasChanged || forceRefresh)
            {
                List<User> onlineUsers = DB.Users.ToList().Where(u => u.Online).ToList();
                ViewBag.LoggedUsersId = onlineUsers.Select(u => u.Id).ToList();
                List<Login> logins = DB.Logins.ToList().OrderByDescending(l => l.LoginDate).ToList();
                return PartialView(logins);
            }
            return null;
        }
        [UserAccess(Access.Admin)]
        public ActionResult EventsJournal()
        {
            return View();
        }
        [UserAccess(Access.Admin)] // RefreshTimout = false otherwise periodical refresh with lead to never timed out session
        public ActionResult GetEventsList(bool forceRefresh = false)
        {
            if (DB.Events.HasChanged || forceRefresh)
            {
                List<Event> events = DB.Events.ToList().OrderByDescending(l => l.CreationDate).ToList();
                return PartialView(events);
            }
            return null;
        }
        [UserAccess(Access.Admin)]
        public ActionResult DeleteLoginsDay(string day)
        {
            try
            {
                DateTime date = DateTime.Parse(day);
                DB.Logins.DeleteLoginsJournalDay(date);
            }
            catch (Exception) { }
            return RedirectToAction("LoginsJournal");
        }
        [UserAccess(Access.Admin)]
        public ActionResult DeleteEventsDay(string day)
        {
            try
            {
                DateTime date = DateTime.Parse(day);
                DB.Events.DeleteEventsJournalDay(date);
            }

            catch (Exception) { }
            return RedirectToAction("EventsJournal");
        }
        #endregion
    }
}