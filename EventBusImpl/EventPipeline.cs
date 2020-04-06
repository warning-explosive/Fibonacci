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

        private bool _isRunning;
        
        private readonly IDictionary<Type, Queue<Action<IDomainEvent>>> _pipelines;
        
        private readonly IEventBus _eventBus;

        private readonly int _concurrencyLevel;

        private readonly List<Task> _concurrencyList;

        private readonly CancellationTokenSource _cts;

        private readonly int _retryLimit;

        public EventPipeline(IEventBus eventBus, int concurrencyLevel, int retryLimit)
        {
            _isLocked = false;
            _isRunning = false;
            _pipelines = new Dictionary<Type, Queue<Action<IDomainEvent>>>();
            _eventBus = eventBus;
            _concurrencyLevel = concurrencyLevel;
            _concurrencyList = new List<Task>(_concurrencyLevel);
            _cts = new CancellationTokenSource();
            _retryLimit = retryLimit;
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
                _pipelines[typeof(TDomainEvent)] = new Queue<Action<IDomainEvent>>();
            }

            _pipelines[typeof(TDomainEvent)].Enqueue(ExecuteHandler);

            void ExecuteHandler(IDomainEvent domainEvent)
            {
                foreach (var @event in step.HandleEvent((TDomainEvent) domainEvent).Result) // TODO: block
                {
                    _eventBus.PlaceEvent(@event);
                }
            }
        }

        public void StopAndRelease()
        {
            _isRunning = false;
            _cts.Cancel();
            try
            {
                Task.WhenAll(_concurrencyList).Wait(); // TODO: block
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
            _isRunning = true;
            Task.Factory.StartNew(RunEventLoopInternal, TaskCreationOptions.DenyChildAttach | TaskCreationOptions.LongRunning);
        }

        private void RunEventLoopInternal()
        {
            while (_isRunning)
            {
                // throttling
                while (_concurrencyList.Count < _concurrencyLevel
                       && _eventBus.TryDequeue(out var domainEvent))
                {
                    _concurrencyList.Add(InvokePipelineAsync(domainEvent, _cts.Token));
                }
                
                if (_concurrencyList.Any())
                {
                    var anyTask = Task.WhenAny(_concurrencyList).Result; // TODO: block
                    _concurrencyList.Remove(anyTask);
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

        private Task InvokePipelineAsyncInternal(IDomainEvent domainEvent, CancellationToken token, int actualRetryCount)
        {
            if (_pipelines.ContainsKey(domainEvent.GetType()))
            {
                // pipeline - step 0
                var action = _pipelines[domainEvent.GetType()].First();
                var copy = (IDomainEvent)domainEvent.DeepCopyBySerialization();
                var task = Task.Factory.StartNew(obj => action((IDomainEvent) obj),
                                                 copy,
                                                 token,
                                                 TaskCreationOptions.DenyChildAttach,
                                                 TaskScheduler.Default);
                
                // pipeline - other steps
                foreach (var eventHandler in _pipelines[domainEvent.GetType()].Skip(1))
                {
                    copy = (IDomainEvent)domainEvent.DeepCopyBySerialization();
                    task = task.ContinueWith((_, obj) => eventHandler.Invoke((IDomainEvent) obj),
                                             copy,
                                             token,
                                             TaskContinuationOptions.NotOnFaulted | TaskContinuationOptions.NotOnCanceled,
                                             TaskScheduler.Default);
                }

                // retry
                copy = (IDomainEvent)domainEvent.DeepCopyBySerialization();
                void Retry(Task prev, object state)
                {
                    var (eventBus, @event) = ((IEventBus eventBus, IDomainEvent domainEvent))state;
                    eventBus.PlaceEvent(new RetryWrap(@event, prev.Exception.FlatMessage(), actualRetryCount));
                }
                
                return task.ContinueWith(Retry,
                                         (_eventBus, copy),
                                         token,
                                         TaskContinuationOptions.OnlyOnFaulted,
                                         TaskScheduler.Default);
            }

            return Task.CompletedTask;
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
