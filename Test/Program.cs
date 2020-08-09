using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Buffers.ArraySegmentPool;
namespace Test
{
    class Program
    {
        private static ArraySegmentPool<byte> s_arraySegmentPool;
        private static int s_segmentSize;
        private static int s_iterations;
        #region "Main"
        public static void Main(string[] args)
        {
            Print("Begin tests for ArraySegmentPool", ConsoleColor.Blue);
            GC.Collect();
            SpeedTest(); // 1.5
            GC.Collect();
            StressTest(); // 4.0
            GC.Collect();
            TrimTest();
            GC.Collect();
            SliceTest();
            Print("Finished tests for ArraySegmentPool", ConsoleColor.Blue);
        }
        #endregion
        #region "SpeedTest"
        private static void SpeedTest()
        {            
            s_segmentSize = 1_000;
            s_iterations = 50_000_000;
            Print($"Speed test: 1 thread rent and return {s_segmentSize} segment size {s_iterations} times");
            s_arraySegmentPool = new ArraySegmentPool<byte>(s_segmentSize, 1, 1);
            Stopwatch Stopwatch = new Stopwatch();
            Stopwatch.Start();
            for (int i = 1; i <= s_iterations; i++)
            {
                ArraySegment<byte> arraySegment = s_arraySegmentPool.DangerousRent();
                arraySegment[0] = 1;
                s_arraySegmentPool.Return(ref arraySegment);
            }
            Print($"Elapsed:{Stopwatch.ElapsedMilliseconds}ms. Capacity:{s_arraySegmentPool.Capacity} Count:{s_arraySegmentPool.Count} Fails:{s_arraySegmentPool.FailsCount}", ConsoleColor.Yellow);
            if (s_arraySegmentPool.Capacity == 1 & s_arraySegmentPool.Count == 0 & s_arraySegmentPool.FailsCount == 0)
                Print("TEST PASSED", ConsoleColor.Green);
            else
                Print("TEST FAILED", ConsoleColor.Red);
        }
        #endregion
        #region "StressTest"        
        private static void StressTest()
        {
            s_segmentSize = 1_000;
            s_iterations = 1_000_000;
            Print($"Speed test: 6 threads rent, copy, copy, check and return {s_segmentSize} segment size {s_iterations} times");
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            s_arraySegmentPool = new ArraySegmentPool<byte>(s_segmentSize, 1, 1_000);
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
            Print($"Elapsed:{stopwatch.ElapsedMilliseconds}ms. Capacity:{s_arraySegmentPool.Capacity} Count:{s_arraySegmentPool.Count} Fails:{s_arraySegmentPool.FailsCount}", ConsoleColor.Yellow);
            if (s_arraySegmentPool.Capacity <= 18 & s_arraySegmentPool.Count == 0 & s_arraySegmentPool.FailsCount < s_iterations * 3)
                Print("TEST PASSED", ConsoleColor.Green);
            else
                Print("TEST FAILED", ConsoleColor.Red);
        }
        private static void StressTestWorker(object P_number)
        {
            byte threadNumber = Convert.ToByte(P_number);
            Stopwatch stopwatch = new Stopwatch();
            byte[] buffer = new byte[s_segmentSize];
            Array.Fill(buffer, threadNumber);
            stopwatch.Start();
            for (int iteration = 0; iteration < s_iterations; iteration++)
            {
                ArraySegment<byte> ArraySegment = s_arraySegmentPool.DangerousRent();
                Array.Copy(buffer, 0, ArraySegment.Array, ArraySegment.Offset, ArraySegment.Count);
                for (int i = 0; i < ArraySegment.Count; i++)
                {
                    if (ArraySegment[i] != threadNumber)
                        throw new Exception("Test failed");
                }                
                Array.Clear(buffer, 0, buffer.Length);
                ArraySegment.CopyTo(buffer);
                for (int i = 0; i < buffer.Length ; i++)
                {
                    if (buffer[i] != threadNumber | ArraySegment[i] != threadNumber)
                        throw new Exception("Test failed");
                }
                s_arraySegmentPool.Return(ref ArraySegment);
            }
            Print($"Thread #{threadNumber} finished. ElapsedMilliseconds:{stopwatch.ElapsedMilliseconds}");
        }
        #endregion
        #region "TrimTest"        
        private static void TrimTest()
        {
            s_segmentSize = 1_000;
            s_iterations = 2_147_483;
            Print($"Trim test: 1 thread rent size:{s_segmentSize} x{s_iterations} times, return all, trim");
            Stopwatch Stopwatch = new Stopwatch();
            Stopwatch.Start();
            List<ArraySegment<byte>> List = new List<ArraySegment<byte>>(s_iterations);
            s_arraySegmentPool = new ArraySegmentPool<byte>(s_segmentSize, 1, s_iterations);
            for (int i = 1; i <= s_iterations; i++)
                List.Add(s_arraySegmentPool.DangerousRent());
            Print($"Rent finished. Elapsed:{Stopwatch.ElapsedMilliseconds}ms. Capacity:{s_arraySegmentPool.Capacity} Count:{s_arraySegmentPool.Count} Fails:{s_arraySegmentPool.FailsCount}");                                                     
            for (int i = 0; i < List.Count; i++)
            {
                ArraySegment<byte> ArraySegment = List[i];
                s_arraySegmentPool.Return(ref ArraySegment);
            }
            List.Clear();
            Print($"Return finished. Elapsed:{Stopwatch.ElapsedMilliseconds}ms. Capacity:{s_arraySegmentPool.Capacity} Count:{s_arraySegmentPool.Count} Fails:{s_arraySegmentPool.FailsCount}");
            s_arraySegmentPool.TrimExcess();
            Print($"Trim finished. Elapsed:{Stopwatch.ElapsedMilliseconds}ms. Capacity:{s_arraySegmentPool.Capacity} Count:{s_arraySegmentPool.Count} Fails:{s_arraySegmentPool.FailsCount}");
            Print($"Elapsed:{Stopwatch.ElapsedMilliseconds}ms. Capacity:{s_arraySegmentPool.Capacity} Count:{s_arraySegmentPool.Count} Fails:{s_arraySegmentPool.FailsCount}", ConsoleColor.Yellow);
            if (s_arraySegmentPool.Capacity == 1 & s_arraySegmentPool.Count == 0 & s_arraySegmentPool.FailsCount == 0)
                Print("TEST PASSED", ConsoleColor.Green);
            else
                Print("TEST FAILED", ConsoleColor.Red);
        }
        #endregion
        #region "SliceTest"
        private static void SliceTest()
        {
            s_segmentSize = 100;
            s_iterations = 0;
            Print($"Slice test: 1 thread rent size:{s_segmentSize}, check, slice, return, check");
            Stopwatch Stopwatch = new Stopwatch();
            Stopwatch.Start();
            s_arraySegmentPool = new ArraySegmentPool<byte>(s_segmentSize, 10);
            bool IsError = false;
            ArraySegment<byte> ArraySegment;
            try
            {
                //case 1
                ArraySegment = s_arraySegmentPool.DangerousRent().Slice(0, s_segmentSize);
                Print($"Segment taken from region:{Array.FindIndex(s_arraySegmentPool.UnderlyingLayoutArray, PredicateFindOne)} ArraySegment.Slice(0, _segment_size)");
                if (ReturnAndCheck(ref ArraySegment) == false)
                {
                    IsError = true;
                    return;                
                }
                //case 2
                ArraySegment = s_arraySegmentPool.DangerousRent().Slice(s_segmentSize / 2, s_segmentSize / 2);
                Print($@"Segment taken from region:{Array.FindIndex(s_arraySegmentPool.UnderlyingLayoutArray, PredicateFindOne)} ArraySegment.Slice(_segment_size / 2, _segment_size / 2)");
                if (ReturnAndCheck(ref ArraySegment) == false)
                {
                    IsError = true;
                    return;
                }                
                //case 3
                ArraySegment = s_arraySegmentPool.DangerousRent().Slice(s_segmentSize, 0);
                Print($"Segment taken from region:{Array.FindIndex(s_arraySegmentPool.UnderlyingLayoutArray, PredicateFindOne)} ArraySegment.Slice(_segment_size, 0)");
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
                Print($"Elapsed:{Stopwatch.ElapsedMilliseconds}ms. Capacity:{s_arraySegmentPool.Capacity} Count:{s_arraySegmentPool.Count} Fails:{s_arraySegmentPool.FailsCount}", ConsoleColor.Yellow);
                if (IsError == false & s_arraySegmentPool.Capacity == 10 & s_arraySegmentPool.Count == 1 & s_arraySegmentPool.FailsCount == 0)
                    Print("TEST PASSED", ConsoleColor.Green);
                else
                    Print("TEST FAILED", ConsoleColor.Red);
            }            
        }
        private static bool ReturnAndCheck(ref ArraySegment<byte> ArraySegment)
        {
            try
            {
                s_arraySegmentPool.Return(ref ArraySegment);
            }
            catch (Exception ex)
            {
                Print($"Exception: {ex.Message}", ConsoleColor.Red);
                return false;
            }
            if (Array.FindIndex<int>(s_arraySegmentPool.UnderlyingLayoutArray, PredicateFindOne) != -1)
            {
                Print($"Error: segment not returned", ConsoleColor.Red);
                return false;
            }
            Print($"Segment returned successfully");
            return true;
        }
        private static bool PredicateFindOne(int obj)
        {
            if (obj == 1)
                return true;
            return false;
        }
        #endregion
        private static void Print(string text)
        {
            Print(text, ConsoleColor.White);
        }
        private static void Print(string text, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(text);
            Console.ForegroundColor = ConsoleColor.White;
        }
    }
}