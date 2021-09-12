﻿namespace SecOpsSteward.Shared.Cryptography
{
    public interface IPubliclySignable : ISignable
    {
        /// <summary>
        /// A signature from a public entity outside the scope of the tenant
        /// </summary>
        PublicSignature PublicSignature { get; set; }
    }
}
