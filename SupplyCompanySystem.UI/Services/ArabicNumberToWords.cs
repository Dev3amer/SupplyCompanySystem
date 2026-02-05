using Humanizer;
using System.Globalization;

namespace SupplyCompanySystem.UI.Services
{
    public static class ArabicNumberToWords
    {
        public static string ConvertToArabicWords(decimal number)
        {
            try
            {
                // فصل العدد إلى جزء صحيح وجزء عشري
                long integerPart = (long)Math.Floor(number);
                decimal fractionalPart = number - integerPart;
                int fractionalValue = (int)Math.Round(fractionalPart * 100); // تحويل إلى قروش (جزء من 100)

                var culture = new CultureInfo("ar");

                // تحويل الجزء الصحيح
                string integerWords = integerPart.ToWords(culture);

                string result = "";

                // إضافة الجزء الصحيح مع "جنيهاً"
                if (integerPart > 0)
                {
                    result = integerWords + " جنيهاً";
                }
                else
                {
                    result = "صفر جنيهاً";
                }

                // إضافة الجزء العشري (القرش) إن وجد
                if (fractionalValue > 0)
                {
                    string fractionalWords = fractionalValue.ToWords(culture);

                    // تنسيق الكسور بشكل صحيح
                    if (fractionalValue == 1)
                        result += " وقرش واحد";
                    else if (fractionalValue == 2)
                        result += " وقرشان";
                    else if (fractionalValue >= 3 && fractionalValue <= 10)
                        result += " و" + fractionalWords + " قروش";
                    else
                        result += " و" + fractionalWords + " قرشاً";
                }

                // إضافة "فقط" في النهاية
                result += " فقط";

                // تحسين النص العربي
                result = ImproveArabicText(result);

                return result;
            }
            catch (Exception ex)
            {
                // في حالة حدوث خطأ، نستخدم الدالة البديلة
                return ConvertToArabicWordsFallback(number);
            }
        }

        private static string ImproveArabicText(string text)
        {
            // تحسينات للغة العربية
            text = text.Replace("  ", " ");
            text = text.Replace("و صفر", "وصفر");
            text = text.Replace("و واحد", "وواحد");
            text = text.Replace("و اثنان", "واثنان");

            // إصلاح مشكلة "عشرون" و "ثلاثون" إلخ
            text = text.Replace("عشرون جنيهاً", "عشرون جنيهاً");
            text = text.Replace("ثلاثون جنيهاً", "ثلاثون جنيهاً");
            text = text.Replace("أربعون جنيهاً", "أربعون جنيهاً");
            text = text.Replace("خمسون جنيهاً", "خمسون جنيهاً");
            text = text.Replace("ستون جنيهاً", "ستون جنيهاً");
            text = text.Replace("سبعون جنيهاً", "سبعون جنيهاً");
            text = text.Replace("ثمانون جنيهاً", "ثمانون جنيهاً");
            text = text.Replace("تسعون جنيهاً", "تسعون جنيهاً");

            return text.Trim();
        }

        // دالة بديلة محسنة
        private static string ConvertToArabicWordsFallback(decimal number)
        {
            try
            {
                // محاولة باستخدام المكتبة بطريقة مختلفة
                return ConvertUsingHumanizerFixed(number);
            }
            catch
            {
                // استخدام دالة يدوية محسنة
                return ConvertToArabicWordsManual(number);
            }
        }

        private static string ConvertUsingHumanizerFixed(decimal number)
        {
            // تحويل decimal إلى double أولاً (Humanizer يدعم double)
            double numberAsDouble = (double)number;

            // فصل العدد إلى جزء صحيح وجزء عشري
            long integerPart = (long)Math.Floor(numberAsDouble);
            double fractionalPart = numberAsDouble - integerPart;
            int fractionalValue = (int)Math.Round(fractionalPart * 100);

            var culture = new CultureInfo("ar");

            string result = "";

            if (integerPart == 0 && fractionalValue == 0)
                return "صفر فقط";

            if (integerPart > 0)
            {
                string integerWords = integerPart.ToWords(culture);
                result = integerWords + " جنيهاً";
            }
            else
            {
                result = "صفر جنيهاً";
            }

            if (fractionalValue > 0)
            {
                string fractionalWords = fractionalValue.ToWords(culture);

                if (fractionalValue == 1)
                    result += " وقرش واحد";
                else if (fractionalValue == 2)
                    result += " وقرشان";
                else if (fractionalValue >= 3 && fractionalValue <= 10)
                    result += " و" + fractionalWords + " قروش";
                else
                    result += " و" + fractionalWords + " قرشاً";
            }

            result += " فقط";
            return ImproveArabicText(result);
        }

