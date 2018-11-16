﻿using System.Collections.Generic;
using UIForia.Rendering;

namespace UIForia.Systems {

    public class BindingSystem : ISystem {

        private SkipTree<BindingNode>.TreeNode treeRoot;
        private readonly List<BindingNode> repeatNodes;
        private readonly SkipTree<BindingNode> bindingSkipTree;

        private bool isTreeDirty;

        public BindingSystem() {
            this.isTreeDirty = true;
            this.repeatNodes = new List<BindingNode>();
            this.bindingSkipTree = new SkipTree<BindingNode>();
            this.bindingSkipTree.onTreeChanged += HandleTreeChanged;
        }

        private void HandleTreeChanged(SkipTree<BindingNode>.TreeChangeType changeType) {
            isTreeDirty = true;
        }

        public void OnReset() {
            this.isTreeDirty = true;
            bindingSkipTree.Clear();
            repeatNodes.Clear();
        }

        public void OnUpdate() {
            for (int i = 0; i < repeatNodes.Count; i++) {
                repeatNodes[i].Validate();
            }

            if (isTreeDirty) {
                isTreeDirty = false;
                bindingSkipTree.RecycleTree(treeRoot);
                treeRoot = bindingSkipTree.GetTraversableTree();
            }

            if (treeRoot.children != null && treeRoot.children.Length > 0) {
                for (int i = 0; i < treeRoot.children.Length; i++) {
                    treeRoot.children[i].item?.OnUpdate(treeRoot.children[i].children);
                }
            }
        }

        public bool EnableBinding(UIElement element, string bindingId) {
            BindingNode binding = bindingSkipTree.GetItem(element);
            if (binding == null) return false;
            for (int i = 0; i < binding.bindings.Length; i++) {
                if (binding.bindings[i].bindingId == bindingId) {
                    binding.bindings[i].isEnabled = true;
                    return true;
                }
            }

            return false;
        }

        public bool DisableBinding(UIElement element, string bindingId) {
            BindingNode binding = bindingSkipTree.GetItem(element);
            if (binding == null) return false;
            for (int i = 0; i < binding.bindings.Length; i++) {
                if (binding.bindings[i].bindingId == bindingId) {
                    binding.bindings[i].isEnabled = false;
                    return true;
                }
            }

            return false;
        }

        public bool HasBinding(UIElement element, string bindingId) {
            BindingNode binding = bindingSkipTree.GetItem(element);
            if (binding == null) return false;
            for (int i = 0; i < binding.bindings.Length; i++) {
                if (binding.bindings[i].bindingId == bindingId) {
                    return true;
                }
            }

            return false;
        }

        public bool IsBindingEnabled(UIElement element, string bindingId) {
            BindingNode binding = bindingSkipTree.GetItem(element);
            if (binding == null) return false;
            for (int i = 0; i < binding.bindings.Length; i++) {
                if (binding.bindings[i].bindingId == bindingId) {
                    return binding.bindings[i].isEnabled;
                }
            }

            return false;
        }

        public void OnDestroy() {
            bindingSkipTree.Clear();
            repeatNodes.Clear();
        }

        public void OnViewAdded(UIView view) { }

        public void OnViewRemoved(UIView view) { }

        public void OnElementCreated(UIElement element) {
            isTreeDirty = true;

            UITemplate template = element.templateRef;
            if (template.constantBindings.Length != 0) {
                for (int i = 0; i < template.constantBindings.Length; i++) {
                    template.constantBindings[i].Execute(element, element.TemplateContext);
                }
            }

            UIRepeatElement repeat = element as UIRepeatElement;
            if (repeat != null) {
                ReflectionUtil.TypeArray2[0] = repeat.listType;
                ReflectionUtil.TypeArray2[1] = repeat.itemType;

                ReflectionUtil.ObjectArray4[0] = repeat.listExpression;
                ReflectionUtil.ObjectArray4[1] = repeat.itemAlias;
                ReflectionUtil.ObjectArray4[2] = repeat.indexAlias;
                ReflectionUtil.ObjectArray4[3] = repeat.lengthAlias;

                RepeatBindingNode node = (RepeatBindingNode) ReflectionUtil.CreateGenericInstanceFromOpenType(
                    typeof(RepeatBindingNode<,>),
                    ReflectionUtil.TypeArray2,
                    ReflectionUtil.ObjectArray4
                );

                node.bindings = template.bindings;
                node.element = element;
                node.context = element.TemplateContext;
                node.template = repeat.template;
                node.scope = repeat.scope;
                repeatNodes.Add(node);
                bindingSkipTree.AddItem(node);
            }

            if (repeat == null && template.bindings.Length > 0) {
                BindingNode node = new BindingNode();
                node.bindings = template.bindings;
                node.element = element;
                node.context = element.TemplateContext;
                bindingSkipTree.AddItem(node);
            }

            if (element.children != null && element.children.Length > 0) {
                for (int i = 0; i < element.children.Length; i++) {
                    OnElementCreated(element.children[i]);
                }
            }
        }

        public void OnElementDestroyed(UIElement element) {
            bindingSkipTree.RemoveHierarchy(element);
            isTreeDirty = true;
        }

        public void OnElementEnabled(UIElement element) { }

        public void OnElementDisabled(UIElement element) { }

    }

}