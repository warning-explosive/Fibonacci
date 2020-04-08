namespace EventBusImpl
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using EventBusApi;
    using Exceptions;

    public class EventPipeline : IConsumerEventPipeline,
                                 IProducerEventPipeline
    {
        private bool _isLocked;
        
        private readonly IDictionary<Type, Queue<Func<IDomainEvent, Task>>> _pipelines;
        
        private readonly IEventBus _eventBus;

        private readonly int _concurrencyLevel;

        private readonly List<Task> _concurrencyList;

        private readonly int _retryLimit;

        private readonly CancellationTokenSource _cts;

        private static ManualResetEvent _mre;

        public EventPipeline(IEventBus eventBus, int concurrencyLevel, int retryLimit)
        {
            _isLocked = false;
            _pipelines = new Dictionary<Type, Queue<Func<IDomainEvent, Task>>>();
            _eventBus = eventBus;
            _concurrencyLevel = concurrencyLevel;
            _concurrencyList = new List<Task>(_concurrencyLevel);
            _retryLimit = retryLimit;
            _cts = new CancellationTokenSource();
            _mre = new ManualResetEvent(false);
        }

        public void RegisterStep<TDomainEvent>(IPipelineStep<TDomainEvent> step)
            where TDomainEvent : IDomainEvent
        {
            if (_isLocked)
            {
                throw new PipelineIsLockedException();
            }

            if (!_pipelines.ContainsKey(typeof(TDomainEvent))
                || _pipelines[typeof(TDomainEvent)] == null)
            {
                _pipelines[typeof(TDomainEvent)] = new Queue<Func<IDomainEvent, Task>>();
            }

            _pipelines[typeof(TDomainEvent)].Enqueue(ExecuteHandler);

            async Task ExecuteHandler(IDomainEvent domainEvent)
            {
                foreach (var @event in await step.HandleEvent((TDomainEvent) domainEvent))
                {
                    _eventBus.PlaceEvent(@event);
                }
            }
        }

        public void StopAndRelease()
        {
            _mre.Reset();
            _cts.Cancel();
            
            try
            {
                Task.WhenAll(_concurrencyList).Wait(); // TODO: block
                _concurrencyList.Clear();
                _cts.Dispose();
            }
            catch (AggregateException ex)
            {
                if (!ex.InnerExceptions.OfType<TaskCanceledException>().Any())
                {
                    throw;
                }
            }
        }

        public void LockAndRunEventLoop()
        {
            Lock();

            RunEventLoop();
        }

        private void Lock()
        {
            _isLocked = true;
        }

        private void RunEventLoop()
        {
            _mre.Set();
            Task.Factory.StartNew(RunEventLoopInternal, TaskCreationOptions.DenyChildAttach | TaskCreationOptions.LongRunning);
        }

        private void RunEventLoopInternal()
        {
            while (_mre.WaitOne())
            {
                // throttling
                while (_concurrencyList.Count < _concurrencyLevel
                       && _eventBus.TryDequeue(out var domainEvent))
                {
                    if (_mre.WaitOne())
                    {
                        _concurrencyList.Add(InvokePipelineAsync(domainEvent, _cts.Token));
                    }
                }
                
                if (_concurrencyList.Any())
                {
                    var anyTask = Task.WhenAny(_concurrencyList).Result; // TODO: block
                    
                    if (_mre.WaitOne())
                    {
                        _concurrencyList.Remove(anyTask);
                    }
                }
            }
        }

        private Task InvokePipelineAsync(IDomainEvent domainEvent, CancellationToken token)
        {
            if (!_isLocked)
            {
                throw new PipelineIsLockedException();
            }

            if (!(domainEvent is RetryWrap wrap))
            {
                return InvokePipelineAsyncInternal(domainEvent, token, 0);
            }
            
            if (wrap.RetryCount < _retryLimit)
            {
                Console.Write($"Retry {wrap.RetryCount + 1}, because '{wrap.Reason}' ");
                return InvokePipelineAsyncInternal(wrap.Original, token, wrap.RetryCount + 1);
            }

            _eventBus.PlaceError(wrap.Original, wrap.Reason);
            return Task.CompletedTask;
        }

        private async Task InvokePipelineAsyncInternal(IDomainEvent domainEvent, CancellationToken token, int actualRetryCount)
        {
            if (_pipelines.ContainsKey(domainEvent.GetType()))
            {
                // pipeline - step 0
                var firstEventHandler = _pipelines[domainEvent.GetType()].First();
                var copy = (IDomainEvent)domainEvent.DeepCopyBySerialization();

                try
                {
                    await firstEventHandler.Invoke(copy);

                    // pipeline - other steps
                    foreach (var nextEventHandler in _pipelines[domainEvent.GetType()].Skip(1))
                    {
                        copy = (IDomainEvent) domainEvent.DeepCopyBySerialization();
                        await nextEventHandler.Invoke(copy);
                    }
                }
                catch (Exception ex)
                {
                    // retry
                    copy = (IDomainEvent) domainEvent.DeepCopyBySerialization();
                    var retry = new RetryWrap(copy, ex.FlatMessage(), actualRetryCount);
                    _eventBus.PlaceEvent(retry);
                }
            }
        }

        private class RetryWrap : IDomainEvent
        {
            internal RetryWrap(IDomainEvent domainEvent, string reason, int retryCount)
            {
                Original = domainEvent;
                Reason = reason;
                RetryCount = retryCount;
            }

            internal IDomainEvent Original { get; }

            internal string Reason { get; }

            internal int RetryCount { get; }
        }
    }
}
