using QubicNS;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace QubicNS
{
    public class TimeLogger
    {
        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        int checkPointCounter = 0;
        long prevCheckpoint = 0;
        string title;
        public bool ShowTimeDifference;

        static Stack<TimeLogger> stack = new Stack<TimeLogger>();

        public static void LogCheckpoint(string title = null)
        {
            LogTime(title, true);
        }

        public static void LogPause(string title = null)
        {
            if (stack.Count == 0)
                return;
            var timing = stack.Peek();
            if (title != null)
                LogTime(title);
            timing.sw.Stop();
        }

        public static void Resume()
        {
            if (stack.Count == 0)
                return;
            var timing = stack.Peek();
            timing.sw.Start();
        }

        public static void Start(string title = null, bool showTimeDifference = false)
        {
            var timing = new TimeLogger() { title = title, ShowTimeDifference = showTimeDifference };
            stack.Push(timing);
            timing.sw.Start();
        }

        public static void LogStop(string title = null)
        {
            if (stack.Count == 0)
                return;
            LogTime(title);
            stack.Pop().sw.Stop();
        }

        static void LogTime(string title = null, bool isCheckPoint = false)
        {
            if (stack.Count == 0)
                return;
            var timing = stack.Peek();
            title = title ?? timing.title;
            if (timing.sw.IsRunning)
            {
                timing.checkPointCounter++;
                var elapsed = timing.sw.ElapsedMilliseconds;
                var message = "";
                var cp = isCheckPoint ? (" " + timing.checkPointCounter.ToString()) : "";

                if (isCheckPoint && timing.ShowTimeDifference)
                {
                    var delta = elapsed - timing.prevCheckpoint;
                    message = $"{title ?? "Time"}{cp}: <color=#00FF00>Δ {delta} ms</color>";
                }
                else
                {
                    message = $"{title ?? "Time"}{cp}: <color=#00FF00>{elapsed} ms</color>";
                }

                Debug.Log(message);

                timing.prevCheckpoint = elapsed;
            }
        }
    }

    public class MethodTimer : IDisposable
    {
        static Dictionary<string, Data> timers = new Dictionary<string, Data>();
        Data data;

        class Data
        {
            public System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
            public long totalElapsedTime = 0;
            public int callCount = 0;
        }

        // Конструктор для автоматического извлечения имени метода из стека вызовов
        public MethodTimer([CallerMemberName] string methodName = "")
        {
            data = timers.GetOrCreate(methodName);
            data.stopwatch.Restart();  // Начинаем измерение времени
        }

        // Завершаем замер и накапливаем данные
        public void Dispose()
        {
            data.stopwatch.Stop();  // Останавливаем таймер

            // Накопление времени
            data.totalElapsedTime += data.stopwatch.ElapsedMilliseconds;
            data.callCount++;
        }

        // Метод для вывода результатов всех измерений после завершения работы
        public static void PrintResults()
        {
            foreach (var pair in timers)
                UnityEngine.Debug.Log($"{pair.Key} total time: <color=#33ff33>{pair.Value.totalElapsedTime} ms</color>, Calls: {pair.Value.callCount}");

            timers.Clear();
        }
    }
}