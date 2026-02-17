using System;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using Solteira_Brands.Models;
using Solteira_Brands.Models.Context;
using Solteira_Brands.Security;

namespace Solteira_Brands.Controllers
{
    /// <summary>
    /// Authentication: Login (cookie + role), Logout, Register.
    /// </summary>
    public class AccountController : Controller
    {
        /// <summary>
        /// POST: Validate username/password, create Forms auth cookie with role, return redirect URL.
        /// </summary>
        [HttpPost]
        public ActionResult Login(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
            {
                return Json(new { success = false, message = "Username and password are required." });
            }

            using (var db = new SolteiraBrandsContext())
            {
                var user = db.tbl_users
                    .Include("Role")
                    .FirstOrDefault(u => u.Username == username.Trim() && u.IsActive);
                if (user == null)
                {
                    return Json(new { success = false, message = "Invalid username or password." });
                }
                if (user.PasswordHash == "SEEDME" || string.IsNullOrEmpty(user.PasswordSalt))
                {
                    return Json(new { success = false, message = "Account not ready. Run /Account/SeedTrialPasswords first." });
                }
                if (!PasswordHasher.Verify(password, user.PasswordHash, user.PasswordSalt))
                {
                    return Json(new { success = false, message = "Invalid username or password." });
                }

                var roleName = user.Role?.Name ?? "BUYER";
                var userData = user.Username + "|" + roleName;
                var ticket = new FormsAuthenticationTicket(
                    version: 1,
                    name: user.Username,
                    issueDate: DateTime.UtcNow,
                    expiration: DateTime.UtcNow.AddMinutes(60),
                    isPersistent: false,
                    userData: userData);

                var encTicket = FormsAuthentication.Encrypt(ticket);
                var cookie = new System.Web.HttpCookie(FormsAuthentication.FormsCookieName, encTicket) { HttpOnly = true };
                Response.Cookies.Add(cookie);

                var redirectUrl = (roleName == "ADMIN" || roleName == "MANAGER" || roleName == "STAFF")
                    ? Url.Action("AdminDashboardPage", "SolteiraAndBrands")
                    : Url.Action("SolteiraBrandsHome", "SolteiraAndBrands");

                return Json(new { success = true, redirectUrl, role = roleName });
            }
        }

        /// <summary>
        /// GET: Sign out and redirect to login page.
        /// </summary>
        [HttpGet]
        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            return RedirectToAction("SolteiraBrandsLogin", "SolteiraAndBrands");
        }

        private const string DefaultAvatarUrl = "/Content/img/shared/default-avatar.svg";

