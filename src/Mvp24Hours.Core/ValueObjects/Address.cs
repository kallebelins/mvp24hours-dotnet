//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Helpers;
using System;
using System.Collections.Generic;
using System.Text;

namespace Mvp24Hours.Core.ValueObjects
{
    /// <summary>
    /// Value Object representing a postal address.
    /// </summary>
    /// <example>
    /// <code>
    /// var address = Address.Create(
    ///     street: "Av. Paulista",
    ///     number: "1000",
    ///     city: "São Paulo",
    ///     state: "SP",
    ///     postalCode: "01310-100",
    ///     country: "Brasil"
    /// );
    /// 
    /// Console.WriteLine(address.FullAddress);
    /// // Av. Paulista, 1000 - São Paulo, SP - 01310-100 - Brasil
    /// </code>
    /// </example>
    public sealed class Address : BaseVO, IEquatable<Address>
    {
        /// <summary>
        /// Gets the street name.
        /// </summary>
        public string Street { get; }

        /// <summary>
        /// Gets the street number.
        /// </summary>
        public string Number { get; }

        /// <summary>
        /// Gets the complement (apartment, suite, etc.).
        /// </summary>
        public string Complement { get; }

        /// <summary>
        /// Gets the neighborhood or district.
        /// </summary>
        public string Neighborhood { get; }

        /// <summary>
        /// Gets the city name.
        /// </summary>
        public string City { get; }

        /// <summary>
        /// Gets the state or province.
        /// </summary>
        public string State { get; }

        /// <summary>
        /// Gets the postal/ZIP code.
        /// </summary>
        public string PostalCode { get; }

        /// <summary>
        /// Gets the country name.
        /// </summary>
        public string Country { get; }

        /// <summary>
        /// Gets the full formatted address.
        /// </summary>
        public string FullAddress => BuildFullAddress();

        private Address(
            string street,
            string number,
            string complement,
            string neighborhood,
            string city,
            string state,
            string postalCode,
            string country)
        {
            Street = street?.Trim() ?? string.Empty;
            Number = number?.Trim() ?? string.Empty;
            Complement = complement?.Trim() ?? string.Empty;
            Neighborhood = neighborhood?.Trim() ?? string.Empty;
            City = city?.Trim() ?? string.Empty;
            State = state?.Trim() ?? string.Empty;
            PostalCode = postalCode?.Trim() ?? string.Empty;
            Country = country?.Trim() ?? string.Empty;
        }

        /// <summary>
        /// Creates a new Address instance.
        /// </summary>
        /// <param name="street">The street name.</param>
        /// <param name="number">The street number.</param>
        /// <param name="city">The city name.</param>
        /// <param name="state">The state or province.</param>
        /// <param name="postalCode">The postal/ZIP code.</param>
        /// <param name="country">The country name.</param>
        /// <param name="complement">Optional complement (apartment, suite, etc.).</param>
        /// <param name="neighborhood">Optional neighborhood or district.</param>
        /// <returns>A new Address instance.</returns>
        /// <exception cref="ArgumentException">Thrown when required parameters are invalid.</exception>
        public static Address Create(
            string street,
            string? number,
            string city,
            string? state,
            string? postalCode,
            string country,
            string? complement = null,
            string? neighborhood = null)
        {
            Guard.Against.NullOrWhiteSpace(street, nameof(street));
            Guard.Against.NullOrWhiteSpace(city, nameof(city));
            Guard.Against.NullOrWhiteSpace(country, nameof(country));

            return new Address(
                street,
                number,
                complement,
                neighborhood,
                city,
                state,
                postalCode,
                country);
        }

        /// <summary>
        /// Creates a minimal address with only street and city.
        /// </summary>
        public static Address CreateMinimal(string street, string city, string country)
        {
            return Create(street, string.Empty, city, string.Empty, string.Empty, country);
        }

        /// <summary>
        /// Returns a new Address with the specified changes.
        /// </summary>
        public Address With(
            string? street = null,
            string? number = null,
            string? complement = null,
            string? neighborhood = null,
            string? city = null,
            string? state = null,
            string? postalCode = null,
            string? country = null)
        {
            return new Address(
                street ?? Street,
                number ?? Number,
                complement ?? Complement,
                neighborhood ?? Neighborhood,
                city ?? City,
                state ?? State,
                postalCode ?? PostalCode,
                country ?? Country);
        }

        private string BuildFullAddress()
        {
            var sb = new StringBuilder();

            if (!string.IsNullOrEmpty(Street))
            {
                sb.Append(Street);

                if (!string.IsNullOrEmpty(Number))
                {
                    sb.Append(", ").Append(Number);
                }

                if (!string.IsNullOrEmpty(Complement))
                {
                    sb.Append(" - ").Append(Complement);
                }
            }

            if (!string.IsNullOrEmpty(Neighborhood))
            {
                if (sb.Length > 0) sb.Append(" - ");
                sb.Append(Neighborhood);
            }

            if (!string.IsNullOrEmpty(City))
            {
                if (sb.Length > 0) sb.Append(" - ");
                sb.Append(City);

                if (!string.IsNullOrEmpty(State))
                {
                    sb.Append(", ").Append(State);
                }
            }

            if (!string.IsNullOrEmpty(PostalCode))
            {
                if (sb.Length > 0) sb.Append(" - ");
                sb.Append(PostalCode);
            }

            if (!string.IsNullOrEmpty(Country))
            {
                if (sb.Length > 0) sb.Append(" - ");
                sb.Append(Country);
            }

            return sb.ToString();
        }

        /// <inheritdoc />
        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Street;
            yield return Number;
            yield return Complement;
            yield return Neighborhood;
            yield return City;
            yield return State;
            yield return PostalCode;
            yield return Country;
        }

        /// <inheritdoc />
        public bool Equals(Address? other)
        {
            if (other is null) return false;
            return Street == other.Street
                && Number == other.Number
                && Complement == other.Complement
                && Neighborhood == other.Neighborhood
                && City == other.City
                && State == other.State
                && PostalCode == other.PostalCode
                && Country == other.Country;
        }

        /// <inheritdoc />
        public override string ToString() => FullAddress;
    }
}

