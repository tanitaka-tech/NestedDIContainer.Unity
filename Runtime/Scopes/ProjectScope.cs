using System;
using System.Collections.Generic;
using NestedDIContainer.Unity.Runtime.Scopes;
using TanitakaTech.NestedDIContainer;
using UnityEngine;

namespace NestedDIContainer.Unity.Runtime
{
    public abstract class ProjectScope : MonoBehaviourScope
    {
        internal static ProjectScope Scope => _scope;
        private static ProjectScope _scope;

        internal static List<IAsyncInitializer> Initializers { get; } = new ();

        internal static ProjectScope CreateProjectScope()
        {
            Dispose();

            if (_scope != null)
            {
                return _scope;
            }
            var loaded = Resources.Load($"ProjectScopeReference");
            var projectScopeReference = loaded as ProjectScopeReference;
            if (projectScopeReference == null)
            {
                throw new Exception("ProjectScopeReference was not found. Please check the ProjectSettings.");
            }
            _scope = projectScopeReference.CreateProjectScope();
            _scope.ScopeId = ScopeId.Create();
            _scope.ConstructScope(_scope.ScopeId, null, optionExtendScope: new ProjectScopeDefaultExtendScope(Scope));
            return _scope;
        }
        
        internal static object PopConfig()
        {
            var temp = _tempConfig;
            _tempConfig = null;
            return temp;
        }
        internal static void PushConfig(object config)
        {
            _tempConfig = config;
        }
        private static object _tempConfig = null;

        internal static ScopeId? PopParentId()
        {
           var temp = _tempParentId;
            _tempParentId = null;
            return temp;
        }
        internal static void PushParentId(ScopeId parentId)
        {
            _tempParentId = parentId;
        }
        private static ScopeId? _tempParentId = null;
        
        protected void Awake()
        {
            _scope = this;
#if UNITY_EDITOR
            if (gameObject.scene.name != "DontDestroyOnLoad")
                throw new ConstructException("ProjectScope must not be in a scene");
#endif
        }
        
        protected void OnDestroy()
        {
            Dispose();
        }

        private static void Dispose()
        {
            GlobalProjectScope.Dispose();
            _scope = null;
            _tempConfig = null;
        }
    }
}