        /// <summary>
        /// GET: Current user profile (username, email, avatarUrl). Use default avatar when no picture.
        /// </summary>
        [HttpGet]
        [Authorize]
        public ActionResult GetMyProfile()
        {
            var username = User.Identity.Name;
            if (string.IsNullOrEmpty(username))
                return Json(new { success = false }, JsonRequestBehavior.AllowGet);
            using (var db = new SolteiraBrandsContext())
            {
                var user = db.tbl_users.Include("AvatarImage").FirstOrDefault(u => u.Username == username);
                if (user == null)
                    return Json(new { success = false }, JsonRequestBehavior.AllowGet);
                var avatarUrl = user.AvatarImage != null
                    ? (user.AvatarImage.ImagePath + user.AvatarImage.ImageFileName)
                    : DefaultAvatarUrl;
                return Json(new { success = true, username = user.Username, email = user.Email, avatarUrl },
                    JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// POST: Update profile (avatar image ID and/or username).
        /// </summary>
        [HttpPost]
        [Authorize]
        public ActionResult UpdateProfile(int? avatarImageId, string username)
        {
            var currentUsername = User.Identity.Name;
            if (string.IsNullOrEmpty(currentUsername))
                return Json(new { success = false, message = "Not logged in." });
            using (var db = new SolteiraBrandsContext())
            {
                var user = db.tbl_users.FirstOrDefault(u => u.Username == currentUsername);
                if (user == null)
                    return Json(new { success = false, message = "User not found." });
                if (avatarImageId.HasValue)
                    user.AvatarImageId = avatarImageId.Value;
                if (!string.IsNullOrWhiteSpace(username))
                {
                    var trimmed = username.Trim();
                    if (db.tbl_users.Any(u => u.Username == trimmed && u.Id != user.Id))
                        return Json(new { success = false, message = "Username already taken." });
                    user.Username = trimmed;
                }
                db.SaveChanges();
            }
            return Json(new { success = true });
        }

        /// <summary>
        /// POST: Upload profile picture. Saves to ~/Content/images/useravatars/, inserts tbl_images, returns imageID.
        /// </summary>
        [HttpPost]
        [Authorize]
        public ActionResult UploadProfileImage(HttpPostedFileBase file)
        {
            if (file == null || file.ContentLength == 0)
                return Json(new { success = false, message = "No file uploaded." });
            var fileName = file.FileName ?? "";
            if (!fileName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) && !fileName.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) && !fileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                return Json(new { success = false, message = "Only JPG, JPEG, or PNG allowed." });
            if (file.ContentLength > 5 * 1024 * 1024)
                return Json(new { success = false, message = "File size exceeds 5MB." });

            var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(fileName);
            var uploadPath = Server.MapPath("~/Content/images/useravatars");
            if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);
            file.SaveAs(Path.Combine(uploadPath, uniqueFileName));

            const string imagePath = "/Content/images/useravatars/";
            var now = DateTime.Now;
            using (var db = new SolteiraBrandsContext())
            {
                var newImage = new tblImagesModel
                {
                    ImagePath = imagePath,
                    ImageFileName = uniqueFileName,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                db.tbl_images.Add(newImage);
                db.SaveChanges();
                return Json(new { success = true, imageID = newImage.Id, imageFileName = uniqueFileName, imagePath = imagePath });
            }
        }

        /// <summary>
        /// Register a new user (buyer). Validates password (8–64 chars, upper, lower, number, special), hashes with PBKDF2, saves to DB.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(string email, string username, string password, string verifyPassword)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(username))
            {
                return Json(new { success = false, message = "Email and username are required." });
            }

            if (string.IsNullOrEmpty(password))
            {
                return Json(new { success = false, message = "Password is required." });
            }

            if (password != verifyPassword)
            {
                return Json(new { success = false, message = "Passwords do not match." });
            }

            if (!PasswordValidation.IsValid(password, out string passwordError))
            {
                return Json(new { success = false, message = passwordError });
            }

            using (var db = new SolteiraBrandsContext())
            {
                if (db.tbl_users.Any(u => u.Email == email))
                {
                    return Json(new { success = false, message = "An account with this email already exists." });
                }
                if (db.tbl_users.Any(u => u.Username == username))
                {
                    return Json(new { success = false, message = "This username is already taken." });
                }

                var buyerRole = db.tbl_roles.FirstOrDefault(r => r.Name == "BUYER");
                if (buyerRole == null)
                {
                    return Json(new { success = false, message = "Registration is not configured. Please contact support." });
                }

                var (hashBase64, saltBase64) = PasswordHasher.HashPassword(password);

                var user = new tblUsersModel
                {
                    RoleId = buyerRole.Id,
                    Email = email.Trim(),
                    Username = username.Trim(),
                    PasswordHash = hashBase64,
                    PasswordSalt = saltBase64,
                    IsVerified = false,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                db.tbl_users.Add(user);
                db.SaveChanges();
            }

            return Json(new { success = true, message = "Account created. You can now log in." });
        }

        /// <summary>
        /// One-time seed: replace SEEDME password_hash/password_salt for trial accounts with PBKDF2 hash for "Password123@".
        /// Run once after executing the SQL script. Safe to call multiple times (only updates rows where password_hash = 'SEEDME').
        /// </summary>
        [HttpGet]
        public ActionResult SeedTrialPasswords()
        {
            const string trialPassword = "Password123@";
            int updated = 0;

            using (var db = new SolteiraBrandsContext())
            {
                var seedUsers = db.tbl_users.Where(u => u.PasswordHash == "SEEDME").ToList();
                foreach (var u in seedUsers)
                {
                    var (hash, salt) = PasswordHasher.HashPassword(trialPassword);
                    u.PasswordHash = hash;
                    u.PasswordSalt = salt;
                    updated++;
                }
                if (updated > 0)
                    db.SaveChanges();
            }

            return Content($"Trial passwords seeded. Updated {updated} account(s). Password for all: {trialPassword}. You can remove or protect this action in production.");
        }
    }
}
