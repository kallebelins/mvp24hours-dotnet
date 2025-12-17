//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Mvp24Hours.WebAPI.Conventions
{
    /// <summary>
    /// API conventions helper methods for common response types and status codes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These conventions help standardize API responses across controllers.
    /// Use with [ApiConventionType(typeof(ApiConventions))] attribute.
    /// </para>
    /// </remarks>
    public static class ApiConventions
    {
        /// <summary>
        /// Convention for GET operations that return a single item.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Expected responses:
        /// - 200: Success with item
        /// - 404: Not found
        /// - 400: Bad request
        /// </para>
        /// </remarks>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Prefix)]
        public static void Get(
            [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Any)]
            [ApiConventionTypeMatch(ApiConventionTypeMatchBehavior.Any)]
            object id)
        {
            // Convention method - not implemented
        }

        /// <summary>
        /// Convention for GET operations that return a list of items.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Expected responses:
        /// - 200: Success with list
        /// - 400: Bad request
        /// </para>
        /// </remarks>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Prefix)]
        public static void List()
        {
            // Convention method - not implemented
        }

        /// <summary>
        /// Convention for POST operations that create a new item.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Expected responses:
        /// - 201: Created
        /// - 400: Bad request
        /// - 409: Conflict
        /// </para>
        /// </remarks>
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Prefix)]
        public static void Post(
            [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Any)]
            [ApiConventionTypeMatch(ApiConventionTypeMatchBehavior.Any)]
            object model)
        {
            // Convention method - not implemented
        }

        /// <summary>
        /// Convention for PUT operations that update an existing item.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Expected responses:
        /// - 200: Success
        /// - 204: No content
        /// - 400: Bad request
        /// - 404: Not found
        /// - 409: Conflict
        /// </para>
        /// </remarks>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Prefix)]
        public static void Put(
            [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Any)]
            [ApiConventionTypeMatch(ApiConventionTypeMatchBehavior.Any)]
            object id,
            [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Any)]
            [ApiConventionTypeMatch(ApiConventionTypeMatchBehavior.Any)]
            object model)
        {
            // Convention method - not implemented
        }

        /// <summary>
        /// Convention for PATCH operations that partially update an item.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Expected responses:
        /// - 200: Success
        /// - 204: No content
        /// - 400: Bad request
        /// - 404: Not found
        /// - 409: Conflict
        /// </para>
        /// </remarks>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Prefix)]
        public static void Patch(
            [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Any)]
            [ApiConventionTypeMatch(ApiConventionTypeMatchBehavior.Any)]
            object id,
            [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Any)]
            [ApiConventionTypeMatch(ApiConventionTypeMatchBehavior.Any)]
            object model)
        {
            // Convention method - not implemented
        }

        /// <summary>
        /// Convention for DELETE operations that remove an item.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Expected responses:
        /// - 200: Success
        /// - 204: No content
        /// - 400: Bad request
        /// - 404: Not found
        /// </para>
        /// </remarks>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Prefix)]
        public static void Delete(
            [ApiConventionNameMatch(ApiConventionNameMatchBehavior.Any)]
            [ApiConventionTypeMatch(ApiConventionTypeMatchBehavior.Any)]
            object id)
        {
            // Convention method - not implemented
        }
    }
}

