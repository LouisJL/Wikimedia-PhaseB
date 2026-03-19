using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using DAL;
using Models;
using static Controllers.AccessControl;

[UserAccess(Access.View)]
public class MediasController : Controller
{

    private void InitSessionVariables()
    {
        // Session is a dictionary that hold keys values specific to a session
        // Each user of this web application have their own Session
        // A Session has a default time out of 20 minutes, after time out it is cleared

        if (Session["CurrentMediaId"] == null) Session["CurrentMediaId"] = 0;
        if (Session["CurrentMediaTitle"] == null) Session["CurrentMediaTitle"] = "";
        if (Session["Search"] == null) Session["Search"] = false;
        if (Session["SearchString"] == null) Session["SearchString"] = "";
        if (Session["SelectedCategory"] == null) Session["SelectedCategory"] = "";
        if (Session["Categories"] == null) Session["Categories"] = DB.Medias.MediasCategories();
        if (Session["SortByTitle"] == null) Session["SortByTitle"] = true;
        if (Session["SortAscending"] == null) Session["SortAscending"] = true;
        ValidateSelectedCategory();
    }

    private void ResetCurrentMediaInfo()
    {
        Session["CurrentMediaId"] = 0;
        Session["CurrentMediaTitle"] = "";
    }

    private void ValidateSelectedCategory()
    {
        if (Session["SelectedCategory"] != null)
        {
            var selectedCategory = (string)Session["SelectedCategory"];
            var Medias = DB.Medias.ToList().Where(c => c.Category == selectedCategory);
            if (Medias.Count() == 0)
                Session["SelectedCategory"] = "";
        }
    }

    public ActionResult GetMediasCategoriesList(bool forceRefresh = false)
    {
        try
        {
            InitSessionVariables();

            bool search = (bool)Session["Search"];

            if (search)
            {
                return PartialView();
            }
            return null;
        }
        catch (System.Exception ex)
        {
            return Content("Erreur interne" + ex.Message, "text/html");
        }
    }
    // This action produce a partial view of Medias
    // It is meant to be called by an AJAX request (from client script)
    public ActionResult GetMediaDetails(bool forceRefresh = false)
    {
        try
        {
            InitSessionVariables();

            int mediaId = (int)Session["CurrentMediaId"];
            Media Media = DB.Medias.Get(mediaId);

            var currentUser = Models.User.ConnectedUser;
            int currentUserId = currentUser?.Id ?? 0;
            bool isAdmin = currentUser?.IsAdmin ?? false;

            if (Media != null)
            {
                if (!(isAdmin || Media.Shared || Media.OwnerId == currentUserId))
                {
                    return PartialView("AccesRefuse");
                }

                if (DB.Users.HasChanged || DB.Medias.HasChanged || forceRefresh)
                {
                    return PartialView(Media);
                }
            }
            return null;
        }
        catch (System.Exception ex)
        {
            return Content("Erreur interne" + ex.Message, "text/html");
        }
    }
    public ActionResult GetMedias(bool forceRefresh = false)
    {
        try
        {
            IEnumerable<Media> result = null;

            if (DB.Medias.HasChanged || forceRefresh)
            {
                InitSessionVariables();

                var currentUser = Models.User.ConnectedUser;
                int currentUserId = currentUser?.Id ?? 0;
                bool isAdmin = currentUser?.IsAdmin ?? false;

                bool search = (bool)Session["Search"];
                string searchString = (string)Session["SearchString"];

                if (search)
                {
                    result = DB.Medias.ToList()
                            .Where(c => c.Title.ToLower().Contains(searchString))
                            .Where(m => isAdmin || m.Shared || m.OwnerId == currentUserId)
                            .OrderBy(c => c.Title);

                    string selectedCategory = (string)Session["SelectedCategory"];
                    if (!string.IsNullOrEmpty(selectedCategory))
                        result = result.Where(c => c.Category == selectedCategory);
                }
                else
                {
                    result = DB.Medias.ToList()
                        .Where(m => isAdmin || m.Shared || m.OwnerId == currentUserId);
                }

                if ((bool)Session["SortAscending"])
                {
                    result = (bool)Session["SortByTitle"]
                        ? result.OrderBy(c => c.Title)
                        : result.OrderBy(c => c.PublishDate);
                }
                else
                {
                    result = (bool)Session["SortByTitle"]
                        ? result.OrderByDescending(c => c.Title)
                        : result.OrderByDescending(c => c.PublishDate);
                }

                var model = result.Select(m => new MediaOwner
                {
                    Media = m,
                    OwnerAvatar = DB.Users.Get(m.OwnerId)?.Avatar ?? "/images/default-avatar.png",
                    OwnerName = DB.Users.Get(m.OwnerId)?.Name ?? "Utilisateur inconnu",
                }).ToList();

                return PartialView(model);
            }

            return null;
        }
        catch (System.Exception ex)
        {
            return Content("Erreur interne: " + ex.Message, "text/html");
        }
    }


