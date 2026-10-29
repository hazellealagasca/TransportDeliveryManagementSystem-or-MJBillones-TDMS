using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Web;

namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions
{
    public static class SecurityHelper
    {
        private static readonly Regex PlateNumberRegex = new Regex(@"^([A-Za-z]{3}[-\s]?[0-9]{3,4}|[A-Za-z]{2}[-\s]?[0-9]{3,4}|[0-9]{3,4}[-\s]?[A-Za-z]{2,3}|[0-9]{4}[-\s]?[0-9]{5,8})$", RegexOptions.Compiled);
        private static readonly Regex LicenseNumberRegex = new Regex(@"^[A-Za-z]\d{2}[-\s]?\d{2}[-\s]?\d{5,6}$|^[A-Za-z]\d{2}[-\s]?\d{6}$", RegexOptions.Compiled);
        private static readonly Regex PhoneRegex = new Regex(@"^(09|\+639)\d{9}$", RegexOptions.Compiled);
        private static readonly Regex NameRegex = new Regex(@"^[a-zA-ZñÑ\s\.\-']{2,60}$", RegexOptions.Compiled);

        public static string SanitizeInput(string? input, int maxLength = 255)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            string sanitized = input.Trim();
            if (sanitized.Length > maxLength) sanitized = sanitized.Substring(0, maxLength);
            return HttpUtility.HtmlEncode(sanitized);
        }

        public static string SanitizeHtmlForPrint(string? rawInput)
        {
            if (string.IsNullOrWhiteSpace(rawInput)) return string.Empty;
            return HttpUtility.HtmlEncode(rawInput.Trim()).Replace("'", "&#39;").Replace("\"", "&quot;");
        }

        public static bool IsValidPlateNumber(string? plateNumber)
        {
            if (string.IsNullOrWhiteSpace(plateNumber)) return false;
            string clean = plateNumber.Trim().ToUpper();
            return PlateNumberRegex.IsMatch(clean);
        }

        public static bool IsValidLicenseNumber(string? licenseNo, bool isDriver = true)
        {
            if (string.IsNullOrWhiteSpace(licenseNo))
            {
                return !isDriver;
            }

            string clean = licenseNo.Trim().ToUpper();
            if (clean.Equals("N/A", StringComparison.OrdinalIgnoreCase) ||
                clean.Equals("NONE", StringComparison.OrdinalIgnoreCase) ||
                clean.Equals("0000", StringComparison.OrdinalIgnoreCase))
            {
                return !isDriver;
            }

            return LicenseNumberRegex.IsMatch(clean);
        }

        public static bool IsValidPhone(string? phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return false;
            string clean = phone.Trim().Replace(" ", "").Replace("-", "");
            return PhoneRegex.IsMatch(clean);
        }

        public static bool IsValidName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            return NameRegex.IsMatch(name.Trim());
        }

        public static bool IsValidFilePath(string? filePath, string allowedExtension = ".db")
        {
            if (string.IsNullOrWhiteSpace(filePath)) return false;
            try
            {
                string fullPath = Path.GetFullPath(filePath);
                string extension = Path.GetExtension(fullPath).ToLowerInvariant();
                if (extension != allowedExtension.ToLowerInvariant()) return false;
                if (fullPath.IndexOfAny(Path.GetInvalidPathChars()) >= 0) return false;
                if (fullPath.Contains("..") || fullPath.Contains("%00")) return false;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool PreventCommandInjection(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return true;
            char[] forbiddenChars = new char[] { '&', '|', ';', '`', '$', '>', '<', '\r', '\n' };
            return input.IndexOfAny(forbiddenChars) < 0;
        }

        public static bool IsValidImageFile(string? filePath, out string errorMessage)
        {
            errorMessage = string.Empty;
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                errorMessage = "Selected image file does not exist.";
                return false;
            }

            try
            {
                FileInfo fi = new FileInfo(filePath);
                if (fi.Length > 100L * 1024L * 1024L)
                {
                    errorMessage = "Image file size exceeds the maximum allowed limit of 100MB.";
                    return false;
                }

                string ext = fi.Extension.ToLowerInvariant();
                if (ext != ".jpg" && ext != ".jpeg" && ext != ".png" && ext != ".bmp" && ext != ".webp")
                {
                    errorMessage = "Invalid image file format. Only JPG, PNG, BMP, and WEBP are allowed.";
                    return false;
                }

                byte[] header = new byte[8];
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    if (fs.Read(header, 0, header.Length) < 4)
                    {
                        errorMessage = "Corrupted or unreadable image header.";
                        return false;
                    }
                }

                bool isJpeg = header[0] == 0xFF && header[1] == 0xD8;
                bool isPng = header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47;
                bool isBmp = header[0] == 0x42 && header[1] == 0x4D;
                bool isWebp = header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46;

                if (!isJpeg && !isPng && !isBmp && !isWebp)
                {
                    errorMessage = "Security Warning: The file content does not match a genuine image format.";
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                errorMessage = $"Error validating image: {ex.Message}";
                return false;
            }
        }

        public static string SaveReferencePhoto(string sourceFilePath, string subFolder = "Incidents")
        {
            if (string.IsNullOrWhiteSpace(sourceFilePath) || !File.Exists(sourceFilePath)) return string.Empty;

            string targetFolder = Path.Combine(AppPaths.DataRoot, "Attachments", "Photos", subFolder);
            if (!Directory.Exists(targetFolder))
            {
                Directory.CreateDirectory(targetFolder);
            }

            string ext = Path.GetExtension(sourceFilePath).ToLowerInvariant();
            string fileName = $"{subFolder.ToLowerInvariant()}_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}{ext}";
            string destPath = Path.Combine(targetFolder, fileName);

            File.Copy(sourceFilePath, destPath, true);

            return Path.Combine("Attachments", "Photos", subFolder, fileName);
        }

        public static string GetAbsolutePhotoPath(string? relativeOrAbsPath)
        {
            if (string.IsNullOrWhiteSpace(relativeOrAbsPath)) return string.Empty;
            if (Path.IsPathRooted(relativeOrAbsPath) && File.Exists(relativeOrAbsPath))
            {
                return relativeOrAbsPath;
            }

            string abs = AppPaths.Resolve(relativeOrAbsPath);
            if (Path.IsPathRooted(abs) && File.Exists(abs)) return abs;
            abs = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativeOrAbsPath);
            if (File.Exists(abs)) return abs;

            string projAbs = Path.Combine(Directory.GetCurrentDirectory(), relativeOrAbsPath);
            if (File.Exists(projAbs)) return projAbs;

            return string.Empty;
        }

        public static System.Drawing.Image? LoadDecoupledBitmap(string? relativeOrAbsPath)
        {
            string abs = GetAbsolutePhotoPath(relativeOrAbsPath);
            if (string.IsNullOrEmpty(abs) || !File.Exists(abs)) return null;

            try
            {
                using (var fs = new FileStream(abs, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var temp = System.Drawing.Image.FromStream(fs))
                {
                    return new System.Drawing.Bitmap(temp);
                }
            }
            catch
            {
                return null;
            }
        }

        public static void SafeSetPictureBoxImage(System.Windows.Forms.PictureBox pb, string? photoPath)
        {
            if (pb == null) return;
            var oldImg = pb.Image;
            pb.Image = null;
            oldImg?.Dispose();

            pb.Image = LoadDecoupledBitmap(photoPath);
        }
    }
}
