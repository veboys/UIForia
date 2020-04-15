using UIForia.Util;

namespace UIForia.Parsing {

    public abstract class ElementNode : TemplateNode {

        public readonly string moduleName;
        public readonly string tagName;

        protected ElementNode(string moduleName, string tagName, ReadOnlySizedArray<AttributeDefinition> attributes, in TemplateLineInfo templateLineInfo)
            : base(attributes, templateLineInfo) {
            this.moduleName = moduleName;
            this.tagName = tagName;
        }

        public override string GetTagName() {
            if (!string.IsNullOrEmpty(moduleName)) {
                return moduleName + ":" + tagName;
            }

            return tagName;
        }

    }

}