namespace SafeEcs
{
    public class Example001
    {
        public static void Main()
        {
            IEcsEngine engine = new LeoEcsLiteEngine();
            
            // use pipelines to build sequences of systems. each system is reference to a method with specific signature.
            // order of system execution is calculated automatically based on the how each system uses components
            
            IPipeline update = engine.NewPipeline("Update")
                .AddSystem(MakeDoubleSystem.Update)
                .AddSystem(MyClass2System.Update)
                .End();

            IPipeline lateUpdate = engine.NewPipeline("LateUpdate")
                .AddSystem(MyClass2System.LateUpdate)
                .End();

            // invoke it at the frame begin (will run all systems of pipeline in correct order)
            update.Invoke();
            
            // invoke it at the end of frame
            lateUpdate.Invoke();
        }

        public class MakeDoubleSystem
        {
            public static SafeSystem Update(ISafeWorld world)
            {
                // this code is invoked at the early initialization phase and should ONLY contain initialization of component accessors
                // it can be invoked multiple times and be used for analysis purposes (for example, dependency graph rendering)
                
                IFilter filter = world.Filter<CompA>().Inc<CompB>().End();
                IGet<CompA> getA = world.ForGet<CompA>();
                IGetMut<CompB> mutB = world.ForGetMut<CompB>();

                return () =>
                {
                    // this code is invoked in the combat whase, when pipeline is invoked. od your logic here
                    
                    foreach (int entity in filter)
                    {
                        mutB.GetMut(entity).y = getA.Get(entity).x * 2;
                    }
                };
            }
        }

        public class MyClass2System
        {
            public static SafeSystem LateUpdate(ISafeWorld world)
            {
                IGetMut<CompA> mutB = world.ForGetMut<CompA>();
                IFilter filter = world.Filter<CompA>().End();
                return () =>
                {
                    foreach (int entity in filter)
                    {
                        mutB.GetMut(entity).x++;
                    }
                };
            }

            public static SafeSystem Update(ISafeWorld world)
            {
                IAdd<CompB> addA = world.ForAdd<CompB>();
                IFilter filter = world.Filter<CompA>().Exc<CompB>().End();
                return () =>
                {
                    foreach (int entity in filter)
                    {
                        addA.Add(entity, new CompB());
                    }
                };
            }
        }

        public struct CompA
        {
            public float x;
        }

        public struct CompB
        {
            public float y;
        }
    }
}