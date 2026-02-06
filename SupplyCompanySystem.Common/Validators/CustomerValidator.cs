using SupplyCompanySystem.Domain.Entities;

namespace SupplyCompanySystem.Common.Validators
{
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; }

        public ValidationResult(bool isValid, string errorMessage = "")
        {
            IsValid = isValid;
            ErrorMessage = errorMessage;
        }
    }

    public class CustomerValidator
    {

        public static ValidationResult ValidateAll(string name, string phoneNumber, string address)
        {
            if (string.IsNullOrWhiteSpace(name))
                return new ValidationResult(false, "اسم العميل مطلوب");

            if (string.IsNullOrWhiteSpace(phoneNumber))
                return new ValidationResult(false, "رقم الهاتف مطلوب");

            if (string.IsNullOrWhiteSpace(address))
                return new ValidationResult(false, "العنوان مطلوب");

            var phoneValidation = ValidatePhoneNumber(phoneNumber);
            if (!phoneValidation.IsValid)
                return phoneValidation;

            if (name.Length < 3)
                return new ValidationResult(false, "اسم العميل يجب أن يكون 3 أحرف على الأقل");

            if (name.Length > 100)
                return new ValidationResult(false, "اسم العميل لا يجب أن يتجاوز 100 حرف");

            return new ValidationResult(true);
        }

        public static ValidationResult ValidatePhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return new ValidationResult(false, "رقم الهاتف مطلوب");

            string cleanNumber = System.Text.RegularExpressions.Regex.Replace(phoneNumber, @"[\s\-\(\)]", "");

            if (!System.Text.RegularExpressions.Regex.IsMatch(cleanNumber, @"^\d+$"))
                return new ValidationResult(false, "رقم الهاتف يجب أن يحتوي على أرقام فقط");

            if (cleanNumber.Length < 10 || cleanNumber.Length > 15)
                return new ValidationResult(false, "رقم الهاتف يجب أن يكون بين 10 و 15 أرقام");

            return new ValidationResult(true);
        }

        public static ValidationResult ValidateNameUniqueness(string name, List<Customer> existingCustomers, int? currentCustomerId = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                return new ValidationResult(false, "اسم العميل مطلوب");

            var isDuplicate = existingCustomers.Any(c =>
                c.Name.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase) &&
                (currentCustomerId == null || c.Id != currentCustomerId.Value)
            );

            if (isDuplicate)
                return new ValidationResult(false, $"العميل باسم '{name}' موجود بالفعل");

            return new ValidationResult(true);
        }

        public static ValidationResult ValidatePhoneUniqueness(string phoneNumber, List<Customer> existingCustomers, int? currentCustomerId = null)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return new ValidationResult(false, "رقم الهاتف مطلوب");

            string cleanNumber = System.Text.RegularExpressions.Regex.Replace(phoneNumber, @"[\s\-\(\)]", "");

            var isDuplicate = existingCustomers.Any(c =>
            {
                string existingClean = System.Text.RegularExpressions.Regex.Replace(c.PhoneNumber, @"[\s\-\(\)]", "");
                return existingClean.Equals(cleanNumber, StringComparison.OrdinalIgnoreCase) &&
                       (currentCustomerId == null || c.Id != currentCustomerId.Value);
            });

            if (isDuplicate)
                return new ValidationResult(false, $"رقم الهاتف '{phoneNumber}' مسجل بالفعل");

            return new ValidationResult(true);
        }
    }
}