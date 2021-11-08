using System;

namespace SafeEcs
{
    public interface IEcsEngine
    {
        IPipelineBuilder NewPipeline(string name);
    }

    public interface IPipelineBuilder
    {
        IPipelineBuilder AddSystem(SystemProvider system);
        IPipeline End();
    }

    public interface IPipeline
    {
        void Invoke();
    }

    public delegate void SafeSystem();

    public delegate SafeSystem SystemProvider(ISafeWorld world);
    
    public interface ISafeWorld
    {
        IFilterBuilder Filter<T>() where T : struct;

        IGet<T> ForGet<T>() where T : struct;

        IUpdate<T> ForUpdate<T>() where T : struct;

        IGetMut<T> ForGetMut<T>() where T : struct;

        IAdd<T> ForAdd<T>() where T : struct;

        IDel<T> ForDel<T>() where T : struct;

        IHas<T> ForHas<T>() where T: struct;

        int NewEntity();

        [Obsolete("can delete entity only by deletion of all components", true)]
        void DelEntity();
    }

    public interface IFilterBuilder
    {
        IFilterBuilder Inc<T>() where T : struct;

        IFilterBuilder Exc<T>() where T : struct;

        IFilter End();
    }

    public interface IFilter
    {
        DelegatingEnumerator<int> GetEnumerator();
    }

    public interface IHas<T>
        where T : struct
    {
        bool Has(int entity);
    }

    public interface IOptionalGet<T> : IGet<T>, IHas<T> where T : struct
    {
        T? GetOrNull(int entity);
    }

    public interface IGet<T>
        where T : struct
    {
        T Get(int entity);
    }

    public interface IUpdate<T>
        where T : struct
    {
        /// <summary>
        /// fails if such component absents
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="value"></param>
        void Update(int entity, in T value);
    }

    public interface IGetMut<T> :
        IGet<T>,
        IUpdate<T>
        where T : struct
    {
        ref T GetMut(int entity);
    }

    public interface IAdd<T>
        where T : struct
    {
        void Add(int entity, in T value);
    }

    public interface IDel<T>
        where T : struct
    {
        void Del(int entity);
    }
}