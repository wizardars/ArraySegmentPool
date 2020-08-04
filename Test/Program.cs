using System;
using System.Collections.Generic;
using System.Diagnostics;
using ArraySegmentPool;
namespace Test
{
    class Program
    {
        private static ArraySegmentPool<byte> _ArraySegmentPool;
        private static int _segment_size;
        private static int _itarations;
        #region "main"
        public static void Main(string[] args)
        {
            print("Begin tests for ArraySegmentPool", ConsoleColor.Blue);
            GC.Collect();
            SpeedTest(); // 1.5
            GC.Collect();
            StressTest(); // 4.0
            GC.Collect();
            TrimTest();
            GC.Collect();
            SliceTest();
            print("Finished tests for ArraySegmentPool", ConsoleColor.Blue);
        }
        #endregion
        #region "SpeedTest"
        private static void SpeedTest()
        {
            _segment_size = 1_000;
            _itarations = 50_000_000;
            print($"Speed test: 1 thread rent and return {_segment_size} segment size {_itarations} times");
            _ArraySegmentPool = new ArraySegmentPool<byte>(_segment_size, 1, 1, true);
            Stopwatch Stopwatch = new Stopwatch();
            Stopwatch.Start();
            for (int i = 1; i <= _itarations; i++)
            {
                ArraySegment<byte> ArraySegment = _ArraySegmentPool.DangerousRent();
                ArraySegment[0] = 1;
                _ArraySegmentPool.Return(ref ArraySegment);
            }
            print($"Elapsed:{Stopwatch.ElapsedMilliseconds}ms. Capacity:{_ArraySegmentPool.Capacity} Count:{_ArraySegmentPool.Count} Fails:{_ArraySegmentPool.FailsCount}", ConsoleColor.Yellow);
            if (_ArraySegmentPool.Capacity == 1 & _ArraySegmentPool.Count == 0 & _ArraySegmentPool.FailsCount == 0)
                print("TEST PASSED", ConsoleColor.Green);
            else
                print("TEST FAILED", ConsoleColor.Red);
        }
        #endregion
        #region "StressTest"        
        private static void StressTest()
        {
            _segment_size = 1_000;
            _itarations = 1_000_000;
            print($"Speed test: 6 threads rent, copy, copy, check and return {_segment_size} segment size {_itarations} times");
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            _ArraySegmentPool = new ArraySegmentPool<byte>(_segment_size, 1, 1_000, true);
            System.Threading.Thread thread1 = new System.Threading.Thread(StressTestWorker) { IsBackground = true };
            System.Threading.Thread thread2 = new System.Threading.Thread(StressTestWorker) { IsBackground = true };
            System.Threading.Thread thread3 = new System.Threading.Thread(StressTestWorker) { IsBackground = true };
            System.Threading.Thread thread4 = new System.Threading.Thread(StressTestWorker) { IsBackground = true };
            System.Threading.Thread thread5 = new System.Threading.Thread(StressTestWorker) { IsBackground = true };
            System.Threading.Thread thread6 = new System.Threading.Thread(StressTestWorker) { IsBackground = true };
            thread1.Start(1);
            thread2.Start(2);
            thread3.Start(3);
            thread4.Start(4);
            thread5.Start(5);
            thread6.Start(6);
            thread1.Join();
            thread2.Join();
            thread3.Join();
            thread4.Join();
            thread5.Join();
            thread6.Join();
            print($"Elapsed:{stopwatch.ElapsedMilliseconds}ms. Capacity:{_ArraySegmentPool.Capacity} Count:{_ArraySegmentPool.Count} Fails:{_ArraySegmentPool.FailsCount}", ConsoleColor.Yellow);
            if (_ArraySegmentPool.Capacity <= 18 & _ArraySegmentPool.Count == 0 & _ArraySegmentPool.FailsCount < _itarations * 3)
                print("TEST PASSED", ConsoleColor.Green);
            else
                print("TEST FAILED", ConsoleColor.Red);
        }
        private static void StressTestWorker(object P_number)
        {
            byte threadNumber = System.Convert.ToByte(P_number);
            Stopwatch stopwatch = new Stopwatch();
            byte[] buffer = new byte[_segment_size];
            Array.Fill<byte>(buffer, threadNumber);
            stopwatch.Start();
            for (int itaration = 0; itaration < _itarations; itaration++)
            {
                ArraySegment<byte> ArraySegment = _ArraySegmentPool.DangerousRent();
                Array.Copy(buffer, 0, ArraySegment.Array, ArraySegment.Offset, ArraySegment.Count);
                for (int i = 0; i < ArraySegment.Count; i++)
                {
                    if (ArraySegment[i] != threadNumber)
                        throw new Exception("Test failed");
                }
                Array.Fill<byte>(buffer, 0);
                ArraySegment.CopyTo(buffer);
                for (int i = 0; i < buffer.Length ; i++)
                {
                    if (ArraySegment[i] != threadNumber)
                        throw new Exception("Test failed");
                }
                _ArraySegmentPool.Return(ref ArraySegment);
            }
            print($"Thread #{threadNumber} finished. ElapsedMilliseconds:{stopwatch.ElapsedMilliseconds}");
        }
        #endregion
        #region "TrimTest"        
        private static void TrimTest()
        {
            _segment_size = 1_000;
            _itarations = 2_147_483;
            print($"Trim test: 1 thread rent size:{_segment_size} x{_itarations} times, return all, trim");
            Stopwatch Stopwatch = new Stopwatch();
            Stopwatch.Start();
            List<ArraySegment<byte>> List = new List<ArraySegment<byte>>(_itarations);
            _ArraySegmentPool = new ArraySegmentPool<byte>(_segment_size, 1, _itarations, true);
            for (int i = 1; i <= _itarations; i++)
                List.Add(_ArraySegmentPool.DangerousRent());
            print($"Rent finished. Elapsed:{Stopwatch.ElapsedMilliseconds}ms. Capacity:{_ArraySegmentPool.Capacity} Count:{_ArraySegmentPool.Count} Fails:{_ArraySegmentPool.FailsCount}");                                                     
            for (int i = 0; i < List.Count; i++)
            {
                ArraySegment<byte> ArraySegment = List[i];
                _ArraySegmentPool.Return(ref ArraySegment);
            }
            List.Clear();
            print($"Return finished. Elapsed:{Stopwatch.ElapsedMilliseconds}ms. Capacity:{_ArraySegmentPool.Capacity} Count:{_ArraySegmentPool.Count} Fails:{_ArraySegmentPool.FailsCount}");
            _ArraySegmentPool.TrimExcess();
            print($"Trim finished. Elapsed:{Stopwatch.ElapsedMilliseconds}ms. Capacity:{_ArraySegmentPool.Capacity} Count:{_ArraySegmentPool.Count} Fails:{_ArraySegmentPool.FailsCount}");
            print($"Elapsed:{Stopwatch.ElapsedMilliseconds}ms. Capacity:{_ArraySegmentPool.Capacity} Count:{_ArraySegmentPool.Count} Fails:{_ArraySegmentPool.FailsCount}", ConsoleColor.Yellow);
            if (_ArraySegmentPool.Capacity == 1 & _ArraySegmentPool.Count == 0 & _ArraySegmentPool.FailsCount == 0)
                print("TEST PASSED", ConsoleColor.Green);
            else
                print("TEST FAILED", ConsoleColor.Red);
        }
        #endregion
        #region "SliceTest"
        private static void SliceTest()
        {
            _segment_size = 100;
            _itarations = 0;
            print($"Slice test: 1 thread rent size:{_segment_size}, check, slice, return, check");
            Stopwatch Stopwatch = new Stopwatch();
            Stopwatch.Start();
            _ArraySegmentPool = new ArraySegmentPool<byte>(_segment_size, 10, 10, false);
            bool IsError = false;
            ArraySegment<byte> ArraySegment;
            try
            {
                //case 1
                ArraySegment = _ArraySegmentPool.DangerousRent().Slice(0, _segment_size);
                print($"Segment taken from region:{Array.FindIndex(_ArraySegmentPool.UnderlyingLayoutArray, predicateFindOne)} ArraySegment.Slice(0, _segment_size)");
                if (ReturnAndCheck(ref ArraySegment) == false)
                {
                    IsError = true;
                    return;                
                }
                //case 2
                ArraySegment = _ArraySegmentPool.DangerousRent().Slice(_segment_size / 2, _segment_size / 2);
                print($@"Segment taken from region:{Array.FindIndex(_ArraySegmentPool.UnderlyingLayoutArray, predicateFindOne)} ArraySegment.Slice(_segment_size / 2, _segment_size / 2)");
                if (ReturnAndCheck(ref ArraySegment) == false)
                {
                    IsError = true;
                    return;
                }                
                //case 3
                ArraySegment = _ArraySegmentPool.DangerousRent().Slice(_segment_size, 0);
                print($"Segment taken from region:{Array.FindIndex(_ArraySegmentPool.UnderlyingLayoutArray, predicateFindOne)} ArraySegment.Slice(_segment_size, 0)");
                if (ReturnAndCheck(ref ArraySegment) == false)
                {
                    IsError = false;
                }
                else
                {
                    IsError = true; //Slice ArraySegment to zero not permitted!
                    return;
                }
            }
            finally
            {
                print($"Elapsed:{Stopwatch.ElapsedMilliseconds}ms. Capacity:{_ArraySegmentPool.Capacity} Count:{_ArraySegmentPool.Count} Fails:{_ArraySegmentPool.FailsCount}", ConsoleColor.Yellow);
                if (IsError == false & _ArraySegmentPool.Capacity == 10 & _ArraySegmentPool.Count == 1 & _ArraySegmentPool.FailsCount == 0)
                    print("TEST PASSED", ConsoleColor.Green);
                else
                    print("TEST FAILED", ConsoleColor.Red);
            }            
        }
        private static bool ReturnAndCheck(ref ArraySegment<byte> ArraySegment)
        {
            try
            {
                _ArraySegmentPool.Return(ref ArraySegment);
            }
            catch (Exception ex)
            {
                print($"Exception: segment not returned", ConsoleColor.Red);
                return false;
            }
            if (Array.FindIndex<int>(_ArraySegmentPool.UnderlyingLayoutArray, predicateFindOne) != -1)
            {
                print($"Error: segment not returned", ConsoleColor.Red);
                return false;
            }
            print($"Segment returned successfully");
            return true;
        }
        private static bool predicateFindOne(int obj)
        {
            if (obj == 1)
                return true;
            return false;
        }
        #endregion
        private static void print(string text)
        {
            print(text, ConsoleColor.White);
        }
        private static void print(string text, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(text);
            Console.ForegroundColor = ConsoleColor.White;
        }
    }
}