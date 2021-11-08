using System;
using System.Runtime.CompilerServices;

namespace SafeEcs
{
    public readonly struct DelegatingEnumerator<T>
    {
        private readonly Func<T> _current;
        private readonly Func<bool> _moveNext;
        private readonly Action _dispose;

        public DelegatingEnumerator(Func<T> current, Func<bool> moveNext, Action dispose)
        {
            _current = current;
            _moveNext = moveNext;
            _dispose = dispose;
        }

        public T Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _current();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            return _moveNext();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            _dispose();
        }
    }
}