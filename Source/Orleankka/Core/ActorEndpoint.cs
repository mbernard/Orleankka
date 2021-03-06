using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Threading.Tasks;

using Orleans;
using Orleans.Concurrency;
using Orleans.Placement;
using Orleans.Runtime;

namespace Orleankka.Core
{
    using Cluster;

    /// <summary> 
    /// FOR INTERNAL USE ONLY!
    /// </summary>
    public abstract class ActorEndpoint : Grain, IRemindable
    {
        internal static IActorActivator Activator;

        internal static void Reset()
        {
            Activator = new DefaultActorActivator();
        }

        static ActorEndpoint()
        {
            Reset();
        }

        Actor actor;

        public async Task<ResponseEnvelope> Receive(RequestEnvelope envelope)
        {
            if (actor == null)
                await Activate(ActorPath.Deserialize(envelope.Target));

            Debug.Assert(actor != null);
            KeepAlive();

            return new ResponseEnvelope(await actor.OnReceive(envelope.Message));
        }

        public Task<ResponseEnvelope> ReceiveReentrant(RequestEnvelope envelope)
        {
            #if DEBUG
                CallContext.LogicalSetData("LastMessageReceivedReentrant", envelope.Message);
            #endif

            return Receive(envelope);
        }

        async Task IRemindable.ReceiveReminder(string reminderName, TickStatus status)
        {
            if (actor == null)
                await Activate(ActorPath.Deserialize(IdentityOf(this)));

            Debug.Assert(actor != null);
            KeepAlive();

            await actor.OnReminder(reminderName);
        }

        async Task Activate(ActorPath path)
        {
            var system = ClusterActorSystem.Current;

            var runtime = new ActorRuntime(system, this);
            var prototype = ActorPrototype.Of(path.Type);

            actor = Activator.Activate(path.Type, path.Id, runtime);
            actor.Initialize(path.Id, runtime, prototype);

            await actor.OnActivate();
        }

        public override Task OnDeactivateAsync()
        {
            return actor != null
                    ? actor.OnDeactivate()
                    : base.OnDeactivateAsync();
        }

        void KeepAlive()
        {
            actor._.KeepAlive(this);
        }

        #region Internals

        internal new void DeactivateOnIdle()
        {
            base.DeactivateOnIdle();
        }

        internal new void DelayDeactivation(TimeSpan timeSpan)
        {
            base.DelayDeactivation(timeSpan);
        }

        internal new Task<IGrainReminder> GetReminder(string reminderName)
        {
            return base.GetReminder(reminderName);
        }

        internal new Task<List<IGrainReminder>> GetReminders()
        {
            return base.GetReminders();
        }

        internal new Task<IGrainReminder> RegisterOrUpdateReminder(string reminderName, TimeSpan dueTime, TimeSpan period)
        {
            return base.RegisterOrUpdateReminder(reminderName, dueTime, period);
        }

        internal new Task UnregisterReminder(IGrainReminder reminder)
        {
            return base.UnregisterReminder(reminder);
        }

        internal new IDisposable RegisterTimer(Func<object, Task> asyncCallback, object state, TimeSpan dueTime, TimeSpan period)
        {
            return base.RegisterTimer(asyncCallback, state, dueTime, period);
        }

        #endregion

        static string IdentityOf(IGrain grain)
        {
            return (grain as IGrainWithStringKey).GetPrimaryKeyString();
        }

        internal static IActorEndpoint Proxy(ActorPath path)
        {
            return ActorEndpointDynamicFactory.Proxy(path);
        }
    }

    namespace Static
    {
        /// <summary>
        ///   FOR INTERNAL USE ONLY!
        ///   Actor with Placement.Random
        /// </summary>
        public class A0 : ActorEndpoint, IA0
        {}

        /// <summary>
        ///   FOR INTERNAL USE ONLY!
        ///   Actor with Placement.PreferLocal
        /// </summary>
        [PreferLocalPlacement]
        public class A1 : ActorEndpoint, IA1
        {}

        /// <summary>
        ///   FOR INTERNAL USE ONLY!
        ///   Actor with Placement.DistributeEvenly
        /// </summary>
        [ActivationCountBasedPlacement]
        public class A2 : ActorEndpoint, IA2
        {}

        /// <summary>
        ///   FOR INTERNAL USE ONLY!
        ///   Worker
        /// </summary>
        [StatelessWorker]
        public class W : ActorEndpoint, IW
        {}
    }
}