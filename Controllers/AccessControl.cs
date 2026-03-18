using Models;
using System;
using System.Web;
using System.Web.Mvc;

namespace Controllers
{

    public class AccessControl
    {

        public class UserAccess : AuthorizeAttribute
        {
            private Access RequiredAccess { get; set; }

            public UserAccess(Access Access = Access.Anonymous) : base()
            {
                RequiredAccess = Access;
            }

            protected override bool AuthorizeCore(HttpContextBase httpContext)
            {
                bool ajaxRequest = HttpContext.Current.Request.Headers[13] == "cors";
                try
                {
                    if (User.ConnectedUser == null)
                    {
                        if (!ajaxRequest)
                            httpContext.Response.Redirect("/Accounts/Login?message=Accès non autorisé!&success=false");
                        return false;
                    }
                    else
                    {
                        if (User.ConnectedUser.Access < RequiredAccess || User.ConnectedUser.Blocked)
                        {
                            return false;
                        }
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    return false;
                }
            }
        }
    }
}
