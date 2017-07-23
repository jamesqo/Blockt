// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Copied from https://github.com/dotnet/corefx/blob/0b6e1fd0cfbeb8f6e8df6d19f30be8fa2e8097be/src/System.Diagnostics.Tools/src/System/Diagnostics/CodeAnalysis/ExcludeFromCodeCoverageAttribute.cs

namespace System.Diagnostics.CodeAnalysis
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Event, Inherited = false, AllowMultiple = false)]
    internal sealed class ExcludeFromCodeCoverageAttribute : Attribute
    {
        public ExcludeFromCodeCoverageAttribute()
        {
        }
    }
}