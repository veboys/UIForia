using System.Xml.Linq;
using UIForia.Templates;
using UIForia.Util;

namespace UIForia.Parsing {

    public struct RawTemplateContent {

        public string templateId;
        public XElement content;

    }
    
    public class TemplateShell {

        public readonly string filePath;
        public StructList<UsingDeclaration> usings;
        public StructList<StyleDefinition> styles;
        public LightList<ElementTemplateNode> elementNodes;
        public StructList<RawTemplateContent> unprocessedContentNodes;

        public TemplateShell(string filePath) {
            this.filePath = filePath;
            this.usings = new StructList<UsingDeclaration>(2);
            this.styles = new StructList<StyleDefinition>(2);
            this.elementNodes = new LightList<ElementTemplateNode>(2);
            this.unprocessedContentNodes = new StructList<RawTemplateContent>(2);
        }

        public ElementTemplateNode GetElementTemplate(string templateId) {
            for (int i = 0; i < elementNodes.size; i++) {
                if (elementNodes.array[i].templateName == templateId) {
                    return elementNodes.array[i];
                }
            }

            return null;
        }

        public bool HasContentNode(string templateId) {
            return GetElementTemplateContent(templateId) != null;
        }

        public XElement GetElementTemplateContent(string templateId) {
            
            for (int i = 0; i < unprocessedContentNodes.size; i++) {
                if (unprocessedContentNodes.array[i].templateId == templateId) {
                    return unprocessedContentNodes.array[i].content;
                }
            }

            return null;
        }

    }

}