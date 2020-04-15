using System;
using System.Collections;
using System.Linq.Expressions;
using Mono.Linq.Expressions;
using UIForia.Parsing;
using UIForia.Systems;
using UIForia.UIInput;
using UIForia.Util;

namespace UIForia.Compilers {

    public enum BindingType {

        Update,
        LateUpdate,
        Const,
        Enable

    }

    public struct BindingOutput {

        public BindingType bindingType;
        public LambdaExpression expression;
        public TemplateNode templateNode;

    }

    public struct TemplateOutput {

        public TemplateNode templateNode;
        public LambdaExpression expression;

    }

    public struct InputEventHandlerOutput {

        public LambdaExpression expression;

    }

    public class TemplateExpressionSet {

        public ProcessedType processedType;
        public LambdaExpression entryPoint;
        public LambdaExpression hydratePoint;
        public BindingOutput[] bindings;
        public TemplateOutput[] elementTemplates;
        public InputEventHandlerOutput[] inputEventHandlers;
        public LightList<SlotOverrideChain> slotOverrideChains;

        private static readonly string s_ElementFnTypeName = typeof(Action<ElementSystem>[]).GetTypeName();
        private static readonly string s_BindingFnTypeName = typeof(Action<LinqBindingNode>[]).GetTypeName();
        private static readonly string s_InputEventHandlerTypeName = typeof(Action<LinqBindingNode, InputEventHolder>[]).GetTypeName();

        private string guid;

        public string GetGUID() {
            if (guid == null) {
                guid = Guid.NewGuid().ToString().Replace("-", "_");
            }

            return guid;
        }

        public IndentedStringBuilder ToCSharpCode(IndentedStringBuilder stringBuilder) {

            stringBuilder.AppendInline("new ");
            stringBuilder.AppendInline(nameof(TemplateData));
            stringBuilder.AppendInline(" (");
            stringBuilder.AppendInline("\"");
            stringBuilder.AppendInline(processedType.tagName);
            stringBuilder.AppendInline("\"");
            stringBuilder.AppendInline(") {");
            stringBuilder.NewLine();
            stringBuilder.Indent();
            stringBuilder.Indent();

            BuildEntryPoint(stringBuilder);
            stringBuilder.NewLine();

            BuildHydratePoint(stringBuilder);

            stringBuilder.NewLine();
            stringBuilder.Indent();

            stringBuilder.Append(nameof(TemplateData.elements));
            stringBuilder.AppendInline(" = new ");
            stringBuilder.AppendInline(s_ElementFnTypeName);
            stringBuilder.AppendInline(" {\n");

            stringBuilder.Indent();
            for (int i = 0; i < elementTemplates.Length; i++) {
                stringBuilder.Append("//");
                stringBuilder.AppendInline(i.ToString());
                stringBuilder.AppendInline(" <");
                stringBuilder.AppendInline(elementTemplates[i].templateNode.GetTagName());
                stringBuilder.AppendInline("> line ");
                stringBuilder.AppendInline(elementTemplates[i].templateNode.lineInfo.ToString());
                stringBuilder.NewLine();
                stringBuilder.Append(elementTemplates[i].expression.ToTemplateBody(3));
                if (i != elementTemplates.Length - 1) {
                    stringBuilder.AppendInline(",\n");
                }
            }

            stringBuilder.Outdent();

            stringBuilder.NewLine();
            stringBuilder.Append("},");

            stringBuilder.NewLine();

            stringBuilder.Append(nameof(TemplateData.bindings));
            stringBuilder.AppendInline(" = new ");
            stringBuilder.AppendInline(s_BindingFnTypeName);
            stringBuilder.AppendInline(" {\n");
            stringBuilder.Indent();

            for (int i = 0; i < bindings.Length; i++) {
                stringBuilder.Append("// ");
                stringBuilder.AppendInline(i.ToString());
                stringBuilder.AppendInline(" ");
                stringBuilder.AppendInline(GetBindingType(bindings[i].bindingType));
                stringBuilder.AppendInline(" <");
                stringBuilder.AppendInline(bindings[i].templateNode.GetTagName());
                stringBuilder.AppendInline("> line ");
                stringBuilder.AppendInline(bindings[i].templateNode.lineInfo.ToString());
                stringBuilder.NewLine();
                stringBuilder.Append(bindings[i].expression.ToTemplateBody(3));
                if (i != bindings.Length - 1) {
                    stringBuilder.AppendInline(",\n");
                }
            }
            stringBuilder.NewLine();
            stringBuilder.Outdent();
            stringBuilder.Append("},");
            stringBuilder.NewLine();
            stringBuilder.Append(nameof(TemplateData.inputEventHandlers));
            stringBuilder.AppendInline(" = new ");
            stringBuilder.AppendInline(s_InputEventHandlerTypeName);
            stringBuilder.AppendInline(" {\n");
            stringBuilder.Indent();

            for (int i = 0; i < inputEventHandlers.Length; i++) {
                stringBuilder.Append("// ");
                stringBuilder.AppendInline(i.ToString());
                // stringBuilder.AppendInline(" ");
                // stringBuilder.AppendInline(GetBindingType(inputEventHandlers[i].bindingType));
                // stringBuilder.AppendInline(" <");
                // stringBuilder.AppendInline(inputEventHandlers[i].templateNode.GetTagName());
                // stringBuilder.AppendInline("> line ");
                // stringBuilder.AppendInline(inputEventHandlers[i].templateNode.lineInfo.ToString());
                stringBuilder.NewLine();
                stringBuilder.Append(inputEventHandlers[i].expression.ToTemplateBody(3));
                if (i != inputEventHandlers.Length - 1) {
                    stringBuilder.AppendInline(",\n");
                }
            }

            stringBuilder.NewLine();
            stringBuilder.Outdent();
            stringBuilder.Append("}");

            stringBuilder.NewLine();
            stringBuilder.Outdent();
            stringBuilder.Append("};");
            return stringBuilder;
        }

