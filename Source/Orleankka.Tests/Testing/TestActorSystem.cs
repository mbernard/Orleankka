using System;
using System.Linq;

namespace Orleankka.Testing
{
    using Typed;

    public static class TestActorSystem
    {
        public static IActorSystem Instance;

        public static ActorRef FreshActorOf<TActor>(this IActorSystem system) where TActor : Actor
        {
            return system.ActorOf<TActor>(Guid.NewGuid().ToString());
        }

        public static TypedActorRef<TActor> FreshTypedActorOf<TActor>(this IActorSystem system) where TActor : TypedActor
        {
            return system.TypedActorOf<TActor>(Guid.NewGuid().ToString());
        }
    }
}
