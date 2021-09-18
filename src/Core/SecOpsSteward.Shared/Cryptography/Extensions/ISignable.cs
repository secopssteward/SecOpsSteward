using System;
using System.Collections.Generic;

namespace SecOpsSteward.Shared.Cryptography.Extensions
{
    /// <summary>
    ///     Denotes an object in the system which can have a digital signature applied to it
    /// </summary>
    public interface ISignable
    {
    }

    /// <summary>
    ///     Denotes an object in the system which can have exactly one digital signature applied to it
    /// </summary>
    public interface ISignableByOne : ISignable
    {
        /// <summary>
        ///     Signature applied to object
        /// </summary>
        ChimeraEntitySignature Signature { get; set; }
    }

    /// <summary>
    ///     Denotes an object in the system which can have multiple chained digital signatures applied to it
    /// </summary>
    public interface ISignableByMany : ISignable
    {
        /// <summary>
        ///     Signature chain, in order
        /// </summary>
        List<ChimeraEntitySignature> Signatures { get; set; }
    }

    /// <summary>
    ///     Mark a Property as not included in the Sign/Verify scope for a given object
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class NotSignedAttribute : Attribute
    {
    }
}