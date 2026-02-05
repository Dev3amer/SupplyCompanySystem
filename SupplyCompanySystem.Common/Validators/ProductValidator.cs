namespace SupplyCompanySystem.Common.Validators
{
    public static class ProductValidator
    {
        // ✅ دوال التحقق من التكرار (دون اعتماد على Entity)
        public static ValidationResult ValidateNameUniqueness(string name, IEnumerable<object> allProducts, Guid? excludeId = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                return new ValidationResult(true);

            try
            {
                var productList = allProducts as dynamic;
                var exists = false;

                // البحث في القائمة للتحقق من التكرار
                foreach (var product in productList)
                {
                    var productName = (string)product.GetType().GetProperty("Name")?.GetValue(product);
                    var productId = (Guid)product.GetType().GetProperty("Id")?.GetValue(product);
                    var isActive = (bool)product.GetType().GetProperty("IsActive")?.GetValue(product);

                    if (productName != null &&
                        productName.Equals(name.Trim(), StringComparison.CurrentCultureIgnoreCase) &&
                        isActive)
                    {
                        if (!excludeId.HasValue || productId != excludeId.Value)
                        {
                            exists = true;
                            break;
                        }
                    }
                }

                return exists
                    ? new ValidationResult(false, $"اسم المنتج '{name}' موجود بالفعل في النظام")
                    : new ValidationResult(true);
            }
            catch
            {
                return new ValidationResult(true); // في حالة الخطأ، لا نمنع العملية
            }
        }

        public static ValidationResult ValidateSkuUniqueness(string sku, IEnumerable<object> allProducts, Guid? excludeId = null)
        {
            if (string.IsNullOrWhiteSpace(sku))
                return new ValidationResult(true);

            try
            {
                var productList = allProducts as dynamic;
                var exists = false;

                foreach (var product in productList)
                {
                    var productSku = (string)product.GetType().GetProperty("SKU")?.GetValue(product);
                    var productId = (Guid)product.GetType().GetProperty("Id")?.GetValue(product);
                    var isActive = (bool)product.GetType().GetProperty("IsActive")?.GetValue(product);

                    if (productSku != null &&
                        productSku.Equals(sku.Trim(), StringComparison.CurrentCultureIgnoreCase) &&
                        isActive)
                    {
                        if (!excludeId.HasValue || productId != excludeId.Value)
                        {
                            exists = true;
                            break;
                        }
                    }
                }

                return exists
                    ? new ValidationResult(false, $"الكود '{sku}' موجود بالفعل في النظام")
                    : new ValidationResult(true);
            }
            catch
            {
                return new ValidationResult(true);
            }
        }

        public static ValidationResult ValidateAllWithUniqueness(
            string name,
            string sku,
            string price,
            string unit,
            string category,
            string description,
            IEnumerable<object> allProducts,
            Guid? excludeId = null)
        {
            // التحقق الأساسي
            var basicValidation = ValidateAll(name, sku, price, unit, category, description);
            if (!basicValidation.IsValid)
                return basicValidation;

            // التحقق من تكرار الاسم
            var nameUniqueness = ValidateNameUniqueness(name, allProducts, excludeId);
            if (!nameUniqueness.IsValid)
                return nameUniqueness;

            // التحقق من تكرار الكود
            var skuUniqueness = ValidateSkuUniqueness(sku, allProducts, excludeId);
            if (!skuUniqueness.IsValid)
                return skuUniqueness;

            return new ValidationResult(true);
        }

        // ✅ دوال التحقق الأساسية الأصلية (محفوظة)
        public static ValidationResult ValidateName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return new ValidationResult(false, "اسم المنتج مطلوب");

            if (name.Length < 3)
                return new ValidationResult(false, "اسم المنتج يجب أن يكون 3 أحرف على الأقل");

            if (name.Length > 100)
                return new ValidationResult(false, "اسم المنتج يجب أن يكون 100 حرف على الأكثر");

            return new ValidationResult(true);
        }

        public static ValidationResult ValidateSKU(string sku)
        {
            if (string.IsNullOrWhiteSpace(sku))
                return new ValidationResult(false, "الكود (SKU) مطلوب");

            if (sku.Length < 2)
                return new ValidationResult(false, "الكود يجب أن يكون حرفين على الأقل");

            if (sku.Length > 50)
                return new ValidationResult(false, "الكود يجب أن يكون 50 حرف على الأكثر");

            return new ValidationResult(true);
        }

        public static ValidationResult ValidatePrice(string priceText)
        {
            if (string.IsNullOrWhiteSpace(priceText))
                return new ValidationResult(false, "السعر مطلوب");

            if (!decimal.TryParse(priceText, out decimal price))
                return new ValidationResult(false, "السعر يجب أن يكون رقم صحيح");

            if (price <= 0)
                return new ValidationResult(false, "السعر يجب أن يكون أكبر من صفر");

            if (price > 999999)
                return new ValidationResult(false, "السعر مرتفع جداً");

            return new ValidationResult(true);
        }

        public static ValidationResult ValidateCategory(string category)
        {
            if (string.IsNullOrWhiteSpace(category))
                return new ValidationResult(false, "التصنيف مطلوب");

            if (category.Length > 50)
                return new ValidationResult(false, "التصنيف يجب أن يكون 50 حرف على الأكثر");

            return new ValidationResult(true);
        }

        public static ValidationResult ValidateUnit(string unit)
        {
            if (string.IsNullOrWhiteSpace(unit))
                return new ValidationResult(false, "الوحدة مطلوبة");

            if (unit.Length > 30)
                return new ValidationResult(false, "الوحدة يجب أن تكون 30 حرف على الأكثر");

            return new ValidationResult(true);
        }

        public static ValidationResult ValidateDescription(string description)
        {
            if (description != null && description.Length > 500)
                return new ValidationResult(false, "الوصف يجب أن يكون 500 حرف على الأكثر");

            return new ValidationResult(true);
        }

        public static ValidationResult ValidateAll(string name, string sku, string price, string unit, string category, string description)
        {
            var nameValidation = ValidateName(name);
            if (!nameValidation.IsValid)
                return nameValidation;

            var skuValidation = ValidateSKU(sku);
            if (!skuValidation.IsValid)
                return skuValidation;

            var priceValidation = ValidatePrice(price);
            if (!priceValidation.IsValid)
                return priceValidation;

            var unitValidation = ValidateUnit(unit);
            if (!unitValidation.IsValid)
                return unitValidation;

            var categoryValidation = ValidateCategory(category);
            if (!categoryValidation.IsValid)
                return categoryValidation;

            var descriptionValidation = ValidateDescription(description);
            if (!descriptionValidation.IsValid)
                return descriptionValidation;

            return new ValidationResult(true);
        }
    }
}