//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Mvp24Hours.Extensions
{
    public static partial class ValidatorExtensions
    {
        public static bool IsInt32(this string value)
        {
            if (!value.HasValue())
            {
                return false;
            }

            return int.TryParse(value, out _);
        }

        public static bool IsLong(this string value)
        {
            if (!value.HasValue())
            {
                return false;
            }

            return long.TryParse(value, out _);
        }

        public static bool IsBoolean(this string value)
        {
            if (!value.HasValue())
            {
                return false;
            }

            return bool.TryParse(value, out _);
        }

        public static bool IsDecimal(this string value)
        {
            if (!value.HasValue())
            {
                return false;
            }

            return decimal.TryParse(value, out _);
        }

        public static bool IsDateTime(this string value, CultureInfo cultureInfo = null)
        {
            if (!value.HasValue())
            {
                return false;
            }

            return DateTime.TryParse(value, cultureInfo ?? new CultureInfo("en-US"), out _);
        }

        public static bool IsValidWebUrl(this string target)
        {
            if (!target.HasValue())
            {
                return false;
            }

            var WebUrlExpression = IsValidWebUrlRegex();
            return target.HasValue() && WebUrlExpression.IsMatch(target);
        }

        public static bool HasValue(this string target)
        {
            return !(string.IsNullOrEmpty(target) || string.IsNullOrWhiteSpace(target));
        }

        public static bool IsValidRegex(this string value, string pattern, RegexOptions options = RegexOptions.IgnoreCase)
        {
            if (!value.HasValue())
            {
                return false;
            }

            return new Regex(pattern, options).IsMatch(value);
        }

        public static bool IsValidEmail(this string email)
        {
            if (!email.HasValue())
            {
                return false;
            }

            string pattern = @"^(?!\.)(""([^""\r\\]|\\[""\r\\])*""|" + @"([-a-z0-9!#$%&'*+/=?^_`{|}~]|(?<!\.)\.)*)(?<!\.)" + @"@[a-z0-9][\w\.-]*[a-z0-9]\.[a-z][a-z\.]*[a-z]$";
            var regex = new Regex(pattern, RegexOptions.IgnoreCase);
            return regex.IsMatch(email);
        }

        public static bool IsValidRange(this DateTime start, DateTime end)
        {
            if (!start.IsValidDate() || !end.IsValidDate())
            {
                return false;
            }

            if (start > end)
            {
                return false;
            }

            return true;
        }

        public static bool IsValidDate(this DateTime start)
        {
            if (start == DateTime.MinValue)
            {
                return false;
            }

            if (start == DateTime.MaxValue)
            {
                return false;
            }

            return true;
        }

        public static bool IsValidPis(this string pis)
        {
            if (!pis.HasValue())
            {
                return false;
            }

            int[] multiplicador = [3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
            int soma;
            int resto;
            if (pis.Trim().Length != 11)
            {
                return false;
            }

            pis = pis.Trim();
            pis = pis.Replace("-", "").Replace(".", "").PadLeft(11, '0');

            soma = 0;
            for (int i = 0; i < 10; i++)
            {
                soma += int.Parse(pis[i].ToString()) * multiplicador[i];
            }

            resto = soma % 11;
            if (resto < 2)
            {
                resto = 0;
            }
            else
            {
                resto = 11 - resto;
            }

            return pis.EndsWith(resto.ToString());
        }

        public static bool IsValidCpf(this string cpf)
        {
            if (!cpf.HasValue())
            {
                return false;
            }

            int[] multiplicador1 = [10, 9, 8, 7, 6, 5, 4, 3, 2];
            int[] multiplicador2 = [11, 10, 9, 8, 7, 6, 5, 4, 3, 2];
            string tempCpf;
            string digito;
            int soma;
            int resto;
            cpf = cpf.Trim();
            cpf = cpf.Replace(".", "").Replace("-", "");
            if (cpf.Length != 11)
            {
                return false;
            }

            tempCpf = cpf[..9];
            soma = 0;

            for (int i = 0; i < 9; i++)
            {
                soma += int.Parse(tempCpf[i].ToString()) * multiplicador1[i];
            }

            resto = soma % 11;
            if (resto < 2)
            {
                resto = 0;
            }
            else
            {
                resto = 11 - resto;
            }

            digito = resto.ToString();
            tempCpf += digito;
            soma = 0;
            for (int i = 0; i < 10; i++)
            {
                soma += int.Parse(tempCpf[i].ToString()) * multiplicador2[i];
            }

            resto = soma % 11;
            if (resto < 2)
            {
                resto = 0;
            }
            else
            {
                resto = 11 - resto;
            }

            digito += resto.ToString();
            return cpf.EndsWith(digito);
        }

        public static bool IsValidCnpj(this string cnpj)
        {
            if (!cnpj.HasValue())
            {
                return false;
            }

            int[] multiplicador1 = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
            int[] multiplicador2 = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
            int soma;
            int resto;
            string digito;
            string tempCnpj;
            cnpj = cnpj.Trim();
            cnpj = cnpj.Replace(".", "").Replace("-", "").Replace("/", "");
            if (cnpj.Length != 14)
            {
                return false;
            }

            tempCnpj = cnpj[..12];
            soma = 0;
            for (int i = 0; i < 12; i++)
            {
                soma += int.Parse(tempCnpj[i].ToString()) * multiplicador1[i];
            }

            resto = (soma % 11);
            if (resto < 2)
            {
                resto = 0;
            }
            else
            {
                resto = 11 - resto;
            }

            digito = resto.ToString();
            tempCnpj += digito;
            soma = 0;
            for (int i = 0; i < 13; i++)
            {
                soma += int.Parse(tempCnpj[i].ToString()) * multiplicador2[i];
            }

            resto = (soma % 11);
            if (resto < 2)
            {
                resto = 0;
            }
            else
            {
                resto = 11 - resto;
            }

            digito += resto.ToString();
            return cnpj.EndsWith(digito);
        }

        public static bool IsValidCreditCard(this string cardNo, string expiryDate, string cvv)
        {
            if (!cardNo.HasValue() || !expiryDate.HasValue() || !cvv.HasValue())
            {
                return false;
            }

            var cardCheck = CardRegex();
            var monthCheck = MonthCheckRegex();
            var yearCheck = YearCheckRegex();
            var cvvCheck = CvvCheckRegex();

            if (!cardCheck.IsMatch(cardNo)) // <1>check card number is valid
            {
                return false;
            }

            if (!cvvCheck.IsMatch(cvv)) // <2>check cvv is valid as "999"
            {
                return false;
            }

            var dateParts = expiryDate.Split('/'); //expiry date in from MM/yyyy            
            if (!monthCheck.IsMatch(dateParts[0]) || !yearCheck.IsMatch(dateParts[1])) // <3 - 6>
            {
                return false; // ^ check date format is valid as "MM/yyyy"
            }

            var year = int.Parse(dateParts[1]);
            var month = int.Parse(dateParts[0]);
            var lastDateOfExpiryMonth = DateTime.DaysInMonth(year, month); //get actual expiry date
            DateTime cardExpiry = new (year, month, lastDateOfExpiryMonth, 23, 59, 59, DateTimeKind.Utc);

            //check expiry greater than today & within next 6 years <7, 8>>
            return (cardExpiry > DateTime.Now && cardExpiry < DateTime.Now.AddYears(6));
        }

        public static bool IsValidCreditCard(this string cardNo)
        {
            if (!cardNo.HasValue())
            {
                return false;
            }

            //Build your Regular Expression
            Regex expression = IsValidCreditCardRegex();

            //Return if it was a match or not
            return expression.IsMatch(cardNo);
        }

        public static bool IsValidCNS(this string cns)
        {
            if (!cns.HasValue())
            {
                return false;
            }

            Regex definitive = IsValidCNSDefinitiveRegex();
            Regex provisional = IsValidCNSProvisionalRegex();

            if (definitive.IsMatch(cns) || provisional.IsMatch(cns))
            {
                return CNSWeightedSum(cns) % 11 == 0;
            }

            return false;
        }

        private static int CNSWeightedSum(string value)
        {
            int soma = 0;
            for (int i = 0; i < value.Length; i++)
            {
                soma += value[i] * (15 - i);
            }

            return soma;
        }

        public static bool IsValidRenavam(this string value)
        {
            if (!value.HasValue())
            {
                return false;
            }

            var renavam = value.OnlyNumbers();

            var sum = 0;
            var renavamArray = renavam.ToCharArray();
            var digitCount = 0;

            for (int i = 5; i >= 2; i--)
            {
                sum += renavamArray[digitCount] * i;
                digitCount++;
            }

            var valor = sum % 11;
            var digit = valor;

            if (valor == 11 || valor == 0 || valor >= 10)
            {
                digit = 0;
            }

            if (digit == renavamArray[4])
            {
                return true;
            }

            return false;
        }

        public static bool IsValidCEP(this string cep)
        {
            return cep.IsValidRegex(@"^\d{5}-\d{3}$");
        }

        public static bool IsValidTituloEleitor(this string sTitulo)
        {
            int d1, d2, d3, d4, d5, d6, d7, d8, d9, d10, d11, d12, DV1, DV2, UltDig;

            if ((sTitulo.Length < 12))
            {
                sTitulo = sTitulo.PadLeft(12, '0');
            }

            UltDig = sTitulo.Length;

            if (sTitulo == "000000000000")
            {
                return false;
            }

            d1 = int.Parse(sTitulo.Substring((UltDig - 10), 1));
            d2 = int.Parse(sTitulo.Substring((UltDig - 9), 1));
            d3 = int.Parse(sTitulo.Substring((UltDig - 8), 1));
            d4 = int.Parse(sTitulo.Substring((UltDig - 7), 1));
            d5 = int.Parse(sTitulo.Substring((UltDig - 6), 1));
            d6 = int.Parse(sTitulo.Substring((UltDig - 5), 1));
            d7 = int.Parse(sTitulo.Substring((UltDig - 4), 1));
            d8 = int.Parse(sTitulo.Substring((UltDig - 3), 1));
            d9 = int.Parse(sTitulo.Substring((UltDig - 2), 1));
            d10 = int.Parse(sTitulo.Substring((UltDig - 1), 1));
            d11 = int.Parse(sTitulo.Substring(UltDig, 1));
            d12 = int.Parse(sTitulo.Substring((UltDig - 1), 1));

            DV1 = (d1 * 2) + (d2 * 3) + (d3 * 4) + (d4 * 5) + (d5 * 6) + (d6 * 7) + (d7 * 8) + (d8 * 9);
            DV1 %= 11;

            if (DV1 == 10)
            {
                DV1 = 0;
            }

            DV2 = (d9 * 7) + (d10 * 8) + (DV1 * 9);
            DV2 %= 11;

            if (DV2 == 10)
            {
                DV2 = 0;
            }

            if ((d11 == DV1) && (d12 == DV2))
            {
                return ((d9 & d10) > 0) && ((d9 & d10) < 29);
            }

            return false;
        }

        public static bool IsValidConstraint(this string text, params string[] values)
        {
            if (string.IsNullOrEmpty(text) || values == null || values.Length == 0)
            {
                return false;
            }
            return values.Contains(text);
        }

        public static bool IsNumeric(this string s)
        {
            foreach (char c in s)
            {
                if (!char.IsDigit(c) && c != '.')
                {
                    return false;
                }
            }
            return true;
        }

        public static bool IsValidPhoneNumber(this string input)
        {
            var phoneNumber = input.NullSafe()
                .Replace(" ", "")
                .Replace("-", "")
                .Replace("(", "")
                .Replace(")", "");
            return IsValidPhoneNumberRegex().Match(phoneNumber).Success;
        }

        [GeneratedRegex(@"(http|https)://([\w-]+\.)+[\w-]+(/[\w- ./?%&=]*)?", RegexOptions.Compiled | RegexOptions.Singleline)]
        private static partial Regex IsValidWebUrlRegex();

        [GeneratedRegex(@"^([\+]{0,1}[0-9]{9,15})$")]
        private static partial Regex IsValidPhoneNumberRegex();

        [GeneratedRegex(@"^(1298|1267|4512|4567|8901|8933)([\-\s]?[0-9]{4}){3}$")]
        private static partial Regex CardRegex();

        [GeneratedRegex(@"^(0[1-9]|1[0-2])$")]
        private static partial Regex MonthCheckRegex();

        [GeneratedRegex(@"^(?:4[0-9]{12}(?:[0-9]{3})?|5[1-5][0-9]{14}|6(?:011|5[0-9][0-9])[0-9]{12}|3[47][0-9]{13}|3(?:0[0-5]|[68][0-9])[0-9]{11}|(?:2131|1800|35\d{3})\d{11})$")]
        private static partial Regex IsValidCreditCardRegex();

        [GeneratedRegex(@"/[1-2][0-9]{10}00[0-1][0-9]/")]
        private static partial Regex IsValidCNSDefinitiveRegex();

        [GeneratedRegex(@"/[7-9][0-9]{14}/")]
        private static partial Regex IsValidCNSProvisionalRegex();

        [GeneratedRegex(@"^20[0-9]{2}$")]
        private static partial Regex YearCheckRegex();

        [GeneratedRegex(@"^\d{3}$")]
        private static partial Regex CvvCheckRegex();
    }
}
