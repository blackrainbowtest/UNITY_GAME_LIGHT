using System;
using System.Collections;
using System.Collections.Generic;

namespace UDA2.SceneFlow
{
    public interface ISceneLoadTaskProvider
    {
        void CollectLoadTasks(List<SceneLoadTask> tasks);
    }

    public sealed class SceneLoadTask
    {
        public string Name { get; }
        public float Weight { get; }

        private readonly Func<IEnumerator> _runnerFactory;

        public SceneLoadTask(string name, float weight, Func<IEnumerator> runnerFactory)
        {
            Name = string.IsNullOrWhiteSpace(name) ? "unnamed" : name;
            Weight = Math.Max(0.001f, weight);
            _runnerFactory = runnerFactory;
        }

        public IEnumerator Run()
        {
            if (_runnerFactory == null)
                yield break;

            var routine = _runnerFactory();
            if (routine == null)
                yield break;

            while (routine.MoveNext())
                yield return routine.Current;
        }
    }
}
