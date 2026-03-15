using System;
using System.Configuration;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using Solteira_Brands.Helpers;
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

        private const string TokenTypeEmailVerification = "EmailVerification";
        private const string TokenTypePasswordReset = "PasswordReset";
        private const string SessionPasswordResetEmail = "PasswordResetEmail";
        private const string SessionPasswordResetExpiry = "PasswordResetExpiry";

        /// <summary>
        /// Generate a cryptographically random 6-digit OTP and its SHA256 hash for storage.
        /// </summary>
        private static (string otpPlain, string otpHash) GenerateOtp()
        {
            var bytes = new byte[4];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(bytes);
            }
            int value = Math.Abs(BitConverter.ToInt32(bytes, 0)) % 1000000;
            string otpPlain = value.ToString("D6");
            using (var sha = SHA256.Create())
            {
                var hashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(otpPlain));
                string otpHash = Convert.ToBase64String(hashBytes);
                return (otpPlain, otpHash);
            }
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
            var loginUsername = username.Trim();
            if (loginUsername.Length > 14)
                return Json(new { success = false, message = "Username must not exceed 14 characters." });

            using (var db = new SolteiraBrandsContext())
            {
                var user = db.tbl_users
                    .Include("Role")
                    .FirstOrDefault(u => u.Username == loginUsername && u.IsActive);
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
                if (roleName == "BUYER" && !user.IsVerified)
                {
                    return Json(new { success = false, message = "Please verify your email before signing in. Check your inbox for the 6-digit OTP." });
                }

                IssueAuthCookie(user);

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
        /// Avatars stored under ~/Content/images_uploaded/avatar/ (same pattern as QR and payment proof).
        /// </summary>
        [HttpGet]
        [Authorize]
        public ActionResult GetMyProfile()
        {
            var username = User.Identity.Name;
            if (string.IsNullOrEmpty(username))
                return Json(new { success = false }, JsonRequestBehavior.AllowGet);
            try
            {
                using (var db = new SolteiraBrandsContext())
                {
                    var user = db.tbl_users.Include("AvatarImage").FirstOrDefault(u => u.Username == username);
                    if (user == null)
                        return Json(new { success = false }, JsonRequestBehavior.AllowGet);
                    var avatarUrl = DefaultAvatarUrl;
                    if (user.AvatarImage != null && !string.IsNullOrEmpty(user.AvatarImage.ImagePath) && !string.IsNullOrEmpty(user.AvatarImage.ImageFileName))
                        avatarUrl = user.AvatarImage.ImagePath + user.AvatarImage.ImageFileName;
                    return Json(new { success = true, username = user.Username, email = user.Email, avatarUrl },
                        JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Unable to load profile." }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// POST: Update profile (avatar image ID and/or username). Avatar image stored under images_uploaded/avatar.
        /// </summary>
        [HttpPost]
        [Authorize]
        public ActionResult UpdateProfile(int? avatarImageId, string username)
        {
            var currentUsername = User.Identity.Name;
            if (string.IsNullOrEmpty(currentUsername))
                return Json(new { success = false, message = "Not logged in." });
            try
            {
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
                        if (trimmed.Length > 14)
                            return Json(new { success = false, message = "Username must not exceed 14 characters." });
                        if (trimmed.Any(char.IsWhiteSpace))
                            return Json(new { success = false, message = "Username must not contain spaces." });
                        if (db.tbl_users.Any(u => u.Username == trimmed && u.Id != user.Id))
                            return Json(new { success = false, message = "Username already taken." });
                        user.Username = trimmed;
                    }
                    db.SaveChanges();
                    IssueAuthCookie(user);
                }
                return Json(new { success = true });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Unable to save profile. Please try again." });
            }
        }

        /// <summary>
        /// POST: Upload profile picture. Saves to ~/Content/images_uploaded/avatar/, inserts tbl_images, returns imageID.
        /// </summary>
        [HttpPost]
        [Authorize]
        public ActionResult UploadProfileImage(HttpPostedFileBase file)
        {
            if (file == null || file.ContentLength == 0)
                return Json(new { success = false, message = "No file uploaded." });
            var fileName = file.FileName ?? "";
            var ext = Path.GetExtension(fileName) ?? "";
            if (!ext.Equals(".jpg", StringComparison.OrdinalIgnoreCase) && !ext.Equals(".jpeg", StringComparison.OrdinalIgnoreCase) && !ext.Equals(".png", StringComparison.OrdinalIgnoreCase))
                return Json(new { success = false, message = "Only JPG, JPEG, or PNG allowed." });
            if (file.ContentLength > 5 * 1024 * 1024)
                return Json(new { success = false, message = "File size exceeds 5MB." });

            var uniqueFileName = Guid.NewGuid().ToString() + ext;
            var uploadPath = Server.MapPath("~/Content/images_uploaded/avatar");
            if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);
            file.SaveAs(Path.Combine(uploadPath, uniqueFileName));

            const string imagePath = "/Content/images_uploaded/avatar/";
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
            else
            {
                var trimmed = username.Trim();
                if (trimmed.Length > 14)
                    errors["username"] = "Username must not exceed 14 characters.";
                if (trimmed.Any(char.IsWhiteSpace))
                    errors["username"] = (errors.ContainsKey("username") ? errors["username"] + " " : "") + "Username must not contain spaces.";
            }

            if (!PasswordValidation.IsValid(password, out string passwordError))
            {
                errors["password"] = passwordError;
            }

            bool emailSent = false;
            using (var db = new SolteiraBrandsContext())
            {
                if (db.tbl_users.Any(u => u.Email == email))
                {
                    errors["email"] = "Email already registered.";
                }
                if (db.tbl_users.Any(u => u.Username == (username ?? "").Trim()))
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

                var (otpPlain, otpHash) = GenerateOtp();
                var expiresAt = DateTime.UtcNow.AddMinutes(15);
                var token = new tblVerificationTokenModel
                {
                    UserId = user.Id,
                    TokenValue = otpHash,
                    TokenType = TokenTypeEmailVerification,
                    ExpiresAt = expiresAt,
                    IsUsed = false,
                    CreatedAt = DateTime.UtcNow
                };
                db.tbl_verification_tokens.Add(token);
                db.SaveChanges();

                emailSent = EmailHelper.SendEmail(
                    user.Email,
                    "Verify your Solteira & Brands account",
                    "Your verification code is: " + otpPlain + "\r\n\r\nThis code expires in 15 minutes. If you did not create an account, please ignore this email.");
            }

            return Json(new
            {
                success = true,
                requireVerification = true,
                emailSent,
                message = emailSent
                    ? "Account created. Check your email for a 6-digit verification code to activate your account."
                    : "Account created but we could not send the verification email. Please check your email address, check spam, or try again later.",
                email = email.Trim()
            });
        }

        /// <summary>
        /// POST: Verify email OTP for account activation. Accepts email and otp; validates token, marks it used, sets user.IsVerified = true.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult VerifyAccount(string email, string otp)
        {
            if (string.IsNullOrWhiteSpace(email))
                return Json(new { success = false, message = "Email is required." });
            if (string.IsNullOrWhiteSpace(otp) || otp.Trim().Length != 6)
                return Json(new { success = false, message = "Please enter the 6-digit code from your email." });

            var emailTrim = email.Trim();
            var otpTrim = otp.Trim();
            string otpHash;
            using (var sha = SHA256.Create())
            {
                otpHash = Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(otpTrim)));
            }

            var nowUtc = DateTime.UtcNow;
            using (var db = new SolteiraBrandsContext())
            {
                var user = db.tbl_users.FirstOrDefault(u => u.Email == emailTrim);
                if (user == null)
                    return Json(new { success = false, message = "Invalid or expired code. Please request a new one." });

                var token = db.tbl_verification_tokens
                    .Where(t => t.UserId == user.Id && t.TokenType == TokenTypeEmailVerification && !t.IsUsed && t.ExpiresAt > nowUtc)
                    .OrderByDescending(t => t.CreatedAt)
                    .FirstOrDefault();
                if (token == null || token.TokenValue != otpHash)
                    return Json(new { success = false, message = "Invalid or expired code. Please check the code or request a new one." });

                token.IsUsed = true;
                user.IsVerified = true;
                db.SaveChanges();
            }

            return Json(new { success = true, message = "Email verified. You can now sign in." });
        }

        /// <summary>
        /// POST: Forgot password - send OTP to email. Finds user by email, generates 6-digit OTP, stores token (PasswordReset), emails OTP.
        /// </summary>
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult ForgotPassword(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return Json(new { success = false, message = "Email is required." });
            var emailTrim = email.Trim();
            using (var db = new SolteiraBrandsContext())
            {
                var user = db.tbl_users.FirstOrDefault(u => u.Email == emailTrim && u.IsActive);
                if (user == null)
                    return Json(new { success = false, message = "No account found with that email address." });

                var (otpPlain, otpHash) = GenerateOtp();
                var nowUtc = DateTime.UtcNow;
                var expiresAt = nowUtc.AddMinutes(15);
                var token = new tblVerificationTokenModel
                {
                    UserId = user.Id,
                    TokenValue = otpHash,
                    TokenType = TokenTypePasswordReset,
                    ExpiresAt = expiresAt,
                    IsUsed = false,
                    CreatedAt = nowUtc
                };
                db.tbl_verification_tokens.Add(token);
                db.SaveChanges();

                var emailSent = EmailHelper.SendEmail(
                    user.Email,
                    "Reset your Solteira & Brands password",
                    "Your password reset code is: " + otpPlain + "\r\n\r\nThis code expires in 15 minutes. If you did not request a password reset, please ignore this email.");
                if (!emailSent)
                    return Json(new { success = false, message = "We could not send the email. Please try again later." });
            }
            return Json(new { success = true, message = "Check your email for the verification code.", email = emailTrim });
        }

        /// <summary>
        /// POST: Validate OTP for password reset. On success, stores email in session so ResetPassword can be called.
        /// </summary>
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult ValidatePasswordResetOtp(string email, string otp)
        {
            if (string.IsNullOrWhiteSpace(email))
                return Json(new { success = false, message = "Email is required." });
            if (string.IsNullOrWhiteSpace(otp) || otp.Trim().Length != 6)
                return Json(new { success = false, message = "Please enter the 6-digit code from your email." });

            var emailTrim = email.Trim();
            var otpTrim = otp.Trim();
            string otpHash;
            using (var sha = SHA256.Create())
            {
                otpHash = Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(otpTrim)));
            }

            var nowUtc = DateTime.UtcNow;
            using (var db = new SolteiraBrandsContext())
            {
                var user = db.tbl_users.FirstOrDefault(u => u.Email == emailTrim && u.IsActive);
                if (user == null)
                    return Json(new { success = false, message = "Invalid or expired code." });

                var token = db.tbl_verification_tokens
                    .Where(t => t.UserId == user.Id && t.TokenType == TokenTypePasswordReset && !t.IsUsed && t.ExpiresAt > nowUtc)
                    .OrderByDescending(t => t.CreatedAt)
                    .FirstOrDefault();
                if (token == null || token.TokenValue != otpHash)
                    return Json(new { success = false, message = "Invalid or expired code. Please try again." });
            }

            if (Session != null)
            {
                Session[SessionPasswordResetEmail] = emailTrim;
                Session[SessionPasswordResetExpiry] = nowUtc.AddMinutes(10);
            }
            return Json(new { success = true, message = "Code verified. You can now set a new password.", email = emailTrim });
        }

        /// <summary>
        /// POST: Set new password after OTP validation. Requires session set by ValidatePasswordResetOtp. Updates password, marks token used.
        /// </summary>
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult ResetPassword(string email, string newPassword, string confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(email))
                return Json(new { success = false, message = "Email is required." });
            if (string.IsNullOrWhiteSpace(newPassword) || string.IsNullOrWhiteSpace(confirmPassword))
                return Json(new { success = false, message = "Password and confirmation are required." });
            if (newPassword != confirmPassword)
                return Json(new { success = false, message = "Passwords do not match." });
            if (!PasswordValidation.IsValid(newPassword, out string passwordError))
                return Json(new { success = false, message = passwordError });

            var emailTrim = email.Trim();
            if (Session == null || Session[SessionPasswordResetEmail] as string != emailTrim)
                return Json(new { success = false, message = "Session expired. Please request a new code from the Forgot Password page." });
            var expiry = Session[SessionPasswordResetExpiry] as DateTime?;
            if (expiry == null || DateTime.UtcNow > expiry.Value)
            {
                if (Session != null) { Session.Remove(SessionPasswordResetEmail); Session.Remove(SessionPasswordResetExpiry); }
                return Json(new { success = false, message = "Session expired. Please request a new code from the Forgot Password page." });
            }

            using (var db = new SolteiraBrandsContext())
            {
                var user = db.tbl_users.FirstOrDefault(u => u.Email == emailTrim && u.IsActive);
                if (user == null)
                {
                    Session.Remove(SessionPasswordResetEmail);
                    Session.Remove(SessionPasswordResetExpiry);
                    return Json(new { success = false, message = "Account not found." });
                }
                var token = db.tbl_verification_tokens
                    .Where(t => t.UserId == user.Id && t.TokenType == TokenTypePasswordReset && !t.IsUsed)
                    .OrderByDescending(t => t.CreatedAt)
                    .FirstOrDefault();
                if (token != null)
                    token.IsUsed = true;
                var (hashBase64, saltBase64) = PasswordHasher.HashPassword(newPassword);
                user.PasswordHash = hashBase64;
                user.PasswordSalt = saltBase64;
                db.SaveChanges();
            }
            if (Session != null)
            {
                Session.Remove(SessionPasswordResetEmail);
                Session.Remove(SessionPasswordResetExpiry);
            }
            return Json(new { success = true, message = "Password updated. You can now sign in." });
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
            if (trimmed.Length > 14)
                return Json(new { available = false }, JsonRequestBehavior.AllowGet);

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
            if (string.IsNullOrWhiteSpace(currentPassword) ||
                string.IsNullOrWhiteSpace(newPassword) ||
                string.IsNullOrWhiteSpace(verifyPassword))
            {
                return Json(new { success = false, field = "", message = "All fields are required." });
            }

            if ((newPassword ?? "").Trim() != (verifyPassword ?? "").Trim())
            {
                return Json(new { success = false, field = "verifyPassword", message = "New password and confirmation do not match." });
            }

            if (string.Equals((currentPassword ?? "").Trim(), (newPassword ?? "").Trim(), StringComparison.Ordinal))
            {
                return Json(new { success = false, field = "newPassword", message = "New password must be different from your current password. Please enter a different new password." });
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

                // Reject if new password is the same as current (verify new against existing hash)
                if (PasswordHasher.Verify(newPassword, user.PasswordHash, user.PasswordSalt))
                {
                    return Json(new { success = false, field = "newPassword", message = "New password must be different from your current password. Please enter a different new password." });
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
