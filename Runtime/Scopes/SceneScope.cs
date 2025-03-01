using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using NestedDIContainer.Unity.Runtime.Core;
using NestedDIContainer.Unity.Runtime.Scopes;
using TanitakaTech.NestedDIContainer;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NestedDIContainer.Unity.Runtime
{
    public abstract class SceneScope : SceneScopeWithConfig<SceneScope.EmptyConfig>
    {
        public abstract class EmptyConfig { }
        protected override void Construct(DependencyBinder binder, EmptyConfig config) => Construct(binder);
        protected abstract void Construct(DependencyBinder binder);
    }
    
    public abstract class SceneScopeWithConfig<TConfig> : MonoBehaviourScopeBase
    {
        protected void Awake()
        {
            // Init ScopeId
            ScopeId = ScopeId.Create();
            var parentScope = ProjectScope.Scope ?? ProjectScope.CreateProjectScope();
            ParentScopeId = ScopeId.Equals(parentScope.ScopeId) ? ScopeId.Create() : parentScope.ScopeId;

            ConstructScope(ScopeId, ParentScopeId.Value, ProjectScope.PopConfig(), new SceneScopeDefaultExtendScope(this));
        }

        protected override void Construct(DependencyBinder binder, object config) => Construct(binder, (TConfig)config);
        protected abstract void Construct(DependencyBinder binder, TConfig config);
    }
}