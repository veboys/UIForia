using System.Xml.Linq;
using Rendering;
using Src.Style;
using UnityEngine;

namespace Src.Parsing.Style {

    public static class PaintStyleParser {

        public static void ParseStyle(XElement element, UIStyle template) {
            if (element == null) return;

            XElement backgroundColor = element.GetChild("BackgroundColor");
            XElement backgroundImage = element.GetChild("BackgroundImage");
            XElement borderColor = element.GetChild("BorderColor");

            if (backgroundColor != null) {
                template.paint.backgroundColor = StyleParseUtil.ParseColor(element.GetAttribute("value").Value);
            }

            if (backgroundImage != null) {
                template.paint.backgroundImage = ParseResourcePath(element.GetAttribute("value").Value);
            }

            if (borderColor != null) {
                template.paint.borderColor = StyleParseUtil.ParseColor(element.GetAttribute("value").Value);
            }
        }

        private static Texture2D ParseResourcePath(string input) {
            return Resources.Load<Texture2D>(input);
        }

    }

}