        // دالة يدوية محسنة للتفقيط
        private static string ConvertToArabicWordsManual(decimal number)
        {
            if (number == 0)
                return "صفر فقط";

            string[] units = { "", "واحد", "اثنان", "ثلاثة", "أربعة", "خمسة", "ستة", "سبعة", "ثمانية", "تسعة" };
            string[] teens = { "عشرة", "أحد عشر", "اثنا عشر", "ثلاثة عشر", "أربعة عشر", "خمسة عشر", "ستة عشر", "سبعة عشر", "ثمانية عشر", "تسعة عشر" };
            string[] tens = { "", "", "عشرون", "ثلاثون", "أربعون", "خمسون", "ستون", "سبعون", "ثمانون", "تسعون" };

            long integerPart = (long)Math.Floor(Math.Abs(number));
            decimal fractionalPart = Math.Abs(number) - integerPart;
            int fractionalValue = (int)Math.Round(fractionalPart * 100);

            string ConvertUnder1000(long n)
            {
                if (n == 0) return "";

                string result = "";

                // المئات
                if (n >= 100)
                {
                    long hundreds = n / 100;
                    if (hundreds == 1)
                        result = "مائة";
                    else if (hundreds == 2)
                        result = "مئتان";
                    else
                        result = units[hundreds] + "مائة";

                    n %= 100;
                    if (n > 0) result += " و";
                }

                // العشرات والوحدات
                if (n > 0)
                {
                    if (n < 10)
                    {
                        result += units[n];
                    }
                    else if (n < 20)
                    {
                        result += teens[n - 10];
                    }
                    else
                    {
                        long tensDigit = n / 10;
                        long unitsDigit = n % 10;

                        if (unitsDigit > 0)
                            result += units[unitsDigit] + " و";

                        result += tens[tensDigit];
                    }
                }

                return result;
            }

            string integerWords = "";

            // مليارات
            if (integerPart >= 1000000000)
            {
                long billions = integerPart / 1000000000;
                if (billions == 1)
                    integerWords += "مليار";
                else if (billions == 2)
                    integerWords += "ملياران";
                else
                    integerWords += ConvertUnder1000(billions) + " مليار";

                integerPart %= 1000000000;
                if (integerPart > 0) integerWords += " و";
            }

            // ملايين
            if (integerPart >= 1000000)
            {
                long millions = integerPart / 1000000;
                if (millions == 1)
                    integerWords += "مليون";
                else if (millions == 2)
                    integerWords += "مليونان";
                else
                    integerWords += ConvertUnder1000(millions) + " مليون";

                integerPart %= 1000000;
                if (integerPart > 0) integerWords += " و";
            }

            // آلاف
            if (integerPart >= 1000)
            {
                long thousands = integerPart / 1000;
                if (thousands == 1)
                    integerWords += "ألف";
                else if (thousands == 2)
                    integerWords += "ألفان";
                else
                    integerWords += ConvertUnder1000(thousands) + " ألف";

                integerPart %= 1000;
                if (integerPart > 0) integerWords += " و";
            }

            // الباقي
            if (integerPart > 0 || string.IsNullOrEmpty(integerWords))
            {
                integerWords += ConvertUnder1000(integerPart);
            }

            if (string.IsNullOrEmpty(integerWords.Trim()))
                integerWords = "صفر";

            string result = integerWords.Trim() + " جنيهاً";

            // إضافة القروش
            if (fractionalValue > 0)
            {
                string fractionalWords = ConvertUnder1000(fractionalValue);

                if (fractionalValue == 1)
                    result += " وقرش واحد";
                else if (fractionalValue == 2)
                    result += " وقرشان";
                else if (fractionalValue >= 3 && fractionalValue <= 10)
                    result += " و" + fractionalWords + " قروش";
                else
                    result += " و" + fractionalWords + " قرشاً";
            }

            // الإشارة السالبة
            if (number < 0)
                result = "سالب " + result;

            result += " فقط";

            return result;
        }
    }
}