//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Collections.Generic;
using System.Linq;

namespace Mvp24Hours.Core.Domain.Enumerations.Examples
{
    /// <summary>
    /// Example Smart Enum for payment methods with associated fees and descriptions.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This example demonstrates how Smart Enums can carry associated data
    /// like processing fees, display names, and descriptions.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var method = PaymentMethod.CreditCard;
    /// var fee = order.Total * method.FeePercentage;
    /// Console.WriteLine($"Payment via {method.DisplayName}: Fee = {fee:C}");
    /// </code>
    /// </example>
    public sealed class PaymentMethod : Enumeration<PaymentMethod>
    {
        /// <summary>
        /// Payment via credit card.
        /// </summary>
        public static readonly PaymentMethod CreditCard = new(
            1,
            nameof(CreditCard),
            "Credit Card",
            "Pay with Visa, Mastercard, or American Express",
            feePercentage: 0.025m,
            supportsInstallments: true);

        /// <summary>
        /// Payment via debit card.
        /// </summary>
        public static readonly PaymentMethod DebitCard = new(
            2,
            nameof(DebitCard),
            "Debit Card",
            "Pay directly from your bank account",
            feePercentage: 0.015m,
            supportsInstallments: false);

        /// <summary>
        /// Payment via PIX (Brazilian instant payment system).
        /// </summary>
        public static readonly PaymentMethod Pix = new(
            3,
            nameof(Pix),
            "PIX",
            "Instant payment with no fees",
            feePercentage: 0m,
            supportsInstallments: false);

        /// <summary>
        /// Payment via bank transfer (boleto).
        /// </summary>
        public static readonly PaymentMethod BankSlip = new(
            4,
            nameof(BankSlip),
            "Bank Slip (Boleto)",
            "Generate a bank slip for payment within 3 business days",
            feePercentage: 0.01m,
            supportsInstallments: false);

        /// <summary>
        /// Payment via PayPal.
        /// </summary>
        public static readonly PaymentMethod PayPal = new(
            5,
            nameof(PayPal),
            "PayPal",
            "Pay with your PayPal account",
            feePercentage: 0.035m,
            supportsInstallments: false);

        /// <summary>
        /// Payment in cash on delivery.
        /// </summary>
        public static readonly PaymentMethod Cash = new(
            6,
            nameof(Cash),
            "Cash on Delivery",
            "Pay in cash when you receive your order",
            feePercentage: 0m,
            supportsInstallments: false);

        /// <summary>
        /// Gets the user-friendly display name.
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// Gets the description of this payment method.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Gets the processing fee percentage (e.g., 0.025 = 2.5%).
        /// </summary>
        public decimal FeePercentage { get; }

        /// <summary>
        /// Gets a value indicating whether this payment method supports installment payments.
        /// </summary>
        public bool SupportsInstallments { get; }

        private PaymentMethod(
            int value,
            string name,
            string displayName,
            string description,
            decimal feePercentage,
            bool supportsInstallments)
            : base(value, name)
        {
            DisplayName = displayName;
            Description = description;
            FeePercentage = feePercentage;
            SupportsInstallments = supportsInstallments;
        }

        /// <summary>
        /// Calculates the processing fee for a given amount.
        /// </summary>
        /// <param name="amount">The payment amount.</param>
        /// <returns>The fee to be charged.</returns>
        public decimal CalculateFee(decimal amount) => amount * FeePercentage;

        /// <summary>
        /// Gets all payment methods that support installments.
        /// </summary>
        public static IEnumerable<PaymentMethod> InstallmentMethods =>
            GetAll().Where(m => m.SupportsInstallments);

        /// <summary>
        /// Gets all payment methods with no fees.
        /// </summary>
        public static IEnumerable<PaymentMethod> NoFeeMethods =>
            GetAll().Where(m => m.FeePercentage == 0);
    }
}

