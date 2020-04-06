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

        private volatile bool _isRunning;
        
        private readonly IDictionary<Type, Queue<Action<IDomainEvent>>> _pipelines = new Dictionary<Type, Queue<Action<IDomainEvent>>>();
        
        private readonly IEventBus _eventBus;

        private readonly CancellationTokenSource _cts;

        public EventPipeline(IEventBus eventBus)
        {
            _isLocked = false;
            _isRunning = false;
            _eventBus = eventBus;
            _cts = new CancellationTokenSource();
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
            // TODO: throttling
            while (_isRunning)
            {
                if (_eventBus.TryDequeue(out var domainEvent))
                {
                    InvokePipelineAsync(domainEvent, _cts.Token);
                }
            }
        }

        private void InvokePipelineAsync(IDomainEvent domainEvent, CancellationToken token)
        {
            if (!_isLocked)
            {
                throw new PipelineIsLockedException();
            }

            if (domainEvent is RetryWrap wrap)
            {
                Console.Write($"Retry, because '{wrap.Reason}' ");
                InvokePipelineAsyncInternal(wrap.Original, token);
            }

            InvokePipelineAsyncInternal(domainEvent, token);
        }

        private void InvokePipelineAsyncInternal(IDomainEvent domainEvent, CancellationToken token)
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
                void Action(Task prev, object state)
                {
                    var (eventBus, @event) = ((IEventBus eventBus, IDomainEvent domainEvent))state;
                    eventBus.PlaceEvent(new RetryWrap(@event, prev.Exception.FlatMessage()));
                }
                task.ContinueWith(Action,
                                  (_eventBus, copy),
                                  token,
                                  TaskContinuationOptions.OnlyOnFaulted,
                                  TaskScheduler.Default);
            }
        }

        private class RetryWrap : IDomainEvent
        {
            internal RetryWrap(IDomainEvent domainEvent, string reason)
            {
                Original = domainEvent;
                Reason = reason;
            }

            internal IDomainEvent Original { get; }

            internal string Reason { get; }
        }
    }
}
