using System;
using Avalonia.Threading;

namespace ErmayMuhasebe.Services
{
    public class SessionService
    {
        private DispatcherTimer? _timer;
        private DateTime _lastActivity;
        private int _timeoutMinutes = 30; // Default 30 min
        
        public event Action? OnTimeout;

        public SessionService()
        {
            _lastActivity = DateTime.Now;
        }

        public void Start(int minutes = 30)
        {
            _timeoutMinutes = minutes;
            _lastActivity = DateTime.Now;

            if (_timer == null)
            {
                _timer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMinutes(1)
                };
                _timer.Tick += Timer_Tick;
            }
            _timer.Start();
        }

        public void Stop()
        {
            _timer?.Stop();
        }

        public void ResetActivity()
        {
            _lastActivity = DateTime.Now;
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (DateTime.Now - _lastActivity > TimeSpan.FromMinutes(_timeoutMinutes))
            {
                Stop();
                OnTimeout?.Invoke();
            }
        }
    }
}
