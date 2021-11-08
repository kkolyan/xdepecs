using System;
using System.Collections.Generic;
using System.Linq;
using Leopotam.EcsLite;

namespace SafeEcs
{
    public class LeoEcsLiteEngine : IEcsEngine
    {
        private LeoEcsLiteSafeWorld _world = new LeoEcsLiteSafeWorld();

        public IPipelineBuilder NewPipeline(string name)
        {
            return new Pipeline(_world, name);
        }
    }

    internal class Pipeline : IPipeline, IRelationConsumer, IPipelineBuilder
    {
        private readonly LeoEcsLiteSafeWorld _world;
        private readonly string _name;
        private List<SafeSystem> _systems = new List<SafeSystem>();

        private Dictionary<SystemProvider, SafeSystem> _systemByProvider = new Dictionary<SystemProvider, SafeSystem>();
        private List<SystemProvider> _systemProviders = new List<SystemProvider>();

        private readonly Dictionary<Type, List<SystemProvider>> _initiates = new Dictionary<Type, List<SystemProvider>>();
        private readonly Dictionary<Type, List<SystemProvider>> _mutates = new Dictionary<Type, List<SystemProvider>>();
        private readonly Dictionary<Type, List<SystemProvider>> _checks = new Dictionary<Type, List<SystemProvider>>();
        private readonly Dictionary<Type, List<SystemProvider>> _reads = new Dictionary<Type, List<SystemProvider>>();
        private readonly Dictionary<Type, List<SystemProvider>> _terminates = new Dictionary<Type, List<SystemProvider>>();

        private SystemProvider _lastAddedSystem;

        public Pipeline(LeoEcsLiteSafeWorld world, string name)
        {
            _world = world;
            _name = name;
        }

        public IPipelineBuilder AddSystem(SystemProvider system)
        {
            _lastAddedSystem = system;
            _systemProviders.Add(system);
            _world.relationConsumer = this;
            SafeSystem safeSystem = system(_world);
            _world.relationConsumer = null;
            _systems.Add(safeSystem);
            _systemByProvider[system] = safeSystem;
            return this;
        }

        public void Invoke()
        {
            foreach (SafeSystem safeSystem in _systems)
            {
                safeSystem.Invoke();
            }
        }

        public IPipeline End()
        {
            KahnTopologicalSorter<SystemProvider> sorter = new KahnTopologicalSorter<SystemProvider>();
            var roots = _systemProviders.ToList();

            ISet<Edge<SystemProvider>> edges = new HashSet<Edge<SystemProvider>>(Edge<SystemProvider>.Comparer);

            ISet<Type> types = new HashSet<Type>();
            foreach (Type type in _initiates.Keys) types.Add(type);
            foreach (Type type in _checks.Keys) types.Add(type);
            foreach (Type type in _mutates.Keys) types.Add(type);
            foreach (Type type in _reads.Keys) types.Add(type);
            foreach (Type type in _terminates.Keys) types.Add(type);

            void AddEdges(List<SystemProvider> tos, List<SystemProvider> froms)
            {
                if (froms == null || tos == null)
                {
                    return;
                }

                foreach (SystemProvider from in froms)
                {
                    foreach (SystemProvider to in tos)
                    {
                        edges.Add(new Edge<SystemProvider>(from, to));
                    }
                }
            }

            foreach (Type type in types)
            {
                AddEdges(_terminates.GetOrDefault(type), _reads.GetOrDefault(type));
                AddEdges(_terminates.GetOrDefault(type), _mutates.GetOrDefault(type));
                AddEdges(_terminates.GetOrDefault(type), _initiates.GetOrDefault(type));
                AddEdges(_terminates.GetOrDefault(type), _checks.GetOrDefault(type));

                AddEdges(_reads.GetOrDefault(type), _mutates.GetOrDefault(type));
                AddEdges(_reads.GetOrDefault(type), _initiates.GetOrDefault(type));

                AddEdges(_mutates.GetOrDefault(type), _initiates.GetOrDefault(type));
                
                AddEdges(_checks.GetOrDefault(type), _initiates.GetOrDefault(type));
            }
            
            foreach (Edge<SystemProvider> edge in edges)
            {
                roots.Remove(edge.to);
            }

            _systems = sorter.GetSorted(roots, edges)
                .Select(provider => _systemByProvider[provider])
                .ToList();
            return this;
        }

        public void Initiates<T>() where T : struct => _initiates.Add(typeof(T), _lastAddedSystem);
        public void Checks<T>() where T : struct => _checks.Add(typeof(T), _lastAddedSystem);
        public void Reads<T>() where T : struct => _reads.Add(typeof(T), _lastAddedSystem);
        public void Mutates<T>() where T : struct => _mutates.Add(typeof(T), _lastAddedSystem);
        public void Terminates<T>() where T : struct => _terminates.Add(typeof(T), _lastAddedSystem);
    }

