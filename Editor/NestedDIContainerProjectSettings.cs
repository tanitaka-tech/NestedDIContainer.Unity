using NestedDIContainer.Unity.Runtime;
using UnityEditor;
using UnityEngine;

namespace _Hierarchy.Common.NestedDIContainer.Unity.Editor
{
    [FilePath("ProjectSettings/TanitakaTech/NestedDIContainerSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    public sealed class NestedDIContainerProjectSettings : ScriptableSingleton<NestedDIContainerProjectSettings>
    {
        [SerializeField] private ProjectScopeReference _projectScopeReference;
        [SerializeField] private ProjectScope _projectScope;

        public ProjectScopeReference ProjectScopeReference
        {
            get => _projectScopeReference;
            set
            {
                _projectScopeReference = value; 
                Save(true);
            }
        }

        public ProjectScope ProjectScope
        {
            get => _projectScopeReference.ProjectScope;
            set => _projectScopeReference.ProjectScope = value;
        }
    }
}