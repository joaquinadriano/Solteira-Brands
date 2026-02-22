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
        /// Authentication: Login (cookie + role), Logout, Register, profile update, password changes.
    /// </summary>
    public class AccountController : Controller
    {
        /// <summary>
        /// Create or refresh the FormsAuthentication cookie for the specified user.
        /// Keeps User.Identity.Name and role in sync after profile changes.
        /// </summary>
        private void IssueAuthCookie(tblUsersModel user)
        {
            if (user == null) return;

            var roleName = user.Role != null ? user.Role.Name : "BUYER";
            var userData = (user.Username ?? string.Empty) + "|" + roleName;

            var ticket = new FormsAuthenticationTicket(
                version: 1,
                name: user.Username,
                issueDate: DateTime.UtcNow,
                expiration: DateTime.UtcNow.AddMinutes(60),
                isPersistent: false,
                userData: userData);

            var encTicket = FormsAuthentication.Encrypt(ticket);
            var cookie = new HttpCookie(FormsAuthentication.FormsCookieName, encTicket) { HttpOnly = true };
            Response.Cookies.Set(cookie);
        }

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

                IssueAuthCookie(user);

                var roleName = user.Role?.Name ?? "BUYER";
                var redirectUrl = (roleName == "ADMIN" || roleName == "MANAGER" || roleName == "STAFF")
                    ? Url.Action("AdminDashboardPage", "SolteiraAndBrands")
                    : Url.Action("SolteiraBrandsHome", "SolteiraAndBrands");

                return Json(new { success = true, redirectUrl, role = roleName });
            }
        }

        /// <summary>
        /// POST: Clear session, abandon session, sign out authentication, redirect to login.
        /// Prevents back-button access via cache headers. Use POST + anti-forgery from all layouts.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            if (Session != null)
            {
                Session.Clear();
                Session.Abandon();
            }
            FormsAuthentication.SignOut();
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();
            Response.AppendHeader("Cache-Control", "no-cache, no-store, must-revalidate");
            Response.AppendHeader("Pragma", "no-cache");
            Response.Cache.SetExpires(DateTime.UtcNow.AddYears(-1));
            return RedirectToAction("SolteiraBrandsLogin", "SolteiraAndBrands");
        }

        /// <summary>
        /// GET: Single Account Settings page for all authenticated roles (ADMIN, MANAGER, STAFF, BUYER).
        /// Loads current user via User.Identity; no role-specific duplication.
        /// </summary>
        [HttpGet]
        [Authorize]
        public ActionResult Settings()
        {
            return View("~/Views/SolteiraAndBrands/SolteiraBrandsAccountSettings.cshtml");
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
                var user = db.tbl_users.Include("Role").FirstOrDefault(u => u.Username == currentUsername);
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

                // Refresh auth cookie so User.Identity.Name and role reflect latest data
                IssueAuthCookie(user);
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
            var errors = new System.Collections.Generic.Dictionary<string, string>();

            if (string.IsNullOrWhiteSpace(email))
            {
                errors["email"] = "Email is required.";
            }

            if (string.IsNullOrEmpty(password))
            {
                errors["password"] = "Password is required.";
            }

            if (password != verifyPassword)
            {
                errors["verifyPassword"] = "Passwords do not match.";
            }

            if (string.IsNullOrWhiteSpace(username))
            {
                errors["username"] = "Username is required.";
            }

            if (!PasswordValidation.IsValid(password, out string passwordError))
            {
                errors["password"] = passwordError;
            }

            using (var db = new SolteiraBrandsContext())
            {
                if (db.tbl_users.Any(u => u.Email == email))
                {
                    errors["email"] = "Email already registered.";
                }
                if (db.tbl_users.Any(u => u.Username == username))
                {
                    errors["username"] = "Username already taken.";
                }

                if (errors.Count > 0)
                {
                    return Json(new { success = false, errors });
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

        [HttpGet]
        [AllowAnonymous]
        public ActionResult IsEmailAvailable(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return Json(new { available = false }, JsonRequestBehavior.AllowGet);

            var trimmed = email.Trim();
            using (var db = new SolteiraBrandsContext())
            {
                bool exists = db.tbl_users.Any(u => u.Email == trimmed);
                return Json(new { available = !exists }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public ActionResult IsUsernameAvailable(string username, bool ignoreCurrent = false)
        {
            if (string.IsNullOrWhiteSpace(username))
                return Json(new { available = false }, JsonRequestBehavior.AllowGet);

            var trimmed = username.Trim();

            using (var db = new SolteiraBrandsContext())
            {
                var query = db.tbl_users.AsQueryable();

                if (ignoreCurrent && User != null && User.Identity != null && User.Identity.IsAuthenticated)
                {
                    var current = db.tbl_users.FirstOrDefault(u => u.Username == User.Identity.Name);
                    if (current != null)
                    {
                        query = query.Where(u => u.Id != current.Id);
                    }
                }

                bool exists = query.Any(u => u.Username == trimmed);
                return Json(new { available = !exists }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// POST: Change password for a logged-in BUYER (no OTP).
        /// Requires current password, validates strength of new password, and ensures new != old.
        /// </summary>
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult ChangePassword(string currentPassword, string newPassword, string verifyPassword)
        {
            string FieldError(string field, string message)
            {
                // helper is inline; we just return once below with proper field name
                return message;
            }

            if (string.IsNullOrWhiteSpace(currentPassword) ||
                string.IsNullOrWhiteSpace(newPassword) ||
                string.IsNullOrWhiteSpace(verifyPassword))
            {
                return Json(new { success = false, field = "", message = "All fields are required." });
            }

            if (newPassword != verifyPassword)
            {
                return Json(new { success = false, field = "verifyPassword", message = "New password and confirmation do not match." });
            }

            if (string.Equals(currentPassword, newPassword, StringComparison.Ordinal))
            {
                return Json(new { success = false, field = "newPassword", message = "New password must be different from current password." });
            }

            if (!PasswordValidation.IsValid(newPassword, out string passwordError))
            {
                return Json(new { success = false, field = "newPassword", message = passwordError });
            }

            var username = User.Identity.Name;
            if (string.IsNullOrEmpty(username))
            {
                return Json(new { success = false, field = "", message = "Not logged in." });
            }

            using (var db = new SolteiraBrandsContext())
            {
                var user = db.tbl_users.Include("Role").FirstOrDefault(u => u.Username == username && u.IsActive);
                if (user == null)
                {
                    return Json(new { success = false, field = "", message = "User not found." });
                }

                if (user.PasswordHash == "SEEDME" || string.IsNullOrEmpty(user.PasswordSalt))
                {
                    return Json(new { success = false, field = "", message = "Account password is not initialized. Please contact support." });
                }

                if (!PasswordHasher.Verify(currentPassword, user.PasswordHash, user.PasswordSalt))
                {
                    return Json(new { success = false, field = "currentPassword", message = "Current password is incorrect." });
                }

                var (hash, salt) = PasswordHasher.HashPassword(newPassword);
                user.PasswordHash = hash;
                user.PasswordSalt = salt;
                db.SaveChanges();

                // Refresh auth cookie so identity stays valid after password rotation
                IssueAuthCookie(user);

                // TODO: send email notification for password change (once SMTP is configured)
                return Json(new { success = true });
            }
        }
    }
}