    internal class FilterBuilder : IFilterBuilder
    {
        private EcsFilter.Mask _mask;
        private readonly IRelationConsumer _world;

        public FilterBuilder(EcsFilter.Mask mask, IRelationConsumer world)
        {
            _mask = mask;
            _world = world;
        }

        public IFilterBuilder Inc<T>() where T : struct
        {
            _world.Checks<T>();
            _mask = _mask.Inc<T>();
            return this;
        }

        public IFilterBuilder Exc<T>() where T : struct
        {
            _world.Checks<T>();
            _mask = _mask.Exc<T>();
            return this;
        }

        public IFilter End()
        {
            return new Filter(_mask.End());
        }
    }

    internal class Filter : IFilter
    {
        private readonly EcsFilter _ecsFilter;
        private readonly List<EcsFilter.Enumerator> _enumerators = new List<EcsFilter.Enumerator>(1);
        private readonly Func<int> _current;
        private readonly Func<bool> _moveNext;
        private readonly Action _dispose;

        public Filter(EcsFilter ecsFilter)
        {
            _ecsFilter = ecsFilter;
            _current = () => _enumerators[_enumerators.Count - 1].Current;
            _moveNext = () => _enumerators[_enumerators.Count - 1].MoveNext();
            _dispose = () =>
            {
                _enumerators[_enumerators.Count - 1].Dispose();
                _enumerators.RemoveAt(_enumerators.Count - 1);
            };
        }

        public DelegatingEnumerator<int> GetEnumerator()
        {
            _enumerators.Add(_ecsFilter.GetEnumerator());
            return new DelegatingEnumerator<int>(_current, _moveNext, _dispose);
        }
    }

    internal class Super<T> :
        IOptionalGet<T>,
        IGetMut<T>,
        IDel<T>,
        IAdd<T>
        where T : struct
    {
        private readonly EcsPool<T> _pool;

        public Super(EcsPool<T> pool)
        {
            _pool = pool;
        }

        public T Get(int entity)
        {
            return _pool.Get(entity);
        }

        public void Update(int entity, in T value)
        {
            _pool.Get(entity) = value;
        }

        public ref T GetMut(int entity)
        {
            return ref _pool.Get(entity);
        }

        public void Add(int entity, in T value)
        {
            _pool.Add(entity) = value;
        }

        public void Del(int entity)
        {
            _pool.Del(entity);
        }

        public bool Has(int entity)
        {
            return _pool.Has(entity);
        }

        public T? GetOrNull(int entity)
        {
            return _pool.Has(entity) ? (T?)_pool.Get(entity) : null;
        }
    }

    interface IRelationConsumer
    {
        void Initiates<T>() where T : struct;
        
        void Checks<T>() where T : struct;
        void Reads<T>() where T : struct;
        void Mutates<T>() where T : struct;
        void Terminates<T>() where T : struct;
    }

    internal class LeoEcsLiteSafeWorld : ISafeWorld
    {
        private EcsWorld _world = new EcsWorld();

        internal IRelationConsumer relationConsumer;


        public IFilterBuilder Filter<T>() where T : struct
        {
            relationConsumer.Checks<T>();
            return new FilterBuilder(_world.Filter<T>(), relationConsumer);
        }

        public IHas<T> ForHas<T>() where T : struct
        {
            relationConsumer.Checks<T>();
            return new Super<T>(_world.GetPool<T>());
        }

        public IGet<T> ForGet<T>() where T : struct
        {
            relationConsumer.Reads<T>();
            return new Super<T>(_world.GetPool<T>());
        }

        public IUpdate<T> ForUpdate<T>() where T : struct
        {
            relationConsumer.Mutates<T>();
            return new Super<T>(_world.GetPool<T>());
        }

        public IGetMut<T> ForGetMut<T>() where T : struct
        {
            relationConsumer.Reads<T>();
            relationConsumer.Mutates<T>();
            return new Super<T>(_world.GetPool<T>());
        }

        public IAdd<T> ForAdd<T>() where T : struct
        {
            relationConsumer.Initiates<T>();
            return new Super<T>(_world.GetPool<T>());
        }

        public IDel<T> ForDel<T>() where T : struct
        {
            relationConsumer.Terminates<T>();
            return new Super<T>(_world.GetPool<T>());
        }

        public int NewEntity()
        {
            return _world.NewEntity();
        }

        public void DelEntity()
        {
            throw new NotSupportedException("can never be invoked as Obsolete attribute has error=true");
        }
    }
}