        private string GetBindingType(BindingType bindingType) {
            switch (bindingType) {

                case BindingType.Update:
                    return "Update Binding";

                case BindingType.LateUpdate:
                    return "Late Update Binding";

                case BindingType.Const:
                    return "Const Binding";

                case BindingType.Enable:
                    return "Enable Binding";

                default:
                    throw new ArgumentOutOfRangeException(nameof(bindingType), bindingType, null);
            }
        }

        private void BuildEntryPoint(IndentedStringBuilder stringBuilder) {
            stringBuilder.Indent();
            stringBuilder.Append(nameof(TemplateData.entry));
            stringBuilder.AppendInline(" = ");
            stringBuilder.AppendInline(entryPoint.ToTemplateBody(2));
            stringBuilder.AppendInline(",");
            stringBuilder.Outdent();
        }

        private void BuildHydratePoint(IndentedStringBuilder stringBuilder) {
            stringBuilder.Indent();
            stringBuilder.Append(nameof(TemplateData.hydrate));
            stringBuilder.AppendInline(" = ");
            stringBuilder.AppendInline(hydratePoint.ToTemplateBody(2));
            stringBuilder.AppendInline(",");
            stringBuilder.Outdent();
        }

        public SlotOverrideInfo[] GetSlotOverrideChain(string slotName) {
            if (slotOverrideChains == null) return null;

            for (int i = 0; i < slotOverrideChains.size; i++) {
                if (slotOverrideChains.array[i].slotName == slotName) {
                    return slotOverrideChains[i].chain;
                }
            }

            return null;
        }

    }

    public struct SlotOverrideInfo {

        public ProcessedType rootType;
        public AttrInfo[] attributes;
        public TemplateNode slotNode;

    }

    public class SlotOverrideChain {

        public string slotName;
        public SlotOverrideInfo[] chain;

        public SlotOverrideChain(string slotName, SlotOverrideInfo[] chain) {
            this.slotName = slotName;
            this.chain = chain;
        }

    }

}