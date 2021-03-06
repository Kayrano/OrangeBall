using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Properties.UI.Internal
{
    class CustomInspectorElement : VisualElement, IBindable, IBinding
    {
        internal class DefaultInspectorElement : VisualElement{}
        
        readonly PropertyPath m_BasePath;
        readonly PropertyElement m_Root;
        readonly VisualElement m_Content;
        readonly PropertyPath m_RelativePath = new PropertyPath();
        readonly PropertyPath m_AbsolutePath = new PropertyPath();

        public IBinding binding { get; set; }
        
        public string bindingPath { get; set; }
        bool HasInspector { get; }

        public IInspector Inspector { get; }

        public CustomInspectorElement(PropertyPath basePath, IInspector inspector, PropertyElement root)
        {
            m_Root = root;
            binding = this;
            m_BasePath = basePath;
            name = inspector.Type.Name;
            Inspector = inspector;
            try
            {
                m_Content = Inspector.Build();

                if (null == m_Content)
                    return;

                HasInspector = true;

                // If `IInspector.Build` was not overridden, it returns this element as its content.     
                if (this != m_Content)
                {
                    Add(m_Content);
                    RegisterBindings(m_Content);
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        void IBinding.PreUpdate()
        {
            // Nothing to do.
        }

        void IBinding.Update()
        {
            if (!HasInspector || !m_Root.IsPathValid(m_BasePath))
                return;

            try
            {
                Inspector.Update();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        void IBinding.Release()
        {
            // Nothing to do.
        }
        
        void RegisterBindings(VisualElement content)
        {
            if (content is CustomInspectorElement && content != this)
                return;

            var popRelativePartCount = 0;
            if (content is BindableElement b && !string.IsNullOrEmpty(b.bindingPath))
            {
                if (b.bindingPath != ".")
                {
                    var previousCount = m_RelativePath.PartsCount;
                    m_RelativePath.AppendPath(b.bindingPath);
                    m_AbsolutePath.AppendPath(b.bindingPath);
                    popRelativePartCount = m_RelativePath.PartsCount - previousCount;
                }

                if (Inspector.IsPathValid(m_RelativePath))
                    RegisterBindings(Inspector, m_RelativePath, content, m_Root);
                else if (Inspector.IsPathValid(m_AbsolutePath))
                    RegisterBindings(Inspector, m_AbsolutePath, content, m_Root);
                m_AbsolutePath.Clear();
            }

            if (!(content is PropertyElement) && !(content is DefaultInspectorElement))
                foreach (var child in content.Children())
                    RegisterBindings(child);

            for(var i = 0; i < popRelativePartCount; ++i)
            {
                m_RelativePath.Pop();
            }
        }

        static void RegisterBindings(IInspector inspector, PropertyPath pathToValue, VisualElement toBind, PropertyElement root)
        {
            var fullPath = new PropertyPath();
            fullPath.PushPath(inspector.PropertyPath);
            fullPath.PushPath(pathToValue);
            root.RegisterBindings(fullPath, toBind);
        }
    }
}