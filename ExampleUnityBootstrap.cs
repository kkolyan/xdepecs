using UnityEngine;

namespace SafeEcs
{
    // such class could be used to run ECS inside Unity project
    public class ExampleUnityBootstrap : MonoBehaviour
    {
        private IEcsEngine _engine;
        private IPipeline _update;
        private IPipeline _lateUpdate;
        
        public void Start()
        {
            
            _engine = new LeoEcsLiteEngine();

            // configure list o  all systems that invoked on Update
            _update = _engine.NewPipeline("Update")
                .AddSystem(Example001.MakeDoubleSystem.Update)
                .AddSystem(Example001.MyClass2System.Update)
                .End();

            // configure list of all systems that invoked on LateUpdate
            _lateUpdate = _engine.NewPipeline("LateUpdate")
                .AddSystem(Example001.MyClass2System.LateUpdate)
                .End();
        }

        private void Update()
        {
            _update.Invoke();
        }

        private void LateUpdate()
        {
            _lateUpdate.Invoke();
        }
    }
}