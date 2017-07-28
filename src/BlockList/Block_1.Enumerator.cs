// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// The implementation of this type was copied from the source of ArraySegment<T> at:
// https://github.com/dotnet/coreclr/blob/b3e859cb5777bb68dd15caac75ee861da98489ae/src/mscorlib/src/System/ArraySegment.cs.

using System;
using System.Collections;
using System.Collections.Generic;
using Clever.Collections.Internal.Diagnostics;

namespace Clever.Collections
{
    public partial struct Block<T>
    {
        public struct Enumerator : IEnumerator<T>
        {
            private readonly T[] _array;
            private readonly int _count;

            private int _index;

            internal Enumerator(T[] array, int count)
            {
                _array = array;
                _count = count;
                _index = -1;
            }

            public T Current => _array[_index];

            public void Dispose()
            {
            }

            public bool MoveNext() => ++_index < _count;

            [ExcludeFromCodeCoverage]
            object IEnumerator.Current => Current;

            [ExcludeFromCodeCoverage]
            void IEnumerator.Reset() => throw new NotSupportedException();
        }
    }
}
