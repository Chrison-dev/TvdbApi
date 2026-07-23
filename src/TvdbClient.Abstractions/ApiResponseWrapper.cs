using Tvdb.Models;
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tvdb.Abstractions;

/// <summary>
/// Wrapper for TVDB API Responses
/// </summary>
/// <typeparam name="TDataType"></typeparam>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public class ApiResponseWrapper<TDataType> 
    where TDataType : class
{
    /// <summary>
    /// Request Status
    /// </summary>
    /// <remarks>Can be success, failure</remarks>
    [System.Text.Json.Serialization.JsonPropertyName("status")]
    public string Status { get; set; }

    /// <summary>
    /// Is Response a success?
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsSuccess => Status.Equals("success");

    /// <summary>
    /// Error Message in case of a Failure
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("message")]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Contains the actual Data
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("data")]
    public TDataType? Data { get; set; }

    /// <summary>
    /// backing field for <see cref="AdditionalProperties"/>"/>
    /// </summary>
    private IDictionary<string, object>? _additionalProperties;

    /// <summary>
    /// Contains additional Properties that havent been mapped yet
    /// </summary>
    [System.Text.Json.Serialization.JsonExtensionData]
    public IDictionary<string, object> AdditionalProperties
    {
        get => _additionalProperties ??= new Dictionary<string, object>();
        set => _additionalProperties = value;
    }

    /// <summary>
    /// O-Data Links for next, previous and current record
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("links")]
    public Links Links { get; set; }
}
