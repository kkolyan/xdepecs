using System.Collections.Generic;
using NUnit.Framework;

namespace SafeEcs
{
    public class TestAll
    {
        private List<string> _order = new List<string>();

        [SetUp]
        public void Setup()
        {
            _order.Clear();
        }

        public struct CompA { }
        
        [Test]
        public void CheckAfterAdd()
        {
            IEcsEngine engine = new LeoEcsLiteEngine();
            IPipeline pipeline = engine.NewPipeline("main")
                .AddSystem(AddA)
                .AddSystem(CheckA)
                .End();
            pipeline.Invoke();
            Assert.AreEqual("AddA, CheckA", _order.ShallowListToString());
        }

        [Test]
        public void CheckAfterAddToo()
        {
            IEcsEngine engine = new LeoEcsLiteEngine();
            IPipeline pipeline = engine.NewPipeline("main")
                .AddSystem(CheckA)
                .AddSystem(AddA)
                .End();
            pipeline.Invoke();
            Assert.AreEqual("AddA, CheckA", _order.ShallowListToString());
        }

        private SafeSystem AddA(ISafeWorld world)
        {
            var addA = world.ForAdd<CompA>();
            return () => { _order.Add(nameof(AddA)); };
        }

        private SafeSystem CheckA(ISafeWorld world)
        {
            var filterA = world.Filter<CompA>().End();
            return () => { _order.Add(nameof(CheckA)); };
        }
    }
}