using System;
namespace ArraySegmentPool
{
    /// <summary>
    /// Implements a dangerous, thread safe, auto resizable Pool that uses an array of objects to store the fixed size ArraySegments.
    /// The best fit is when one thread rent, same or others returns back short-lived objects.
    /// (!) WARNING: If the ArraySegment is not returned, a memory leak will occur.
    /// (!) WARNING: Do not use this pool for long-lived objects, otherwise the memory consumption will be enormous.
    /// (!) WARNING: Slice ArraySegment to zero not permitted.
    /// (!) WARNING: User of this pool must strictly understand how the structure differs from the class.
    /// (!) WARNING: If the user has made many copies of the ArraySegment, then only one copy needs to be returned to the pool. After returning, you should not use the leftover copies, as this will corrupt the data.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ArraySegmentPool<T>
    {
        private volatile _pool _current_pool;
        private readonly bool _is_auto_resize;
        private readonly int _default_ArraySegment_length;
        private readonly int _max_capacity;
        private readonly int _initial_capacity;
        private readonly object _pool_lock = new object();               
        /// <summary>
        /// Gets the number of rented segments after last resize.
        /// </summary>
        public int Count
        {
            get
            {
                return _current_pool.Count;
            }
        }
        /// <summary>
        /// Gets the total number of segments after last resize.
        /// </summary>
        public int Capacity
        {
            get
            {
                return _current_pool.Array_layout.Length;
            }
        }
#if UT
        /// <summary>
        /// (Debug) Shows how many unsuccessful attempts were made before the segment was taken.
        /// </summary>
        private long _fails_count;
        public long FailsCount
        {
            get
            {
                return _fails_count;
            }
        }
        /// <summary>
        /// (Debug)
        /// </summary>        
        public int LastRentedSegment
        {
            get
            {
                return _current_pool.Last_rented_segment;
            }
        }
        /// <summary>
        /// (Debug)
        /// </summary>        
        public int[] UnderlyingLayoutArray
        {
            get
            {
                return _current_pool.Array_layout;
            }
        }        
#endif
        /// <summary>
        /// Main storage of the pool.                                                                                                                                                 
        /// </summary>
        private class _pool
        {
            volatile public T[] Array; // Stores segments.
            volatile public int[] Array_layout; // Stores information about rented segments.(Interlocked not support byte)
            volatile public int Count; // Stores number of used segments.
            volatile public int Last_rented_segment; // DangerousRent using that value to find next free segment.
        }
        /// <summary>
        /// Constructs a new pool.
        /// </summary>
        /// <param name="DefaultLength">Default length of the segment</param>
        /// <param name="InitialCapacity">Initial segments count</param>
        /// <param name="MaxCapacity">Maximum count of ArraySegments</param>
        /// <param name="AutoResize">Allow auto resize</param>
        public ArraySegmentPool(int DefaultLength, int InitialCapacity, int MaxCapacity, bool AutoResize)
        {
            if (DefaultLength < 1 | InitialCapacity < 1 | MaxCapacity < 1)
                throw new ArgumentOutOfRangeException("Arguments must be greater than 1");
            if (InitialCapacity > MaxCapacity)
                throw new ArgumentOutOfRangeException("InitialCapacity > MaxCapacity");
            if ((long)DefaultLength * MaxCapacity > int.MaxValue)
                throw new OverflowException("MaxCapacity");
            _default_ArraySegment_length = DefaultLength;
            _initial_capacity = InitialCapacity;
            _max_capacity = MaxCapacity;
            _is_auto_resize = AutoResize;
            _current_pool = new _pool() { Array_layout = new int[InitialCapacity], Array = new T[DefaultLength * InitialCapacity] };
        }
        /// <summary>
        /// (!) Dangerous. Gets an ArraySegment of the default length. ArraySegment must be returned via <see cref="Return"/> on the same <see cref="ArraySegmentPool{T}"/> instance to avoid memory leaks.
        /// </summary>
        /// <returns></returns>
        public ArraySegment<T> DangerousRent()
        {
            return DangerousRent(_default_ArraySegment_length);
        }
        /// <summary>
        /// (!) Dangerous. Gets an ArraySegment of the custom length. ArraySegment must be returned via <see cref="Return"/> on the same <see cref="ArraySegmentPool{T}"/> instance to avoid memory leaks.
        /// </summary>    
        /// <param name="Length">Lenght of the rented segment. Lenght must be equal or smaller than "DefaultLength"</param>
        /// <returns></returns>
        public ArraySegment<T> DangerousRent(int Length)
        {
            if (Length < 1 | Length > _default_ArraySegment_length)
                throw new ArgumentOutOfRangeException("Length must be greater than one and smaller than default length");
            _pool pool = _current_pool;
            //Get new resized pool if free segment not finded.
            if (pool.Count >= pool.Array_layout.Length)
                pool = get_new_pool(pool);
            //Try find free segment and ocupy.
            int position = pool.Last_rented_segment + 1;
            int search_count = 0;
            do
            {
                if (position > pool.Array_layout.GetUpperBound(0))
                    position = 0;
                if (System.Threading.Interlocked.CompareExchange(ref pool.Array_layout[position], 1, 0) == 0)
                {
                    System.Threading.Interlocked.Increment(ref pool.Count);
                    pool.Last_rented_segment = position;
                    return new ArraySegment<T>(pool.Array, position * _default_ArraySegment_length, Length);
                }
#if UT
                System.Threading.Interlocked.Increment(ref _fails_count);
#endif
                position += 1;
                search_count += 1;
                //That check prevent state, where thread will loop forever.
                if (search_count == pool.Array_layout.Length)
                {
                    pool = get_new_pool(pool);
                    position = 0;
                    search_count = 0;
                }
            }
            while (true);
        }
        /// <summary>
        /// Returns to the pool an segment that was previously obtained via <see cref="DangerousRent()"/> on the same <see cref="ArraySegmentPool{T}"/> instance.
        /// </summary>
        /// <param name="ArraySegment"></param>
        public void Return(ref ArraySegment<T> ArraySegment)
        {
            if (ArraySegment.Count == 0)
                throw new ArgumentException("Do not Slice rented ArraySegment to zero, since the pool will not be able to free memory");
            _pool pool = _current_pool;
            if (ArraySegment.Array == pool.Array)
            {
                //return segment.
                int position = ArraySegment.Offset / _default_ArraySegment_length;
                if (System.Threading.Interlocked.Exchange(ref pool.Array_layout[position], 0) == 0)
                    throw new Exception("ArraySegment was returned already");
                System.Threading.Interlocked.Decrement(ref pool.Count);
            }
            ArraySegment = ArraySegment<T>.Empty;
        }
        /// <summary>
        /// Sets the capacity of this pool to the size of the used ArraySegments or initial capacity. This method can be used to minimize a pool's memory overhead once it is known that no new segments will be added to the pool.
        /// </summary>
        public void TrimExcess()
        {
            lock (_pool_lock)
            {
                if (_is_auto_resize == false)
                    throw new AccessViolationException("Can't trim while auto resize false");
                int count = _current_pool.Count;
                int new_layout_length = count > _initial_capacity ? count : _initial_capacity;
                int new_length = new_layout_length * _default_ArraySegment_length;
                _current_pool = new _pool() { Array_layout = new int[new_layout_length], Array = new T[new_length] };
            }
        }
        /// <summary>
        /// Resize the pool and update the instance reference.
        /// </summary>
        /// <param name="pool"></param>
        /// <returns>New pool</returns>
        private _pool get_new_pool(_pool pool)
        {
            lock (_pool_lock)
            {
                if (_is_auto_resize == false)
                    throw new OverflowException("ArraySegmentPool size out of max capacity");
                //check if other thread already create new resized pool.
                if (pool != _current_pool)
                    return _current_pool;
                //check limits.
                if (pool.Array_layout.Length == _max_capacity)
                    throw new OverflowException("ArraySegmentPool size out of max capacity");
                //create new resized pool and refresh current ref
                int new_layout_length = pool.Array_layout.Length * 2L < _max_capacity ? pool.Array_layout.Length * 2 : _max_capacity;
                int new_length = _default_ArraySegment_length * new_layout_length;
                _current_pool = new _pool() { Array_layout = new int[new_layout_length], Array = new T[new_length] };
                //return new pool.
                return _current_pool;
            }
        }
    }
}