    public ActionResult List()
    {
        ResetCurrentMediaInfo();
        return View();
    }

    public ActionResult ToggleSearch()
    {
        if (Session["Search"] == null) Session["Search"] = false;
        Session["Search"] = !(bool)Session["Search"];
        return RedirectToAction("List");
    }
    public ActionResult SortByTitle()
    {
        Session["SortByTitle"] = true;
        return RedirectToAction("List");
    }
    public ActionResult ToggleSort()
    {
        Session["SortAscending"] = !(bool)Session["SortAscending"];
        return RedirectToAction("List");
    }
    public ActionResult SortByDate()
    {
        Session["SortByTitle"] = false;
        return RedirectToAction("List");
    }

    public ActionResult SetSearchString(string value)
    {
        Session["SearchString"] = value.ToLower();
        return RedirectToAction("List");
    }

    public ActionResult SetSearchCategory(string value)
    {
        Session["SelectedCategory"] = value;
        return RedirectToAction("List");
    }
    public ActionResult About()
    {
        return View();
    }


    public ActionResult Details(int id)
    {
        Session["CurrentMediaId"] = id;
        Media Media = DB.Medias.Get(id);
        if (Media != null)
        {
            Session["CurrentMediaTitle"] = Media.Title;
            Session["CurrentMediaOwnerId"] = Media.OwnerId;
            return View(Media);
        }
        return RedirectToAction("List");
    }
    [UserAccess(Access.Write)]
    public ActionResult Create()
    {
        return View(new Media());
    }

    [HttpPost]
    [UserAccess(Access.Write)]
    [ValidateAntiForgeryToken]
    public ActionResult Create(Media media)
    {
        var currentUser = Models.User.ConnectedUser;

        if (currentUser == null)
            return RedirectToAction("List");

        media.OwnerId = currentUser.Id;

        media.PublishDate = DateTime.Now;

        DB.Medias.Add(media);

        return RedirectToAction("List");
    }

    [UserAccess(Access.Write)]
    public ActionResult Edit()
    {
        int id = Session["CurrentMediaId"] != null ? (int)Session["CurrentMediaId"] : 0;
        if (id == 0) return RedirectToAction("List");

        Media media = DB.Medias.Get(id);
        var currentUser = Models.User.ConnectedUser;
        if (media == null || currentUser == null) return RedirectToAction("List");

        if (!currentUser.IsAdmin && media.OwnerId != currentUser.Id)
            return Content("Vous n'êtes pas autorisé à modifier ce média.");

        return View(media);
    }

    [UserAccess(Access.Write)]
    [HttpPost]
    [ValidateAntiForgeryToken()]
    public ActionResult Edit(Media media)
    {
        int id = Session["CurrentMediaId"] != null ? (int)Session["CurrentMediaId"] : 0;
        Media storedMedia = DB.Medias.Get(id);
        var currentUser = Models.User.ConnectedUser;

        if (storedMedia == null || currentUser == null)
            return RedirectToAction("List");

        if (!currentUser.IsAdmin && storedMedia.OwnerId != currentUser.Id)
            return Content("Vous n'êtes pas autorisé à modifier ce média.");

        media.Id = id;
        media.PublishDate = storedMedia.PublishDate;
        media.OwnerId = storedMedia.OwnerId;
        DB.Medias.Update(media);

        return RedirectToAction("Details/" + id);
    }

    [UserAccess(Access.Write)]
    public ActionResult Delete()
    {
        int id = Session["CurrentMediaId"] != null ? (int)Session["CurrentMediaId"] : 0;
        Media media = DB.Medias.Get(id);
        var currentUser = Models.User.ConnectedUser;

        if (media == null || currentUser == null)
            return RedirectToAction("List");

        if (!currentUser.IsAdmin && media.OwnerId != currentUser.Id)
            return Content("Vous n'êtes pas autorisé à supprimer ce média.");

        DB.Medias.Delete(id);
        return RedirectToAction("List");
    }

    // This action is meant to be called by an AJAX request
    // Return true if there is a name conflict
    // Look into validation.js for more details
    // and also into Views/Medias/MediaForm.cshtml
    public JsonResult CheckConflict(string YoutubeId)
    {
        int id = Session["CurrentMediaId"] != null ? (int)Session["CurrentMediaId"] : 0;
        // Response json value true if name is used in other Medias than the current Media
        return Json(DB.Medias.ToList().Where(c => c.YoutubeId == YoutubeId && c.Id != id).Any(),
                    JsonRequestBehavior.AllowGet /* must have for CORS verification by client browser */);
    }

}
