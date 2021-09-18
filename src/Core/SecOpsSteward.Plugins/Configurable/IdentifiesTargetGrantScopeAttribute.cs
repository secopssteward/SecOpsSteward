using System;

namespace SecOpsSteward.Plugins.Configurable
{
    /// <summary>
    ///     Denotes that the decorated property in the configuration object is used to identify a target resource for the
    ///     purposes of authorization
    /// </summary>
    public class IdentifiesTargetGrantScopeAttribute : Attribute
    {
    }
}