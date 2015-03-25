﻿using System;
using System.Linq;

using NUnit.Framework;

namespace Orleankka.Scenarios
{
    using Testing;

    [TestFixture]
    [RequiresSilo]
    public class Request_response
    {
        readonly IActorSystem system = TestActorSystem.Instance;

        [Test]
        public async void Client_to_actor()
        {
            var actor = system.FreshActorOf<TestActor>();
            
            await actor.Tell(new SetText("c-a"));
            Assert.AreEqual("c-a", await actor.Ask<string>(new GetText()));
        }

        [Test]
        public async void Actor_to_actor()
        {
            var one = system.FreshActorOf<TestInsideActor>();
            var another = system.FreshActorOf<TestActor>();

            await one.Tell(new DoTell(another, new SetText("a-a")));
            Assert.AreEqual("a-a", await one.Ask<string>(new DoAsk(another, new GetText())));
        }
    }
}