using ServiceLibrary.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dc_app.ServiceLibrary.Utilities;

public class LongPolling
{
    private static List<LongPolling> _sSubscribers = new List<LongPolling>();
    private string _Channel { get; set; }
    private LongPollMessage _Message { get; set; }

    private TaskCompletionSource<bool> _TaskCompleteion = new TaskCompletionSource<bool>();
    public LongPolling(string channel)
    {
        this._Channel = channel;
        lock (_sSubscribers)
        {
            _sSubscribers.Add(this);
        }
    }

    public static void Publish(string channel, LongPollMessage message)
    {
        lock (_sSubscribers)
        {
            var all = _sSubscribers.ToList();
            foreach (var poll in all)
            {
                if (poll._Channel == channel) poll.Notify(message);
            }
        }
    }

    private void Notify(LongPollMessage message)
    {
        this._Message = message;
        this._TaskCompleteion.SetResult(true);
    }

    public async Task<LongPollMessage> WaitAsync()
    {
        await Task.WhenAny(_TaskCompleteion.Task, Task.Delay(10_000)); // blocking wait until event occurs or timeout
        lock (_sSubscribers)
        {
            _sSubscribers.Remove(this);
        }
        return this._Message;
    }
}