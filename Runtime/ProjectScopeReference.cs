using System;
using UnityEngine;

namespace NestedDIContainer.Unity.Runtime
{
    public class ProjectScopeReference : ScriptableObject
    {
        // TODO: Unity6以前ではabstract classに代入できないため、対応予定
        [SerializeField] ProjectScope _projectScope;
        
        public ProjectScope ProjectScope
        {
            get => _projectScope;
            set
            {
                if (_projectScope != null && value != _projectScope)
                {
                    _projectScope = value;
                }
            }
        }
        
        internal ProjectScope CreateProjectScope()
        {
            if (_projectScope == null)
                throw new Exception("ProjectScope is not set. Please set the ProjectScope in the ProjectScopeReference.");
            
            _projectScope.gameObject.SetActive(false);
            var instance = Instantiate(_projectScope, null);
            DontDestroyOnLoad(instance);
            
            instance.gameObject.SetActive(true);
            
            return instance;
        }
    }
}