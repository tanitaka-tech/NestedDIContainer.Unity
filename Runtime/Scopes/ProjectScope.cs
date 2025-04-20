using System;
using System.Collections.Generic;
using NestedDIContainer.Unity.Runtime.Scopes;
using TanitakaTech.NestedDIContainer;
using UnityEngine;

namespace NestedDIContainer.Unity.Runtime
{
    public abstract class ProjectScope : MonoBehaviourScope
    {
        internal static ProjectScope Scope => _projectScope;
        private static ProjectScope _projectScope;

        internal static List<IAsyncInitializer> Initializers { get; } = new ();

        internal static ProjectScope CreateProjectScope()
        {
            Dispose();

            if (_projectScope != null)
            {
                return _projectScope;
            }
            var loaded = Resources.Load($"ProjectScopeReference");
            var projectScopeReference = loaded as ProjectScopeReference;
            if (projectScopeReference == null)
            {
                throw new Exception("ProjectScopeReference was not found. Please check the ProjectSettings.");
            }
            _projectScope = projectScopeReference.CreateProjectScope();
            _projectScope.ConstructScope(ScopeId.Create(), null, optionExtendScope: new ProjectScopeDefaultExtendScope(Scope));
            return _projectScope;
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

        internal static IScope PopParentScope()
        {
           var temp = _tempParentId;
            _tempParentId = null;
            return temp;
        }
        internal static void PushParentScope(IScope parentScope)
        {
            _tempParentId = parentScope;
        }
        private static IScope _tempParentId = null;
        
        protected void Awake()
        {
            _projectScope = this;
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
            _projectScope = null;
            _tempConfig = null;
        }
    }
}